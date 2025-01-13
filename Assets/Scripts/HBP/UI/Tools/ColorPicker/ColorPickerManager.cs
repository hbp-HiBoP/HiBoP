using HBP.Core.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI.Tools
{
    public class ColorPickerManager : Manager<ColorPickerManager>
    {
        #region Properties
        [SerializeField] private ColorPicker m_ColorPicker;
        #endregion

        #region Public Methods
        public static void OpenColorPicker(Color color, UnityAction<Color> action)
        {
            m_Instance.m_ColorPicker.Open(color, action);
        }
        public static Color GetDefaultColor(int index)
        {
            return m_Instance.m_ColorPicker.GetDefaultColor(index);
        }
        #endregion

        #region Private Methods
        protected override void Initialization()
        {
            base.Initialization();
            m_ColorPicker.gameObject.SetActive(false);
        }
        #endregion
    }
}