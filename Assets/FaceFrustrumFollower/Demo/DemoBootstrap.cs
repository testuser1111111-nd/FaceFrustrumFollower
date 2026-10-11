using UnityEngine;

namespace FaceFrustrumFollower
{
    public sealed class DemoBootstrap : MonoBehaviour
    {
        public bool useRealCameras;
        public int firstCameraIndex;
        public int secondCameraIndex = 2;
        public TextAsset firstCalibration;
        public TextAsset secondCalibration;
        public TextAsset faceModel;
        public Shader anaglyphShader;
        public float baselineMeters = 0.15f;
        public Vector2 screenSizeMeters = new Vector2(0.6f, 0.335f);
        private RenderTexture leftTexture, rightTexture, combined;
        private StereoFaceTracker tracker;
        private TextureCombiner combiner;

        private void Start()
        {
            var screen = new GameObject("Screen center (+Z into virtual world)").transform;
            screen.SetParent(transform, false);
            leftTexture = MakeTexture("Left eye"); rightTexture = MakeTexture("Right eye");
            combined = MakeTexture("Anaglyph");
            var left = MakeEye("Left eye", screen, -0.032f, leftTexture);
            var right = MakeEye("Right eye", screen, 0.032f, rightTexture);
            combiner = gameObject.AddComponent<TextureCombiner>();
            combiner.enabled = false;
            combiner.shader = anaglyphShader;
            combiner.sourceR = leftTexture; combiner.sourceGB = rightTexture; combiner.destination = combined;
            combiner.enabled = true;
            if (useRealCameras)
            {
                // Add to an inactive object so OnEnable sees fully configured fields.
                var input = new GameObject("Stereo face input"); input.SetActive(false);
                input.transform.SetParent(transform, false);
                tracker = input.AddComponent<StereoFaceTracker>();
                tracker.firstCameraIndex = firstCameraIndex; tracker.secondCameraIndex = secondCameraIndex;
                tracker.firstCalibration = firstCalibration; tracker.secondCalibration = secondCalibration;
                tracker.faceModel = faceModel; tracker.baselineMeters = baselineMeters;
                tracker.screen = screen; tracker.leftEye = left; tracker.rightEye = right;
                input.SetActive(true);
            }
            else
            {
                var simulation = gameObject.AddComponent<SimulatedViewer>();
                simulation.screen = screen; simulation.leftEye = left; simulation.rightEye = right;
            }
            var light = new GameObject("Light").AddComponent<Light>();
            light.transform.SetParent(transform, false);
            light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(35, -25, 0);
            for (int i = 0; i < 9; ++i)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = $"Depth cue {i + 1}"; cube.transform.SetParent(transform, false);
                cube.transform.localPosition = new Vector3((i % 3 - 1) * 0.16f, (i / 3 - 1) * 0.09f, 0.2f + i * 0.12f);
                cube.transform.localScale = Vector3.one * 0.055f;
            }
        }

        private Transform MakeEye(string name, Transform screen, float x, RenderTexture texture)
        {
            var eye = new GameObject(name); eye.transform.SetParent(transform, false);
            eye.transform.localPosition = new Vector3(x, 0, -0.7f);
            var camera = eye.AddComponent<Camera>();
            camera.nearClipPlane = 0.02f; camera.farClipPlane = 20f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.03f, 0.04f, 0.07f);
            camera.targetTexture = texture;
            var offAxis = eye.AddComponent<OffAxisCamera>(); offAxis.screen = screen; offAxis.screenSizeMeters = screenSizeMeters;
            return eye.transform;
        }

        private static RenderTexture MakeTexture(string name)
        {
            var texture = new RenderTexture(1280, 720, 24) { name = name };
            texture.Create(); return texture;
        }

        private void OnGUI()
        {
            if (combined == null) return;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), combined, ScaleMode.ScaleToFit);
            GUI.Box(new Rect(8, 8, 580, 52), "FaceFrustrumFollower — red/cyan stereo\n" +
                (useRealCameras ? tracker?.Status : "Simulated viewer; enable Use Real Cameras before Play for tracking"));
            if (useRealCameras && tracker != null)
            {
                DrawPreview(tracker.FirstPreview, 8);
                DrawPreview(tracker.SecondPreview, 216);
            }
        }

        private static void DrawPreview(Texture texture, int x)
        {
            if (texture != null) GUI.DrawTexture(new Rect(x, 68, 200, 112), texture, ScaleMode.ScaleToFit);
        }

        private void OnDestroy()
        {
            if (combiner != null) combiner.enabled = false;
            foreach (var texture in new[] { leftTexture, rightTexture, combined })
            {
                if (texture == null) continue;
                texture.Release(); Destroy(texture);
            }
        }
    }
}
