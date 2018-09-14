using System.Linq;
using d = HBP.Data.Experience.Dataset;
using UnityEngine;

namespace HBP.UI.Experience.Dataset
{
	public class DatasetGestion : ItemGestion<d.Dataset>
    {
        #region Properties
        [SerializeField] DatasetListGestion m_datasetListGestion;

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
                    ApplicationState.ProjectLoaded.SetDatasets(Items.ToArray());
                    base.Save();
                    DataManager.Clear();
                    ApplicationState.Module3D.ReloadScenes();
                });
            }
            else
            {
                ApplicationState.ProjectLoaded.SetDatasets(Items.ToArray());
                base.Save();
            }
        }
        #endregion

        #region Private Methods
        protected override void SetFields()
		{
            m_datasetListGestion.Initialize(m_SubWindows);
            m_datasetListGestion.Items = ApplicationState.ProjectLoaded.Datasets.ToList();
            base.SetFields();
        }
        #endregion
    }
}
