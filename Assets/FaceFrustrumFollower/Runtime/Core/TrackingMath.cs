using UnityEngine;

namespace FaceFrustrumFollower
{
    /// <summary>Prototype distance estimate, not calibrated stereo triangulation.</summary>
    public static class TrackingMath
    {
        public static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        public static bool TryEstimate(Vector2 first, Vector2 second, Vector4 firstIntrinsics,
            Vector4 secondIntrinsics, float baselineMeters, float minDepth, float maxDepth,
            out Vector3 position)
        {
            position = default;
            if (!IsFinite(baselineMeters) || baselineMeters <= 0 || minDepth <= 0 ||
                maxDepth < minDepth || !ValidIntrinsics(firstIntrinsics) || !ValidIntrinsics(secondIntrinsics))
                return false;
            var a = Normalize(first, firstIntrinsics);
            var b = Normalize(second, secondIntrinsics);
            var disparity = (a - b).magnitude;
            if (!IsFinite(disparity) || disparity < 0.000001f) return false;
            float depth = baselineMeters / disparity;
            if (!IsFinite(depth) || depth < minDepth || depth > maxDepth) return false;
            var center = (a + b) * 0.5f;
            position = new Vector3(center.x * depth, -center.y * depth, depth);
            return IsFinite(position.x) && IsFinite(position.y);
        }

        private static bool ValidIntrinsics(Vector4 k) =>
            IsFinite(k.x) && IsFinite(k.y) && IsFinite(k.z) && IsFinite(k.w) && k.x > 0 && k.y > 0;

        private static Vector2 Normalize(Vector2 p, Vector4 k) =>
            new Vector2((p.x - k.z) / k.x, (p.y - k.w) / k.y);

        public static bool TryProjection(Vector3 eyeInScreenSpace, Vector2 screenSize,
            float near, float far, out Matrix4x4 projection)
        {
            projection = Matrix4x4.identity;
            float distance = -eyeInScreenSpace.z;
            if (!IsFinite(eyeInScreenSpace.x) || !IsFinite(eyeInScreenSpace.y) || !IsFinite(distance) ||
                !IsFinite(screenSize.x) || !IsFinite(screenSize.y) || screenSize.x <= 0 || screenSize.y <= 0 ||
                !IsFinite(near) || !IsFinite(far) || near <= 0 || far <= near || distance <= near)
                return false;
            float scale = near / distance;
            projection = Matrix4x4.Frustum(
                (-screenSize.x * 0.5f - eyeInScreenSpace.x) * scale,
                (screenSize.x * 0.5f - eyeInScreenSpace.x) * scale,
                (-screenSize.y * 0.5f - eyeInScreenSpace.y) * scale,
                (screenSize.y * 0.5f - eyeInScreenSpace.y) * scale, near, far);
            return true;
        }
    }
}
