using UnityEngine;
using HBP.UI.Tools;

namespace HBP.UI.Main
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
            WindowsManager.Open("Patient gestion window");
        }

        public void OpenGroupGestion()
        {
            WindowsManager.Open("Group gestion window");
        }
        #endregion
    }
}