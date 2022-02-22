using HBP.Data.Preferences;
using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.UserPreferences
{
    public class _3DPreferencesSubModifier : SubModifier<_3DPreferences>
    {
        #region Properties
        [SerializeField] Toggle m_AutomaticEEGUpdateToggle;
        [SerializeField] Dropdown m_VisualizationsLayoutDirectionDropdown;
        [SerializeField] Dropdown m_SiteInfluenceDropdown;

        [SerializeField] InputField m_DefaultSelectedMRIInSinglePatientInputField;
        [SerializeField] InputField m_DefaultSelectedMeshInSinglePatientInputField;
        [SerializeField] InputField m_DefaultSelectedImplantionSinglePatientInputField;

        [SerializeField] InputField m_DefaultSelectedMRIInMultiPatientsInputField;
        [SerializeField] InputField m_DefaultSelectedMeshInMultiPatientsInputField;
        [SerializeField] InputField m_DefaultSelectedImplantionInMultiPatientsInputField;


        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
            set
            {
                base.Interactable = value;

                m_AutomaticEEGUpdateToggle.interactable = value;
                m_VisualizationsLayoutDirectionDropdown.interactable = value;
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

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();

            m_AutomaticEEGUpdateToggle.onValueChanged.AddListener((value) => Object.AutomaticEEGUpdate = value);
            m_VisualizationsLayoutDirectionDropdown.onValueChanged.AddListener((value) => Object.VisualizationsLayoutDirection = (Data.Enums.LayoutDirection)value);
            m_SiteInfluenceDropdown.onValueChanged.AddListener((value) => Object.SiteInfluenceByDistance = (Data.Enums.SiteInfluenceByDistanceType)value);

            m_DefaultSelectedMRIInSinglePatientInputField.onValueChanged.AddListener((value) => Object.DefaultSelectedMRIInSinglePatientVisualization = value);
            m_DefaultSelectedMeshInSinglePatientInputField.onValueChanged.AddListener((value) => Object.DefaultSelectedMeshInSinglePatientVisualization = value);
            m_DefaultSelectedImplantionSinglePatientInputField.onValueChanged.AddListener((value) => Object.DefaultSelectedImplantationInSinglePatientVisualization = value);

            m_DefaultSelectedMRIInMultiPatientsInputField.onValueChanged.AddListener((value) => Object.DefaultSelectedMRIInMultiPatientsVisualization = value);
            m_DefaultSelectedMeshInMultiPatientsInputField.onValueChanged.AddListener((value) => Object.DefaultSelectedMeshInMultiPatientsVisualization = value);
            m_DefaultSelectedImplantionInMultiPatientsInputField.onValueChanged.AddListener((value) => Object.DefaultSelectedImplantationInMultiPatientsVisualization = value);
        }
        #endregion

        #region Private Methods
        protected override void SetFields(_3DPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_AutomaticEEGUpdateToggle.isOn = objectToDisplay.AutomaticEEGUpdate;
            m_VisualizationsLayoutDirectionDropdown.Set(typeof(Data.Enums.LayoutDirection), (int)objectToDisplay.VisualizationsLayoutDirection);
            m_SiteInfluenceDropdown.Set(typeof(Data.Enums.SiteInfluenceByDistanceType), (int)objectToDisplay.SiteInfluenceByDistance);

            m_DefaultSelectedMRIInSinglePatientInputField.text = objectToDisplay.DefaultSelectedMRIInSinglePatientVisualization;
            m_DefaultSelectedMeshInSinglePatientInputField.text = objectToDisplay.DefaultSelectedMeshInSinglePatientVisualization;
            m_DefaultSelectedImplantionSinglePatientInputField.text = objectToDisplay.DefaultSelectedImplantationInSinglePatientVisualization;

            m_DefaultSelectedMRIInMultiPatientsInputField.text = objectToDisplay.DefaultSelectedMRIInMultiPatientsVisualization;
            m_DefaultSelectedMeshInMultiPatientsInputField.text = objectToDisplay.DefaultSelectedMeshInMultiPatientsVisualization;
            m_DefaultSelectedImplantionInMultiPatientsInputField.text = objectToDisplay.DefaultSelectedImplantationInMultiPatientsVisualization;
        }
        #endregion
    }
}