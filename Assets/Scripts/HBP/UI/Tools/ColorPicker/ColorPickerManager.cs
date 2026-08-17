using Cysharp.Threading.Tasks;
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

        public static async UniTask<Color> OpenColorPickerAsync(Color color)
        {
            return await m_Instance.m_ColorPicker.OpenAsync(color);
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
