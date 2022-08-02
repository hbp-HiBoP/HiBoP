using Tools.Unity.Components;
using UnityEngine;

namespace HBP.UI
{
    public class TagValueCreator : ObjectCreator<Core.Data.BaseTagValue>
    {
        #region Properties
        [SerializeField] Core.Data.BaseTag[] m_Tags;
        /// <summary>
        /// Possible Tags.
        /// </summary>
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
        /// <summary>
        /// Create a TagValue from scratch.
        /// </summary>
        public override void CreateFromScratch()
        {
            OpenModifier(new Core.Data.IntTagValue());
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Open a new TagValueModifier.
        /// </summary>
        /// <param name="item">TagValue to modify</param>
        /// <returns>TagValueModifier</returns>
        protected override ObjectModifier<Core.Data.BaseTagValue> OpenModifier(Core.Data.BaseTagValue item)
        {
            TagValueModifier modifier = (TagValueModifier) base.OpenModifier(item);
            modifier.Tags = Tags;
            return modifier;
        }
        #endregion
    }
}