using Tools.Unity.Components;
using UnityEngine;

namespace HBP.UI.Experience.Dataset
{
    public class DatasetGestion : GestionWindow<Core.Data.Dataset>
    {
        #region Properties
        [SerializeField] DatasetListGestion m_ListGestion;
        public override ListGestion<Core.Data.Dataset> ListGestion => m_ListGestion;
        #endregion

        #region Public Methods
        public override void OK()
		{
            if (Core.Data.DataManager.HasData)
            {
                ApplicationState.DialogBoxManager.Open(Tools.Unity.DialogBoxManager.AlertType.WarningMultiOptions, "Reload required", "Some data have already been loaded. Your changes will not be applied unless you reload.\n\nWould you like to reload ?", () =>
                {
                    base.OK();
                    ApplicationState.ProjectLoaded.SetDatasets(m_ListGestion.List.Objects);
                    Core.Data.DataManager.Clear();
                    ApplicationState.Module3D.ReloadScenes();
                });
            }
            else
            {
                base.OK();
                ApplicationState.ProjectLoaded.SetDatasets(m_ListGestion.List.Objects);
            }
            FindObjectOfType<MenuButtonState>().SetInteractables();
        }
        #endregion

        #region Private Methods
        protected override void SetFields()
		{
            ListGestion.List.Set(ApplicationState.ProjectLoaded.Datasets);
            base.SetFields();
        }
        #endregion
    }
}
