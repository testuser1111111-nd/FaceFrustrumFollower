using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FaceFrustrumFollower.Tests
{
    public sealed class DemoTests
    {
        [UnityTest]
        public IEnumerator SimulatedDemoRendersAndMovesWithoutCameras()
        {
            yield return SceneManager.LoadSceneAsync("FaceFrustrumDemo");
            yield return null;
            var simulation = Object.FindFirstObjectByType<SimulatedViewer>();
            Assert.That(simulation, Is.Not.Null);
            var initial = simulation.leftEye.position;
            for (int i = 0; i < 10; ++i) yield return null;
            Assert.That((simulation.leftEye.position - initial).magnitude, Is.GreaterThan(0));
            var combiner = Object.FindFirstObjectByType<TextureCombiner>();
            Assert.That(combiner.destination.IsCreated(), Is.True);
            // Camera.Render provides deterministic pixels even in the batchmode Test Runner.
            foreach (var offAxis in Object.FindObjectsByType<OffAxisCamera>(FindObjectsSortMode.None))
                offAxis.GetComponent<Camera>().Render();
            combiner.Combine();
            var previous = RenderTexture.active;
            var image = new Texture2D(combiner.destination.width, combiner.destination.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = combiner.destination;
                image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0); image.Apply();
                var colors = image.GetPixels32();
                bool hasGeometry = false;
                foreach (var color in colors)
                    if (color.r > 50 || color.g > 50 || color.b > 50) { hasGeometry = true; break; }
                Assert.That(hasGeometry, Is.True, "Demo should render lit geometry, not just the background.");
                var folder = Path.GetFullPath("TestResults"); Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder, "demo-preview.png"), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                Object.Destroy(image);
            }
            Assert.That(Object.FindFirstObjectByType<StereoFaceTracker>(), Is.Null);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
