using System.Collections;
using System.Collections.Generic;
using Tools.Unity.Components;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI.Extensions.ColorPicker;

namespace Tools.Unity
{
    public class ColorPicker : MonoBehaviour
    {
        #region Properties
        [SerializeField] private ColorPickerControl m_ColorPickerControl;
        [SerializeField] private UnityEngine.UI.Button m_Blocker;
        private ColorEvent m_OnColorPicked = new ColorEvent();
        #endregion

        #region Public Methods
        public void Open(Color color, UnityAction<Color> action)
        {
            m_OnColorPicked.RemoveAllListeners();
            m_OnColorPicked.AddListener(action);
            
            GetComponent<MousePositionAndClamp>().Clamp();
            SetBlockerPosition();
            gameObject.SetActive(true);

            m_ColorPickerControl.CurrentColor = color;
        }
        public void Close()
        {
            gameObject.SetActive(false);
            m_OnColorPicked.Invoke(m_ColorPickerControl.CurrentColor);
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
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