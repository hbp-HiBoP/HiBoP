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

namespace HBP.UI.Module3D
{
    public class ExportToCSVSection : SiteToolSection
    {
        #region Public Methods
        public override async UniTask ApplyAsync()
        {
#if UNITY_STANDALONE_OSX
            FileBrowser.GetSavedFileNameAsync(async (csvPath) =>
            {
                if (!string.IsNullOrEmpty(csvPath))
                {
                    await LoadingManager.LoadAsync((update, token) => ExportSitesAsync(Sites, csvPath, update, token));

                    await UniTask.SwitchToMainThread();
                    DialogBoxManager.Open(Core.Enums.DialogBoxType.Informational, "Sites exported", "The filtered sites have been sucessfully exported to " + csvPath).Forget();
                }
                ;
            }, new string[] { "csv" }, "Save sites to");
#else
            string csvPath = FileBrowser.GetSavedFileName(new string[] { "csv" }, "Save sites to");
            if (!string.IsNullOrEmpty(csvPath))
            {
                await LoadingManager.LoadAsync((update, token) => ExportSitesAsync(Sites, csvPath, update, token));

                await UniTask.SwitchToMainThread();
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Informational, "Sites exported", "The filtered sites have been sucessfully exported to " + csvPath).Forget();
            }
#endif
        }
        public override void StoreSettings()
        {
            // No settings to store
        }
        public override void LoadSettings()
        {
            // No settings to load
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

            // Create string builder
            if (token.IsCancellationRequested) return;
            System.Text.StringBuilder csvBuilder = new System.Text.StringBuilder();
            string tagsString = "";
            IEnumerable<BaseTag> tags = PersistentDataManager.Tags.GeneralTags.Concat(PersistentDataManager.Tags.SitesTags);
            if (tags.Count() > 0) tagsString = string.Format(",{0}", string.Join(",", tags.Select(t => !t.Name.Contains(",") ? t.Name : string.Format("\"{0}\"", t.Name))));
            csvBuilder.AppendLine("Site,Patient,Place,Date,X,Y,Z,CoordSystem,Labels,DataType,DataFiles" + tagsString);

            // Prepare sites positions for performance increase
            await UniTask.SwitchToMainThread();
            List<Vector3> sitePositions = sites.Select(s => s.transform.localPosition).ToList();
            await UniTask.SwitchToThreadPool();

            for (int i = 0; i < length; i++)
            {
                if (token.IsCancellationRequested) return;
                // Get required values
                Core.Object3D.Site site = sites[i];
                Vector3 sitePosition = sitePositions[i];
                DataInfo dataInfo = null;
                if (Scene.SelectedColumn is Column3DDynamic || Scene.SelectedColumn is Column3DStatic)
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
                    else
                    {
                        throw new Exception("Invalid data container type");
                    }
                }
                IEnumerable<BaseTagValue> tagValues = tags.Select(t => site.Information.SiteData.Tags.FirstOrDefault(tv => tv.Tag == t));
                string tagValuesString = "";
                if (tagValues.Count() > 0)
                {
                    System.Text.StringBuilder tagValuesStringBuilder = new System.Text.StringBuilder();
                    foreach (var tagValue in tagValues)
                    {
                        tagValuesStringBuilder.Append(",");
                        if (tagValue != null)
                        {
                            string value = tagValue.DisplayableValue;
                            if (value.Contains(","))
                            {
                                value = string.Format("\"{0}\"", value);
                            }
                            tagValuesStringBuilder.Append(value);
                        }
                    }
                    tagValuesString = tagValuesStringBuilder.ToString();
                }
                // Write in string builder
                csvBuilder.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}{11}",
                        site.Information.Name,
                        site.Information.Patient.Name,
                        site.Information.Patient.Place,
                        site.Information.Patient.Date,
                        sitePosition.x.ToString("N2", System.Globalization.CultureInfo.InvariantCulture),
                        sitePosition.y.ToString("N2", System.Globalization.CultureInfo.InvariantCulture),
                        sitePosition.z.ToString("N2", System.Globalization.CultureInfo.InvariantCulture),
                        Scene.ImplantationManager.SelectedImplantation.Name,
                        string.Join(";", site.State.Labels),
                        dataType,
                        dataFiles,
                        tagValuesString));
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