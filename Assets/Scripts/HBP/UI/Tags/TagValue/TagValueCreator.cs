using Tools.Unity.Components;
using UnityEngine;

namespace HBP.UI
{
    public class TagValueCreator : ObjectCreator<Data.BaseTagValue>
    {
        #region Properties
        [SerializeField] Data.BaseTag[] m_Tags;
        /// <summary>
        /// Possible Tags.
        /// </summary>
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
        #endregion

        #region Public Methods
        /// <summary>
        /// Create a TagValue from scratch.
        /// </summary>
        public override void CreateFromScratch()
        {
            OpenModifier(new Data.IntTagValue());
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Open a new TagValueModifier.
        /// </summary>
        /// <param name="item">TagValue to modify</param>
        /// <returns>TagValueModifier</returns>
        protected override ObjectModifier<Data.BaseTagValue> OpenModifier(Data.BaseTagValue item)
        {
            TagValueModifier modifier = (TagValueModifier) base.OpenModifier(item);
            modifier.Tags = Tags;
            return modifier;
        }
        #endregion
    }
}