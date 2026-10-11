[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$lock = Get-Content (Join-Path $PSScriptRoot 'dependencies.lock.json') -Raw | ConvertFrom-Json
$cache = Join-Path $repo 'work/dependencies'
New-Item $cache -ItemType Directory -Force | Out-Null

function Download([string]$FileName) {
    $entry = $lock.downloads | Where-Object { $_.file -eq $FileName }
    if (!$entry) { throw "No download lock entry for $FileName" }
    $Path = Join-Path $cache $FileName
    if (!(Test-Path -LiteralPath $Path)) {
        $partial = "$Path.partial"
        Invoke-WebRequest -Uri $entry.url -OutFile $partial
        Move-Item -LiteralPath $partial -Destination $Path -Force
    }
    if ((Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry.sha256) {
        throw "Download hash mismatch: $FileName. Remove the cached archive and retry."
    }
}

$archive = Join-Path $cache 'mediapipe-0.16.1.tgz'
Download 'mediapipe-0.16.1.tgz'
$extract = Join-Path $cache 'mediapipe'
New-Item $extract -ItemType Directory -Force | Out-Null
tar -xzf $archive -C $extract
if ($LASTEXITCODE -ne 0) { throw 'MediaPipe archive extraction failed.' }
$mediaSource = Join-Path $extract 'package'
foreach ($id in @('opencvsharp4', 'opencvsharp4.runtime.win')) {
    $zip = Join-Path $cache "$id.zip"
    Download "$id.zip"
    Expand-Archive -LiteralPath $zip -DestinationPath (Join-Path $cache $id) -Force
}
$cvManaged = Join-Path $cache 'opencvsharp4/lib/netstandard2.1/OpenCvSharp.dll'
$cvNative = Join-Path $cache 'opencvsharp4.runtime.win/runtimes/win-x64/native'

$version = (Get-Content (Join-Path $mediaSource 'package.json') -Raw | ConvertFrom-Json).version
if ($version -ne '0.16.1') { throw "Expected MediaPipe 0.16.1, got $version" }
$mediaTarget = Join-Path $repo 'Packages/com.github.homuler.mediapipe'
New-Item $mediaTarget -ItemType Directory -Force | Out-Null
# Keep runtime, notices and non-model resources. Samples and other platforms are unnecessary here.
foreach ($file in (Get-ChildItem -LiteralPath $mediaSource -Recurse -File)) {
    $relative = $file.FullName.Substring($mediaSource.Length + 1).Replace('\', '/')
    if ($relative -match '^Runtime/Plugins/(Android|iOS)(/|\.meta$)' -or
        $relative -match '^Runtime/Plugins/libmediapipe_c\.(so|dylib)(\.meta)?$') { continue }
    if ($relative -match '^Runtime/' -or $relative -match '^PackageResources/(Shaders|Prefabs)/' -or
        $relative -in @('package.json','LICENSE.md','Third Party Notices.md','Runtime.meta','PackageResources.meta')) {
        $destination = Join-Path $mediaTarget $relative
        New-Item (Split-Path $destination -Parent) -ItemType Directory -Force | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $destination -Force
    }
}

$plugins = Join-Path $repo 'Assets/Plugins/Dependencies'
New-Item $plugins -ItemType Directory -Force | Out-Null
Copy-Item -LiteralPath $cvManaged -Destination (Join-Path $plugins 'OpenCvSharp.dll') -Force
foreach ($name in @('OpenCvSharpExtern.dll','opencv_videoio_ffmpeg4110_64.dll')) {
    Copy-Item -LiteralPath (Join-Path $cvNative $name) -Destination (Join-Path $plugins $name) -Force
    # Explicitly enable x64 Windows Editor and player only.
    $guid = if ($name -eq 'OpenCvSharpExtern.dll') { '742c0dbb345f4171837d063ac37bf301' } else { 'efad30f81aab4a709de5baab62d896ea' }
    @"
fileFormatVersion: 2
guid: $guid
PluginImporter:
  externalObjects: {}
  serializedVersion: 2
  isPreloaded: 0
  platformData:
  - first:
      Any:
    second:
      enabled: 0
      settings: {}
  - first:
      Editor: Editor
    second:
      enabled: 1
      settings:
        CPU: x86_64
        OS: Windows
  - first:
      Standalone: Win64
    second:
      enabled: 1
      settings:
        CPU: x86_64
  userData:
  assetBundleName:
  assetBundleVariant:
"@ | Set-Content -LiteralPath (Join-Path $plugins "$name.meta") -Encoding utf8
}
$modelTarget = Join-Path $repo 'Assets/FaceFrustrumFollower/Resources/face_landmarker.bytes'
New-Item (Split-Path $modelTarget -Parent) -ItemType Directory -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $mediaSource 'PackageResources/MediaPipe/face_landmarker_v2_with_blendshapes.bytes') -Destination $modelTarget -Force
@'
fileFormatVersion: 2
guid: 5838e83559205ed6b9bdd94fa57e9a18
TextScriptImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
'@ | Set-Content -LiteralPath "$modelTarget.meta" -Encoding utf8
foreach ($entry in $lock.files) {
    $path = Join-Path $repo $entry.path
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry.sha256) {
        throw "Dependency hash mismatch: $($entry.path)"
    }
}
Write-Host 'Dependencies restored and SHA-256 checked (Windows x64). Open this folder in Unity Hub.'
