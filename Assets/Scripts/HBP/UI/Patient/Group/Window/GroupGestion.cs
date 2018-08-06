using UnityEngine;
using System.Linq;
using HBP.Data;

namespace HBP.UI.Anatomy
{
    public class GroupGestion : ItemGestion<Group>
    {
        #region Properties
        [SerializeField] GroupList m_GroupList;
        #endregion

        #region Public Methods
        public override void Save()
        {
            ApplicationState.ProjectLoaded.SetGroups(Items.ToArray());
            base.Save();
        }
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            m_List = m_GroupList;
            base.Initialize();
            AddItem(ApplicationState.ProjectLoaded.Groups.ToArray());
            m_GroupList.SortByName(GroupList.Sorting.Descending);
        }
        protected override void SetInteractable(bool interactable)
        {
            base.SetInteractable(interactable);
            m_GroupList.Interactable = interactable;
        }
        #endregion
    }
}