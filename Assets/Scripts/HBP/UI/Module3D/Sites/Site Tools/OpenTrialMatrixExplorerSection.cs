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
        public enum DataSource { Project, Database }
        #endregion

        #region Properties
        [SerializeField] private Dropdown m_DataSourceDropdown;
        [SerializeField] private Dropdown m_DataNameDropdown;
        List<IEEGDataInfo> m_IEEGDataInfos = new();

        static string m_DataNameDropdownValue;
        static DataSource m_DataSourceDropdownValue = DataSource.Project;
        #endregion

        #region Private Methods
        private void UpdateDataNameDropdown()
        {
            m_IEEGDataInfos = (DataSource)m_DataSourceDropdown.value switch
            {
                DataSource.Project => ApplicationState.LoadedProject.Datasets.SelectMany(ds => ds.GetIEEGDataInfos()).ToList(),
                DataSource.Database => DatabaseManager.Database.DataInfos.OfType<IEEGDataInfo>().ToList(),
                _ => new List<IEEGDataInfo>(),
            };
            m_DataNameDropdown.options = m_IEEGDataInfos.Select(d => d.Name).Distinct().OrderBy(name => name).Select(dataName => new Dropdown.OptionData(dataName)).ToList();
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

            var trialMatrixExplorerWindow = WindowsManager.Open("Trial matrix explorer window", null) as TrialMatrixExplorerWindow;
            trialMatrixExplorerWindow.SetWithPredefinedData(channelStructs, m_IEEGDataInfos, selectedDataName);
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
            UpdateDataNameDropdown();
            int index = m_IEEGDataInfos.FindIndex(info => info.Name == m_DataNameDropdownValue);
            m_DataNameDropdown.value = index >= 0 && index < m_DataNameDropdown.options.Count ? index : 0;
        }
        #endregion
    }
}