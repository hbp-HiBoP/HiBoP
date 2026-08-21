using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.Core.Database;
using HBP.Data.Informations;
using HBP.UI.Database;
using HBP.UI.Tools;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class OpenTrialMatrixExplorerSection : SiteToolSection
    {
        #region Enums

        public enum DataSource
        {
            Project,
            Database
        }

        #endregion

        #region Properties

        [SerializeField] private Dropdown m_DataSourceDropdown;
        [SerializeField] private Dropdown m_DataNameDropdown;
        List<IEEGDataInfo> m_IEEGDataInfos = new();
        private int m_DataNameRefreshGeneration;

        static string m_DataNameDropdownValue;
        static DataSource m_DataSourceDropdownValue = DataSource.Project;

        #endregion

        #region Private Methods

        private void UpdateDataNameDropdown()
        {
            int generation = ++m_DataNameRefreshGeneration;
            DataSource source = (DataSource)m_DataSourceDropdown.value;
            if (source == DataSource.Database)
            {
                UpdateDatabaseDataNamesAsync(generation).Forget();
                return;
            }

            SetDataInfos(source == DataSource.Project ? ApplicationState.LoadedProject.Datasets.SelectMany(dataset => dataset.GetIEEGDataInfos()).ToList() : new List<IEEGDataInfo>());
        }

        private async UniTaskVoid UpdateDatabaseDataNamesAsync(int generation)
        {
            GlobalDatabase database = DatabaseManager.Database;
            if (!await HBP.UI.Database.DatabaseWorkflow.EnsureDatabaseReadyAndInformAsync()) return;

            await UniTask.SwitchToMainThread();
            if (generation != m_DataNameRefreshGeneration || (DataSource)m_DataSourceDropdown.value != DataSource.Database)
            {
                return;
            }

            SetDataInfos(database.DataInfos.OfType<IEEGDataInfo>().ToList());
        }

        private void SetDataInfos(List<IEEGDataInfo> dataInfos)
        {
            m_IEEGDataInfos = dataInfos;
            m_DataNameDropdown.options = m_IEEGDataInfos.Select(d => d.Name).Distinct().OrderBy(name => name).Select(dataName => new Dropdown.OptionData(dataName)).ToList();
            int index = m_IEEGDataInfos.FindIndex(info => info.Name == m_DataNameDropdownValue);
            m_DataNameDropdown.SetValueWithoutNotify(index >= 0 && index < m_DataNameDropdown.options.Count ? index : 0);
            m_DataNameDropdown.RefreshShownValue();
        }

        #endregion

        #region Public Methods

        public override void Initialize()
        {
            base.Initialize();

            m_DataSourceDropdown.onValueChanged.AddListener((value) => UpdateDataNameDropdown());
        }

        public override async UniTask ApplyAsync()
        {
            await UniTask.SwitchToMainThread();

            if ((DataSource)m_DataSourceDropdown.value == DataSource.Database)
            {
                GlobalDatabase database = DatabaseManager.Database;
                SetDataInfos(database.DataInfos.OfType<IEEGDataInfo>().ToList());
            }

            List<ChannelStruct> channelStructs = Sites.Select(s => new ChannelStruct(s)).Distinct().ToList();

            if (channelStructs.Count == 0)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "No sites selected", "Please select at least one site to open the trial matrix explorer.").Forget();
                return;
            }

            if (m_DataNameDropdown.value < 0 || m_DataNameDropdown.value >= m_DataNameDropdown.options.Count)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Invalid data selection", "Please select a valid data name from the dropdown.").Forget();
                return;
            }

            string selectedDataName = m_DataNameDropdown.options[m_DataNameDropdown.value].text;

            if (!m_IEEGDataInfos.Any(info => info.Name == selectedDataName))
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Data not found", $"No IEEG data found with the name '{selectedDataName}'.").Forget();
                return;
            }

            List<Patient> selectedPatients = channelStructs.Select(channelStruct => channelStruct.Patient).Distinct().ToList();
            List<IEEGDataInfo> selectedDataInfos = TrialMatrixDisplayer.SelectDataInfos(m_IEEGDataInfos, selectedPatients, selectedDataName);
            if (selectedDataInfos.Count == 0)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Data not found", $"No IEEG data named '{selectedDataName}' was found for the selected patients.").Forget();
                return;
            }

            ValidationRequest validationRequest = new(ValidationAspect.SourceAvailability | ValidationAspect.SourceReadability | ValidationAspect.Epoching | ValidationAspect.ChannelMapping, dataInfoIDs: selectedDataInfos.Select(info => info.ID));
            try
            {
                if ((DataSource)m_DataSourceDropdown.value == DataSource.Database)
                {
                    GlobalDatabase database = DatabaseManager.Database;
                    if (database.RequiresValidation(validationRequest))
                    {
                        await LoadingManager.LoadAsync((update, token) => database.EnsureDatabaseValidatedForImmediateLoadAsync(validationRequest, update, token));
                    }
                }
                else
                {
                    Project project = ApplicationState.LoadedProject;
                    if (project != null && project.RequiresValidation(validationRequest))
                    {
                        await LoadingManager.LoadAsync((update, token) => project.EnsureProjectValidatedForImmediateLoadAsync(validationRequest, update, token));
                    }
                }
            }
            catch (System.Exception)
            {
                return;
            }

            OpenTrialMatrixExplorer(channelStructs, selectedDataInfos, selectedDataName);
        }

        protected virtual void OpenTrialMatrixExplorer(List<ChannelStruct> channelStructs, List<IEEGDataInfo> dataInfos, string dataName)
        {
            var trialMatrixExplorerWindow = WindowsManager.Open("Trial matrix explorer window", null) as TrialMatrixExplorerWindow;
            trialMatrixExplorerWindow.SetWithPredefinedData(channelStructs, dataInfos, dataName);
        }

        public override void StoreSettings()
        {
            m_DataSourceDropdownValue = (DataSource)m_DataSourceDropdown.value;
            int index = m_DataNameDropdown.value >= 0 && m_DataNameDropdown.value < m_DataNameDropdown.options.Count ? m_DataNameDropdown.value : 0;
            if (m_DataNameDropdown.options.Count > 0) m_DataNameDropdownValue = m_DataNameDropdown.options[index].text;
        }

        public override void LoadSettings()
        {
            m_DataSourceDropdown.Set(typeof(DataSource), (int)m_DataSourceDropdownValue);
        }

        #endregion
    }
}
