using Cysharp.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI.Extensions.ColorPicker;

namespace HBP.UI.Tools
{
    public class ColorPicker : MonoBehaviour
    {
        #region Properties
        [SerializeField] private ColorPickerControl m_ColorPickerControl;
        [SerializeField] private UnityEngine.UI.Button m_Blocker;
        private bool m_ColorPicked;
        #endregion

        #region Public Methods
        public async UniTask<Color> OpenAsync(Color color)
        {
            m_ColorPicked = false;

            GetComponent<MousePositionAndClamp>().Clamp();
            SetBlockerPosition();
            gameObject.SetActive(true);

            m_ColorPickerControl.CurrentColor = color;

            await UniTask.WaitUntil(() => m_ColorPicked);

            return m_ColorPickerControl.CurrentColor;
        }
        public void Close()
        {
            gameObject.SetActive(false);
            m_ColorPicked = true;
        }
        public Color GetDefaultColor(int index)
        {
            Color[] defaultColors = GetComponentsInChildren<DefaultColor>().Select(dc => dc.GetComponent<UnityEngine.UI.Image>().color).ToArray();
            if (index > defaultColors.Length) index = defaultColors.Length - 1;
            return defaultColors[index];
        }
        #endregion

        #region Private Methods
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