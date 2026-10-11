using NUnit.Framework;
using UnityEngine;

namespace FaceFrustrumFollower.Tests
{
    public sealed class TrackingMathTests
    {
        private static readonly Vector4 K = new Vector4(1000, 1000, 960, 540);

        [Test]
        public void KnownDisparityProducesMetricDepth()
        {
            Assert.That(TrackingMath.TryEstimate(new Vector2(1110, 540), new Vector2(810, 540),
                K, K, 0.15f, 0.1f, 3, out var position), Is.True);
            Assert.That(position.x, Is.EqualTo(0).Within(0.00001));
            Assert.That(position.y, Is.EqualTo(0).Within(0.00001));
            Assert.That(position.z, Is.EqualTo(0.5f).Within(0.00001));
        }

        [Test]
        public void ZeroDisparityDoesNotReturnInfinity()
        {
            Assert.That(TrackingMath.TryEstimate(Vector2.zero, Vector2.zero, K, K,
                0.15f, 0.1f, 3, out _), Is.False);
        }

        [Test]
        public void RejectsNonFiniteAndOutOfRangeObservations()
        {
            Assert.That(TrackingMath.TryEstimate(new Vector2(float.NaN, 0), Vector2.zero, K, K,
                0.15f, 0.1f, 3, out _), Is.False);
            Assert.That(TrackingMath.TryEstimate(new Vector2(1, 0), Vector2.zero, K, K,
                0.15f, 0.1f, 3, out _), Is.False);
            Assert.That(TrackingMath.TryEstimate(Vector2.one, Vector2.zero, Vector4.zero, K,
                0.15f, 0.1f, 3, out _), Is.False);
        }

        [Test]
        public void EachCameraUsesItsOwnIntrinsics()
        {
            var secondK = new Vector4(500, 500, 480, 270);
            Assert.That(TrackingMath.TryEstimate(new Vector2(1110, 540), new Vector2(405, 270),
                K, secondK, 0.15f, 0.1f, 3, out var position), Is.True);
            Assert.That(position.z, Is.EqualTo(0.5f).Within(0.00001));
        }

        [Test]
        public void ScreenCornersProjectToViewportEdgesForAnOffsetViewer()
        {
            var eye = new Vector3(0.12f, 0.04f, -0.8f);
            Assert.That(TrackingMath.TryProjection(eye, new Vector2(0.6f, 0.335f),
                0.02f, 20, out var projection), Is.True);
            var corners = new[] { new Vector3(-0.3f, -0.1675f, 0), new Vector3(0.3f, 0.1675f, 0) };
            for (int i = 0; i < corners.Length; ++i)
            {
                var view = corners[i] - eye;
                var clip = projection * new Vector4(view.x, view.y, -view.z, 1);
                float expected = i == 0 ? -1 : 1;
                Assert.That(clip.x / clip.w, Is.EqualTo(expected).Within(0.00001));
                Assert.That(clip.y / clip.w, Is.EqualTo(expected).Within(0.00001));
            }
        }

        [Test]
        public void ViewerOnOrBehindScreenIsRejected()
        {
            Assert.That(TrackingMath.TryProjection(Vector3.zero, Vector2.one, 0.02f, 20, out _), Is.False);
            Assert.That(TrackingMath.TryProjection(Vector3.forward, Vector2.one, 0.02f, 20, out _), Is.False);
        }
    }
}
