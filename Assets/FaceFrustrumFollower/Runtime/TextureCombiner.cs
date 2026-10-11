using UnityEngine;

namespace FaceFrustrumFollower
{
    [ExecuteAlways]
    public sealed class TextureCombiner : MonoBehaviour
    {
        public RenderTexture sourceR;
        public RenderTexture sourceGB;
        public RenderTexture destination;
        [Tooltip("Assign explicitly so player shader stripping cannot remove it.")]
        public Shader shader;
        private Material material;

        private void OnEnable()
        {
            if (shader != null) material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        public void Combine()
        {
            if (material == null || sourceR == null || sourceGB == null || destination == null) return;
            material.SetTexture("_SecondTex", sourceGB);
            Graphics.Blit(sourceR, destination, material);
        }

        private void LateUpdate() => Combine();
        private void OnDisable()
        {
            if (material == null) return;
            if (Application.isPlaying) Destroy(material);
            else DestroyImmediate(material);
            material = null;
        }
    }
}
