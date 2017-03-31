using UnityEngine;
using Tools.Unity;

namespace HBP.UI
{
    /**
    * \class WindowsGestion
    * \author Adrien Gannerie
    * \version 1.0
    * \date 03 janvier 2017
    * \brief Open the windows.
    * 
    * \details Class which open the windows prefabs.
    */
    public class WindowsGestion : MonoBehaviour
    {
        #region Properties
        // Others.
        [SerializeField]
        GameObject progressWindowPrefab;
        [SerializeField]
        GameObject popUpPrefab;

        // Patient.
        [SerializeField]
        GameObject patientGestionPrefab;
        [SerializeField]
        GameObject patientModifierPrefab;
        [SerializeField]
        GameObject groupGestionPrefab;
        [SerializeField]
        GameObject groupModifierPrefab;
        [SerializeField]
        GameObject groupSelectionPrefab;
        #endregion

        #region Public Methods
        // Others.
        /**
        * \brief Open a progress bar.
        * \returns Progress bar opened.
        */
        public ProgressWindow OpenProgressBar()
        {
            return Instantiate(progressWindowPrefab).GetComponent<ProgressWindow>();
        }
        /**
        * \brief Open a PopUp.
        * \returns PopUp opened.
        */
        public PopUp OpenPopUp()
        {
            return Instantiate(popUpPrefab).GetComponent<PopUp>();
        }

        // Patient.
        /**
        * \brief Open a patient gestion window.
        * \returns Patient gestion window opened.
        */
        public void OpenPatientGestion()
        {
            Patient.PatientGestion patientGestion = Instantiate(patientGestionPrefab).GetComponent<Patient.PatientGestion>();
            patientGestion.Open();
        }

        /**
        * \brief Open a patient modifier window.
        * \returns patient modifier window opened.
        */
        public Patient.PatientModifier OpenPatientModifier()
        {
            Patient.PatientModifier patientModifier = Instantiate(patientModifierPrefab).GetComponent<Patient.PatientModifier>();
            patientModifier.Open();
            return patientModifier;
        }
        /**
        * \brief Open a group gestion window.
        * \returns group gestion window opened.
        */
        public void OpenGroupGestion()
        {
            Patient.GroupGestion groupGestion = Instantiate(groupGestionPrefab).GetComponent<Patient.GroupGestion>();
            groupGestion.Open();
        }

        /**
        * \brief Open a group modifier window.
        * \returns group modifier window opened.
        */
        public Patient.GroupModifier OpenGroupModifier()
        {
            Patient.GroupModifier groupModifier = Instantiate(groupModifierPrefab).GetComponent<Patient.GroupModifier>();
            groupModifier.Open();
            return groupModifier;
        }
        /**
        * \brief Open a group selection window.
        * \returns group selection window opened.
        */
        public Patient.GroupSelection OpenGroupSelection()
        {
            Patient.GroupSelection groupSelection = Instantiate(groupSelectionPrefab).GetComponent<Patient.GroupSelection>();
            groupSelection.Open();
            return groupSelection;
        }
        #endregion

        #region Private Methods
        GameObject Instantiate(GameObject prefab)
        {
            return Instantiate(prefab, Vector3.zero, Quaternion.identity, transform) as GameObject;
        }
        #endregion
    }
}