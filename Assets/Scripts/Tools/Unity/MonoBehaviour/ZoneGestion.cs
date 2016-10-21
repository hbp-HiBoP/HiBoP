using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity
{
    [RequireComponent(typeof(Slider))]
    public class ZoneGestion : MonoBehaviour
    {
        #region Properties
        Slider m_slider;
        public Vector2 Limits;
        public RectTransform LeftBot;
        public RectTransform RightTop;
        public enum SideEnum { Vertical, Horizontal }
        public SideEnum Side;
        #endregion

        #region Initialisation
        void Start()
        {
            m_slider = GetComponent<Slider>();
            m_slider.onValueChanged.AddListener(OnValueChanged);
            m_slider.minValue = 0;
            m_slider.maxValue = 1;
            if(Side == SideEnum.Horizontal)
            {
                m_slider.direction = Slider.Direction.LeftToRight;
                m_slider.normalizedValue = LeftBot.anchorMax.x;
                OnValueChanged(LeftBot.anchorMax.x);
            }
            else
            {
                m_slider.direction = Slider.Direction.BottomToTop;
                m_slider.normalizedValue = LeftBot.anchorMax.y;
                OnValueChanged(LeftBot.anchorMax.y);
            }
        }
        #endregion

        #region Private Methods
        void OnValueChanged(float value)
        {
            if (value < Limits.x)
            {
                if (LeftBot.gameObject.activeSelf)
                {
                    LeftBot.gameObject.SetActive(false);
                }
                if (Side == SideEnum.Horizontal)
                {
                    RightTop.anchorMin = new Vector2(0, RightTop.anchorMin.y);
                }
                else
                {
                    RightTop.anchorMin = new Vector2(RightTop.anchorMin.x, 0);
                }
            }
            else if (value > Limits.y)
            {
                if (RightTop.gameObject.activeSelf)
                {
                    RightTop.gameObject.SetActive(false);
                }
                if (Side == SideEnum.Horizontal)
                {
                    LeftBot.anchorMax = new Vector2(1, LeftBot.anchorMax.y);
                }
                else
                {
                    LeftBot.anchorMax = new Vector2(LeftBot.anchorMax.x, 1);
                }
            }
            else
            {
                if (!LeftBot.gameObject.activeSelf)
                {
                    LeftBot.gameObject.SetActive(true);
                }
                if (!RightTop.gameObject.activeSelf)
                {
                    RightTop.gameObject.SetActive(true);
                }
                if (Side == SideEnum.Horizontal)
                {
                    LeftBot.anchorMax = new Vector2(value, LeftBot.anchorMax.y);
                    RightTop.anchorMin = new Vector2(value, RightTop.anchorMin.y);
                }
                else
                {
                    LeftBot.anchorMax = new Vector2(LeftBot.anchorMax.x, value);
                    RightTop.anchorMin = new Vector2(RightTop.anchorMin.x, value);
                }
            }
            LeftBot.sizeDelta = new Vector2(0, 0);
            RightTop.sizeDelta = new Vector2(0, 0);
        }
        #endregion
    }
}