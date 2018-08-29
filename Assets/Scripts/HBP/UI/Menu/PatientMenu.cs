using UnityEngine;

namespace HBP.UI.Anatomy
{
    public class PatientMenu : MonoBehaviour
    {
        public void OpenPatientGestion()
        {
            ApplicationState.WindowsManager.Open("Patient Gestion Window");
        }

        public void OpenGroupGestion()
        {
            ApplicationState.WindowsManager.Open("Group Gestion Window");
        }
    }
}