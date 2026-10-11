# サルベージ記録

## 回収元と移行方針

2026-10-11に `frustrum_test` を読み取り、既存のREADMEとApache-2.0ライセンスがあるFaceFrustrumFollowerへ移行しました。元フォルダには変更を加えていません。元のUnityバージョンは6000.3.10f1、移行先は利用可能な6000.3.14f1で検証します。

| 回収対象 | 移行先 / 処理 |
| --- | --- |
| `NewMonoBehaviourScript.cs` 内の `CameraCalibrator` | `CalibratedCameraStream` / `StereoFaceTracker` / `TrackingMath` / `OffAxisCamera` に分割 |
| `TextureCombiner.cs` / `AnaglyphCombine.shader` | `TextureCombiner` / `AnaglyphCombine.shader` として整理 |
| `Resources/calibration.xml` | `calibration.example.xml` として保存。別カメラへの適用は未検証 |
| 顔モデル、MediaPipe 0.16.1、OpenCvSharp 4.11 | バージョン固定の復元スクリプト、主要7ファイルのSHA-256 |
| `CharacterPoseController.cs` / `LocalAnimationServer.cs` | `docs/legacy/*.cs.txt` に原文を保管。視点追跡とは独立した実験なので起動しない |
| 背景・アバター・emotes・Demo・Mine | 元フォルダに保全。移行先デモはUnityプリミティブから生成 |
| Library、Logs、UserSettings、生成csproj/sln、復旧シーン | 移行不要。Git対象外 |

元のスクリプトはCP932からUTF-8へ変換して保管しています。元シーンはスクリプトのファイル名とクラス名が一致せず、`Assembly-CSharp::NewMonoBehaviourScript` の参照が残っていました。新しいデモでは同名クラスとファイル、安定したmetaを使います。

## 修正内容

- カメラ番号、各カメラの校正、ベースライン、画面寸法、許容深さ、鮮度、平滑化を設定化。
- 歪み補正後の内部行列を距離計算にも使用。
- 顔未検出、ゼロ視差、非有限値、距離範囲外、古いフレーム、時刻差の大きなペアを棄却。
- デバッグマーカーや特定アバターを必須にしない構成。
- MediaPipe Image・FaceLandmarker、OpenCV Mat、Unity Texture・Materialを解放。
- カメラ読み取りは専用スレッド、Unity APIと推論はメインスレッド。停止待ちは有限。
- 物理カメラの焦点距離操作を画面寸法に基づく非対称投影へ置換。
- 毎フレームの成功ログと不要な画像の二重リサイズ・コピーを除去。
- 元フォルダのMediaPipe DLLは公式0.16.1配布とSHA-256が異なったため、公式配布に統一。残る6つの主要ファイルは一致を確認。

## 検証結果

Unity 6000.3.14f1 / Windows x64で以下を確認しました。

- Unityコンパイルとデモ生成: 成功、終了コード0。
- EditMode: 8/8件成功。視差・投影の検証に加え、OpenCV 4.11のネイティブロード、XML読み込み、MediaPipe顔モデルを使った空画像の推論も成功。
- PlayMode: 1/1件成功。実カメラなしの視点移動、左右カメラの描画、アナグリフ合成を検証。`docs/demo-preview.png` はその実レンダー。
- Windows x64 Development player: ビルド成功。
- 元プロジェクトを参照しない公式配布からの依存復元: 成功。配布アーカイブ3件と主要バイナリ・モデル7件のSHA-256を固定して確認。
- カメラを開く試験、2台の実機追跡、深さ精度、入力遅延、長時間試験: 未実施。

ログとテストXMLはローカルの `work` / `TestResults`、プレイヤーは `Builds` に生成されます。これらはGit対象外です。

## 残る範囲

ステレオ外部校正、画面とカメラ間の厳密な座標変換、ハードウェア同期、複数人の対応付けは未実装です。Windows x64以外の環境は復元対象にしていません。実機の追跡精度・処理速度・長時間動作は本移行だけでは保証しません。
