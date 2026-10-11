using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FaceFrustrumFollower.Editor
{
    public static class ProjectTools
    {
        [MenuItem("FaceFrustrumFollower/Create demo scene")]
        public static void CreateDemo()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CreateDemoInternal();
        }

        public static void CreateDemoInternal()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var demo = new GameObject("FaceFrustrumFollower demo").AddComponent<DemoBootstrap>();
            demo.firstCalibration = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/FaceFrustrumFollower/Resources/calibration.example.xml");
            demo.secondCalibration = demo.firstCalibration;
            demo.faceModel = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/FaceFrustrumFollower/Resources/face_landmarker.bytes");
            demo.anaglyphShader = Shader.Find("FaceFrustrumFollower/AnaglyphCombine");
            EditorSceneManager.SaveScene(scene, "Assets/FaceFrustrumFollower/Demo/FaceFrustrumDemo.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scene.path, true) };
            AssetDatabase.SaveAssets();
        }

        public static void BuildWindows()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/FaceFrustrumFollower/Demo/FaceFrustrumDemo.unity" },
                locationPathName = "Builds/FaceFrustrumFollower.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception("Windows player build failed.");
        }
    }
}
