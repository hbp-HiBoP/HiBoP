using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Experience.Protocol;

namespace HBP.UI.Experience.Protocol
{
    /// <summary>
    /// The script which manage the icon panel.
    /// </summary>
    public class IconItem : Tools.Unity.Lists.ActionnableItem<Icon>
    {
        #region Properties
        [SerializeField] Text m_NameText;
        [SerializeField] Text m_WindowText;

        [SerializeField] Image m_ImageIcon;
        [SerializeField] Image m_IllustrationImage;

        public override Icon Object
        {
            get
            {
                return base.Object;
            }

            set
            {
                base.Object = value;
                m_NameText.text = value.Name;
                m_ImageIcon.sprite = value.Image;
                m_IllustrationImage.sprite = value.Image;
                m_WindowText.text = value.Window.Start.ToString() + "ms to " + value.Window.End.ToString() + "ms";
            }
        }
        #endregion
    }
}