using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Data.Preferences;
using HBP.UI.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
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

        static bool m_ExportHighlightedValue;
        static bool m_ExportBlacklistedValue;
        static bool m_ExportColorValue;
        static bool m_ExportLabelsValue;
        static bool m_ExportPositionValue;
        static bool m_ExportDataValue;
        static bool m_ExportTagsValue;
        #endregion

        #region Public Methods
        public override async UniTask ApplyAsync()
        {
            string csvPath = await FileBrowser.GetSavedFileNameAsync(new string[] { "csv" }, "Save sites to");
            if (!string.IsNullOrEmpty(csvPath))
            {
                await LoadingManager.LoadAsync((update, token) => ExportSitesAsync(Sites, csvPath, update, token));

                await UniTask.SwitchToMainThread();
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Informational, "Sites exported", "The filtered sites have been sucessfully exported to " + csvPath).Forget();
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
        }
        #endregion

        #region Private Methods
        private async UniTask ExportSitesAsync(List<Core.Object3D.Site> sites, string csvPath, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            int length = sites.Count;
            float progress = 0;

            // Prepare DataInfo by Patient for performance increase
            await UniTask.SwitchToThreadPool();
            Dictionary<Patient, DataInfo> dataInfoByPatient = new Dictionary<Patient, DataInfo>();

            if (m_ExportData.isOn)
            {
                for (int i = 0; i < length; i++)
                {
                    if (token.IsCancellationRequested) return;
                    Core.Object3D.Site site = sites[i];
                    if (!dataInfoByPatient.ContainsKey(site.Information.Patient))
                    {
                        if (Scene.SelectedColumn is Column3DIEEG columnIEEG)
                        {
                            DataInfo dataInfo = Scene.Visualization.GetDataInfo(site.Information.Patient, columnIEEG.ColumnIEEGData);
                            dataInfoByPatient.Add(site.Information.Patient, dataInfo);
                        }
                        else if (Scene.SelectedColumn is Column3DCCEP columnCCEP)
                        {
                            DataInfo dataInfo = Scene.Visualization.GetDataInfo(site.Information.Patient, columnCCEP.ColumnCCEPData);
                            dataInfoByPatient.Add(site.Information.Patient, dataInfo);
                        }
                        else if (Scene.SelectedColumn is Column3DStatic columnStatic)
                        {
                            DataInfo dataInfo = Scene.Visualization.GetDataInfo(site.Information.Patient, columnStatic.ColumnStaticData);
                            dataInfoByPatient.Add(site.Information.Patient, dataInfo);
                        }
                    }
                    progress = 0.5f * ((float)(i + 1) / length);
                    updateProgress(progress, 0, new LoadingText(""));
                }
            }

            // Create string builder
            if (token.IsCancellationRequested) return;
            System.Text.StringBuilder csvBuilder = new System.Text.StringBuilder();

            // Generate header
            System.Text.StringBuilder headerBuilder = new System.Text.StringBuilder("Site");
            if (m_ExportHighlighted.isOn) headerBuilder.Append(",Highlighted");
            if (m_ExportBlacklisted.isOn) headerBuilder.Append(",Blacklisted");
            if (m_ExportColor.isOn) headerBuilder.Append(",Color");
            if (m_ExportLabels.isOn) headerBuilder.Append(",Labels");
            if (m_ExportPosition.isOn) headerBuilder.Append(",X,Y,Z,CoordSystem");
            if (m_ExportData.isOn) headerBuilder.Append(",DataType,DataFiles");
            if (m_ExportTags.isOn)
            {
                List<BaseTag> tags = PersistentDataManager.Tags.GeneralTags.Concat(PersistentDataManager.Tags.SitesTags).ToList();
                if (tags.Count != 0)
                {
                    headerBuilder.Append(",");
                    headerBuilder.Append(string.Join(",", tags.Select(t => !t.Name.Contains(",") ? t.Name : string.Format("\"{0}\"", t.Name))));
                }
            }

            csvBuilder.AppendLine(headerBuilder.ToString());

            // Prepare sites positions for performance increase
            await UniTask.SwitchToMainThread();
            List<Vector3> sitePositions = m_ExportPosition.isOn ? sites.Select(s => s.transform.localPosition).ToList() : new List<Vector3>();
            await UniTask.SwitchToThreadPool();

            for (int i = 0; i < length; i++)
            {
                if (token.IsCancellationRequested) return;

                // Get required values
                Core.Object3D.Site site = sites[i];
                Vector3 sitePosition = m_ExportPosition.isOn ? sitePositions[i] : Vector3.zero;

                // Build row
                System.Text.StringBuilder rowBuilder = new System.Text.StringBuilder();
                rowBuilder.Append(site.Information.FullID);

                // Append site state data based on export flags
                if (m_ExportHighlighted.isOn) rowBuilder.AppendFormat(",{0}", site.State.IsHighlighted);
                if (m_ExportBlacklisted.isOn) rowBuilder.AppendFormat(",{0}", site.State.IsBlackListed);
                if (m_ExportColor.isOn) rowBuilder.AppendFormat(",{0}", site.State.Color.ToHexString());
                if (m_ExportLabels.isOn) rowBuilder.AppendFormat(",{0}", string.Join(";", site.State.Labels));
                if (m_ExportPosition.isOn)
                {
                    rowBuilder.AppendFormat(",{0},{1},{2},{3}",
                        sitePosition.x.ToString("N2", System.Globalization.CultureInfo.InvariantCulture),
                        sitePosition.y.ToString("N2", System.Globalization.CultureInfo.InvariantCulture),
                        sitePosition.z.ToString("N2", System.Globalization.CultureInfo.InvariantCulture),
                        Scene.ImplantationManager.SelectedImplantation.Name);
                }
                if (m_ExportData.isOn)
                {
                    DataInfo dataInfo = null;
                    if ((Scene.SelectedColumn is Column3DDynamic || Scene.SelectedColumn is Column3DStatic)
                        && dataInfoByPatient.ContainsKey(site.Information.Patient))
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
                if (m_ExportTags.isOn)
                {
                    List<BaseTag> tags = PersistentDataManager.Tags.GeneralTags.Concat(PersistentDataManager.Tags.SitesTags).ToList();
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

            // Write csv file
            if (token.IsCancellationRequested) return;
            using System.IO.StreamWriter sw = new System.IO.StreamWriter(csvPath);
            sw.Write(csvBuilder.ToString());
        }
        #endregion
    }
}