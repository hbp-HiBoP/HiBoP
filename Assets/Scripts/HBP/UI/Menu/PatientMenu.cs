using UnityEngine;

namespace HBP.UI
{
    public class PatientMenu : MonoBehaviour
    {
        public void OpenPatientGestion()
        {
            ApplicationState.WindowsManager.Open("Patient gestion window");
        }

        public void OpenGroupGestion()
        {
            ApplicationState.WindowsManager.Open("Group gestion window");
        }
    }
}