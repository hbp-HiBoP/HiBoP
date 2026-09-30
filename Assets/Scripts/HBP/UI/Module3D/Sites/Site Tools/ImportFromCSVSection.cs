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
using System.Threading.Tasks;
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

        private async UniTask ImportSitesAsync(string csvPath, Action<float, float, LoadingText> updateProgress, CancellationToken token, Func<Task> beforeBackgroundParsing = null)
        {
            Base3DScene scene = Scene;
            bool importHighlighted = m_ImportHighlighted.isOn;
            bool importBlacklisted = m_ImportBlacklisted.isOn;
            bool importColor = m_ImportColor.isOn;
            bool importLabels = m_ImportLabels.isOn;
            bool mergeLabels = m_LabelsImportModeDropdown.value == 1;
            List<Column3D> columns = m_ScopeDropdown.value == 0 ? new() { scene.SelectedColumn } : scene.Columns.ToList();
            var targets = columns.Where(column => column != null).SelectMany(column => column.Sites.Where(site => !site.State.IsMasked && (ApplyFor != ApplyFor.FilteredSites || site.State.IsFiltered)).Select(site => new SiteImportTarget(column, site))).ToList();

            await UniTask.SwitchToThreadPool();
            if (beforeBackgroundParsing != null) await beforeBackgroundParsing();
            token.ThrowIfCancellationRequested();

            // Regex pattern to parse CSV correctly (respecting quotes)
            Regex csvParser = new(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

            using StreamReader sr = new(csvPath);
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

            Dictionary<(string ColumnId, string SiteId), ImportedSiteState> stateBySite = new();
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
                SiteState state = new();

                // Get highlighted state
                if (importHighlighted && highlightedIndex != -1 && values.Length > highlightedIndex)
                {
                    bool.TryParse(values[highlightedIndex], out bool highlighted);
                    state.IsHighlighted = highlighted;
                }

                // Get blacklisted state
                if (importBlacklisted && blacklistedIndex != -1 && values.Length > blacklistedIndex)
                {
                    bool.TryParse(values[blacklistedIndex], out bool blacklisted);
                    state.IsBlackListed = blacklisted;
                }

                // Get color
                if (importColor && colorIndex != -1 && values.Length > colorIndex)
                {
                    ColorUtility.TryParseHtmlString(values[colorIndex], out Color color);
                    state.Color = color;
                }

                // Get labels
                if (importLabels && labelsIndex != -1 && values.Length > labelsIndex)
                {
                    string labelsString = values[labelsIndex].Trim(' ', '"');
                    state.Labels = labelsString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                }

                // Store the state for the corresponding sites
                List<SiteImportTarget> sitesToApply = targets.Where(target => target.SiteId.Equals(siteID, StringComparison.OrdinalIgnoreCase)).ToList();
                if (sitesToApply.Count == 0) sitesToApply = targets.Where(target => target.FullName.Equals(siteID, StringComparison.OrdinalIgnoreCase)).ToList();
                var importedState = new ImportedSiteState(state.IsHighlighted, state.IsBlackListed, state.Color, state.Labels.ToArray());
                foreach (SiteImportTarget target in sitesToApply)
                {
                    stateBySite[(target.ColumnId, target.SiteId)] = importedState;
                }

                processedLines++;
                progress = (float)processedLines / totalLines;
                updateProgress(progress, 0, new LoadingText($"Processing site {processedLines}/{totalLines}"));
            }

            // Apply states to the sites
            sr.Dispose();
            await UniTask.SwitchToMainThread();
            token.ThrowIfCancellationRequested();
            if (!scene) throw new InvalidOperationException("The scene was closed while importing site states.");
            var changes = new List<SiteConfigurationChange>(stateBySite.Count);
            foreach (SiteImportTarget target in targets)
            {
                if (!stateBySite.TryGetValue((target.ColumnId, target.SiteId), out ImportedSiteState imported)) continue;
                Column3D column = scene.Columns.FirstOrDefault(candidate => candidate && StringComparer.Ordinal.Equals(candidate.ColumnData?.ID, target.ColumnId));
                Site site = column?.Sites.FirstOrDefault(candidate => candidate && StringComparer.OrdinalIgnoreCase.Equals(candidate.Information.FullID, target.SiteId));
                if (!column || !site || site.State == null) continue;
                bool highlighted = importHighlighted ? imported.Highlighted : site.State.IsHighlighted;
                bool blacklisted = importBlacklisted ? imported.Blacklisted : site.State.IsBlackListed;
                Color color = importColor ? imported.Color : site.State.Color;
                string[] labels = site.State.Labels.ToArray();
                if (importLabels)
                {
                    if (!mergeLabels)
                    {
                        labels = imported.Labels;
                    }
                    else
                    {
                        var merged = labels.ToList();
                        foreach (string label in imported.Labels)
                            if (!merged.Contains(label))
                                merged.Add(label);
                        labels = merged.ToArray();
                    }
                }

                var configuration = new Core.Data.SiteConfiguration(blacklisted, highlighted, color, labels);
                if (configuration.IsBlacklisted == site.State.IsBlackListed && configuration.IsHighlighted == site.State.IsHighlighted && configuration.Color == site.State.Color && configuration.Labels.SequenceEqual(site.State.Labels)) continue;
                changes.Add(new SiteConfigurationChange(column, target.SiteId, site.State, configuration));
            }

            scene.ApplySiteConfigurationBatch(changes);
        }

        private sealed class SiteImportTarget
        {
            public string ColumnId { get; }
            public string SiteId { get; }
            public string FullName { get; }

            public SiteImportTarget(Column3D column, Site site)
            {
                ColumnId = column.ColumnData.ID;
                SiteId = site.Information.FullID;
                FullName = site.Information.FullName;
            }
        }

        private readonly struct ImportedSiteState
        {
            public bool Highlighted { get; }
            public bool Blacklisted { get; }
            public Color Color { get; }
            public string[] Labels { get; }

            public ImportedSiteState(bool highlighted, bool blacklisted, Color color, string[] labels)
            {
                Highlighted = highlighted;
                Blacklisted = blacklisted;
                Color = color;
                Labels = labels;
            }
        }

        #endregion
    }
}
