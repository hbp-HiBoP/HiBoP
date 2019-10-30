using Tools.Unity.Components;
using UnityEngine;
using HBP.Data.Tags;
using System.Linq;

namespace HBP.UI.Tags
{
    public class TagValueListGestion : ListGestion<BaseTagValue>
    {
        #region Properties
        [SerializeField] protected TagValueList m_List;
        public override Tools.Unity.Lists.SelectableListWithItemAction<BaseTagValue> List => m_List;

        [SerializeField] protected TagValueCreator m_ObjectCreator;
        public override ObjectCreator<BaseTagValue> ObjectCreator => m_ObjectCreator;

        [SerializeField] Tag[] m_Tags;
        public Tag[] Tags
        {
            get
            {
                return m_Tags;
            }
            set
            {
                m_Tags = value;
            }
        }
        #endregion

        #region Protected Methods
        protected override ObjectModifier<BaseTagValue> OpenModifier(BaseTagValue item, bool interactable)
        {
            TagValueModifier modifier = (TagValueModifier) base.OpenModifier(item, interactable);
            modifier.Tags = Tags.Where(t => !List.Objects.Any(o => o.Tag == t) || t == item.Tag).ToArray();
            return modifier;
        }
        #endregion
    }
}
