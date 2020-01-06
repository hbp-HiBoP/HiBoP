using System.Collections.Generic;
using System.Collections.ObjectModel;
using HBP.Data;
using Tools.Unity.Components;
using UnityEngine;

namespace HBP.UI
{
    public class TagListGestion : ListGestion<Data.BaseTag>
    {
        #region Properties
        [SerializeField] TagList m_List;
        public override Tools.Unity.Lists.ActionableList<Data.BaseTag> List => m_List;

        [SerializeField] TagCreator m_ObjectCreator;
        public override ObjectCreator<Data.BaseTag> ObjectCreator => m_ObjectCreator;

        [SerializeField] List<Data.BaseTag> m_ModifiedTags = new List<Data.BaseTag>();
        public ReadOnlyCollection<Data.BaseTag> ModifiedTags
        {
            get
            {
                return new ReadOnlyCollection<Data.BaseTag>(m_ModifiedTags);
            }
        }
        #endregion

        #region Protected Methods
        protected override void OnSaveModifier(Data.BaseTag obj)
        {
            m_ModifiedTags.Add(obj);
            base.OnSaveModifier(obj);
        }
        protected override void OnObjectCreated(BaseTag obj)
        {
            m_ModifiedTags.Add(obj);
            base.OnObjectCreated(obj);
        }
        #endregion
    }
}