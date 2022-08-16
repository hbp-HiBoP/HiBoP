using HBP.UI.Components;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI.Extensions.ColorPicker;

namespace HBP.UI
{
    public class ColorPicker : MonoBehaviour
    {
        #region Properties
        private static ColorPicker m_Instance;

        [SerializeField] private ColorPickerControl m_ColorPickerControl;
        [SerializeField] private UnityEngine.UI.Button m_Blocker;
        private ColorEvent m_OnColorPicked = new ColorEvent();
        #endregion

        #region Public Methods
        public static void Open(Color color, UnityAction<Color> action)
        {
            m_Instance.m_OnColorPicked.RemoveAllListeners();
            m_Instance.m_OnColorPicked.AddListener(action);

            m_Instance.GetComponent<MousePositionAndClamp>().Clamp();
            m_Instance.SetBlockerPosition();
            m_Instance.gameObject.SetActive(true);

            m_Instance.m_ColorPickerControl.CurrentColor = color;
        }
        public static void Close()
        {
            m_Instance.gameObject.SetActive(false);
            m_Instance.m_OnColorPicked.Invoke(m_Instance.m_ColorPickerControl.CurrentColor);
        }
        public static Color GetDefaultColor(int index)
        {
            Color[] defaultColors = m_Instance.GetComponentsInChildren<DefaultColor>().Select(dc => dc.GetComponent<UnityEngine.UI.Image>().color).ToArray();
            if (index > defaultColors.Length) index = defaultColors.Length - 1;
            return defaultColors[index];
        }
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

            m_Blocker.onClick.AddListener(Close);
        }
        private void SetBlockerPosition()
        {
            RectTransform rectTransform = m_Blocker.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(Screen.width * 2, Screen.height * 2);
        }
        #endregion
    }
}