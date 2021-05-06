using CielaSpike;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Tools.CSharp;
using Tools.Unity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class SiteCorrelations : Tool
    {
        #region Internal Classes
        [DataContract]
        public class CorrelationsContainer
        {
            [DataMember] public string PatientName { get; set; }
            [DataMember] public string PatientID { get; set; }
            [DataMember] public List<ColumnContainer> Columns { get; set; }
            [DataMember] public Data.Enums.NormalizationType DefaultNormalization { get; set; }
            [DataMember] public float CorrelationThreshold { get; set; }
            [DataMember] public bool UseBonferroniCorrection { get; set; }
        }
        [DataContract]
        public class ColumnContainer
        {
            [DataMember] public string Name { get; set; }
            [DataMember] public Bloc Bloc { get; set; }
            [DataMember] public DataInfo Data { get; set; }
            [DataMember] public string CorrelationsFile { get; set; }
            [DataMember] public string CorrelationsBinaryFile { get; set; }
            [DataMember] public string CorrelationsMeanFile { get; set; }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Trigger the computation of the projection of the iEEG activity
        /// </summary>
        [SerializeField] private Button m_Compute;
        /// <summary>
        /// Load a correlation folder to the visualization
        /// </summary>
        [SerializeField] private Button m_Load;
        /// <summary>
        /// Save the data that has been computed to a folder
        /// </summary>
        [SerializeField] private Button m_Save;
        /// <summary>
        /// Reset the correlation data
        /// </summary>
        [SerializeField] private Button m_Reset;
        /// <summary>
        /// Remove the projection of the iEEG activity
        /// </summary>
        [SerializeField] private Toggle m_Display;
        /// <summary>
        /// Are the correlations being computed ?
        /// </summary>
        private bool m_CorrelationsComputing = false;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Compute.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
                ApplicationState.LoadingManager.Load(c_ComputeCorrelations((progress, duration, text) => onChangeProgress.Invoke(progress, duration, text)), onChangeProgress);
            });
            m_Load.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                LoadCorrelations();
            });
            m_Save.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SaveCorrelations();
            });
            m_Reset.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                ResetCorrelations();
            });
            m_Display.onValueChanged.AddListener((isOn) =>
            {
                if (ListenerLock) return;

                SelectedScene.DisplayCorrelations = isOn;
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            gameObject.SetActive(false);
            m_Compute.interactable = false;
            m_Compute.gameObject.SetActive(true);
            m_Display.interactable = false;
            m_Display.isOn = false;
            m_Display.gameObject.SetActive(false);
            m_Save.interactable = false;
            m_Load.interactable = false;
            m_Reset.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isSinglePatientScene = SelectedScene.Type == Data.Enums.SceneType.SinglePatient;
            bool areCorrelationsComputed = SelectedColumn is Column3DIEEG column ? column.AreCorrelationsComputed : false;
            bool isColumnIEEG = SelectedColumn is Column3DIEEG;

            gameObject.SetActive(isColumnIEEG && isSinglePatientScene);
            m_Compute.interactable = isColumnIEEG && !areCorrelationsComputed && !m_CorrelationsComputing && isSinglePatientScene;
            m_Compute.gameObject.SetActive(!areCorrelationsComputed);
            m_Display.interactable = isColumnIEEG && areCorrelationsComputed && !m_CorrelationsComputing && isSinglePatientScene;
            m_Display.gameObject.SetActive(areCorrelationsComputed);
            m_Save.interactable = isColumnIEEG && areCorrelationsComputed && !m_CorrelationsComputing && isSinglePatientScene;
            m_Load.interactable = isColumnIEEG && !m_CorrelationsComputing && isSinglePatientScene;
            m_Reset.interactable = isColumnIEEG && areCorrelationsComputed && !m_CorrelationsComputing && isSinglePatientScene;
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            m_Display.isOn = SelectedScene.DisplayCorrelations;
        }
        #endregion

        #region
        private void SaveCorrelations()
        {
            CorrelationsContainer container = new CorrelationsContainer()
            {
                PatientName = SelectedScene.Visualization.Patients[0].CompleteName,
                PatientID = SelectedScene.Visualization.Patients[0].ID,
                DefaultNormalization = ApplicationState.UserPreferences.Data.EEG.Normalization,
                CorrelationThreshold = ApplicationState.UserPreferences.Data.EEG.CorrelationAlpha,
                UseBonferroniCorrection = ApplicationState.UserPreferences.Data.EEG.BonferroniCorrection,
                Columns = new List<ColumnContainer>(SelectedScene.ColumnsIEEG.Count)
            };
            foreach (var column in SelectedScene.ColumnsIEEG)
            {
                Bloc bloc = column.ColumnIEEGData.Bloc.Clone() as Bloc;
                IEEGDataInfo dataInfo = column.ColumnIEEGData.Dataset.GetIEEGDataInfos().FirstOrDefault(d => d.Patient == SelectedScene.Visualization.Patients[0] && d.Name == column.ColumnIEEGData.DataName).Clone() as IEEGDataInfo;
                dataInfo.DataContainer.ConvertAllPathsToFullPaths();
                container.Columns.Add(new ColumnContainer() { Name = column.Name, Bloc = bloc, Data = dataInfo, CorrelationsFile = string.Format("{0}_correlations.csv", column.Name), CorrelationsBinaryFile = string.Format("{0}_significant.csv", column.Name), CorrelationsMeanFile = string.Format("{0}_pearson.csv", column.Name) });
            }
            string saveDirectory = Path.Combine(SelectedScene.GenerateExportDirectory(), "Correlations");
            ClassLoaderSaver.GenerateUniqueDirectoryPath(ref saveDirectory);
            if (!Directory.Exists(saveDirectory)) Directory.CreateDirectory(saveDirectory);
            ClassLoaderSaver.SaveToJSon(container, Path.Combine(saveDirectory, "Correlations.json"));

            int siteWeight(string name)
            {
                int weight = 0;
                string label = "";
                string digits = "";
                for (int i = 0; i < name.Length; ++i)
                {
                    if (char.IsDigit(name[i])) digits += name[i];
                    else label += name[i];
                }
                for (int i = 0; i < label.Length; i++)
                {
                    if (name[i] == '\'') weight += 1;
                    else weight += 100 * name[i];
                }
                if (digits.Length > 0)
                    weight += int.Parse(digits);
                return weight;
            }

            foreach (var column in SelectedScene.ColumnsIEEG)
            {
                StringBuilder csvText = new StringBuilder();
                StringBuilder csvBinaryText = new StringBuilder();
                StringBuilder csvMeanText = new StringBuilder();
                var sites = column.CorrelationBySitePair.Keys.OrderBy(s => siteWeight(s.Information.Name));
                int siteCount = sites.Count();
                csvText.AppendLine(string.Format("{0},{1}", "Channel", string.Join(",", sites.Select(c => c.Information.Name))));
                csvBinaryText.AppendLine(string.Format("{0},{1}", "Channel", string.Join(",", sites.Select(c => c.Information.Name))));
                csvMeanText.AppendLine(string.Format("{0},{1}", "Channel", string.Join(",", sites.Select(c => c.Information.Name))));
                foreach (var site in sites)
                {
                    if (column.CorrelationBySitePair.TryGetValue(site, out Dictionary<Site, float> correlationsOfSite))
                    {
                        csvText.Append(site.Information.Name);
                        csvBinaryText.Append(site.Information.Name);
                        foreach (var s in sites)
                        {
                            csvText.Append(",");
                            csvBinaryText.Append(",");
                            if (correlationsOfSite.TryGetValue(s, out float correlationValue))
                            {
                                csvText.Append(correlationValue.ToString("R", CultureInfo.InvariantCulture));
                                float threshold = ApplicationState.UserPreferences.Data.EEG.CorrelationAlpha;
                                if (ApplicationState.UserPreferences.Data.EEG.BonferroniCorrection) threshold /= siteCount * (siteCount - 1) / 2;
                                csvBinaryText.Append(correlationValue < threshold ? 1 : 0);
                            }
                            else
                            {
                                csvText.Append(0);
                                csvBinaryText.Append(1);
                            }
                        }
                        csvText.AppendLine();
                        csvBinaryText.AppendLine();
                    }
                    if (column.CorrelationMeanBySitePair.TryGetValue(site, out Dictionary<Site, float> meanOfSite))
                    {
                        csvMeanText.Append(site.Information.Name);
                        foreach (var s in sites)
                        {
                            csvMeanText.Append(",");
                            if (meanOfSite.TryGetValue(s, out float meanValue))
                            {
                                csvMeanText.Append(meanValue.ToString("R", CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                csvMeanText.Append(1);
                            }
                        }
                        csvMeanText.AppendLine();
                    }
                }
                try
                {
                    using (StreamWriter sw = new StreamWriter(Path.Combine(saveDirectory, string.Format("{0}_correlations.csv", column.Name))))
                    {
                        sw.Write(csvText.ToString());
                    }
                    using (StreamWriter sw = new StreamWriter(Path.Combine(saveDirectory, string.Format("{0}_significant.csv", column.Name))))
                    {
                        sw.Write(csvBinaryText.ToString());
                    }
                    using (StreamWriter sw = new StreamWriter(Path.Combine(saveDirectory, string.Format("{0}_pearson.csv", column.Name))))
                    {
                        sw.Write(csvMeanText.ToString());
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Can not save correlations", "Please verify your rights.");
                    return;
                }
            }
            ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Informational, "Site correlations saved", "Site correlations of this visualization have been saved to <color=#3080ffff>" + saveDirectory + "</color>");
        }
        private void LoadCorrelations()
        {
            void Load(string path)
            {
                try
                {
                    CorrelationsContainer container = ClassLoaderSaver.LoadFromJson<CorrelationsContainer>(path);
                    // Checks
                    if (SelectedScene.Visualization.Patients[0].ID != container.PatientID)
                    {
                        ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Correlation file is not compatible", "The patient of the correlations files you are trying to load is different from the patient in the visualization.");
                        return;
                    }
                    if (!container.Columns.All(c => SelectedScene.ColumnsIEEG.Any(col => col.Name == c.Name && col.ColumnIEEGData.Bloc == c.Bloc)))
                    {
                        ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Correlation file is not compatible", "One of the columns in the correlations files has no corresponding column in the visualization.");
                        return;
                    }
                    // Load
                    foreach (var column in SelectedScene.ColumnsIEEG)
                    {
                        column.CorrelationBySitePair.Clear();
                    }
                    string directory = new FileInfo(path).Directory.FullName;
                    foreach (var column in SelectedScene.ColumnsIEEG)
                    {
                        ColumnContainer columnContainer = container.Columns.FirstOrDefault(c => c.Name == column.Name);
                        string csvFilePath = Path.Combine(directory, columnContainer.CorrelationsFile);
                        using (StreamReader sr = new StreamReader(csvFilePath))
                        {
                            string firstLine = sr.ReadLine();
                            string[] siteNames = firstLine.Split(',');
                            string line;
                            Dictionary<Site, Dictionary<Site, float>> correlationsBySitePair = new Dictionary<Site, Dictionary<Site, float>>();
                            while ((line = sr.ReadLine()) != null)
                            {
                                string[] values = line.Split(',');
                                if (values.Length == 0) continue;
                                Site site = column.Sites.FirstOrDefault(s => s.Information.Name == values[0]);
                                if (site)
                                {
                                    Dictionary<Site, float> valueBySite = new Dictionary<Site, float>();
                                    for (int i = 1; i < values.Length; ++i)
                                    {
                                        Site comparedSite = column.Sites.FirstOrDefault(s => s.Information.Name == siteNames[i]);
                                        if (comparedSite)
                                        {
                                            if (NumberExtension.TryParseFloat(values[i], out float value))
                                            {
                                                valueBySite[comparedSite] = value;
                                            }
                                        }
                                    }
                                    correlationsBySitePair[site] = valueBySite;
                                }
                            }
                            column.CorrelationBySitePair = correlationsBySitePair;
                        }
                        string csvMeanFilePath = Path.Combine(directory, columnContainer.CorrelationsMeanFile);
                        using (StreamReader sr = new StreamReader(csvMeanFilePath))
                        {
                            string firstLine = sr.ReadLine();
                            string[] siteNames = firstLine.Split(',');
                            string line;
                            Dictionary<Site, Dictionary<Site, float>> meanByPair = new Dictionary<Site, Dictionary<Site, float>>();
                            while ((line = sr.ReadLine()) != null)
                            {
                                string[] values = line.Split(',');
                                if (values.Length == 0) continue;
                                Site site = column.Sites.FirstOrDefault(s => s.Information.Name == values[0]);
                                if (site)
                                {
                                    Dictionary<Site, float> valueBySite = new Dictionary<Site, float>();
                                    for (int i = 1; i < values.Length; ++i)
                                    {
                                        Site comparedSite = column.Sites.FirstOrDefault(s => s.Information.Name == siteNames[i]);
                                        if (comparedSite)
                                        {
                                            if (NumberExtension.TryParseFloat(values[i], out float value))
                                            {
                                                valueBySite[comparedSite] = value;
                                            }
                                        }
                                    }
                                    meanByPair[site] = valueBySite;
                                }
                            }
                            column.CorrelationMeanBySitePair = meanByPair;
                        }
                    }
                    SelectedScene.DisplayCorrelations = true;
                    ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Can not load correlations", "One or multiple files are either missing or invalid.");
                }
            }
#if UNITY_STANDALONE_OSX
            FileBrowser.GetExistingFileNameAsync((loadPath) =>
            {
                if (!string.IsNullOrEmpty(loadPath))
                {
                    Load(loadPath);
                }
            }, new string[] { "json" }, "Load correlations");
#else
            string loadPath = FileBrowser.GetExistingFileName(new string[] { "json" }, "Load correlations");
            if (!string.IsNullOrEmpty(loadPath))
            {
                Load(loadPath);
            }
#endif
        }
        private void ResetCorrelations()
        {
            ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.WarningMultiOptions, "Reset correlations", "This will erase all loaded or computed correlations. Please make sure you saved the computed correlations to files before reseting them.", () =>
            {
                foreach (var column in SelectedScene.ColumnsIEEG)
                {
                    column.CorrelationBySitePair.Clear();
                }
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            }, "Reset", () => { }, "Cancel");
        }
        #endregion

        #region Coroutines
        /// <summary>
        /// Compute correlations for all ieeg columns
        /// </summary>
        /// <param name="onChangeProgress">Action for the loading circle</param>
        /// <returns>Coroutine return</returns>
        private IEnumerator c_ComputeCorrelations(Action<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpToUnity;
            m_CorrelationsComputing = true;
            UpdateInteractable();
            yield return Ninja.JumpBack;
            List<Column3DIEEG> columns = SelectedScene.ColumnsIEEG;
            for (int i = 0; i < columns.Count; i++)
            {
                columns[i].ComputeCorrelations((progress, duration, text) => { onChangeProgress((i + progress) / columns.Count, duration, text); } );
            }
            yield return Ninja.JumpToUnity;
            m_CorrelationsComputing = false;
            onChangeProgress(1, 0, new LoadingText("Correlations computed"));
            SelectedScene.DisplayCorrelations = true;
            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        #endregion
    }
}