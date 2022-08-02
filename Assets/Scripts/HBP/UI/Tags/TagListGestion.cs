using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace HBP.UI
{
    public class TagListGestion : ListGestion<Core.Data.BaseTag>
    {
        #region Properties
        [SerializeField] TagList m_List;
        public override Tools.Unity.Lists.ActionableList<Core.Data.BaseTag> List => m_List;

        [SerializeField] TagCreator m_ObjectCreator;
        public override ObjectCreator<Core.Data.BaseTag> ObjectCreator => m_ObjectCreator;

        [SerializeField] List<Core.Data.BaseTag> m_ModifiedTags = new List<Core.Data.BaseTag>();
        public ReadOnlyCollection<Core.Data.BaseTag> ModifiedTags
        {
            get
            {
                return new ReadOnlyCollection<Core.Data.BaseTag>(m_ModifiedTags);
            }
        }
        #endregion

        #region Protected Methods
        protected override void OnSaveModifier(Core.Data.BaseTag obj)
        {
            m_ModifiedTags.Add(obj);
            base.OnSaveModifier(obj);
        }
        protected override void OnObjectCreated(Core.Data.BaseTag obj)
        {
            m_ModifiedTags.Add(obj);
            base.OnObjectCreated(obj);
        }
        #endregion
    }
}