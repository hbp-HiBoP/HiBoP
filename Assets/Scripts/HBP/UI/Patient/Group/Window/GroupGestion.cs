using System.Linq;
using HBP.Data;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Anatomy
{
    public class GroupGestion : ItemGestion<Group>
    {
        #region Properties
        [SerializeField] Text m_GroupCounter;
        [SerializeField] GroupList m_GroupList;
        #endregion

        #region Public Methods
        public override void Save()
        {
            ApplicationState.ProjectLoaded.SetGroups(Items.ToArray());
            base.Save();
        }
        public override void Remove()
        {
            base.Remove();
            m_GroupCounter.text = m_GroupList.ObjectsSelected.Count().ToString();
        }
        public override void Open()
        {
            base.Open();
            m_GroupList.SortByName(GroupList.Sorting.Descending);
        }
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            m_List = m_GroupList;
            m_GroupList.OnAction.AddListener((item,i) => OpenModifier(item,true));
            AddItem(ApplicationState.ProjectLoaded.Groups.ToArray());
            m_List.OnSelectionChanged.AddListener((g,b) => m_GroupCounter.text = m_List.ObjectsSelected.Count().ToString());
        }
        #endregion
    }
}