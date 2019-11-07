using Tools.Unity.Components;
using UnityEngine;

namespace HBP.UI
{
    public class TagValueCreator : ObjectCreator<Data.BaseTagValue>
    {
        [SerializeField] Data.BaseTag[] m_Tags;
        public Data.BaseTag[] Tags
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

        public override void CreateFromScratch()
        {
            OpenModifier(new Data.IntTagValue());
        }
        protected override ObjectModifier<Data.BaseTagValue> OpenModifier(Data.BaseTagValue item)
        {
            TagValueModifier modifier = (TagValueModifier) base.OpenModifier(item);
            modifier.Tags = Tags;
            return modifier;
        }
    }
}