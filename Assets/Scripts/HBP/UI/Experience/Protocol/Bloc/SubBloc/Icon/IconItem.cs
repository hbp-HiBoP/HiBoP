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

        [SerializeField] Text m_StartText;
        [SerializeField] Text m_EndText;

        [SerializeField] Image m_ImageIcon;

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

                m_StartText.text = value.Window.Start.ToString();
                m_EndText.text = value.Window.End.ToString();

                m_ImageIcon.sprite = value.Image;
            }
        }
        #endregion
    }
}