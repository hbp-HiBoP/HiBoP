using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Exceptions;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Core.Preferences;
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
    public class ExportToCSVSection : SiteToolSection
    {
        #region Properties

        [SerializeField] private Toggle m_ExportHighlighted;
        [SerializeField] private Toggle m_ExportBlacklisted;
        [SerializeField] private Toggle m_ExportColor;
        [SerializeField] private Toggle m_ExportLabels;
        [SerializeField] private Toggle m_ExportPosition;
        [SerializeField] private Toggle m_ExportData;
        [SerializeField] private Toggle m_ExportTags;
        [SerializeField] private Dropdown m_ExportModeDropdown;

        static bool m_ExportHighlightedValue;
        static bool m_ExportBlacklistedValue;
        static bool m_ExportColorValue;
        static bool m_ExportLabelsValue;
        static bool m_ExportPositionValue;
        static bool m_ExportDataValue;
        static bool m_ExportTagsValue;
        static int m_ExportModeValue;

        #endregion

        #region Public Methods

        public override async UniTask ApplyAsync()
        {
            var request = new ExportRequest(this);
            if (!request.Merge)
            {
                // Create new file mode
                string csvPath = await HBP.UI.Toolbar.ToolbarExternalActions.GetSavedFileNameAsync(new string[] { "csv" }, "Save sites to");
                if (!string.IsNullOrEmpty(csvPath))
                {
                    await LoadingManager.LoadAsync((update, token) => ExportSitesAsync(request, csvPath, update, token));
                    DialogBoxManager.Open(Core.Enums.DialogBoxType.Informational, "Sites exported", "The filtered sites have been sucessfully exported to " + csvPath).Forget();
                }
            }
            else
            {
                // Merge with existing file mode
                string csvPath = await HBP.UI.Toolbar.ToolbarExternalActions.GetExistingFileNameAsync(new string[] { "csv" }, "Select CSV file to merge with");
                if (!string.IsNullOrEmpty(csvPath))
                {
                    await LoadingManager.LoadAsync((update, token) => MergeSitesWithExistingCSVAsync(request, csvPath, update, token));
                    DialogBoxManager.Open(Core.Enums.DialogBoxType.Informational, "Sites merged", "The filtered sites have been successfully merged with " + csvPath).Forget();
                }
            }
        }

        public override void StoreSettings()
        {
            m_ExportHighlightedValue = m_ExportHighlighted.isOn;
            m_ExportBlacklistedValue = m_ExportBlacklisted.isOn;
            m_ExportColorValue = m_ExportColor.isOn;
            m_ExportLabelsValue = m_ExportLabels.isOn;
            m_ExportPositionValue = m_ExportPosition.isOn;
            m_ExportDataValue = m_ExportData.isOn;
            m_ExportTagsValue = m_ExportTags.isOn;
            m_ExportModeValue = m_ExportModeDropdown.value;
        }

        public override void LoadSettings()
        {
            m_ExportHighlighted.isOn = m_ExportHighlightedValue;
            m_ExportBlacklisted.isOn = m_ExportBlacklistedValue;
            m_ExportColor.isOn = m_ExportColorValue;
            m_ExportLabels.isOn = m_ExportLabelsValue;
            m_ExportPosition.isOn = m_ExportPositionValue;
            m_ExportData.isOn = m_ExportDataValue;
            m_ExportTags.isOn = m_ExportTagsValue;
            m_ExportModeDropdown.value = m_ExportModeValue;
        }

        #endregion

        #region Private Methods

        private async UniTask ExportSitesAsync(ExportRequest request, string csvPath, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            // Prepare data and generate CSV file
            System.Text.StringBuilder csvBuilder = await PrepareCSVContentAsync(request, updateProgress, token);

            // Write the CSV file
            token.ThrowIfCancellationRequested();
            using StreamWriter sw = new(csvPath);
            sw.Write(csvBuilder.ToString());
        }

        private async UniTask<System.Text.StringBuilder> PrepareCSVContentAsync(ExportRequest request, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            int length = request.Sites.Length;
            float progress = 0;

            // Prepare DataInfo by Patient for performance increase
            await UniTask.SwitchToThreadPool();
            Dictionary<Patient, DataInfo> dataInfoByPatient = new();

            if (request.Data)
            {
                for (int i = 0; i < length; i++)
                {
                    token.ThrowIfCancellationRequested();
                    ExportSite site = request.Sites[i];
                    if (!dataInfoByPatient.ContainsKey(site.Information.Patient))
                    {
                        if (request.Column is IEEGColumn columnIEEG)
                        {
                            DataInfo dataInfo = request.Visualization.GetDataInfo(site.Information.Patient, columnIEEG);
                            dataInfoByPatient.Add(site.Information.Patient, dataInfo);
                        }
                        else if (request.Column is CCEPColumn columnCCEP)
                        {
                            DataInfo dataInfo = request.Visualization.GetDataInfo(site.Information.Patient, columnCCEP);
                            dataInfoByPatient.Add(site.Information.Patient, dataInfo);
                        }
                        else if (request.Column is StaticColumn columnStatic)
                        {
                            DataInfo dataInfo = request.Visualization.GetDataInfo(site.Information.Patient, columnStatic);
                            dataInfoByPatient.Add(site.Information.Patient, dataInfo);
                        }
                    }

                    progress = 0.5f * ((float)(i + 1) / length);
                    updateProgress(progress, 0, new LoadingText(""));
                }
            }

            // Create string builder
            token.ThrowIfCancellationRequested();
            System.Text.StringBuilder csvBuilder = new();

            // Generate header
            System.Text.StringBuilder headerBuilder = new("Site");
            if (request.Highlighted) headerBuilder.Append(",Highlighted");
            if (request.Blacklisted) headerBuilder.Append(",Blacklisted");
            if (request.Color) headerBuilder.Append(",Color");
            if (request.Labels) headerBuilder.Append(",Labels");
            if (request.Position) headerBuilder.Append(",X,Y,Z,CoordSystem");
            if (request.Data) headerBuilder.Append(",DataType,DataFiles");
            if (request.Tags)
            {
                List<BaseTag> tags = request.TagDefinitions;
                if (tags.Count != 0)
                {
                    headerBuilder.Append(",");
                    headerBuilder.Append(string.Join(",", tags.Select(t => !t.Name.Contains(",") ? t.Name : string.Format("\"{0}\"", t.Name))));
                }
            }

            csvBuilder.AppendLine(headerBuilder.ToString());

            // Prepare sites positions for performance increase

            for (int i = 0; i < length; i++)
            {
                token.ThrowIfCancellationRequested();

                // Get required values
                ExportSite site = request.Sites[i];
                Vector3 sitePosition = site.Position;

                // Build row
                System.Text.StringBuilder rowBuilder = new();
                rowBuilder.Append(site.Information.FullID);

                // Append site state data based on export flags
                if (request.Highlighted) rowBuilder.AppendFormat(",{0}", site.State.IsHighlighted);
                if (request.Blacklisted) rowBuilder.AppendFormat(",{0}", site.State.IsBlacklisted);
                if (request.Color) rowBuilder.AppendFormat(",{0}", site.State.Color.ToHexString());
                if (request.Labels) rowBuilder.AppendFormat(",{0}", string.Join(";", site.State.Labels));
                if (request.Position)
                {
                    rowBuilder.AppendFormat(",{0},{1},{2},{3}", sitePosition.x.ToString("N2", System.Globalization.CultureInfo.InvariantCulture), sitePosition.y.ToString("N2", System.Globalization.CultureInfo.InvariantCulture), sitePosition.z.ToString("N2", System.Globalization.CultureInfo.InvariantCulture), request.ImplantationName);
                }

                if (request.Data)
                {
                    DataInfo dataInfo = null;
                    if ((request.Column is IEEGColumn || request.Column is CCEPColumn || request.Column is StaticColumn) && dataInfoByPatient.ContainsKey(site.Information.Patient))
                    {
                        dataInfo = dataInfoByPatient[site.Information.Patient];
                    }

                    string dataType = "", dataFiles = "";
                    if (dataInfo != null)
                    {
                        if (dataInfo.DataContainer is Core.Data.Container.BrainVision brainVisionDataContainer)
                        {
                            dataType = "BrainVision";
                            dataFiles = string.Join(";", new string[] { brainVisionDataContainer.Header }.Where(s => !string.IsNullOrEmpty(s)));
                        }
                        else if (dataInfo.DataContainer is Core.Data.Container.EDF edfDataContainer)
                        {
                            dataType = "EDF";
                            dataFiles = string.Join(";", new string[] { edfDataContainer.File }.Where(s => !string.IsNullOrEmpty(s)));
                        }
                        else if (dataInfo.DataContainer is Core.Data.Container.Elan elanDataContainer)
                        {
                            dataType = "ELAN";
                            dataFiles = string.Join(";", new string[] { elanDataContainer.EEG, elanDataContainer.POS, elanDataContainer.Notes }.Where(s => !string.IsNullOrEmpty(s)));
                        }
                        else if (dataInfo.DataContainer is Core.Data.Container.Micromed micromedDataContainer)
                        {
                            dataType = "Micromed";
                            dataFiles = string.Join(";", new string[] { micromedDataContainer.Path }.Where(s => !string.IsNullOrEmpty(s)));
                        }
                        else if (dataInfo.DataContainer is Core.Data.Container.FIF fifDataContainer)
                        {
                            dataType = "FIF";
                            dataFiles = string.Join(";", new string[] { fifDataContainer.File }.Where(s => !string.IsNullOrEmpty(s)));
                        }
                    }

                    rowBuilder.AppendFormat(",{0},{1}", dataType, dataFiles);
                }

                if (request.Tags)
                {
                    List<BaseTag> tags = request.TagDefinitions;
                    IEnumerable<BaseTagValue> tagValues = tags.Select(t => site.Information.SiteData.Tags.FirstOrDefault(tv => tv.Tag == t));
                    foreach (var tagValue in tagValues)
                    {
                        rowBuilder.Append(",");
                        if (tagValue != null)
                        {
                            string value = tagValue.DisplayableValue;
                            if (value.Contains(","))
                            {
                                value = string.Format("\"{0}\"", value);
                            }

                            rowBuilder.Append(value);
                        }
                    }
                }

                // Add the complete row to the CSV builder
                csvBuilder.AppendLine(rowBuilder.ToString());

                progress = 0.5f * (1 + (float)(i + 1) / length);
                updateProgress(progress, 0, new LoadingText(""));
            }

            return csvBuilder;
        }

        private async UniTask MergeSitesWithExistingCSVAsync(ExportRequest request, string csvPath, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            await UniTask.SwitchToThreadPool();

            token.ThrowIfCancellationRequested();
            updateProgress(0.1f, 0, new LoadingText("Loading existing CSV file..."));

            // Reading the existing CSV file
            Dictionary<string, Dictionary<string, string>> existingData = new();
            List<string> headers = new();

            // Regex for parsing CSV correctly (respecting quotes)
            Regex csvParser = new(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

            using (StreamReader sr = new(csvPath))
            {
                // Read header
                string headerLine = sr.ReadLine();

                // Split and retrieve headers
                headers.AddRange(csvParser.Split(headerLine));

                // Read data line by line
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    token.ThrowIfCancellationRequested();

                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] values = csvParser.Split(line);
                    if (values.Length == 0 || string.IsNullOrEmpty(values[0])) continue;

                    // Using SiteID as key for the dictionary
                    string siteId = values[0].Trim('"');
                    Dictionary<string, string> rowData = new();

                    // Store all columns for this site
                    for (int i = 0; i < values.Length && i < headers.Count; i++)
                    {
                        rowData[headers[i]] = values[i];
                    }

                    existingData[siteId] = rowData;
                }
            }

            updateProgress(0.3f, 0, new LoadingText("Preparing new data..."));

            // Prepare new data
            System.Text.StringBuilder newCsvContent = await PrepareCSVContentAsync(request, (p, _, t) => updateProgress(0.3f + p * 0.5f, 0, t), token);

            token.ThrowIfCancellationRequested();
            if (newCsvContent == null) throw new HBPException("Export error", "No data to export. Please check the selected sites and try again.");

            // Parse the content of the new CSV
            string[] newLines = newCsvContent.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            if (newLines.Length < 2) return;

            // Headers of the new CSV
            string newHeaderLine = newLines[0];
            List<string> newHeaders = csvParser.Split(newHeaderLine).ToList();

            // Merge headers
            List<string> mergedHeaders = new() { "Site" }; // Always start with the site identifier
            // Add all unique existing headers
            foreach (string header in headers)
            {
                if (header != "Site" && !mergedHeaders.Contains(header))
                {
                    mergedHeaders.Add(header);
                }
            }

            // Add new headers
            foreach (string header in newHeaders)
            {
                if (header != "Site" && !mergedHeaders.Contains(header))
                {
                    mergedHeaders.Add(header);
                }
            }

            updateProgress(0.8f, 0, new LoadingText("Merging data..."));

            // Create a dictionary with the new data
            Dictionary<string, Dictionary<string, string>> newData = new();
            for (int i = 1; i < newLines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(newLines[i])) continue;

                string[] values = csvParser.Split(newLines[i]);
                if (values.Length == 0 || string.IsNullOrEmpty(values[0])) continue;

                string siteId = values[0].Trim('"');
                Dictionary<string, string> rowData = new();

                for (int j = 0; j < values.Length && j < newHeaders.Count; j++)
                {
                    rowData[newHeaders[j]] = values[j];
                }

                newData[siteId] = rowData;
            }

            // Merge the data
            System.Text.StringBuilder mergedCsvContent = new();
            // Merged headers
            mergedCsvContent.AppendLine(string.Join(",", mergedHeaders));

            // Merge all existing and new entries
            HashSet<string> processedSites = new();

            // First process existing sites
            foreach (var siteEntry in existingData)
            {
                string siteId = siteEntry.Key;
                var existingRow = siteEntry.Value;

                System.Text.StringBuilder rowBuilder = new();
                rowBuilder.Append(siteId); // Site identifier

                // For each header in the merged list (except the first which is the ID)
                for (int i = 1; i < mergedHeaders.Count; i++)
                {
                    string header = mergedHeaders[i];
                    rowBuilder.Append(",");

                    // Special handling for Labels column - merge labels from both sources
                    if (header == "Labels" && newData.TryGetValue(siteId, out var newRow) && existingRow.TryGetValue(header, out string existingLabels) && newRow.TryGetValue(header, out string newLabels))
                    {
                        // Parse labels from both sources
                        var existingLabelsList = existingLabels.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToList();

                        var newLabelsList = newLabels.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToList();

                        // Merge labels ensuring uniqueness
                        HashSet<string> mergedLabels = new(existingLabelsList);
                        foreach (var label in newLabelsList)
                        {
                            mergedLabels.Add(label);
                        }

                        // Join labels back into a string
                        rowBuilder.Append(string.Join(";", mergedLabels));
                    }
                    // If the site is also in the new data and the header is present
                    else if (newData.TryGetValue(siteId, out var row) && row.TryGetValue(header, out string newValue))
                    {
                        rowBuilder.Append(newValue);
                    }
                    // Otherwise use existing if available
                    else if (existingRow.TryGetValue(header, out string existingValue))
                    {
                        rowBuilder.Append(existingValue);
                    }
                }

                mergedCsvContent.AppendLine(rowBuilder.ToString());
                processedSites.Add(siteId);
            }

            // Then add new sites that were not in the existing data
            foreach (var siteEntry in newData)
            {
                string siteId = siteEntry.Key;
                if (processedSites.Contains(siteId)) continue;

                var newRow = siteEntry.Value;
                System.Text.StringBuilder rowBuilder = new();
                rowBuilder.Append(siteId); // Site identifier

                // For each header in the merged list (except the first which is the ID)
                for (int i = 1; i < mergedHeaders.Count; i++)
                {
                    string header = mergedHeaders[i];
                    rowBuilder.Append(",");

                    // Use the new value if available
                    if (newRow.TryGetValue(header, out string newValue))
                    {
                        rowBuilder.Append(newValue);
                    }
                }

                mergedCsvContent.AppendLine(rowBuilder.ToString());
            }

            updateProgress(0.95f, 0, new LoadingText("Writing merged file..."));

            // Write the merged file
            using StreamWriter sw = new(csvPath);
            sw.Write(mergedCsvContent.ToString());
        }

        private sealed class ExportRequest
        {
            public bool Merge, Highlighted, Blacklisted, Color, Labels, Position, Data, Tags;
            public Column Column;
            public Visualization Visualization;
            public string ImplantationName;
            public List<BaseTag> TagDefinitions;
            public ExportSite[] Sites;

            public ExportRequest(ExportToCSVSection section)
            {
                Merge = section.m_ExportModeDropdown.value != 0;
                Highlighted = section.m_ExportHighlighted.isOn;
                Blacklisted = section.m_ExportBlacklisted.isOn;
                Color = section.m_ExportColor.isOn;
                Labels = section.m_ExportLabels.isOn;
                Position = section.m_ExportPosition.isOn;
                Data = section.m_ExportData.isOn;
                Tags = section.m_ExportTags.isOn;
                Column = section.Scene.SelectedColumn.ColumnData;
                Visualization = section.Scene.Visualization;
                ImplantationName = Position ? section.Scene.ImplantationManager.SelectedImplantation.Name : string.Empty;
                TagDefinitions = Tags ? PersistentDataManager.Tags.GeneralTags.Concat(PersistentDataManager.Tags.SitesTags).ToList() : new();
                Sites = section.Sites.Select(site => new ExportSite
                {
                    Information = site.Information,
                    State = new SiteConfiguration(site.State.IsBlackListed, site.State.IsHighlighted, site.State.Color, site.State.Labels),
                    Position = Position ? site.transform.localPosition : Vector3.zero
                }).ToArray();
            }
        }

        private sealed class ExportSite
        {
            public Core.Object3D.SiteInformation Information;
            public SiteConfiguration State;
            public Vector3 Position;
        }

        #endregion
    }
}
