using HBP.Core.Data;
using UnityEngine;
using HBP.UI.Tools;
using HBP.Core.Tools;

namespace HBP.UI.Main
{
    public class GroupGestion : GestionWindow<Group>
    {
        #region Properties
        [SerializeField] GroupListGestion m_ListGestion;
        public override ListGestion<Group> ListGestion => m_ListGestion;
        #endregion

        #region Public Methods
        public override void OK()
        {
            base.OK();
            ApplicationState.ProjectLoaded.SetGroups(ListGestion.List.Objects);
            FindObjectOfType<MenuButtonState>().SetInteractables();
            UITools.CheckProjectIDAndAskForRegeneration();
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