using Tools.Unity.Lists;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI
{
    public class AliasItem : ActionnableItem<Data.Alias>
    {
        #region Properties
        [SerializeField] Text m_KeyText;
        [SerializeField] Text m_ValueText;

        public override Data.Alias Object
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