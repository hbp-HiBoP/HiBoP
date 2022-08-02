using UnityEngine;
using System.Linq;

namespace HBP.UI
{
    public class TagValueListGestion : ListGestion<Core.Data.BaseTagValue>
    {
        #region Properties
        [SerializeField] protected TagValueList m_List;
        public override Tools.Unity.Lists.ActionableList<Core.Data.BaseTagValue> List => m_List;

        [SerializeField] protected TagValueCreator m_ObjectCreator;
        public override ObjectCreator<Core.Data.BaseTagValue> ObjectCreator => m_ObjectCreator;

        [SerializeField] Core.Data.BaseTag[] m_Tags;
        public Core.Data.BaseTag[] Tags
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

        #region Public Methods
        public override void Create()
        {
            m_ObjectCreator.Tags = Tags.Where(t => !List.Objects.Any(o => o.Tag == t)).ToArray();
            base.Create();
        }
        #endregion

        #region Protected Methods
        protected override ObjectModifier<Core.Data.BaseTagValue> OpenModifier(Core.Data.BaseTagValue item)
        {
            TagValueModifier modifier = (TagValueModifier) base.OpenModifier(item);
            modifier.Tags = Tags.Where(t => !List.Objects.Any(o => o.Tag == t) || t == item.Tag).ToArray();
            return modifier;
        }
        #endregion
    }
}
