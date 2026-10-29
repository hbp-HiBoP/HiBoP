using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>Zooms the body inside a fixed viewport, preserving the window's unzoomed layout constraints.</summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(300)]
    public sealed class QuestWindowContentZoom : LayoutGroup
    {
        public const int MinimumPercent = 50, MaximumPercent = 150, StepPercent = 10;
        [SerializeField] private QuestWindow window;
        [SerializeField] private RectTransform content;
        [SerializeField] private Button decrease, increase;
        [SerializeField] private Text percentage;
        private int zoomPercent = 100;
        private Vector2 baselineMinimum, baselinePreferred, baselineFlexible;
        private bool hasBaseline;
        private int settlingFrames;
        public int ZoomPercent => zoomPercent;
        public float Factor => zoomPercent / 100f;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (decrease != null) decrease.onClick.AddListener(ZoomOut);
            if (increase != null) increase.onClick.AddListener(ZoomIn);
            RefreshControls();
        }

        protected override void OnDisable()
        {
            if (decrease != null) decrease.onClick.RemoveListener(ZoomOut);
            if (increase != null) increase.onClick.RemoveListener(ZoomIn);
            base.OnDisable();
        }

        public void ZoomOut() => SetZoomPercent(zoomPercent - StepPercent);
        public void ZoomIn() => SetZoomPercent(zoomPercent + StepPercent);

        public void SetZoomPercent(int value)
        {
            int next = Mathf.Clamp(Mathf.RoundToInt(value / (float)StepPercent) * StepPercent, MinimumPercent, MaximumPercent);
            if (next == zoomPercent) return;
            var root = window != null ? (RectTransform)window.transform : rectTransform;
            if (!hasBaseline) LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            zoomPercent = next;
            // Width-dependent children, including the Signals splitter, need to settle before measuring at 100% again.
            settlingFrames = next == 100 ? 2 : 0;
            SetLayoutHorizontal();
            SetLayoutVertical();
            RefreshControls();
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        private void LateUpdate()
        {
            if (settlingFrames > 0 && --settlingFrames == 0) SetDirty();
        }

        private void RefreshControls()
        {
            if (decrease != null) decrease.interactable = zoomPercent > MinimumPercent;
            if (increase != null) increase.interactable = zoomPercent < MaximumPercent;
            if (percentage != null) percentage.text = $"{zoomPercent} %";
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            MeasureBaseline(0);
            SetLayoutInputForAxis(baselineMinimum.x, baselinePreferred.x, baselineFlexible.x, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            MeasureBaseline(1);
            hasBaseline = content != null;
            SetLayoutInputForAxis(baselineMinimum.y, baselinePreferred.y, baselineFlexible.y, 1);
        }

        private void MeasureBaseline(int axis)
        {
            if (content == null || zoomPercent != 100 || settlingFrames != 0) return;
            baselineMinimum[axis] = LayoutUtility.GetMinSize(content, axis);
            baselinePreferred[axis] = LayoutUtility.GetPreferredSize(content, axis);
            baselineFlexible[axis] = LayoutUtility.GetFlexibleSize(content, axis);
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
