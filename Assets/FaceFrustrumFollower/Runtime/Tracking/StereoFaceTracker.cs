using System;
using System.Diagnostics;
using UnityEngine;

namespace FaceFrustrumFollower
{
    public sealed class StereoFaceTracker : MonoBehaviour
    {
        public int firstCameraIndex = 0;
        public int secondCameraIndex = 2;
        public TextAsset firstCalibration;
        public TextAsset secondCalibration;
        public TextAsset faceModel;
        public Transform screen;
        public Transform leftEye;
        public Transform rightEye;
        [Min(0.001f)] public float baselineMeters = 0.15f;
        public Vector2 depthRangeMeters = new Vector2(0.15f, 3f);
        [Min(0.01f)] public float maximumFrameAgeSeconds = 0.25f;
        [Min(0.001f)] public float maximumPairSkewSeconds = 0.10f;
        [Min(0f)] public float smoothingRate = 12f;
        public Vector3 viewerOffsetMeters;
        public bool invertX;
        public bool invertY;
        public bool IsTracking { get; private set; }
        public string Status { get; private set; } = "Disabled";
        public Texture FirstPreview => first?.Preview;
        public Texture SecondPreview => second?.Preview;
        private CalibratedCameraStream first, second;
        private Stopwatch clock;
        private bool receivedPose;

        private void OnEnable()
        {
            try
            {
                if (screen == null || leftEye == null || rightEye == null || firstCameraIndex == secondCameraIndex)
                    throw new ArgumentException("Assign screen and both eyes, and select two different camera indices.");
                if (faceModel == null) faceModel = Resources.Load<TextAsset>("face_landmarker");
                clock = Stopwatch.StartNew();
                receivedPose = false;
                first = new CalibratedCameraStream(firstCameraIndex, firstCalibration, faceModel, clock);
                second = new CalibratedCameraStream(secondCameraIndex, secondCalibration, faceModel, clock);
                Status = "Waiting for a face in both cameras";
            }
            catch (Exception exception)
            {
                Status = exception.Message;
                UnityEngine.Debug.LogError($"Face tracker: {Status}", this);
                enabled = false;
            }
        }

        private void Update()
        {
            try
            {
                first.ProcessLatest(); second.ProcessLatest();
                IsTracking = false;
                if (first.Error != null || second.Error != null)
                {
                    Status = first.Error ?? second.Error;
                    enabled = false;
                    return;
                }
                long now = clock.ElapsedMilliseconds;
                if (!first.HasFace || !second.HasFace ||
                    now - first.ObservationTime > maximumFrameAgeSeconds * 1000 ||
                    now - second.ObservationTime > maximumFrameAgeSeconds * 1000 ||
                    Math.Abs(first.ObservationTime - second.ObservationTime) > maximumPairSkewSeconds * 1000)
                {
                    Status = "No fresh matching face pair; holding last pose";
                    return;
                }
                if (!Estimate(first.LeftIris, second.LeftIris, out var left) ||
                    !Estimate(first.RightIris, second.RightIris, out var right))
                {
                    Status = "Invalid disparity or depth; holding last pose";
                    return;
                }
                float alpha = !receivedPose || smoothingRate <= 0 ? 1 : 1 - Mathf.Exp(-smoothingRate * Time.deltaTime);
                leftEye.position = Vector3.Lerp(leftEye.position, ToWorld(left), alpha);
                rightEye.position = Vector3.Lerp(rightEye.position, ToWorld(right), alpha);
                receivedPose = IsTracking = true;
                Status = "Tracking (approximate stereo)";
            }
            catch (Exception exception)
            {
                Status = exception.Message;
                UnityEngine.Debug.LogError($"Face tracker: {Status}", this);
                enabled = false;
            }
        }

        private bool Estimate(Vector2 a, Vector2 b, out Vector3 position) =>
            TrackingMath.TryEstimate(a, b, first.Intrinsics, second.Intrinsics,
                baselineMeters, depthRangeMeters.x, depthRangeMeters.y, out position);

        private Vector3 ToWorld(Vector3 point) => screen.TransformPoint(new Vector3(
            invertX ? -point.x : point.x, invertY ? -point.y : point.y, -point.z) + viewerOffsetMeters);

        private void OnDisable()
        {
            IsTracking = false;
            first?.Dispose(); first = null;
            second?.Dispose(); second = null;
            clock?.Stop();
        }
    }
}
