using UnityEngine;
using System.Linq;
using HBP.Data;

namespace HBP.UI.Anatomy
{
    public class GroupGestion : ItemGestion<Group>
    {
        #region Properties
        [SerializeField] GroupList m_GroupList;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;

                m_GroupList.Interactable = value;
            }
        }
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
        #endregion
    }
}