using UnityEngine;

namespace HBP.UI
{
    public class GroupGestion : GestionWindow<Core.Data.Group>
    {
        #region Properties
        [SerializeField] GroupListGestion m_ListGestion;
        public override ListGestion<Core.Data.Group> ListGestion => m_ListGestion;
        #endregion

        #region Public Methods
        public override void OK()
        {
            base.OK();
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