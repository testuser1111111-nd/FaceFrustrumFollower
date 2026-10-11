using UnityEngine;

namespace FaceFrustrumFollower
{
    public sealed class SimulatedViewer : MonoBehaviour
    {
        public Transform screen;
        public Transform leftEye;
        public Transform rightEye;
        public float eyeSeparationMeters = 0.064f;
        public float distanceMeters = 0.7f;
        public bool animate = true;

        private void Update()
        {
            if (screen == null || leftEye == null || rightEye == null) return;
            var center = new Vector3(animate ? Mathf.Sin(Time.time * 0.7f) * 0.16f : 0,
                animate ? Mathf.Sin(Time.time * 0.5f) * 0.07f : 0, -distanceMeters);
            leftEye.position = screen.TransformPoint(center + Vector3.left * eyeSeparationMeters * 0.5f);
            rightEye.position = screen.TransformPoint(center + Vector3.right * eyeSeparationMeters * 0.5f);
        }
    }
}
