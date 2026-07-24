using UnityEngine;
using HBP.Core.Tools;
using HBP.Core.Data;
using HBP.Data.Module3D;
using HBP.UI.Tools;
using HBP.Core.Database;
using HBP.UI.Database;
using Cysharp.Threading.Tasks;
using System.Linq;

namespace HBP.UI.Main
{
    public class ProtocolGestion : GestionWindow<Protocol>
    {
        #region Properties
        [SerializeField] ProtocolListGestion m_ListGestion;
        public override ListGestion<Protocol> ListGestion => m_ListGestion;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;

                m_ListGestion.Interactable = value;
                m_ListGestion.Modifiable = value;
            }
        }
        #endregion

        #region Public Methods
        public override async void OK()
        {
            bool requiresReload = false;

            if (DataManager.HasData)
            {
                int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Reload required", "Some data is already loaded. Your recent changes won't be applied unless you reload the data.\n\nWould you like to save and reload now?", "Save&Reload", "Cancel");
                
                if (result == 0)
                    requiresReload = true;
                else
                    return;
            }

            Protocol[] modifiedProtocols = m_ListGestion.ModifiedProtocols.ToArray();
            base.OK();
            DatabaseManager.Database.SetProtocols(m_ListGestion.List.Objects);
            ApplicationState.LoadedProject?.InvalidateValidation();
            await DatabaseWorkflow.SaveProtocolsAsync();
            InteractableStateManager.SetInteractables();
            if (modifiedProtocols.Length > 0)
            {
                DataInfo[] dataInfos = DatabaseManager.Database.DataInfos
                    .Where(dataInfo => modifiedProtocols.Contains(dataInfo.Protocol))
                    .ToArray();
                await LoadingManager.LoadAsync(
                    update => Dataset.CheckDatasetsAsync(dataInfos, true, update));
            }
            await UniTask.SwitchToMainThread();
            if (requiresReload)
            {
                DataManager.ClearDerivedData();
            }
            if (ApplicationState.LoadedProject != null)
            {
                var visualizations = Module3DMain.PrepareReloadScenes();
                await LoadingManager.LoadAsync((update, token) => Module3DMain.LoadAsync(visualizations, update, token));
                UITools.CheckProjectIDAndAskForRegeneration().Forget();
            }
        }
        public override void Close()
        {
            if (m_ListGestion.HasBeenModified)
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
