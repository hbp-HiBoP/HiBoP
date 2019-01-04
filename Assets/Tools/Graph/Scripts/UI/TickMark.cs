using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Graph
{
    public class TickMark : MonoBehaviour
    {
        #region Parameters
        [SerializeField]
        protected RectTransform imageRectTransform;
        [SerializeField]
        protected RectTransform rectTransform;

        protected const float TICK_MARK_LENGHT = 12.0f;
        protected const float TICK_MARK_THICKNESS = 2.0f;
        #endregion

        #region Public Methods
        public virtual void Set(float position, Axe.SideEnum side, Color color)
        {
            gameObject.SetActive(true);
            SetPosition(position, side);
            SetImage(side);
            SetColor(color);
        }
        public virtual void SetColor(Color color)
        {
            imageRectTransform.GetComponent<Image>().color = color;
        }
        #endregion

        #region Private Methods
        protected virtual void SetPosition(float position, Axe.SideEnum side)
        {
            switch (side)
            {
                case Axe.SideEnum.abscissa:
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(0, 1);
                    rectTransform.pivot = new Vector2(0.5f, 1f);
                    rectTransform.sizeDelta = new Vector2(rectTransform.parent.GetComponent<RectTransform>().rect.width / 11.0f, 0);
                    rectTransform.localPosition = position * Vector3.right;
                    break;

                case Axe.SideEnum.ordinate:
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(1, 0);
                    rectTransform.pivot = new Vector2(1f, 0.5f);
                    rectTransform.sizeDelta = new Vector2(0, rectTransform.parent.GetComponent<RectTransform>().rect.height / 11.0f);
                    rectTransform.localPosition = position * Vector3.up;
                    break;
            }
        }
        protected virtual void SetImage(Axe.SideEnum side)
        {
            switch (side)
            {
                case Axe.SideEnum.abscissa:
                    imageRectTransform.anchorMin = new Vector2(0.5f, 1f);
                    imageRectTransform.anchorMax = new Vector2(0.5f, 1f);
                    imageRectTransform.pivot = new Vector2(0.5f, 0.5f);
                    imageRectTransform.sizeDelta = new Vector2(TICK_MARK_THICKNESS, TICK_MARK_LENGHT);
                    imageRectTransform.localPosition = Vector3.zero;
                    break;

                case Axe.SideEnum.ordinate:
                    imageRectTransform.anchorMin = new Vector2(1f, 0.5f);
                    imageRectTransform.anchorMax = new Vector2(1f, 0.5f);
                    imageRectTransform.pivot = new Vector2(0.5f, 0.5f);
                    imageRectTransform.sizeDelta = new Vector2(TICK_MARK_LENGHT, TICK_MARK_THICKNESS);
                    imageRectTransform.localPosition = Vector3.zero;
                    break;
            }
        }
        #endregion
    }
}