using HBP.Data.Tags;
using Tools.Unity.Lists;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Tags
{
    public class TagItem : ActionnableItem<Tag>
    {
        #region Properties
        [SerializeField] Text m_NameText;
        [SerializeField] Text m_TypeText;

        public override Tag Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_NameText.text = value.Name;
                if (value is EmptyTag) m_TypeText.text = "Empty";
                else if (value is BoolTag) m_TypeText.text = "Boolean";
                else if (value is EnumTag) m_TypeText.text = "Enumeration";
                else if (value is IntTag) m_TypeText.text = "Integer";
                else if (value is FloatTag) m_TypeText.text = "Decimal";
                else if (value is StringTag) m_TypeText.text = "Text";
                else m_TypeText.text = "Unknown";
            }
        }
        #endregion
    }
}