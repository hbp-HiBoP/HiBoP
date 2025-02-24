using HBP.Data.Database;
using HBP.UI.Tools;
using HBP.UI.Tools.Lists;
using UnityEngine;

namespace HBP.UI.Database
{
    public class WorkspaceListGestion : ListGestion<Workspace>
    {
        #region Properties
        public GlobalDatabaseSettings Settings { get; set; }

        [SerializeField] protected WorkspaceList m_List;
        public override ActionableList<Workspace> List => m_List;

        [SerializeField] protected WorkspaceCreator m_ObjectCreator;
        public override ObjectCreator<Workspace> ObjectCreator => m_ObjectCreator;
        #endregion

        #region Public Methods
        public void UpdateSelectedWorkspace(Workspace workspace)
        {
            foreach (var item in m_List.Items)
                if (item is WorkspaceItem workspaceItem)
                    workspaceItem.UpdateSelectedWorkspace(workspace);
        }
        public override void RemoveSelected()
        {
            if (m_List.ObjectsSelected.Length > 0 && Settings.SelectedWorkspace == m_List.ObjectsSelected[0])
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Can't remove the selected workspace", "You can't remove the selected workspace.").Forget();
            }
            else
            {
                base.RemoveSelected();
            }
        }
        #endregion
    }
}