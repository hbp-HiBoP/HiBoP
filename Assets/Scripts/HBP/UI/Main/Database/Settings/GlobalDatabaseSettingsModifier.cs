using HBP.Core.Data;
using HBP.Core.Database;
using HBP.UI.Main;
using HBP.UI.Tools;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Database
{
    public class GlobalDatabaseSettingsModifier : ObjectModifier<GlobalDatabaseSettings>
    {
        #region Properties
        [SerializeField] Button m_SwitchWorkspaceButton;
        [SerializeField] WorkspaceListGestion m_WorkspaceListGestion;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;

                m_WorkspaceListGestion.Interactable = value;
                m_WorkspaceListGestion.Modifiable = value;

                SetSwitchWorkspaceButtonInteractableState();
            }
        }
        #endregion

        #region Public Methods
        public override async void OK()
        {
            bool switchedWorkspace = ObjectTemp.SelectedWorkspace != Object.SelectedWorkspace;
            base.OK();
            DatabaseManager.Database.SaveSettings();

            if (switchedWorkspace)
            {
                try
                {
                    await DatabaseWorkflow.LoadDatabaseAsync();
                }
                catch (System.Exception)
                {
                }
            }
        }
        public void SwitchWorkspace()
        {
            ObjectTemp.SelectedWorkspace = m_WorkspaceListGestion.List.ObjectsSelected[0];
            m_WorkspaceListGestion.List.Refresh();
        }
        #endregion

        #region Protected Methods
        private void Update()
        {
            if (ObjectTemp != null)
                m_WorkspaceListGestion.UpdateSelectedWorkspace(ObjectTemp.SelectedWorkspace);
        }
        protected override void Initialize()
        {
            base.Initialize();

            m_WorkspaceListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_WorkspaceListGestion.List.OnAddObject.AddListener(AddWorkspace);
            m_WorkspaceListGestion.List.OnRemoveObject.AddListener(RemoveWorkspace);
            m_WorkspaceListGestion.List.OnUpdateObject.AddListener(UpdateWorkspace);
            m_WorkspaceListGestion.List.OnSelect.AddListener((workspace) => SetSwitchWorkspaceButtonInteractableState());
            m_WorkspaceListGestion.List.OnDeselect.AddListener((workspace) => SetSwitchWorkspaceButtonInteractableState());
            m_WorkspaceListGestion.List.OnRemoveObject.AddListener((workspace) => SetSwitchWorkspaceButtonInteractableState());
            m_WorkspaceListGestion.List.OnAddObject.AddListener((workspace) => SetSwitchWorkspaceButtonInteractableState());
        }

        protected void AddWorkspace(Workspace workspace)
        {
            if (!ObjectTemp.Workspaces.Contains(workspace))
            {
                ObjectTemp.Workspaces.Add(workspace);
            }
            SetSwitchWorkspaceButtonInteractableState();
        }
        protected void RemoveWorkspace(Workspace workspace)
        {
            if (ObjectTemp.Workspaces.Contains(workspace))
            {
                ObjectTemp.Workspaces.Remove(workspace);
            }
            SetSwitchWorkspaceButtonInteractableState();
        }
        protected void UpdateWorkspace(Workspace workspace)
        {
            int index = ObjectTemp.Workspaces.FindIndex(m => m.Equals(workspace));
            if (index != -1)
            {
                ObjectTemp.Workspaces[index] = workspace;
            }
            SetSwitchWorkspaceButtonInteractableState();
        }

        private void SetSwitchWorkspaceButtonInteractableState()
        {
            m_SwitchWorkspaceButton.interactable = m_WorkspaceListGestion.List.ObjectsSelected.Length == 1 && Interactable;
        }
        protected override void SetFields(GlobalDatabaseSettings objectToModify)
        {
            m_WorkspaceListGestion.Settings = objectToModify;
            m_WorkspaceListGestion.List.Set(objectToModify.Workspaces);
        }
        #endregion
    }
}
