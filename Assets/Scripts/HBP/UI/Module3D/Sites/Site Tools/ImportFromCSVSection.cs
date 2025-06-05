using Cysharp.Threading.Tasks;
using HBP.Core.Object3D;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.UI.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class ImportFromCSVSection : SiteToolSection
    {
        #region Properties
        [SerializeField] private Toggle m_ImportHighlighted;
        [SerializeField] private Toggle m_ImportBlacklisted;
        [SerializeField] private Toggle m_ImportColor;
        [SerializeField] private Toggle m_ImportLabels;
        [SerializeField] private Dropdown m_LabelsImportModeDropdown;
        [SerializeField] private Dropdown m_ScopeDropdown;

        protected override List<Site> Sites
        {
            get
            {
                List<Column3D> columns = m_ScopeDropdown.value == 0 ? new() { Scene.SelectedColumn } : Scene.Columns;
                return ApplyFor switch
                {
                    ApplyFor.FilteredSites => columns.SelectMany(c => c.Sites).Where(s => s.State.IsFiltered && !s.State.IsMasked).ToList(),
                    ApplyFor.AllSites => columns.SelectMany(c => c.Sites).Where(s => !s.State.IsMasked).ToList(),
                    _ => throw new ArgumentOutOfRangeException(nameof(ApplyFor), ApplyFor, null),
                };
            }
        }

        static bool m_ImportHighlightedValue;
        static bool m_ImportBlacklistedValue;
        static bool m_ImportColorValue;
        static bool m_ImportLabelsValue;
        static int m_LabelsImportModeValue;
        static int m_ScopeDropdownValue;
        #endregion

        #region Public Methods
        public override async UniTask ApplyAsync()
        {
            string csvPath = await FileBrowser.GetExistingFileNameAsync(new string[] { "csv" }, "Load site states from");
            if (!string.IsNullOrEmpty(csvPath))
            {
                try
                {
                    await LoadingManager.LoadAsync((update, token) => ImportSitesAsync(csvPath, update, token));
                    DialogBoxManager.Open(Core.Enums.DialogBoxType.Informational, "Sites imported", "The site states have been successfully imported from " + csvPath).Forget();
                }
                catch (Core.Exceptions.HBPException e)
                {
                    DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, e.Title, e.Message).Forget();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Import error", "An error occurred during import: " + e.Message).Forget();
                }
            }
        }
        public override void StoreSettings()
        {
            m_ImportHighlightedValue = m_ImportHighlighted.isOn;
            m_ImportBlacklistedValue = m_ImportBlacklisted.isOn;
            m_ImportColorValue = m_ImportColor.isOn;
            m_ImportLabelsValue = m_ImportLabels.isOn;
            m_LabelsImportModeValue = m_LabelsImportModeDropdown.value;
            m_ScopeDropdownValue = m_ScopeDropdown.value;
        }
        public override void LoadSettings()
        {
            m_ImportHighlighted.isOn = m_ImportHighlightedValue;
            m_ImportBlacklisted.isOn = m_ImportBlacklistedValue;
            m_ImportColor.isOn = m_ImportColorValue;
            m_ImportLabels.isOn = m_ImportLabelsValue;
            m_LabelsImportModeDropdown.value = m_LabelsImportModeValue;
            m_ScopeDropdown.value = m_ScopeDropdownValue;
        }
        #endregion

        #region Private Methods
        private async UniTask ImportSitesAsync(string csvPath, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            await UniTask.SwitchToThreadPool();

            var sites = Sites;

            token.ThrowIfCancellationRequested();

            bool mergeLabels = m_LabelsImportModeDropdown.value == 1;

            // Regex pattern to parse CSV correctly (respecting quotes)
            Regex csvParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

            using StreamReader sr = new StreamReader(csvPath);
            // Parse header
            string headerLine = sr.ReadLine() ?? throw new Core.Exceptions.HBPException("Import error", "The CSV file is empty");
            string[] headers = csvParser.Split(headerLine);

            // Find indices of key columns
            int siteIndex = Array.FindIndex(headers, h => h.Equals("Site", StringComparison.OrdinalIgnoreCase));
            int highlightedIndex = Array.FindIndex(headers, h => h.Equals("Highlighted", StringComparison.OrdinalIgnoreCase));
            int blacklistedIndex = Array.FindIndex(headers, h => h.Equals("Blacklisted", StringComparison.OrdinalIgnoreCase));
            int colorIndex = Array.FindIndex(headers, h => h.Equals("Color", StringComparison.OrdinalIgnoreCase));
            int labelsIndex = Array.FindIndex(headers, h => h.Equals("Labels", StringComparison.OrdinalIgnoreCase));

            // Validate that we have at least the site ID column
            if (siteIndex == -1)
                throw new Core.Exceptions.HBPException("Import error", "The CSV file does not contain a 'Site' column.");

            float progress = 0;
            int totalLines = File.ReadAllLines(csvPath).Length - 1; // Subtract header line
            int processedLines = 0;

            Dictionary<Site, SiteState> stateBySite = new();
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                token.ThrowIfCancellationRequested();

                if (string.IsNullOrEmpty(line)) continue;

                // Parse CSV line using regex
                string[] values = csvParser.Split(line);

                // Get site ID
                if (values.Length <= siteIndex) continue;
                string siteID = values[siteIndex];

                // Remove quotes if present
                siteID = siteID.Trim(' ', '"');

                // Process attributes to import
                SiteState state = new SiteState();

                // Get highlighted state
                if (m_ImportHighlighted.isOn && highlightedIndex != -1 && values.Length > highlightedIndex)
                {
                    bool.TryParse(values[highlightedIndex], out bool highlighted);
                    state.IsHighlighted = highlighted;
                }

                // Get blacklisted state
                if (m_ImportBlacklisted.isOn && blacklistedIndex != -1 && values.Length > blacklistedIndex)
                {
                    bool.TryParse(values[blacklistedIndex], out bool blacklisted);
                    state.IsBlackListed = blacklisted;
                }

                // Get color
                if (m_ImportColor.isOn && colorIndex != -1 && values.Length > colorIndex)
                {
                    ColorUtility.TryParseHtmlString(values[colorIndex], out Color color);
                    state.Color = color;
                }

                // Get labels
                if (m_ImportLabels.isOn && labelsIndex != -1 && values.Length > labelsIndex)
                {
                    string labelsString = values[labelsIndex].Trim(' ', '"');
                    state.Labels = labelsString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                }

                // Store the state for the corresponding sites
                IEnumerable<Site> sitesToApply = sites.Where(s => s.Information.FullID.Equals(siteID, StringComparison.OrdinalIgnoreCase));
                foreach (var site in sitesToApply)
                {
                    stateBySite[site] = state;
                }

                processedLines++;
                progress = (float)processedLines / totalLines;
                updateProgress(progress, 0, new LoadingText($"Processing site {processedLines}/{totalLines}"));
            }

            // Apply states to the sites
            await UniTask.SwitchToMainThread();
            foreach (var kv in stateBySite)
            {
                kv.Key.State.ApplySpecificState(m_ImportHighlighted.isOn, kv.Value.IsHighlighted, m_ImportBlacklisted.isOn, kv.Value.IsBlackListed, m_ImportColor.isOn, kv.Value.Color, m_ImportLabels.isOn, kv.Value.Labels, mergeLabels);
            }
        }
        #endregion
    }
}