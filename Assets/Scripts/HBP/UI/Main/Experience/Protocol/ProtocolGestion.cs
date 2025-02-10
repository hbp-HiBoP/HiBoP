using UnityEngine;
using HBP.Core.Tools;
using HBP.Core.Data;
using HBP.Data.Module3D;
using HBP.UI.Tools;
using HBP.Data.Database;

namespace HBP.UI.Main
{
    public class ProtocolGestion : GestionWindow<Protocol>
    {
        #region Properties
        [SerializeField] ProtocolListGestion m_ListGestion;
        public override ListGestion<Protocol> ListGestion => m_ListGestion;
        #endregion

        #region Public Methods
        public override async void OK()
        {
            if (DataManager.HasData)
            {
                int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Reload required", "Some data have already been loaded. Your changes will not be applied unless you reload.\n\nWould you like to reload ?", "Save & Reload", "Cancel");
                if (result == 0)
                {
                    base.OK();
                    DatabaseManager.Database.SetProtocols(m_ListGestion.List.Objects);
                    DatabaseManager.Database.SaveProtocols().Forget();
                    InteractableStateManager.SetInteractables();
                    LoadingManager.Load(update => Dataset.CheckDatasetsAsync(m_ListGestion.ModifiedProtocols, update));
                    DataManager.Clear();
                    Module3DMain.ReloadScenes();
                    UITools.CheckProjectIDAndAskForRegeneration().Forget();
                }
            }
            else
            {
                base.OK();
                DatabaseManager.Database.SetProtocols(m_ListGestion.List.Objects);
                DatabaseManager.Database.SaveProtocols().Forget();
                InteractableStateManager.SetInteractables();
                if (ApplicationState.LoadedProject != null)
                {
                    LoadingManager.Load(update => Dataset.CheckDatasetsAsync(m_ListGestion.ModifiedProtocols, update));
                    UITools.CheckProjectIDAndAskForRegeneration().Forget();
                }
            }
        }
        public override void Close()
        {
            LoadingManager.Load(update => RestoreOldValuesAsync(DatabaseManager.Database.Protocols, update), false);
            base.Close();
        }
        #endregion

        #region Private Methods
        protected override void SetFields()
        {
            base.SetFields();
            SetList(DatabaseManager.Database.Protocols);
        }
        #endregion
    }
}