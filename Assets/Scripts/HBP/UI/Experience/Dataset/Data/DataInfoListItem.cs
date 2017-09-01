using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using Tools.Unity;
using HBP.Data.Experience.Dataset;

namespace HBP.UI.Experience.Dataset
{
    /// <summary>
    /// The Script which manage the dataInfo list panel.
    /// </summary>
    public class DataInfoListItem : Tools.Unity.Lists.SavableItem<Data.Experience.Dataset.DataInfo>
    {
		#region Properties
		/// <summary>
		/// The label inputField.
		/// </summary>
		[SerializeField] InputField m_LabelInputField;
        /// <summary>
        /// The Measurelabel inputField.
        /// </summary>
        [SerializeField] InputField m_MeasureLabelInputField;
        /// <summary>
        /// The EEG fileSelector.
        /// </summary>
        [SerializeField] FileSelector m_EEGFileSelector;
        /// <summary>
        /// The POS fileSelector.
        /// </summary>
        [SerializeField] FileSelector m_POSFileSelector;
        /// <summary>
        /// The protocol visualization dropdown.
        /// </summary>
        [SerializeField] Dropdown m_ProtocolDropdown;
		/// <summary>
		/// The patient dropdown.
		/// </summary>
		[SerializeField] Dropdown m_PatientDropdown;
		/// <summary>
		/// The state toggle.
		/// </summary>
		[SerializeField] Image m_IsOk;
        [SerializeField] Text m_ErrorText;

        public override DataInfo Object
        {
            get
            {
                return base.Object;
            }

            set
            {
                base.Object = value;
                m_LabelInputField.text = value.Name;
                m_LabelInputField.onValueChanged.AddListener((name) => { value.Name = name; m_Object.GetNameErrors(); SetColor(value.isOk); });

                m_MeasureLabelInputField.text = value.Measure;
                m_MeasureLabelInputField.onValueChanged.AddListener((measure) => { value.Measure = measure; m_Object.GetMeasureErrors(); SetColor(value.isOk); });

                m_EEGFileSelector.File = value.EEG;
                m_EEGFileSelector.onValueChanged.AddListener((eeg) => { value.EEG = eeg; m_Object.GetEEGErrors(); SetColor(value.isOk); });

                m_POSFileSelector.File = value.POS;
                m_POSFileSelector.onValueChanged.AddListener((pos) => { value.POS = pos; m_Object.GetPOSErrors(); SetColor(value.isOk); });

                m_ProtocolDropdown.options = (from protocol in ApplicationState.ProjectLoaded.Protocols select new Dropdown.OptionData(protocol.Name, null)).ToList();
                m_ProtocolDropdown.value = Mathf.Max(0, ApplicationState.ProjectLoaded.Protocols.IndexOf(value.Protocol));
                m_ProtocolDropdown.onValueChanged.AddListener((protocol) => { value.Protocol = ApplicationState.ProjectLoaded.Protocols[protocol]; m_Object.GetProtocolErrors(); SetColor(value.isOk); });

                m_PatientDropdown.options = (from patient in ApplicationState.ProjectLoaded.Patients select new Dropdown.OptionData(patient.Name, null)).ToList();
                m_PatientDropdown.value = Mathf.Max(0, ApplicationState.ProjectLoaded.Patients.IndexOf(value.Patient));
                m_PatientDropdown.onValueChanged.AddListener((patient) => { value.Patient = ApplicationState.ProjectLoaded.Patients[patient]; m_Object.GetPatientErrors(); SetColor(value.isOk); });

                SetColor(value.isOk);
            }
        }
        #endregion

        #region Public Methods
        public override void Save()
        {
            Object.Name = m_LabelInputField.text;
            Object.Measure = m_MeasureLabelInputField.text;
            Object.EEG = m_EEGFileSelector.File;
            Object.POS = m_POSFileSelector.File;
            Object.Patient = ApplicationState.ProjectLoaded.Patients[m_PatientDropdown.value];
            Object.Protocol = ApplicationState.ProjectLoaded.Protocols[m_ProtocolDropdown.value];
        }
        public void SetErrors()
        {
            m_ErrorText.text = Object.GetErrorsMessage();
        }
        #endregion

        #region Protected Methods
        protected void SetColor(bool isOk)
        {
            if(isOk)
            {
                m_IsOk.color = ApplicationState.Theme.Color.OK;
            }
            else
            {
                m_IsOk.color = ApplicationState.Theme.Color.Error;
            }
        }

        #endregion
    }
}