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
            bool requiresCheck = false;

            if (DataManager.HasData)
            {
                int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Reload required", "Some data is already loaded. Your recent changes won't be applied unless you reload the data.\n\nWould you like to save and reload now?", "Save&Reload", "Cancel");
                
                if (result == 0)
                    requiresReload = true;
                else
                    return;
            }

            if (m_ListGestion.ModifiedProtocols.Count > 0)
            {
                int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Data check required", "Some protocols have been modified. A data integrity check is required to ensure there are no errors.\n\nWould you like to proceed with the check?", "Check", "Cancel");

                if (result == 0)
                    requiresCheck = true;
                else
                    return;
            }

            base.OK();
            DatabaseManager.Database.SetProtocols(m_ListGestion.List.Objects);
            DatabaseManager.Database.SaveProtocols().Forget();
            InteractableStateManager.SetInteractables();
            if (requiresCheck)
            {
                await LoadingManager.LoadAsync(update => Dataset.CheckDatasetsAsync(m_ListGestion.ModifiedProtocols, true, update));
            }
            if (requiresReload)
            {
                DataManager.Clear();
                Module3DMain.ReloadScenes();
            }
            if (ApplicationState.LoadedProject != null)
            {
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