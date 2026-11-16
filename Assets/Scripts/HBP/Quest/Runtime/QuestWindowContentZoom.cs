using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>Zooms the window body and can include the zoomed content in the window's sizing constraints.</summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(300)]
    public sealed class QuestWindowContentZoom : LayoutGroup
    {
        public const int MinimumPercent = 50, MaximumPercent = 150, StepPercent = 10;
        [SerializeField] private QuestWindow window;
        [SerializeField] private RectTransform content;
        [SerializeField] private Button decrease, increase;
        [SerializeField] private Text percentage;
        [SerializeField] private bool fitZoomedContent;
        private int zoomPercent = 100;
        private int? pendingZoom;
        private Vector2 baselineMinimum, baselinePreferred, baselineFlexible;
        private bool hasBaseline;
        private int settlingFrames;
        public int ZoomPercent => pendingZoom ?? zoomPercent;
        public float Factor => zoomPercent / 100f;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (decrease != null) decrease.onClick.AddListener(ZoomOut);
            if (increase != null) increase.onClick.AddListener(ZoomIn);
            if (pendingZoom.HasValue)
            {
                int next = pendingZoom.Value;
                pendingZoom = null;
                SetZoomPercent(next, false);
            }

            RefreshControls();
        }

        protected override void OnDisable()
        {
            if (decrease != null) decrease.onClick.RemoveListener(ZoomOut);
            if (increase != null) increase.onClick.RemoveListener(ZoomIn);
            base.OnDisable();
        }

        public void ZoomOut() => SetZoomPercent(ZoomPercent - StepPercent);
        public void ZoomIn() => SetZoomPercent(ZoomPercent + StepPercent);

        public void SetZoomPercent(int value) => SetZoomPercent(value, true);
        internal void RestoreZoomPercent(int value) => SetZoomPercent(value, false);

        private void SetZoomPercent(int value, bool notify)
        {
            int next = Mathf.Clamp(Mathf.RoundToInt(value / (float)StepPercent) * StepPercent, MinimumPercent, MaximumPercent);
            if (next == ZoomPercent) return;
            if (!isActiveAndEnabled)
            {
                pendingZoom = next;
                RefreshControls();
                if (notify && window != null) window.NotifySettingsChanged();
                return;
            }

            var root = window != null ? (RectTransform)window.transform : rectTransform;
            if (!fitZoomedContent && !hasBaseline) LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            zoomPercent = next;
            // Width-dependent children, including the Signals splitter, need to settle before measuring at 100% again.
            settlingFrames = !fitZoomedContent && next == 100 ? 2 : 0;
            SetLayoutHorizontal();
            SetLayoutVertical();
            RefreshControls();
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            if (notify && window != null) window.NotifySettingsChanged();
        }

        private void LateUpdate()
        {
            if (settlingFrames > 0 && --settlingFrames == 0) SetDirty();
        }

        private void RefreshControls()
        {
            if (decrease != null) decrease.interactable = ZoomPercent > MinimumPercent;
            if (increase != null) increase.interactable = ZoomPercent < MaximumPercent;
            if (percentage != null) percentage.text = $"{ZoomPercent} %";
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            MeasureContent(0);
        }

        public override void CalculateLayoutInputVertical()
        {
            MeasureContent(1);
            hasBaseline = content != null;
        }

        private void MeasureContent(int axis)
        {
            if (fitZoomedContent && content != null)
            {
                SetLayoutInputForAxis(LayoutUtility.GetMinSize(content, axis) * Factor, LayoutUtility.GetPreferredSize(content, axis) * Factor, LayoutUtility.GetFlexibleSize(content, axis), axis);
                return;
            }

            if (content != null && zoomPercent == 100 && settlingFrames == 0)
            {
                baselineMinimum[axis] = LayoutUtility.GetMinSize(content, axis);
                baselinePreferred[axis] = LayoutUtility.GetPreferredSize(content, axis);
                baselineFlexible[axis] = LayoutUtility.GetFlexibleSize(content, axis);
            }

            SetLayoutInputForAxis(baselineMinimum[axis], baselinePreferred[axis], baselineFlexible[axis], axis);
        }

        public override void SetLayoutHorizontal()
        {
            if (content == null) return;
            m_Tracker.Add(this, content, DrivenTransformProperties.ScaleX | DrivenTransformProperties.ScaleY);
            content.localScale = new Vector3(Factor, Factor, 1);
            SetChildAlongAxisWithScale(content, 0, 0, rectTransform.rect.width / Factor, Factor);
        }

        public override void SetLayoutVertical()
        {
            if (content != null) SetChildAlongAxisWithScale(content, 1, 0, rectTransform.rect.height / Factor, Factor);
        }
    }
}
