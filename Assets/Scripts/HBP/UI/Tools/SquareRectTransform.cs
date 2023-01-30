using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Tools
{
    public class SquareRectTransform : LayoutElement
    {
        #region Properties
        public enum ControlType { HeightControlsWidth, WidthControlsHeight }
        public ControlType Type;
        RectTransform m_RectTransform;
        #endregion

        #region Public Methods
        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            CalculateLayoutParameters();
        }
        #endregion

        #region Private Methods
        protected override void OnEnable()
        {
            m_RectTransform = GetComponent<RectTransform>();
        }
        void CalculateLayoutParameters()
        {
            switch (Type)
            {
                case ControlType.HeightControlsWidth:
                    float width = m_RectTransform.rect.height;
                    minWidth = width;
                    preferredWidth = width;
                    flexibleWidth = 0;
                    break;
                case ControlType.WidthControlsHeight:
                    float height = m_RectTransform.rect.width;
                    minHeight = height;
                    preferredHeight = height;
                    flexibleHeight = 0;
                    break;
                default:
                    break;
            }
        }
        #endregion
    }
}