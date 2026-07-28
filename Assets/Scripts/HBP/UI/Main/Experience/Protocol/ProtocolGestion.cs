using UnityEngine;
using HBP.Core.Tools;
using HBP.Core.Data;
using HBP.Data.Module3D;
using HBP.UI.Tools;
using HBP.Core.Database;
using HBP.UI.Database;
using Cysharp.Threading.Tasks;

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
            bool requiresReload = DataManager.HasData;
            ValidationRequest validationRequest =
                ValidationImpactAnalyzer.ForProtocols(
                    m_OldValues,
                    m_ListGestion.List.Objects);

            base.OK();
            DatabaseManager.Database.SetProtocols(
                m_ListGestion.List.Objects,
                validationRequest);
            if (validationRequest.Aspects != ValidationAspect.None)
            {
                ApplicationState.LoadedProject?.InvalidateValidation(
                    validationRequest);
            }
            await DatabaseWorkflow.SaveProtocolsAsync();
            InteractableStateManager.SetInteractables();
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
