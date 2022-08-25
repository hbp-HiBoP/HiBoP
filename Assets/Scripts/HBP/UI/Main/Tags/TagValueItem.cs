using UnityEngine;
using UnityEngine.UI;
using HBP.UI.Tools.Lists;

namespace HBP.UI.Main
{
    /// <summary>
    /// Component to display TagValue in list.
    /// </summary>
    public class TagValueItem : ActionnableItem<Core.Data.BaseTagValue>
    {
        #region Properties
        [SerializeField] Text m_NameText;
        [SerializeField] Text m_ValueText;

        /// <summary>
        /// Object to display.
        /// </summary>
        public override Core.Data.BaseTagValue Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_NameText.text = value.Tag.Name;
                m_ValueText.text = value.DisplayableValue;
            }
        }
        #endregion
    }
}