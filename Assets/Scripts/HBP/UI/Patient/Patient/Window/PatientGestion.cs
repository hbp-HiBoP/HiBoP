using Tools.Unity;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Anatomy
{
    public class PatientGestion : SavableWindow
    {
        #region Properties
        [SerializeField] PatientListGestion m_PatientListGestion;
        [SerializeField] Button m_AddButton;
        [SerializeField] Button m_RemoveButton;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;

                m_PatientListGestion.Interactable = value;
                m_AddButton.interactable = value;
                m_RemoveButton.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Save()
        {
            if (DataManager.HasData)
            {
                ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.WarningMultiOptions, "Reload required", "Some data have already been loaded. Your changes will not be applied unless you reload.\n\nWould you like to reload ?", () =>
                {
                    ApplicationState.ProjectLoaded.SetPatients(m_PatientListGestion.Objects);
                    base.Save();
                    DataManager.Clear();
                    ApplicationState.Module3D.ReloadScenes();
                });
            }
            else
            {
                ApplicationState.ProjectLoaded.SetPatients(m_PatientListGestion.Objects);
                base.Save();
            }
            FindObjectOfType<MenuButtonState>().SetInteractables();
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            m_PatientListGestion.Initialize(m_SubWindows);
            base.Initialize();
        }
        protected override void SetFields()
        {
            m_PatientListGestion.Objects = ApplicationState.ProjectLoaded.Patients.ToList();
            base.SetFields();
        }
        #endregion
    }
}