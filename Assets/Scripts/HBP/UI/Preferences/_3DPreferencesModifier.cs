using System;
using Tools.Unity;
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


        protected bool m_Interactable;
        public virtual bool Interactable
        {
            get
            {
                return m_Interactable;
            }
            set
            {
                m_Interactable = value;

                m_AutomaticEEGUpdateToggle.interactable = value;
                m_SiteInfluenceDropdown.interactable = value;
                m_DefaultSelectedMRIInSinglePatientInputField.interactable = value;
                m_DefaultSelectedMeshInSinglePatientInputField.interactable = value;
                m_DefaultSelectedImplantionSinglePatientInputField.interactable = value;
                m_DefaultSelectedMRIInMultiPatientsInputField.interactable = value;
                m_DefaultSelectedMeshInMultiPatientsInputField.interactable = value;
                m_DefaultSelectedImplantionInMultiPatientsInputField.interactable = value;
            }
        }
        #endregion

        #region Private Methods
        public void SetFields()
        {
            Data.Preferences._3DPreferences preferences = ApplicationState.UserPreferences.Visualization._3D;

            m_AutomaticEEGUpdateToggle.isOn = preferences.AutomaticEEGUpdate;

            m_SiteInfluenceDropdown.Set(typeof(Data.Enums.SiteInfluenceByDistanceType), (int)preferences.SiteInfluenceByDistance);

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
            preferences.SiteInfluenceByDistance = (Data.Enums.SiteInfluenceByDistanceType) m_SiteInfluenceDropdown.value;

            preferences.DefaultSelectedMRIInSinglePatientVisualization = m_DefaultSelectedMRIInSinglePatientInputField.text;
            preferences.DefaultSelectedMeshInSinglePatientVisualization = m_DefaultSelectedMeshInSinglePatientInputField.text;
            preferences.DefaultSelectedImplantationInSinglePatientVisualization = m_DefaultSelectedImplantionSinglePatientInputField.text;

            preferences.DefaultSelectedMRIInMultiPatientsVisualization = m_DefaultSelectedMRIInMultiPatientsInputField.text;
            preferences.DefaultSelectedMeshInMultiPatientsVisualization = m_DefaultSelectedMeshInMultiPatientsInputField.text;
            preferences.DefaultSelectedImplantationInMultiPatientsVisualization = m_DefaultSelectedImplantionInMultiPatientsInputField.text;
        }
        #endregion
    }
}