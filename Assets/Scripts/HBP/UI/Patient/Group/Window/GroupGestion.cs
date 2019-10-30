using HBP.Data;
using Tools.Unity.Components;
using UnityEngine;

namespace HBP.UI.Anatomy
{
    public class GroupGestion : GestionWindow<Group>
    {
        #region Properties
        [SerializeField] GroupListGestion m_ListGestion;
        public override ListGestion<Group> ListGestion => m_ListGestion;
        #endregion

        #region Public Methods
        public override void Save()
        {
            base.Save();
            ApplicationState.ProjectLoaded.SetGroups(ListGestion.List.Objects);
            FindObjectOfType<MenuButtonState>().SetInteractables();
        }
        #endregion

        #region Private Methods
        protected override void SetFields()
        {
            base.SetFields();
            m_ListGestion.List.Set(ApplicationState.ProjectLoaded.Groups);
        }
        #endregion
    }
}