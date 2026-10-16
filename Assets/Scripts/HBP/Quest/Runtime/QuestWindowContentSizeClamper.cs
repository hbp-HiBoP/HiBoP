using HBP.UI.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>Feeds the Desktop clamper with the inner panel's layout, independently of the detached control bar.</summary>
    [ExecuteAlways]
    public sealed class QuestWindowContentSizeClamper : ContentSizeClamper, ILayoutElement
    {
        [SerializeField] private RectTransform layoutRoot;
        [SerializeField] private Vector2 minimumSize = new Vector2(500, 160);
        [SerializeField] private bool fitHeightWhenContentChanges;
        private float previousPreferredHeight = -1;
        private QuestWindow window;

        public float minWidth => Mathf.Max(minimumSize.x, layoutRoot == null ? 0 : LayoutUtility.GetMinWidth(layoutRoot));
        public float minHeight => Mathf.Max(minimumSize.y, layoutRoot == null ? 0 : LayoutUtility.GetPreferredHeight(layoutRoot));
        public float preferredWidth => Mathf.Max(minWidth, layoutRoot == null ? 0 : LayoutUtility.GetPreferredWidth(layoutRoot));
        public float preferredHeight => minHeight;
        public float flexibleWidth => -1;
        public float flexibleHeight => -1;
        public int layoutPriority => 1;

        public void CalculateLayoutInputHorizontal()
        {
        }

        public void CalculateLayoutInputVertical()
        {
        }

        private void LateUpdate() => RefreshSize();

        public void RefreshSize()
        {
            if (!IsActive() || layoutRoot == null || !layoutRoot.gameObject.activeInHierarchy) return;
            if (window == null) window = GetComponent<QuestWindow>();
            if (window != null && window.IsManipulated) return;
            float height = preferredHeight;
            if (fitHeightWhenContentChanges && !Mathf.Approximately(previousPreferredHeight, height))
                RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            previousPreferredHeight = height;
            SetLayoutHorizontal();
            SetLayoutVertical();
        }
    }
}
