using HBP.Core.Data;
using HBP.Data.Module3D;
using UnityEngine;
using HBP.UI.Tools;
using HBP.Core.Tools;

namespace HBP.UI.Main
{
    public class DatasetGestion : GestionWindow<Dataset>
    {
        #region Properties
        [SerializeField] DatasetListGestion m_ListGestion;
        public override ListGestion<Dataset> ListGestion => m_ListGestion;
        #endregion

        #region Public Methods
        public override void OK()
		{
            if (DataManager.HasData)
            {
                DialogBoxManager.Open(DialogBoxManager.AlertType.WarningMultiOptions, "Reload required", "Some data have already been loaded. Your changes will not be applied unless you reload.\n\nWould you like to reload ?", () =>
                {
                    base.OK();
                    ApplicationState.LoadedProject.SetDatasets(m_ListGestion.List.Objects);
                    DataManager.Clear();
                    Module3DMain.ReloadScenes();
                    UITools.CheckProjectIDAndAskForRegeneration();
                });
            }
            else
            {
                base.OK();
                ApplicationState.LoadedProject.SetDatasets(m_ListGestion.List.Objects);
                UITools.CheckProjectIDAndAskForRegeneration();
            }
            InteractableStateManager.SetInteractables();
        }
        #endregion

        #region Private Methods
        protected override void SetFields()
		{
            ListGestion.List.Set(ApplicationState.LoadedProject.Datasets);
            base.SetFields();
        }
        #endregion
    }
}
