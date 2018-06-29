using UnityEngine;

namespace HBP.UI.Anatomy
{
    public class PatientMenu : MonoBehaviour
    {
        [SerializeField]
        GameObject patientGestionPrefab;

        [SerializeField]
        GameObject groupGestionPrefab;

        public void OpenPatientGestion()
        {
            PatientGestion.Open(true);
        }

        public void OpenGroupGestion()
        {
            GroupGestion.Open(true);
        }
    }
}