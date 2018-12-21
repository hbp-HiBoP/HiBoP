using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Components
{
    [RequireComponent(typeof(Image))]
    public class ImageRatio : LayoutElement
    {
        #region Properties
        public enum ControlType { HeightControlsWidth, WidthControlsHeight }
        public ControlType Type;
        Image m_Image;
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
            m_Image = GetComponent<Image>();
        }
        void CalculateLayoutParameters()
        {
            switch (Type)
            {
                case ControlType.HeightControlsWidth:
                    float width = m_Image.sprite == null ? m_RectTransform.rect.height : m_RectTransform.rect.height * m_Image.sprite.texture.width / m_Image.sprite.texture.height;
                    minWidth = width;
                    preferredWidth = width;
                    flexibleWidth = -1;
                    break;
                case ControlType.WidthControlsHeight:
                    float height = m_Image.sprite == null ? m_RectTransform.rect.width : m_RectTransform.rect.width * m_Image.sprite.texture.height / m_Image.sprite.texture.width;
                    minHeight = height;
                    preferredHeight = height;
                    flexibleHeight = -1;
                    break;
                default:
                    break;
            }
        }
        #endregion
    }
}