using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Sync.Scene;
using HBP.Core.Preferences;
using HBP.UI.Tools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    public class SiteCorrelations : Tool
    {
        private const long MaximumLegacyMetadataBytes = 1024 * 1024;
        private const long MaximumLegacyMatrixBytes = 64 * 1024 * 1024;
        private const int MaximumLegacyMatrixRows = 4096;
        private const int MaximumLegacyMatrixPairs = 100000;

        #region Internal Classes

        [JsonObject(MemberSerialization.OptIn), Preserve]
        public class CorrelationsContainer
        {
            [JsonProperty] public string PatientName { get; set; }
            [JsonProperty] public string PatientID { get; set; }
            [JsonProperty] public List<ColumnContainer> Columns { get; set; }
            [JsonProperty] public NormalizationType DefaultNormalization { get; set; }
            [JsonProperty] public float CorrelationThreshold { get; set; }
            [JsonProperty] public bool UseBonferroniCorrection { get; set; }
        }

        [JsonObject(MemberSerialization.OptIn), Preserve]
        public class ColumnContainer
        {
            [JsonProperty] public string Column { get; set; }
            [JsonProperty] public string Protocol { get; set; }
            [JsonProperty] public string Bloc { get; set; }
            [JsonProperty] public string Dataset { get; set; }
            [JsonProperty] public string Data { get; set; }
            [JsonProperty] public string CorrelationsFile { get; set; }
            [JsonProperty] public string CorrelationsBinaryFile { get; set; }
            [JsonProperty] public string CorrelationsMeanFile { get; set; }
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

                ToolbarExternalActions.LoadCancelable((update, token) => ComputeCorrelations(update, token));
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
            bool isSinglePatientScene = SelectedScene.Type == SceneType.SinglePatient;
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
            CorrelationsContainer container = new()
            {
                PatientName = SelectedScene.Visualization.Patients[0].Name,
                PatientID = SelectedScene.Visualization.Patients[0].ID,
                DefaultNormalization = PersistentDataManager.UserPreferences.Data.EEG.Normalization,
                CorrelationThreshold = PersistentDataManager.UserPreferences.Data.EEG.CorrelationAlpha,
                UseBonferroniCorrection = PersistentDataManager.UserPreferences.Data.EEG.BonferroniCorrection,
                Columns = new List<ColumnContainer>(SelectedScene.ColumnsIEEG.Count)
            };
            foreach (var column in SelectedScene.ColumnsIEEG)
            {
                Bloc bloc = column.ColumnIEEGData.Bloc.Clone() as Bloc;
                IEEGDataInfo dataInfo = column.ColumnIEEGData.Dataset.GetIEEGDataInfos().FirstOrDefault(d => d.Patient == SelectedScene.Visualization.Patients[0] && d.Name == column.ColumnIEEGData.DataName).Clone() as IEEGDataInfo;
                dataInfo.DataContainer.ConvertAllPathsToFullPaths();
                container.Columns.Add(new ColumnContainer()
                {
                    Column = column.Name,
                    Protocol = column.ColumnIEEGData.Dataset.Protocol.Name,
                    Bloc = bloc.Name,
                    Dataset = column.ColumnIEEGData.Dataset.Name,
                    Data = dataInfo.Name,
                    CorrelationsFile = string.Format("{0}_{1}_correlations.csv", container.PatientID, column.Name),
                    CorrelationsBinaryFile = string.Format("{0}_{1}_significant.csv", container.PatientID, column.Name),
                    CorrelationsMeanFile = string.Format("{0}_{1}_pearson.csv", container.PatientID, column.Name)
                });
            }

            string saveDirectory = Path.Combine(SelectedScene.GenerateExportDirectory(), "Correlations").GenerateUniqueDirectoryPath();
            if (!Directory.Exists(saveDirectory)) Directory.CreateDirectory(saveDirectory);
            ClassLoaderSaver.SaveToJSon(container, Path.Combine(saveDirectory, string.Format("{0}_Correlations.json", container.PatientID)));

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
                StringBuilder csvText = new();
                StringBuilder csvBinaryText = new();
                StringBuilder csvMeanText = new();
                var sites = column.CorrelationBySitePair.Keys.OrderBy(s => siteWeight(s.Information.Name));
                int siteCount = sites.Count();
                csvText.AppendLine(string.Format("{0},{1}", "Channel", string.Join(",", sites.Select(c => c.Information.Name))));
                csvBinaryText.AppendLine(string.Format("{0},{1}", "Channel", string.Join(",", sites.Select(c => c.Information.Name))));
                csvMeanText.AppendLine(string.Format("{0},{1}", "Channel", string.Join(",", sites.Select(c => c.Information.Name))));
                foreach (var site in sites)
                {
                    if (column.CorrelationBySitePair.TryGetValue(site, out Dictionary<Core.Object3D.Site, float> correlationsOfSite))
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
                                float threshold = PersistentDataManager.UserPreferences.Data.EEG.CorrelationAlpha;
                                if (PersistentDataManager.UserPreferences.Data.EEG.BonferroniCorrection) threshold /= siteCount * (siteCount - 1) / 2;
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

                    if (column.CorrelationMeanBySitePair.TryGetValue(site, out Dictionary<Core.Object3D.Site, float> meanOfSite))
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
                    using (StreamWriter sw = new(Path.Combine(saveDirectory, string.Format("{0}_{1}_correlations.csv", container.PatientID, column.Name))))
                    {
                        sw.Write(csvText.ToString());
                    }

                    using (StreamWriter sw = new(Path.Combine(saveDirectory, string.Format("{0}_{1}_significant.csv", container.PatientID, column.Name))))
                    {
                        sw.Write(csvBinaryText.ToString());
                    }

                    using (StreamWriter sw = new(Path.Combine(saveDirectory, string.Format("{0}_{1}_pearson.csv", container.PatientID, column.Name))))
                    {
                        sw.Write(csvMeanText.ToString());
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    DialogBoxManager.Open(DialogBoxType.Error, "Can not save correlations", "Please verify your rights.").Forget();
                    return;
                }
            }

            DialogBoxManager.Open(DialogBoxType.Informational, "Site correlations saved", "Site correlations of this visualization have been saved to <color=#3080ffff>" + saveDirectory + "</color>").Forget();
        }

        private async void LoadCorrelations()
        {
            string loadPath = await ToolbarExternalActions.GetExistingFileNameAsync(new string[] { "json" }, "Load correlations");
            if (string.IsNullOrEmpty(loadPath)) return;

            try
            {
                Base3DScene scene = SelectedScene;
                CorrelationResultResource resource = ReadLegacyCorrelationResult(scene, loadPath);
                byte[] resultBytes = resource.Encode();
                if (V2CorrelationRequestRouter.TryGetHandler(scene, out var handler))
                {
                    if (!await handler(V2CorrelationRequest.Load(resultBytes), CancellationToken.None)) return;
                }
                else
                {
                    resource.Apply(scene);
                }

                await UniTask.SwitchToMainThread();
                scene.DisplayCorrelations = true;
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                DialogBoxManager.Open(DialogBoxType.Error, "Can not load correlations", "One or multiple files are either missing or invalid.").Forget();
            }
        }

        private static CorrelationResultResource ReadLegacyCorrelationResult(Base3DScene scene, string path)
        {
            if (!scene) throw new ArgumentNullException(nameof(scene));
            if (new FileInfo(path).Length > MaximumLegacyMetadataBytes) throw new InvalidDataException("The correlation metadata file exceeds its size limit.");
            CorrelationsContainer container = ClassLoaderSaver.LoadFromJson<CorrelationsContainer>(path);
            Core.Data.Patient patient = scene.Visualization.Patients.FirstOrDefault();
            if (patient == null || patient.ID != container?.PatientID)
                throw new InvalidDataException("The patient in the correlation file differs from the patient in the visualization.");
            if (container == null || container.Columns == null || container.Columns.Count != scene.ColumnsIEEG.Count || container.Columns.Any(column => column == null) || container.Columns.Select(column => column.Column).Distinct(StringComparer.Ordinal).Count() != scene.ColumnsIEEG.Count || !container.Columns.All(source => scene.ColumnsIEEG.Any(column => column.Name == source.Column)))
                throw new InvalidDataException("The correlation file does not contain exactly one result for every visualization column.");

            string directory = new FileInfo(path).Directory?.FullName ?? throw new InvalidDataException("The correlation file has no parent directory.");
            var results = new List<CorrelationResultData>(scene.ColumnsIEEG.Count);
            foreach (Column3DIEEG column in scene.ColumnsIEEG)
            {
                ColumnContainer source = container.Columns.Single(item => item.Column == column.Name);
                Dictionary<Core.Object3D.Site, Dictionary<Core.Object3D.Site, float>> correlations = ReadLegacyMatrix(ResolveLegacyMatrixPath(directory, source.CorrelationsFile), column);
                Dictionary<Core.Object3D.Site, Dictionary<Core.Object3D.Site, float>> means = ReadLegacyMatrix(ResolveLegacyMatrixPath(directory, source.CorrelationsMeanFile), column);
                var dataset = column.ColumnIEEGData?.Dataset;
                var protocol = dataset?.Protocol;
                var bloc = column.ColumnIEEGData?.Bloc;
                bool datasetMatches = dataset != null && dataset.Name == source.Dataset;
                bool protocolMatches = datasetMatches && protocol != null && protocol.Name == source.Protocol;
                bool blocMatches = protocolMatches && bloc != null && bloc.Name == source.Bloc;
                IEEGDataInfo dataInfo = datasetMatches ? dataset.GetIEEGDataInfos().FirstOrDefault(info => info.Patient == patient && info.Name == source.Data) : null;
                var provenance = new CorrelationProvenance(CorrelationResultSource.Imported, container.PatientID, container.PatientName, column.ColumnData.ID, datasetMatches ? dataset.ID : string.Empty, source.Dataset, protocolMatches ? protocol.ID : string.Empty, source.Protocol, blocMatches ? bloc.ID : string.Empty, source.Bloc, dataInfo?.ID ?? string.Empty, source.Data, container.DefaultNormalization, container.CorrelationThreshold, container.UseBonferroniCorrection);
                results.Add(new CorrelationResultData(column.ColumnData.ID, correlations, means, provenance));
            }

            return CorrelationResultResource.FromResults(scene, results);
        }

        private static string ResolveLegacyMatrixPath(string directory, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || Path.IsPathRooted(fileName) || !StringComparer.Ordinal.Equals(Path.GetFileName(fileName), fileName))
                throw new InvalidDataException("Correlation matrix references must be file names in the selected folder.");
            return Path.Combine(directory, fileName);
        }

        private static Dictionary<Core.Object3D.Site, Dictionary<Core.Object3D.Site, float>> ReadLegacyMatrix(string path, Column3DIEEG column)
        {
            if (new FileInfo(path).Length > MaximumLegacyMatrixBytes) throw new InvalidDataException("A correlation matrix file exceeds its size limit.");
            using StreamReader reader = new(path);
            string firstLine = reader.ReadLine() ?? throw new InvalidDataException("A correlation matrix has no header.");
            string[] siteNames = firstLine.Split(',');
            if (siteNames.Length < 2 || siteNames.Length > MaximumLegacyMatrixRows + 1 || siteNames.Skip(1).Distinct(StringComparer.Ordinal).Count() != siteNames.Length - 1)
                throw new InvalidDataException("A correlation matrix has an invalid header.");
            var sitesByName = column.Sites.ToDictionary(site => site.Information.Name, StringComparer.Ordinal);
            var matrix = new Dictionary<Core.Object3D.Site, Dictionary<Core.Object3D.Site, float>>();
            int rowCount = 0;
            int pairCount = 0;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (++rowCount > MaximumLegacyMatrixRows) throw new InvalidDataException("A correlation matrix has too many rows.");
                string[] values = line.Split(',');
                if (values.Length != siteNames.Length) throw new InvalidDataException("A correlation matrix row has the wrong number of values.");
                if (!sitesByName.TryGetValue(values[0], out Core.Object3D.Site site)) continue;
                var valueBySite = new Dictionary<Core.Object3D.Site, float>();
                for (int i = 1; i < values.Length; ++i)
                {
                    if (!sitesByName.TryGetValue(siteNames[i], out Core.Object3D.Site comparedSite)) continue;
                    if (NumberExtension.TryParseFloat(values[i], out float value) && !float.IsNaN(value) && !float.IsInfinity(value))
                    {
                        if (++pairCount > MaximumLegacyMatrixPairs) throw new InvalidDataException("A correlation matrix has too many site pairs.");
                        valueBySite[comparedSite] = value;
                    }
                }

                matrix[site] = valueBySite;
            }

            return matrix;
        }

        private async void ResetCorrelations()
        {
            int result = await DialogBoxManager.OpenAsync(DialogBoxType.Informational, "Reset correlations", "This will erase all loaded or computed correlations. Please make sure you saved the computed correlations to files before reseting them.", "Reset", "Cancel");
            if (result == 0)
            {
                SelectedScene.ResetCorrelations();
            }
        }

        #endregion

        #region Coroutines

        /// <summary>
        /// Compute correlations for all ieeg columns
        /// </summary>
        /// <param name="updateProgress">Action for the loading circle</param>
        /// <returns>Coroutine return</returns>
        private async UniTask ComputeCorrelations(Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            Base3DScene scene = SelectedScene;
            m_CorrelationsComputing = true;
            UpdateInteractable();
            try
            {
                if (V2CorrelationRequestRouter.TryGetHandler(scene, out var handler))
                    await handler(V2CorrelationRequest.Compute(externalLoadingIndicator: true, progress: updateProgress), token);
                else
                    await scene.ComputeCorrelationsAsync(updateProgress, token);

                await UniTask.SwitchToMainThread();
                scene.DisplayCorrelations = true;
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                m_CorrelationsComputing = false;
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            }
        }

        #endregion
    }
}
