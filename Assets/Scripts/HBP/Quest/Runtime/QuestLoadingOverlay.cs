using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>Keeps the complete loading presentation above world-space windows, including themed graphics.</summary>
    [RequireComponent(typeof(Canvas))]
    public sealed class QuestLoadingOverlay : MonoBehaviour
    {
        [SerializeField] private Material material;
        private Graphic[] graphics;

        private void Awake() => graphics = GetComponentsInChildren<Graphic>(true);

        private void OnEnable()
        {
            Canvas.preWillRenderCanvases += ApplyMaterial;
            ApplyMaterial();
        }

        private void OnDisable() => Canvas.preWillRenderCanvases -= ApplyMaterial;

        private void ApplyMaterial()
        {
            if (material == null || graphics == null) return;
            foreach (var graphic in graphics)
                if (graphic != null && graphic.material != material)
                    graphic.material = material;
        }
    }
}
