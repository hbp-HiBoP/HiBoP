using UnityEngine;
using CielaSpike;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using p = HBP.Data.Patient;
using d = HBP.Data.Experience.Dataset;
using HBP.Data.Settings;
using Tools.CSharp;
using Tools.Unity;

namespace HBP.UI.Experience.Dataset
{
	/// <summary>
	/// The Script which manage the dataInfo list panel.
	/// </summary>
	public class DataInfoListItem : Tools.Unity.Lists.ListItemWithSave<d.DataInfo>
    {
		#region Attributs
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
		InputField m_EEGInputField;

        /// <summary>
        /// The EEG path button.
        /// </summary>
        [SerializeField]
        Button m_EEGPathButton;

        /// <summary>
        /// The Pos inputField.
        /// </summary>
        [SerializeField]
		InputField m_PosInputField;

        /// <summary>
        /// The Pos path button.
        /// </summary>
        [SerializeField]
        Button m_POSPathButton;

        /// <summary>
        /// The protocol visualization dropdown.
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
            m_EEGInputField.text = dataInfo.EEG;
            m_PosInputField.text = dataInfo.POS;

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
            Object.EEG = m_EEGInputField.text;
            Object.POS = m_PosInputField.text;
            Object.Patient = ApplicationState.ProjectLoaded.Patients.ToArray()[m_PatientDropdown.value];
            Object.Protocol = ApplicationState.ProjectLoaded.Protocols.ToArray()[m_ProvDropdown.value];
        }


        /// <summary>
        /// Open the POS file dialog.
        /// </summary>
        public void OpenPOSPath()
        {
            string l_filePath = m_PosInputField.text;
            string l_path;
            if (l_filePath != string.Empty && new System.IO.FileInfo(l_filePath).Exists && new System.IO.FileInfo(l_filePath).Extension == Data.Localizer.POS.EXTENSION)
            {
                l_path = l_filePath;
            }
            else
            {
                if (m_EEGInputField.text != string.Empty)
                {
                    l_path = m_EEGInputField.text;
                }
                else
                {
                    l_path = ApplicationState.ProjectLoaded.Settings.LocalizerDatabase;
                }
            }

            string l_resultStandalone = HBP.Module3D.DLL.QtGUI.GetExistingFileName(new string[] { "pos" }, "Please select the POS file.", l_path);
            StringExtension.StandardizeToPath(ref l_resultStandalone);
            if (l_resultStandalone != string.Empty)
            {
                m_PosInputField.text = l_resultStandalone;
                OnEndEditPOSPath();
            }
        }

        /// <summary>
        /// Open the EEGF file dialog.
        /// </summary>
        public void OpenEEGPath()
        {
            string l_filePath = m_EEGInputField.text;
            string l_path;
            if (l_filePath != string.Empty && new System.IO.FileInfo(l_filePath).Exists && new System.IO.FileInfo(l_filePath).Extension == Elan.EEG.EXTENSION)
            {
                l_path = l_filePath;
            }
            else
            {
                l_path = ApplicationState.ProjectLoaded.Settings.LocalizerDatabase;
            }

            string l_resultStandalone = HBP.Module3D.DLL.QtGUI.GetExistingFileName(new string[] { "eeg" }, "Please select the EEG file.", l_path);
            StringExtension.StandardizeToPath(ref l_resultStandalone);
            if (l_resultStandalone != string.Empty)
            {
                m_EEGInputField.text = l_resultStandalone;
                OnEndEditEEGPath();
            }
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

        public void OnEndEditEEGPath()
        {
            if (!IsSettingFields)
            {
                m_object.EEG = m_EEGInputField.text;
                UpdateState();
            }
        }

        public void OnEndEditPOSPath()
        {
            if (!IsSettingFields)
            {
                m_object.POS = m_PosInputField.text;
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