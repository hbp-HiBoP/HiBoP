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
        [SerializeField] Image m_ImageIcon;
        [SerializeField] Image m_IllustrationImage;
        [SerializeField] Text m_MinText;
        [SerializeField] Text m_MaxText;
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
                m_MinText.text = value.Window.Start.ToString();
                m_MaxText.text = value.Window.End.ToString();
            }
        }
        #endregion
    }
}