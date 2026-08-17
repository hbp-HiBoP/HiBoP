using HBP.Core.Data;
using HBP.Data.Module3D;
using UnityEngine;
using HBP.UI.Tools;
using HBP.Core.Tools;
using HBP.Core.Database;
using Cysharp.Threading.Tasks;

namespace HBP.UI.Main
{
    public class DatasetGestion : GestionWindow<Dataset>
    {
        #region Properties

        [SerializeField] DatasetListGestion m_ListGestion;
        public override ListGestion<Dataset> ListGestion => m_ListGestion;

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
            ValidationRequest validationRequest = ValidationImpactAnalyzer.ForDatasets(m_OldValues, m_ListGestion.List.Objects);
            if (DataManager.HasData)
            {
                int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Reload required", "Some data have already been loaded. Your changes will not be applied unless you reload.\n\nWould you like to reload ?", "Save & Reload", "Cancel");
                if (result == 0)
                {
                    base.OK();
                    await UniTask.SwitchToMainThread();
                    ApplicationState.LoadedProject.SetDatasets(m_ListGestion.List.Objects, validationRequest);
                    DataManager.Clear();
                    var visualizations = Module3DMain.PrepareReloadScenes();
                    await LoadingManager.LoadAsync((update, token) => Module3DMain.LoadAsync(visualizations, update, token));
                    UITools.CheckProjectIDAndAskForRegeneration().Forget();
                }
            }
            else
            {
                base.OK();
                ApplicationState.LoadedProject.SetDatasets(m_ListGestion.List.Objects, validationRequest);
                UITools.CheckProjectIDAndAskForRegeneration().Forget();
            }

            InteractableStateManager.SetInteractables();
        }

        public override void Close()
        {
            if (m_ListGestion.HasBeenModified)
                LoadingManager.Load(update => RestoreOldValuesAsync(ApplicationState.LoadedProject.Datasets, update), false);
            base.Close();
        }

        #endregion

        #region Private Methods

        protected override void SetFields()
        {
            base.SetFields();
            SetList(ApplicationState.LoadedProject.Datasets);
        }

        #endregion
    }
}
