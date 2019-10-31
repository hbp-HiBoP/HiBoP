using Tools.Unity;
using UnityEngine;
using HBP.Data;
using Tools.Unity.Components;

namespace HBP.UI.Anatomy
{
    public class PatientGestion : GestionWindow<Patient>
    {
        #region Properties
        [SerializeField] PatientListGestion m_ListGestion;
        public override ListGestion<Patient> ListGestion => m_ListGestion;
        #endregion

        #region Public Methods
        public override void Save()
        {
            if (DataManager.HasData)
            {
                ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.WarningMultiOptions, "Reload required", "Some data have already been loaded. Your changes will not be applied unless you reload.\n\nWould you like to reload ?", () =>
                {
                    base.Save();
                    ApplicationState.ProjectLoaded.SetPatients(ListGestion.List.Objects);
                    DataManager.Clear();
                    ApplicationState.Module3D.ReloadScenes();
                });
            }
            else
            {
                base.Save();
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