using System;
using NUnit.Framework;
using OpenCvSharp;
using UnityEngine;
using Mediapipe.Tasks.Vision.FaceLandmarker;

namespace FaceFrustrumFollower.Tests
{
    public sealed class NativeDependencyTests
    {
        [Test]
        public void OpenCvNativeRuntimeAndExampleCalibrationLoad()
        {
            Assert.That(Cv2.GetVersionString(), Does.StartWith("4.11"));
            var xml = Resources.Load<TextAsset>("calibration.example");
            Assert.That(xml, Is.Not.Null);
            using var storage = new FileStorage(xml.text, FileStorage.Modes.Read | FileStorage.Modes.Memory);
            using var matrix = storage["camera_matrix"].ReadMat();
            Assert.That(matrix.Rows, Is.EqualTo(3));
            Assert.That(matrix.At<double>(0, 0), Is.GreaterThan(0));
            Assert.That(storage["image_width"].ReadInt(), Is.EqualTo(1920));
        }

        [Test]
        public void MediaPipeModelRunsOnBlankImageWithoutOpeningCameras()
        {
            var model = Resources.Load<TextAsset>("face_landmarker");
            Assert.That(model, Is.Not.Null);
            var options = new FaceLandmarkerOptions(new Mediapipe.Tasks.Core.BaseOptions(
                Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU, modelAssetBuffer: model.bytes));
            using var task = (IDisposable)FaceLandmarker.CreateFromOptions(options);
            var texture = new Texture2D(64, 64, TextureFormat.RGB24, false);
            try
            {
                texture.LoadRawTextureData(new byte[64 * 64 * 3]); texture.Apply();
                using var image = new Mediapipe.Image(texture);
                var result = ((FaceLandmarker)task).Detect(image);
                Assert.That(result.faceLandmarks == null || result.faceLandmarks.Count == 0, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
