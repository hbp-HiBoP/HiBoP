using System.Collections.Generic;
using System.Collections.ObjectModel;
using HBP.Data.Tags;
using Tools.Unity.Components;
using UnityEngine;

namespace HBP.UI.Tags
{
    public class TagListGestion : ListGestion<Tag>
    {
        #region Properties
        [SerializeField] TagList m_List;
        public override Tools.Unity.Lists.SelectableListWithItemAction<Tag> List => m_List;

        [SerializeField] TagCreator m_ObjectCreator;
        public override ObjectCreator<Tag> ObjectCreator => m_ObjectCreator;

        [SerializeField] List<Tag> m_ModifiedTags = new List<Tag>();
        public ReadOnlyCollection<Tag> ModifiedTags
        {
            get
            {
                return new ReadOnlyCollection<Tag>(m_ModifiedTags);
            }
        }
        #endregion

        #region Protected Methods
        protected override void Add(Tag obj)
        {
            m_ModifiedTags.Add(obj);
            base.Add(obj);
        }
        #endregion
    }
}