using Tools.Unity.Lists;
using UnityEngine;
using HBP.Data.Tags;
using UnityEngine.UI;

namespace HBP.UI.Tags
{
    public class TagValueItem : ActionnableItem<BaseTagValue>
    {
        #region Properties
        [SerializeField] Text m_NameText;
        [SerializeField] Text m_ValueText;

        public override BaseTagValue Object
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