using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Experience.Dataset
{
	public class DatasetGestion : SavableWindow
    {
        #region Properties
        [SerializeField] DatasetListGestion m_datasetListGestion;
        [SerializeField] Button m_CreateDatasetButton, m_RemoveDatasetButton;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;

                m_datasetListGestion.Interactable = value;
                m_CreateDatasetButton.interactable = value;
                m_RemoveDatasetButton.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Save()
		{
            if (DataManager.HasData)
            {
                ApplicationState.DialogBoxManager.Open(Tools.Unity.DialogBoxManager.AlertType.WarningMultiOptions, "Reload required", "Some data have already been loaded. Your changes will not be applied unless you reload.\n\nWould you like to reload ?", () =>
                {
                    ApplicationState.ProjectLoaded.SetDatasets(m_datasetListGestion.Objects);
                    base.Save();
                    DataManager.Clear();
                    ApplicationState.Module3D.ReloadScenes();
                });
            }
            else
            {
                ApplicationState.ProjectLoaded.SetDatasets(m_datasetListGestion.Objects);
                base.Save();
            }
            FindObjectOfType<MenuButtonState>().SetInteractables();
        }
        #endregion

        #region Private Methods
        protected override void SetFields()
		{
            m_datasetListGestion.Initialize(m_SubWindows);
            m_datasetListGestion.Objects = ApplicationState.ProjectLoaded.Datasets.ToList();
            base.SetFields();
        }
        #endregion
    }
}
