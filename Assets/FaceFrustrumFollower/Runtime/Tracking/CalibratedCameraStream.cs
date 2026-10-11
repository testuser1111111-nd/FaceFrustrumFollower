using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using OpenCvSharp;
using UnityEngine;
using Mediapipe.Tasks.Vision.FaceLandmarker;

namespace FaceFrustrumFollower
{
    /// <summary>Capture owns its native handle on a worker; inference stays on Unity's main thread.</summary>
    internal sealed class CalibratedCameraStream : IDisposable
    {
        private readonly object gate = new object();
        private readonly Mat latest = new Mat();
        private readonly Mat frame = new Mat();
        private readonly Mat corrected = new Mat();
        private readonly Mat rgb = new Mat();
        private readonly Mat mapX = new Mat();
        private readonly Mat mapY = new Mat();
        private readonly Stopwatch clock;
        private VideoCapture capture;
        private FaceLandmarker landmarker;
        private Texture2D texture;
        private byte[] pixels;
        private Thread worker;
        private volatile bool running;
        private volatile string error;
        private long frameSequence, consumedSequence, captureTime, lastInferenceTime = -1;
        public Vector4 Intrinsics { get; private set; }
        public Vector2 RightIris { get; private set; }
        public Vector2 LeftIris { get; private set; }
        public long ObservationTime { get; private set; }
        public bool HasFace { get; private set; }
        public Texture Preview => texture;
        public string Error => error;

        public CalibratedCameraStream(int index, TextAsset calibration, TextAsset model, Stopwatch clock)
        {
            this.clock = clock;
            try
            {
                if (calibration == null || model == null) throw new ArgumentException("Calibration and face model are required.");
                using var storage = new FileStorage(calibration.text, FileStorage.Modes.Read | FileStorage.Modes.Memory);
                using var matrix = storage["camera_matrix"].ReadMat();
                using var distortion = storage["distortion_coefficients"].ReadMat();
                int width = storage["image_width"].ReadInt();
                int height = storage["image_height"].ReadInt();
                if (width <= 0 || height <= 0 || matrix.Rows != 3 || matrix.Cols != 3 || distortion.Empty())
                    throw new ArgumentException("Invalid OpenCV calibration XML.");
                var size = new Size(width, height);
                using var adjusted = Cv2.GetOptimalNewCameraMatrix(matrix, distortion, size, 0, size, out _, true);
                using var identity = new Mat();
                Cv2.InitUndistortRectifyMap(matrix, distortion, identity, adjusted, size, MatType.CV_32FC1, mapX, mapY);
                Intrinsics = new Vector4((float)adjusted.At<double>(0, 0), (float)adjusted.At<double>(1, 1),
                    (float)adjusted.At<double>(0, 2), (float)adjusted.At<double>(1, 2));
                capture = new VideoCapture(index, VideoCaptureAPIs.DSHOW);
                if (!capture.IsOpened()) throw new InvalidOperationException($"Camera {index} could not be opened.");
                capture.Set(VideoCaptureProperties.FourCC, FourCC.FromString("MJPG"));
                capture.Set(VideoCaptureProperties.FrameWidth, width);
                capture.Set(VideoCaptureProperties.FrameHeight, height);
                landmarker = FaceLandmarker.CreateFromOptions(new FaceLandmarkerOptions(
                    baseOptions: new Mediapipe.Tasks.Core.BaseOptions(Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU,
                        modelAssetBuffer: model.bytes),
                    runningMode: Mediapipe.Tasks.Vision.Core.RunningMode.VIDEO, numFaces: 1));
                running = true;
                worker = new Thread(() => CaptureLoop(width, height)) { IsBackground = true, Name = $"Camera {index}" };
                worker.Start();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void CaptureLoop(int width, int height)
        {
            try
            {
                using var captured = new Mat();
                while (running)
                {
                    if (!capture.Read(captured) || captured.Empty())
                    {
                        error = "Camera stopped delivering frames.";
                        break;
                    }
                    if (captured.Width != width || captured.Height != height)
                    {
                        error = $"Camera returned {captured.Width}x{captured.Height}; calibration requires {width}x{height}.";
                        break;
                    }
                    lock (gate)
                    {
                        captured.CopyTo(latest);
                        captureTime = clock.ElapsedMilliseconds;
                        ++frameSequence;
                    }
                    Thread.Sleep(1);
                }
            }
            catch (Exception exception) { error = exception.Message; }
            finally
            {
                running = false;
                capture.Dispose();
                lock (gate) latest.Dispose();
            }
        }

        public void ProcessLatest()
        {
            if (!running || error != null) { HasFace = false; return; }
            long timestamp;
            lock (gate)
            {
                if (!running || consumedSequence == frameSequence) return;
                latest.CopyTo(frame);
                consumedSequence = frameSequence;
                timestamp = captureTime;
            }
            Cv2.Remap(frame, corrected, mapX, mapY, InterpolationFlags.Linear);
            Cv2.CvtColor(corrected, rgb, ColorConversionCodes.BGR2RGB);
            if (texture == null)
            {
                texture = new Texture2D(rgb.Width, rgb.Height, TextureFormat.RGB24, false);
                pixels = new byte[checked((int)(rgb.Total() * rgb.ElemSize()))];
            }
            Marshal.Copy(rgb.Data, pixels, 0, pixels.Length);
            texture.LoadRawTextureData(pixels);
            texture.Apply(false);
            using var image = new Mediapipe.Image(texture);
            lastInferenceTime = Math.Max(timestamp, lastInferenceTime + 1);
            var result = landmarker.DetectForVideo(image, lastInferenceTime);
            ObservationTime = timestamp;
            HasFace = result.faceLandmarks != null && result.faceLandmarks.Count == 1 &&
                result.faceLandmarks[0].landmarks.Count > 473;
            if (!HasFace) return;
            var landmarks = result.faceLandmarks[0].landmarks;
            RightIris = new Vector2(landmarks[468].x * rgb.Width, landmarks[468].y * rgb.Height);
            LeftIris = new Vector2(landmarks[473].x * rgb.Width, landmarks[473].y * rgb.Height);
        }

        public void Dispose()
        {
            running = false;
            if (worker == null)
            {
                capture?.Dispose();
                latest.Dispose();
            }
            else if (!worker.Join(1000))
                UnityEngine.Debug.LogWarning("Camera driver is still blocked. Its worker will release capture when the driver returns.");
            (landmarker as IDisposable)?.Dispose();
            frame.Dispose(); corrected.Dispose(); rgb.Dispose(); mapX.Dispose(); mapY.Dispose();
            if (texture != null) UnityEngine.Object.Destroy(texture);
        }
    }
}
