using System;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Preferences
{
    public class _3DPreferencesModifier : MonoBehaviour
    {
        #region Properties
        [SerializeField] Toggle m_AutomaticEEGUpdateToggle;
        [SerializeField] Dropdown m_SiteInfluenceDropdown;

        [SerializeField] InputField m_DefaultSelectedMRIInSinglePatientInputField;
        [SerializeField] InputField m_DefaultSelectedMeshInSinglePatientInputField;
        [SerializeField] InputField m_DefaultSelectedImplantionSinglePatientInputField;

        [SerializeField] InputField m_DefaultSelectedMRIInMultiPatientsInputField;
        [SerializeField] InputField m_DefaultSelectedMeshInMultiPatientsInputField;
        [SerializeField] InputField m_DefaultSelectedImplantionInMultiPatientsInputField;
        #endregion

        #region Private Methods
        public void Initialize()
        {
            Data.Preferences._3DPreferences preferences = ApplicationState.UserPreferences.Visualization._3D;

            m_AutomaticEEGUpdateToggle.isOn = preferences.AutomaticEEGUpdate;

            string[] options = Enum.GetNames(typeof(Data.Enums.SiteInfluenceByDistanceType));
            m_SiteInfluenceDropdown.ClearOptions();
            foreach (string option in options)
            {
                m_SiteInfluenceDropdown.options.Add(new Dropdown.OptionData(option));
            }
            m_SiteInfluenceDropdown.value = (int) ApplicationState.UserPreferences.Visualization._3D.SiteInfluenceByDistance;
            m_SiteInfluenceDropdown.RefreshShownValue();

            m_DefaultSelectedMRIInSinglePatientInputField.text = preferences.DefaultSelectedMRIInSinglePatientVisualization;
            m_DefaultSelectedMeshInSinglePatientInputField.text = preferences.DefaultSelectedMeshInSinglePatientVisualization;
            m_DefaultSelectedImplantionSinglePatientInputField.text = preferences.DefaultSelectedImplantationInSinglePatientVisualization;

            m_DefaultSelectedMRIInMultiPatientsInputField.text = preferences.DefaultSelectedMRIInMultiPatientsVisualization;
            m_DefaultSelectedMeshInMultiPatientsInputField.text = preferences.DefaultSelectedMeshInMultiPatientsVisualization;
            m_DefaultSelectedImplantionInMultiPatientsInputField.text = preferences.DefaultSelectedImplantationInMultiPatientsVisualization;
        }
        public void Save()
        {
            Data.Preferences._3DPreferences preferences = ApplicationState.UserPreferences.Visualization._3D;

            preferences.AutomaticEEGUpdate = m_AutomaticEEGUpdateToggle.isOn;
            ApplicationState.UserPreferences.Visualization._3D.SiteInfluenceByDistance = (Data.Enums.SiteInfluenceByDistanceType) m_SiteInfluenceDropdown.value;

            preferences.DefaultSelectedMRIInSinglePatientVisualization = m_DefaultSelectedMRIInSinglePatientInputField.text;
            preferences.DefaultSelectedMeshInSinglePatientVisualization = m_DefaultSelectedMeshInSinglePatientInputField.text;
            preferences.DefaultSelectedImplantationInSinglePatientVisualization = m_DefaultSelectedImplantionSinglePatientInputField.text;

            preferences.DefaultSelectedMRIInMultiPatientsVisualization = m_DefaultSelectedMRIInMultiPatientsInputField.text;
            preferences.DefaultSelectedMeshInMultiPatientsVisualization = m_DefaultSelectedMeshInMultiPatientsInputField.text;
            preferences.DefaultSelectedImplantationInMultiPatientsVisualization = m_DefaultSelectedImplantionInMultiPatientsInputField.text;
        }
        public void SetInteractable(bool interactable)
        {
            m_AutomaticEEGUpdateToggle.interactable = interactable;
            m_SiteInfluenceDropdown.interactable = interactable;
            m_DefaultSelectedMRIInSinglePatientInputField.interactable = interactable;
            m_DefaultSelectedMeshInSinglePatientInputField.interactable = interactable;
            m_DefaultSelectedImplantionSinglePatientInputField.interactable = interactable;
            m_DefaultSelectedMRIInMultiPatientsInputField.interactable = interactable;
            m_DefaultSelectedMeshInMultiPatientsInputField.interactable = interactable;
            m_DefaultSelectedImplantionInMultiPatientsInputField.interactable = interactable;
        }
        #endregion
    }
}