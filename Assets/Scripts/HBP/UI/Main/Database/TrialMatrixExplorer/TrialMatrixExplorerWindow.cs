using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.Core.Database;
using HBP.Data.Informations;
using HBP.UI.Tools;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Database
{
    public class TrialMatrixExplorerWindow : DialogWindow
    {
        #region Properties
        [SerializeField] private Button m_SelectPatientsButton;
        [SerializeField] private Text m_PatientsSelectedText;
        [SerializeField] private Dropdown m_DataDropdown;
        [SerializeField] private Button m_DisplayMatrixButton;
        [SerializeField] private TrialMatrixDisplayer m_TrialMatrixDisplayer;
        [SerializeField] private Transform m_ConfigurationContainer;

        List<Patient> m_AvailablePatients;
        List<Patient> m_SelectedPatients = new();
        
        List<string> m_AvailableDataNames;
        string m_SelectedDataName;
        #endregion

        #region Public Methods
        public void SetWithPredefinedData(List<ChannelStruct> channelStructs, List<IEEGDataInfo> dataInfos, string dataName)
        {
            if (channelStructs == null || channelStructs.Count == 0)
            {
                Debug.LogWarning("No channels provided to SetWithChannels");
                return;
            }

            m_SelectedDataName = dataName;
            m_SelectedPatients = channelStructs.Select(cs => cs.Patient).Distinct().ToList();

            // Disable configuration UI elements
            m_ConfigurationContainer.gameObject.SetActive(false);

            // Set the displayer with predefined channels and start loading automatically
            m_TrialMatrixDisplayer.Set(channelStructs, dataInfos, dataName);
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_SelectPatientsButton.onClick.AddListener(OpenPatientSelector);
            m_DataDropdown.onValueChanged.AddListener(OnChangeDataName);
            m_DisplayMatrixButton.onClick.AddListener(DisplayTrialMatrices);
        }
        protected override void SetFields()
        {
            base.SetFields();

            SetAvailablePatients();
            SetAvailableDataNames();
            UpdateUI();
        }
        protected void SetAvailablePatients()
        {
            m_AvailablePatients = DatabaseManager.Database.Patients.Where(p => DatabaseManager.Database.DataInfos.OfType<IEEGDataInfo>().Any(d => d.Patient == p)).OrderBy(p => p.Name).ToList();
        }
        protected void SetAvailableDataNames()
        {
            m_AvailableDataNames = DatabaseManager.Database.DataInfos.OfType<IEEGDataInfo>().Select(d => d.Name).Distinct().OrderBy(name => name).ToList();
            m_DataDropdown.options = m_AvailableDataNames.Select(name => new Dropdown.OptionData(name)).ToList();
            m_DataDropdown.SetValue(0);
            m_SelectedDataName = m_AvailableDataNames.FirstOrDefault();
        }
        protected void OpenPatientSelector()
        {
            ObjectSelector<Patient> selector = WindowsManager.OpenSelector(m_AvailablePatients, this);
            selector.ObjectsSelected = m_SelectedPatients.ToArray();
            selector.OnOk.AddListener(() => OnPatientsSelected(selector.ObjectsSelected));
            WindowsReferencer.Add(selector);
        }
        protected void OnPatientsSelected(Patient[] selectedPatients)
        {
            m_SelectedPatients = selectedPatients.ToList();
            UpdateUI();
        }
        protected void OnChangeDataName(int index)
        {
            m_SelectedDataName = m_AvailableDataNames[index];
        }
        protected void UpdateUI()
        {
            // Update patients text
            if (m_SelectedPatients.Count == 0)
            {
                m_PatientsSelectedText.text = "No patients selected";
            }
            else if (m_SelectedPatients.Count == 1)
            {
                m_PatientsSelectedText.text = "1 patient selected";
            }
            else
            {
                m_PatientsSelectedText.text = $"{m_SelectedPatients.Count} patients selected";
            }
            
            // Enable/disable display button
            m_DisplayMatrixButton.interactable = m_SelectedPatients.Count > 0 && !string.IsNullOrEmpty(m_SelectedDataName);
        }
        protected void DisplayTrialMatrices()
        {
            if (m_SelectedPatients.Count > 0 && !string.IsNullOrEmpty(m_SelectedDataName))
            {
                m_TrialMatrixDisplayer.Set(m_SelectedPatients, m_SelectedDataName);
            }
        }
        #endregion
    }
}