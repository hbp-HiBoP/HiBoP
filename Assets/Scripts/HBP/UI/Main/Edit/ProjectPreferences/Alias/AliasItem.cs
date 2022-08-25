using HBP.UI.Tools.Lists;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    /// <summary>
    /// Component to display alias in list.
    /// </summary>
    public class AliasItem : ActionnableItem<Core.Data.Alias>
    {
        #region Properties
        [SerializeField] Text m_KeyText;
        [SerializeField] Text m_ValueText;

        /// <summary>
        /// Object to display.
        /// </summary>
        public override Core.Data.Alias Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                string keyString = value.Key;
                if (value.Key.Length > 30) keyString = value.Key.Substring(0, 30) + "...";
                string valueString = value.Value;
                if (value.Value.Length > 30) valueString = value.Value.Substring(0, 30) + "...";
                m_KeyText.text = keyString;
                m_ValueText.text = valueString;
            }
        }
        #endregion
    }
}