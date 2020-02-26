using UnityEngine;

namespace HBP.UI
{
    public class PatientMenu : Menu
    {
        #region Properties
        [SerializeField] InteractableConditions m_PatientsInteractableConditions;
        public InteractableConditions PatientsInteractableConditions { get { return m_PatientsInteractableConditions; } }
        [SerializeField] InteractableConditions m_GroupsInteractableConditions;
        public InteractableConditions GroupsInteractableConditions { get { return m_GroupsInteractableConditions; } }
        #endregion

        #region Public Methods
        public void OpenPatientGestion()
        {
            ApplicationState.WindowsManager.Open("Patient gestion window");
        }

        public void OpenGroupGestion()
        {
            ApplicationState.WindowsManager.Open("Group gestion window");
        }
        #endregion
    }
}