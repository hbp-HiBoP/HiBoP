using System.Linq;
using HBP.Data;
using UnityEngine;

namespace HBP.UI.Anatomy
{
    public class GroupSelection : SavableWindow
    {
        #region Properties
        public Group[] SelectedGroups
        {
            get { return m_GroupListGestion.List.ObjectsSelected; }
        }
        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;
                m_GroupListGestion.Interactable = false;
            }
        }
        [SerializeField] GroupListGestion m_GroupListGestion;
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            m_GroupListGestion.Initialize(m_SubWindows);
            m_GroupListGestion.Objects = ApplicationState.ProjectLoaded.Groups.ToList();
            base.Initialize();
        }
        #endregion
    }
}
