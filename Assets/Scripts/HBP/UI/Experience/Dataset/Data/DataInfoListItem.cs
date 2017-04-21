using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.UI;
using d = HBP.Data.Experience.Dataset;
using Tools.Unity;

namespace HBP.UI.Experience.Dataset
{
    /// <summary>
    /// The Script which manage the dataInfo list panel.
    /// </summary>
    public class DataInfoListItem : Tools.Unity.Lists.ListItemWithSave<d.DataInfo>
    {
		#region Properties
		/// <summary>
		/// The label inputField.
		/// </summary>
		[SerializeField]
		InputField m_labelInputField;

		/// <summary>
		/// The Measurelabel inputField.
		/// </summary>
		[SerializeField]
		InputField m_measureLabelInputField;
		
		/// <summary>
		/// The EEG inputField.
		/// </summary>
		[SerializeField]
		FileSelector m_EEGFileSelector;

        /// <summary>
        /// The Pos inputField.
        /// </summary>
        [SerializeField]
		FileSelector m_PosFileSelector;

        /// <summary>
        /// The protocol visualisation dropdown.
        /// </summary>
        [SerializeField]
		Dropdown m_ProvDropdown;

		/// <summary>
		/// The patient dropdown.
		/// </summary>
		[SerializeField]
        Dropdown m_PatientDropdown;

		/// <summary>
		/// The toggle.
		/// </summary>
		[SerializeField]
		Toggle m_toggle;

        /// <summary>
        /// IsUsable
        /// </summary>
        [SerializeField]
        ImageColorSwitcher m_SPstateColor;

        /// <summary>
        /// IsUsable
        /// </summary>
        [SerializeField]
        ImageColorSwitcher m_MPstateColor;

        bool IsSettingFields = false;   
        #endregion

        #region Public Methods
        protected override void SetObject(d.DataInfo dataInfo)
        {
            IsSettingFields = true;
            m_labelInputField.text = dataInfo.Name;
            m_measureLabelInputField.text = dataInfo.Measure;

            m_EEGFileSelector.Path = dataInfo.EEG;
            m_PosFileSelector.Path = dataInfo.POS;
            m_EEGFileSelector.onValueChanged.AddListener((eeg) =>
            {
                if (!IsSettingFields)
                {
                    dataInfo.EEG = eeg;
                    UpdateState();
                }
            });
            m_PosFileSelector.onValueChanged.AddListener((pos) =>
            {
                if (!IsSettingFields)
                {
                    dataInfo.POS = pos;
                    UpdateState();
                }
            });

            Data.Experience.Protocol.Protocol[] l_protocols = ApplicationState.ProjectLoaded.Protocols.ToArray();
            List<Dropdown.OptionData> l_protocolOptions = new List<Dropdown.OptionData>(l_protocols.Length);
            int protocolValue = 0;
            for (int i = 0; i < l_protocols.Length; i++)
            {
                if (dataInfo.Protocol == l_protocols[i])
                {
                    protocolValue = i;
                }
                l_protocolOptions.Add(new Dropdown.OptionData(l_protocols[i].Name, null));
            }
            m_ProvDropdown.options = l_protocolOptions;
            m_ProvDropdown.value = protocolValue;
            m_object.Protocol = ApplicationState.ProjectLoaded.Protocols[protocolValue];

            Data.Patient[] l_patients = ApplicationState.ProjectLoaded.Patients.ToArray();
            List<Dropdown.OptionData> l_patientOptions = new List<Dropdown.OptionData>(l_patients.Length);
            int patientValue = 0;
            for (int i = 0; i < l_patients.Length; i++)
            {
                if (dataInfo.Patient == l_patients[i])
                {
                    patientValue = i;
                }
                l_patientOptions.Add(new Dropdown.OptionData(l_patients[i].Name, null));
            }
            m_PatientDropdown.options = l_patientOptions;
            m_PatientDropdown.value = patientValue;
            m_object.Patient = ApplicationState.ProjectLoaded.Patients[patientValue];

            UpdateState();
            IsSettingFields = false;
        }

        public override void Save()
        {
            Object.Name = m_labelInputField.text;
            Object.Measure = m_measureLabelInputField.text;
            Object.EEG = m_EEGFileSelector.Path;
            Object.POS = m_PosFileSelector.Path;
            Object.Patient = ApplicationState.ProjectLoaded.Patients.ToArray()[m_PatientDropdown.value];
            Object.Protocol = ApplicationState.ProjectLoaded.Protocols.ToArray()[m_ProvDropdown.value];
        }

        public void OnEndEditLabel()
        {
            if (!IsSettingFields)
            {
                m_object.Name = m_labelInputField.text;
                UpdateState();
            }
        }

        public void OnEndEditMeasure()
        {
            if (!IsSettingFields)
            {
                m_object.Measure = m_measureLabelInputField.text;
                UpdateState();
            }
        }

        public void OnChangeProtocol()
        {
            if (!IsSettingFields)
            {
                m_object.Protocol = ApplicationState.ProjectLoaded.Protocols.ToArray()[m_ProvDropdown.value];
                UpdateState();
            }
        }

        public void OnChangePatient()
        {
            if (!IsSettingFields)
            {
                m_object.Patient = ApplicationState.ProjectLoaded.Patients.ToArray()[m_PatientDropdown.value];
                UpdateState();
            }
        }

        void UpdateState()
        {
            m_object.UpdateStates();
            int l_stateSP = (int)m_object.SinglePatientState;
            int l_stateMP = (int)m_object.MultiPatientsState;
            m_SPstateColor.Set(l_stateSP);
            m_MPstateColor.Set(l_stateMP);
        }
        #endregion   
    }
}