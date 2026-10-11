using UnityEngine;

namespace FaceFrustrumFollower
{
    [RequireComponent(typeof(Camera))]
    public sealed class OffAxisCamera : MonoBehaviour
    {
        [Tooltip("Screen center; local +Z points into the virtual world. Keep its scale at one.")]
        public Transform screen;
        public Vector2 screenSizeMeters = new Vector2(0.6f, 0.335f);
        private Camera target;

        private void OnEnable() => target = GetComponent<Camera>();

        private void LateUpdate()
        {
            if (screen == null) return;
            var eye = screen.InverseTransformPoint(transform.position);
            if (TrackingMath.TryProjection(eye, screenSizeMeters, target.nearClipPlane,
                target.farClipPlane, out var projection))
            {
                transform.rotation = screen.rotation;
                target.projectionMatrix = projection;
            }
            else target.ResetProjectionMatrix();
        }

        private void OnDisable()
        {
            if (target != null) target.ResetProjectionMatrix();
        }
    }
}
