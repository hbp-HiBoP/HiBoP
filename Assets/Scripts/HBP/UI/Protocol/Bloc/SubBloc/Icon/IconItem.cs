using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Experience.Protocol;
using Tools.Unity;

namespace HBP.UI.Experience.Protocol
{
    /// <summary>
    /// Component to display Icon in list.
    /// </summary>
    public class IconItem : Tools.Unity.Lists.ActionnableItem<Icon>
    {
        #region Properties
        [SerializeField] Text m_NameText;

        [SerializeField] Text m_StartText;
        [SerializeField] Text m_EndText;

        [SerializeField] Image m_ImageIcon;
        [SerializeField] Tooltip m_ImageTooltip;

        /// <summary>
        /// Object to display.
        /// </summary>
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

                m_StartText.text = value.Window.Start.ToString() + "ms";
                m_EndText.text = value.Window.End.ToString() + "ms";

                m_ImageIcon.sprite = value.Image;
                m_ImageTooltip.Image = value.Image;
            }
        }
        #endregion
    }
}