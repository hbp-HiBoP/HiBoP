using HBP.Data.Module3D;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    public sealed class QuestAtlasHoverTooltip : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private Image background;
        [SerializeField] private Text label;
        [SerializeField] private Material overlayMaterial;

        private AtlasInfo displayedRegion;

        internal void Show(AtlasInfo region, Vector3 impact, Transform head)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);

            if (displayedRegion != region)
            {
                displayedRegion = region;
                label.text = region.Type == AtlasInfo.AtlasType.MarsAtlas
                    ? $"<b>Name:</b> {region.Information1}\n" +
                      $"<b>Hemisphere:</b> {region.Information2}\n" +
                      $"<b>Lobe:</b> {region.Information3}\n" +
                      $"<b>Name FS:</b> {region.Information4}\n" +
                      $"<b>Full Name:</b> {region.Information5}"
                    : $"<b>Region:</b> {region.Information1}";
                panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Clamp(label.preferredWidth + 32, 140, 440));
                panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, label.preferredHeight + 24);
            }

            // Theme changes can reapply their standard materials.
            if (background.material != overlayMaterial) background.material = overlayMaterial;
            if (label.material != overlayMaterial) label.material = overlayMaterial;

            Vector3 fromHead = impact - head.position;
            float impactDistance = fromHead.magnitude;
            Vector3 direction = impactDistance > .001f ? fromHead / impactDistance : head.forward;
            float distance = Mathf.Clamp(impactDistance, .35f, 2.5f);
            float side = Vector3.Dot(fromHead, head.right) > .18f * distance ? -1 : 1;
            panel.pivot = new Vector2(side > 0 ? 0 : 1, .5f);
            panel.localScale = Vector3.one * (.0007f * distance);
            Vector3 position = head.position + direction * Mathf.Max(.35f, impactDistance - .015f)
                + head.right * (.065f * distance * side) + head.up * (.035f * distance);
            panel.SetPositionAndRotation(position, Quaternion.LookRotation(position - head.position, head.up));
        }

        internal void Hide()
        {
            displayedRegion = null;
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }
    }
}
