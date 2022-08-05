using HBP.Core.Data;
using HBP.Module3D;
using Tools.Unity;
using UnityEngine;

namespace HBP.UI
{
    public class PatientGestion : GestionWindow<Core.Data.Patient>
    {
        #region Properties
        [SerializeField] PatientListGestion m_ListGestion;
        public override ListGestion<Core.Data.Patient> ListGestion => m_ListGestion;
        #endregion

        #region Public Methods
        public override void OK()
        {
            if (Core.Data.DataManager.HasData)
            {
                DialogBoxManager.Open(DialogBoxManager.AlertType.WarningMultiOptions, "Reload required", "Some data have already been loaded. Your changes will not be applied unless you reload.\n\nWould you like to reload ?", () =>
                {
                    base.OK();
                    ApplicationState.ProjectLoaded.SetPatients(ListGestion.List.Objects);
                    Core.Data.DataManager.Clear();
                    HBP3DModule.ReloadScenes();
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