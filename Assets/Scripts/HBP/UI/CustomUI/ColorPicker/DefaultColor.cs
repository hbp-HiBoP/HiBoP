using UnityEngine;
using UnityEngine.UI.Extensions.ColorPicker;
using UnityEngine.UI;

namespace HBP.UI.Tools
{
    public class DefaultColor : MonoBehaviour
    {
        #region Properties
        [SerializeField] ColorPickerControl m_ColorPicker;
        #endregion

        #region Private Methods
        private void Awake()
        {
            Color color = GetComponent<Image>().color;
            GetComponent<Button>().onClick.AddListener(() =>
            {
                m_ColorPicker.CurrentColor = color;
            });
        }
        #endregion
    }
}