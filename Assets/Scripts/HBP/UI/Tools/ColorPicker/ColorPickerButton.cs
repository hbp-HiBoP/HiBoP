using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using HBP.Core.Tools;

namespace HBP.UI.Tools
{
    public class ColorPickerButton : MonoBehaviour
    {
        #region Properties

        [SerializeField] Button m_Button;
        [SerializeField] Image m_Image;

        #endregion

        #region Events

        public GenericEvent<Color> OnColorPicked = new();

        #endregion

        #region Public Methods

        public void Initialize(Color initialColor)
        {
            m_Image.color = initialColor;
            m_Button.onClick.AddListener(async () =>
            {
                Color color = await ColorPickerManager.OpenColorPickerAsync(m_Image.color);
                m_Image.color = color;
                OnColorPicked.Invoke(color);
            });
        }

        #endregion
    }
}
