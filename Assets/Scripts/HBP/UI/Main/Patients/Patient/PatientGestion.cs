using HBP.Core.Data;
using HBP.Data.Module3D;
using UnityEngine;
using HBP.UI.Tools;
using HBP.Core.Tools;

namespace HBP.UI.Main
{
    public class PatientGestion : GestionWindow<Patient>
    {
        #region Properties
        [SerializeField] PatientListGestion m_ListGestion;
        public override ListGestion<Patient> ListGestion => m_ListGestion;
        #endregion

        #region Public Methods
        public override void OK()
        {
            if (DataManager.HasData)
            {
                DialogBoxManager.Open(DialogBoxManager.AlertType.WarningMultiOptions, "Reload required", "Some data have already been loaded. Your changes will not be applied unless you reload.\n\nWould you like to reload ?", () =>
                {
                    base.OK();
                    ApplicationState.ProjectLoaded.SetPatients(ListGestion.List.Objects);
                    DataManager.Clear();
                    Module3DMain.ReloadScenes();
                });
            }
            else
            {
                base.OK();
                ApplicationState.ProjectLoaded.SetPatients(ListGestion.List.Objects);
            }
            FindObjectOfType<MenuButtonState>().SetInteractables();
        }
        #endregion

        #region Private Methods
        protected override void SetFields()
        {
            base.SetFields();
            ListGestion.List.Set(ApplicationState.ProjectLoaded.Patients);
        }
        #endregion
    }
}