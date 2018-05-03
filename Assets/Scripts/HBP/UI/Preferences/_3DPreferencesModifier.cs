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
        [SerializeField] InputField m_DefaultSelectedMRIInputField;
        [SerializeField] InputField m_DefaultSelectedMeshInputField;
        [SerializeField] InputField m_DefaultSelectedImplantionInputField; 
        #endregion

        #region Private Methods
        public void Set()
        {
            Data.Preferences._3DPreferences preferences = ApplicationState.UserPreferences.Visualization._3D;

            m_AutomaticEEGUpdateToggle.isOn = preferences.AutomaticEEGUpdate;

            string[] options = Enum.GetNames(typeof(Data.Enums.SiteInfluenceType));
            m_SiteInfluenceDropdown.ClearOptions();
            foreach (string option in options)
            {
                m_SiteInfluenceDropdown.options.Add(new Dropdown.OptionData(option));
            }
            m_SiteInfluenceDropdown.value = (int) ApplicationState.UserPreferences.Visualization._3D.SiteInfluence;
            m_SiteInfluenceDropdown.RefreshShownValue();

            m_DefaultSelectedMRIInputField.text = preferences.DefaultSelectedMRI;
            m_DefaultSelectedMeshInputField.text = preferences.DefaultSelectedMesh;
            m_DefaultSelectedImplantionInputField.text = preferences.DefaultSelectedImplantation;
        }
        public void Save()
        {
            Data.Preferences._3DPreferences preferences = ApplicationState.UserPreferences.Visualization._3D;

            preferences.AutomaticEEGUpdate = m_AutomaticEEGUpdateToggle.isOn;
            ApplicationState.UserPreferences.Visualization._3D.SiteInfluence = (Data.Enums.SiteInfluenceType) m_SiteInfluenceDropdown.value;

            preferences.DefaultSelectedMRI = m_DefaultSelectedMRIInputField.text;
            preferences.DefaultSelectedMesh = m_DefaultSelectedMeshInputField.text;
            preferences.DefaultSelectedImplantation = m_DefaultSelectedImplantionInputField.text;
        }
        #endregion
    }
}