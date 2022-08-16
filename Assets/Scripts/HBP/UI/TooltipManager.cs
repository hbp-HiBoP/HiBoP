using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI
{
    public class TooltipManager : MonoBehaviour
    {
        #region Properties
        private static TooltipManager m_Instance;

        private static Vector3 m_Offset = new Vector3(0, -20, 0);
        public const float TIME_TO_DISPLAY = 0.7f;

        public static bool IsTooltipDisplayed { get; private set; } = false;

        private float m_TimeBeforeHiding = TIME_TO_DISPLAY;
        public static bool TooltipHasBeenDisplayedRecently { get; private set; } = false;

        private bool m_FollowMouse = false;

        /// <summary>
        /// Canvas on which the tooltip is displayed
        /// </summary>
        [SerializeField] RectTransform m_Canvas;
        /// <summary>
        /// Tooltip's RectTransform
        /// </summary>
        [SerializeField] RectTransform m_Tooltip;
        /// <summary>
        /// Tooltip's Textfield
        /// </summary>
        [SerializeField] Text m_TextField;
        /// <summary>
        /// Tooltip's Icon
        /// </summary>
        [SerializeField] Image m_ImageField;
        #endregion

        #region Private Methods
        private void Awake()
        {
            if (m_Instance == null)
            {
                m_Instance = this;
            }
            else
            {
                Destroy(this);
            }
        }
        private void Update()
        {
            if (!IsTooltipDisplayed)
            {
                m_TimeBeforeHiding -= Time.deltaTime;
            }
            if (m_TimeBeforeHiding < 0)
            {
                TooltipHasBeenDisplayedRecently = false;
            }
            if(m_FollowMouse)
            {
                MoveAtMousePosition();
            }
        }
        private void ClampToCanvas()
        {
            Vector3 l_pos = m_Tooltip.localPosition;
            Vector3 l_minPosition = m_Canvas.rect.min - m_Tooltip.rect.min;
            Vector3 l_maxPosition = m_Canvas.rect.max - m_Tooltip.rect.max;

            l_minPosition = new Vector3(l_minPosition.x, l_minPosition.y, l_minPosition.z);
            l_maxPosition = new Vector3(l_maxPosition.x, l_maxPosition.y, l_maxPosition.z);

            l_pos.x = Mathf.Clamp(m_Tooltip.localPosition.x, l_minPosition.x, l_maxPosition.x);
            l_pos.y = Mathf.Clamp(m_Tooltip.localPosition.y, l_minPosition.y, l_maxPosition.y);

            m_Tooltip.localPosition = l_pos;
        }
        private void MoveAtMousePosition()
        {
            m_Tooltip.position = Input.mousePosition + m_Offset;
            ClampToCanvas();
        }
        #endregion

        #region Public Methods
        public static void ShowTooltip(string text, Sprite icon, bool followMouse = false)
        {
            m_Instance.m_FollowMouse = followMouse;
            m_Instance.m_Tooltip.gameObject.SetActive(true);
            m_Instance.m_TextField.text = text;
            if (icon != null)
            {
                m_Instance.m_ImageField.sprite = icon;
                m_Instance.m_ImageField.gameObject.SetActive(true);
            }
            else m_Instance.m_ImageField.gameObject.SetActive(false);
            m_Instance.MoveAtMousePosition();

            IsTooltipDisplayed = true;
            TooltipHasBeenDisplayedRecently = true;
            m_Instance.m_TimeBeforeHiding = TIME_TO_DISPLAY;
        }
        public static void HideTooltip()
        {
            m_Instance.m_Tooltip.gameObject.SetActive(false);
            IsTooltipDisplayed = false;
        }
        #endregion
    }
}