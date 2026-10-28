using HBP.Data.Informations;
using HBP.UI.Informations.TrialMatrix;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Enums;
using HBP.UI.Informations.Graphs;
using HBP.Data.Informations.Graphs;
using HBP.Core.Preferences;
using HBP.Core.DLL;
using HBP.Core.Tools;

namespace HBP.UI.Informations
{
    // Scientific calculation has no dependency on an instantiated matrix or graph view.
    internal sealed class InformationGraphBuilder
    {
        internal Column[] m_Columns;
        internal ChannelStruct[] m_Channels;
        internal Dictionary<ChannelStruct, List<LocalizerCurveData>> m_LocalizersCurves = new();
        internal HBP.Data.Informations.TrialMatrix.TrialMatrixGrid Matrices;
        internal Tuple<Tuple<Core.Data.Bloc, Core.Data.SubBloc>[], Core.Tools.TimeWindow>[] m_SubBlocsAndWindowByColumn;
        private readonly List<Tuple<HBP.Data.Informations.TrialMatrix.ChannelBloc, UnityEngine.Events.UnityAction>> listeners = new();

        internal void ClearListeners()
        {
            foreach (var item in listeners) item.Item1.OnChangeTrialSelected.RemoveListener(item.Item2);
            listeners.Clear();
        }

        readonly Color m_DefaultColor = new(220f / 255, 220f / 255, 220f / 255, 1);

        public Tuple<Graph.Curve[], Core.Tools.TimeWindow, bool>[] GenerateDataCurve(Column[] columns, ChannelStruct[] channels)
        {
            List<Tuple<Graph.Curve[], Core.Tools.TimeWindow, bool>> result = new();

            // Epoched Data
            // Find all visualized blocs and sort by column.
            IEnumerable<Column> epochedDataColumns = columns.Where(c => c.Data is IEEGData || c.Data is CCEPData);
            IEnumerable<Core.Data.Bloc> blocs = epochedDataColumns.Select(c => c.Data.Bloc);
            m_SubBlocsAndWindowByColumn = Core.Data.Bloc.GetSubBlocsAndWindowByColumn(blocs);

            foreach (var subBlocsAndWindow in m_SubBlocsAndWindowByColumn)
            {
                List<Graph.Curve> curves = new();
                foreach (var column in epochedDataColumns)
                {
                    var tuple = subBlocsAndWindow.Item1.FirstOrDefault(p => p.Item1 == column.Data.Bloc);
                    if (tuple != null)
                    {
                        Graph.Curve curve = GenerateColumnCurve(column, channels, tuple.Item2);
                        curves.Add(curve);
                    }
                }

                // Localizer data
                if (channels.Length == 1 && m_LocalizersCurves.ContainsKey(channels[0]))
                {
                    curves.Add(GenerateLocalizersCurve(channels[0]));
                }

                result.Add(new Tuple<Graph.Curve[], Core.Tools.TimeWindow, bool>(curves.ToArray(), subBlocsAndWindow.Item2, subBlocsAndWindow.Item1[0].Item2.Type == MainSecondaryEnum.Main));
            }

            // Non-epoched data
            IEnumerable<Column> nonEpochedDataColumns = columns.Where(c => c.Data is MEGData);
            foreach (var column in nonEpochedDataColumns)
            {
                Graph.Curve curve = GenerateNonEpochedColumnCurve(column, channels);
                result.Add(new Tuple<Graph.Curve[], Core.Tools.TimeWindow, bool>(new Graph.Curve[] { curve }, (column.Data as MEGData).Window, true));
            }

            return result.ToArray();
        }

        Graph.Curve GenerateColumnCurve(Column column, ChannelStruct[] channels, Core.Data.SubBloc subBloc)
        {
            string ID = column.Name + "_" + column.Data.Name + "_" + column.Data.Bloc.Name + "_" + column.Data.Dataset.Name;
            List<Graph.Curve> subcurves = new();

            // Add ROI Curve.
            for (int i = 0; i < column.ChannelGroups.Count; i++)
            {
                subcurves.Add(GenerateGroupsCurve(column, i, subBloc, ID));
            }

            // Generate Channels By Patient.
            Dictionary<Core.Data.Patient, List<ChannelStruct>> channelsByPatient = new();
            foreach (var channel in channels)
            {
                if (!channelsByPatient.ContainsKey(channel.Patient)) channelsByPatient[channel.Patient] = new List<ChannelStruct>();
                channelsByPatient[channel.Patient].Add(channel);
            }

            // Add Patient Curves.
            foreach (var patient in channelsByPatient.Keys)
            {
                subcurves.Add(GeneratePatientCurve(column, channelsByPatient[patient].ToArray(), subBloc, ID));
            }

            Graph.Curve curve = new(column.Name, null, true, ID, subcurves.ToArray(), m_DefaultColor);
            return curve;
        }

        Graph.Curve GenerateGroupsCurve(Column column, int index, Core.Data.SubBloc subBloc, string ID)
        {
            ID += "_" + column.ChannelGroups[index].ID;
            CurveData curveData = null;
            var groupChannels = column.ChannelGroups[index].Channels.Where(channel => column.Data is not CCEPData ccep || channel.Patient == ccep.Source.Patient).ToArray();
            Dictionary<Core.Data.Patient, List<string>> channelsByPatient = new();
            foreach (var channel in groupChannels)
            {
                channelsByPatient.AddIfAbsent(channel.Patient, new List<string>());
                channelsByPatient[channel.Patient].Add(channel.Channel);
            }

            Dictionary<Core.Data.Patient, Core.Data.PatientDataInfo> dataInfoByPatient = new(channelsByPatient.Count);
            if (column.Data is IEEGData ieegDataStruct)
            {
                Core.Data.IEEGDataInfo[] ieegDataInfo = ieegDataStruct.Dataset.GetIEEGDataInfos();
                foreach (var patient in channelsByPatient.Keys)
                {
                    dataInfoByPatient.Add(patient, ieegDataInfo.First(d => d.Patient == patient && d.Name == ieegDataStruct.Name));
                }
            }
            else if (column.Data is CCEPData ccepDataStruct)
            {
                Core.Data.CCEPDataInfo[] ccepDataInfo = ccepDataStruct.Dataset.GetCCEPDataInfos();
                foreach (var patient in channelsByPatient.Keys)
                {
                    dataInfoByPatient.Add(patient, ccepDataInfo.First(d => d.Patient == patient && d.Patient == ccepDataStruct.Source.Patient && d.StimulatedChannel == ccepDataStruct.Source.Channel && d.Name == ccepDataStruct.Name));
                }
            }

            var color = column.ChannelGroups[index].Type switch
            {
                ChannelStructsGroup.GroupType.ROI => PersistentDataManager.UserPreferences.Visualization.Graph.ROIColors.GetColor(0, Array.IndexOf(m_Columns, column)),
                ChannelStructsGroup.GroupType.Custom => PersistentDataManager.UserPreferences.Visualization.Graph.GroupColors.GetColor(index, Array.IndexOf(m_Columns, column)),
                _ => PersistentDataManager.UserPreferences.Visualization.Graph.GroupColors.GetColor(index, Array.IndexOf(m_Columns, column)),
            };
            if (groupChannels.Length > 1)
            {
                int channelCount = groupChannels.Length;
                // Get the statistics for all channels in the ROI
                Core.Data.BlocChannelStatistics[] statistics = new Core.Data.BlocChannelStatistics[channelCount];
                for (int c = 0; c < channelCount; ++c)
                {
                    statistics[c] = Core.Data.DataManager.GetStatistics(dataInfoByPatient[groupChannels[c].Patient], column.Data.Bloc, groupChannels[c].Channel);
                }

                // Create all the required variables
                float[][] series = new float[channelCount][];
                for (int c = 0; c < channelCount; ++c)
                {
                    series[c] = statistics[c].Trial.ChannelSubTrialBySubBloc[subBloc].Values;
                }

                Core.Data.StreamingStatistics.Calculate(series, Core.Data.DataManager.DefaultAveraging, out float[] values, out float[] standardDeviations);

                int start = subBloc.Window.Start;
                int end = subBloc.Window.End;
                curveData = ShapedCurveData.CreateRegular(values, standardDeviations, start, end, color);
            }
            else if (groupChannels.Length == 1)
            {
                // Use GetCurveData with all valid trials selected
                ChannelStruct channel = groupChannels[0];
                Core.Data.PatientDataInfo dataInfo = dataInfoByPatient[channel.Patient];
                Core.Data.BlocChannelData blocChannelData = Core.Data.DataManager.GetData(dataInfo, column.Data.Bloc, channel.Channel);

                if (blocChannelData == null)
                {
                    Graph.Curve nullResult = new(column.ChannelGroups[index].Name, null, true, ID, new Graph.Curve[0], m_DefaultColor);
                    return nullResult;
                }

                // Create a selection array with all valid trials selected
                Core.Data.ChannelTrial[] validTrials = blocChannelData.Trials.Where(t => t.IsValid).ToArray();
                bool[] allValidTrialsSelected = new bool[validTrials.Length];
                for (int i = 0; i < allValidTrialsSelected.Length; i++)
                {
                    allValidTrialsSelected[i] = true;
                }

                // A group channel is not necessarily displayed as an individual channel.
                // Use the group color directly instead of indexing the site color grid.
                curveData = GetCurveData(column, subBloc, channel, allValidTrialsSelected, color);

                Graph.Curve result = new(column.ChannelGroups[index].Name, curveData, true, ID, new Graph.Curve[0], m_DefaultColor);
                return result;
            }

            Graph.Curve finalResult = new(column.ChannelGroups[index].Name, curveData, true, ID, new Graph.Curve[0], m_DefaultColor);
            return finalResult;
        }

        Graph.Curve GeneratePatientCurve(Column column, ChannelStruct[] channels, Core.Data.SubBloc subBloc, string ID)
        {
            ID += "_" + channels[0].Patient.Name;
            Graph.Curve[] subcurves = channels.Select(channel => GenerateChannelCurve(column, channel, subBloc, ID)).ToArray();
            return new Graph.Curve(channels[0].Patient.Name, null, true, ID, subcurves, m_DefaultColor);
        }

        Graph.Curve GenerateChannelCurve(Column column, ChannelStruct channel, Core.Data.SubBloc subBloc, string ID)
        {
            ID += "_" + channel.Channel;
            if (column.Data is CCEPData ccep && channel.Patient != ccep.Source.Patient)
                return new Graph.Curve(channel.Channel, null, true, ID, Array.Empty<Graph.Curve>(), m_DefaultColor);
            var channelBloc = Matrices.Data.First(d => d.DataStruct.Dataset == column.Data.Dataset && d.DataStruct.Blocs.Contains(column.Data.Bloc) && d.DataStruct.Name == column.Data.Name && (column.Data is CCEPData response ? d.DataStruct is HBP.Data.Informations.TrialMatrix.TrialMatrixGrid.CCEPTrialMatrixData c && c.Source.Equals(response.Source) : d.DataStruct is HBP.Data.Informations.TrialMatrix.TrialMatrixGrid.IEEGTrialMatrixData)).Blocs.First(b => b.Data == column.Data.Bloc).ChannelBlocs.First(c => c.Channel.Equals(channel));
            CurveData curveData = GetCurveData(column, subBloc, channel, channelBloc.TrialIsSelected);
            Graph.Curve result = new(channel.Channel, curveData, true, ID, new Graph.Curve[0], m_DefaultColor);
            UnityEngine.Events.UnityAction changed = () =>
            {
                var previous = result.Data;
                result.Data = GetCurveData(column, subBloc, channel, channelBloc.TrialIsSelected);
                if (previous != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(previous);
                    else UnityEngine.Object.DestroyImmediate(previous);
                }
            };
            channelBloc.OnChangeTrialSelected.AddListener(changed);
            listeners.Add(Tuple.Create(channelBloc, changed));
            return result;
        }

        Graph.Curve GenerateLocalizersCurve(ChannelStruct channel)
        {
            // Grouper les données par protocole
            var dataByProtocol = new Dictionary<string, Dictionary<string, LocalizerCurveData>>();

            if (m_LocalizersCurves.TryGetValue(channel, out List<LocalizerCurveData> localizerDatas))
            {
                foreach (var localizerData in localizerDatas)
                {
                    if (!dataByProtocol.ContainsKey(localizerData.ProtocolName))
                    {
                        dataByProtocol[localizerData.ProtocolName] = new Dictionary<string, LocalizerCurveData>();
                    }

                    dataByProtocol[localizerData.ProtocolName][localizerData.BlocName] = localizerData;
                }
            }

            // Créer les sous-courbes de protocoles
            List<Graph.Curve> protocolCurves = new();

            int blocIndex = 0;
            foreach (var protocolPair in dataByProtocol)
            {
                string protocolName = protocolPair.Key;
                var blocData = protocolPair.Value;
                List<Graph.Curve> blocCurves = new();
                foreach (var blocPair in blocData)
                {
                    string blocName = blocPair.Key;
                    string blocID = $"Localizers_{protocolName}_{blocName}";
                    CurveData blocCurveData = null;
                    if (blocPair.Value.Points.Length > 0)
                    {
                        if (blocPair.Value.SEM != null)
                        {
                            blocCurveData = ShapedCurveData.CreateInstance(blocPair.Value.Points, blocPair.Value.SEM, PersistentDataManager.UserPreferences.Visualization.Graph.LocalizersColors.GetColor(0, blocIndex++));
                        }
                        else
                        {
                            blocCurveData = CurveData.CreateInstance(blocPair.Value.Points, PersistentDataManager.UserPreferences.Visualization.Graph.LocalizersColors.GetColor(0, blocIndex++));
                        }
                    }

                    var blocCurve = new Graph.Curve(blocName, blocCurveData, true, blocID, new Graph.Curve[0], m_DefaultColor);
                    blocCurves.Add(blocCurve);
                }

                string protocolID = $"Localizers_{protocolName}";
                var protocolCurve = new Graph.Curve(protocolName, null, true, protocolID, blocCurves.ToArray(), m_DefaultColor);
                protocolCurves.Add(protocolCurve);
            }

            // Créer la courbe principale "Localizers" (sans data, contenant les courbes de protocoles)
            return new Graph.Curve("Localizers", null, true, "Localizers", protocolCurves.ToArray(), m_DefaultColor);
        }

        Graph.Curve GenerateNonEpochedColumnCurve(Column column, ChannelStruct[] channels)
        {
            string ID = column.Name + "_" + column.Data.Name + "_" + column.Data.Dataset.Name;
            List<Graph.Curve> subcurves = new();

            // Add Channels curves.
            subcurves.AddRange(channels.Select(channel => GenerateNonEpochedChannelCurve(column, channel, ID)));

            Graph.Curve curve = new(column.Name, null, true, ID, subcurves.ToArray(), m_DefaultColor);
            return curve;
        }

        Graph.Curve GenerateNonEpochedChannelCurve(Column column, ChannelStruct channel, string ID)
        {
            ID += "_" + channel.Channel;
            CurveData curveData = GetNonEpochedCurveData(column, channel);
            Graph.Curve result = new(channel.Channel, curveData, true, ID, new Graph.Curve[0], m_DefaultColor);
            return result;
        }

        CurveData GetCurveData(Column column, Core.Data.SubBloc subBloc, ChannelStruct channel, bool[] selected, Color? colorOverride = null)
        {
            CurveData result = null;
            Core.Data.PatientDataInfo dataInfo = null;
            if (column.Data is IEEGData ieegDataStruct)
            {
                dataInfo = ieegDataStruct.Dataset.GetIEEGDataInfos().FirstOrDefault(d => (d.Patient == channel.Patient && d.Name == ieegDataStruct.Name));
            }
            else if (column.Data is CCEPData ccepDataStruct)
            {
                dataInfo = ccepDataStruct.Dataset.GetCCEPDataInfos().FirstOrDefault(d => (d.Patient == channel.Patient && d.StimulatedChannel == ccepDataStruct.Source.Channel && d.Patient == ccepDataStruct.Source.Patient && d.Name == ccepDataStruct.Name));
            }

            if (dataInfo == null) return null;
            Core.Data.BlocData blocData = Core.Data.DataManager.GetData(dataInfo, column.Data.Bloc);
            Core.Data.BlocChannelData blocChannelData = Core.Data.DataManager.GetData(dataInfo, column.Data.Bloc, channel.Channel);
            Color color = colorOverride ?? PersistentDataManager.UserPreferences.Visualization.Graph.SiteColors.GetColor(Array.IndexOf(m_Channels, channel), Array.IndexOf(m_Columns, column));
            if (blocChannelData == null)
                return null;

            Core.Data.ChannelTrial[] validTrials = blocChannelData.Trials.Where(t => t.IsValid).ToArray();
            List<Core.Data.ChannelTrial> trialsToUse = new(blocChannelData.Trials.Length);
            for (int i = 0; i < validTrials.Length; i++)
            {
                if (selected[i]) trialsToUse.Add(validTrials[i]);
            }

            float start = blocData.Frequency.ConvertNumberOfSamplesToMilliseconds(blocData.Frequency.ConvertToCeiledNumberOfSamples(subBloc.Window.Start));
            float end = blocData.Frequency.ConvertNumberOfSamplesToMilliseconds(blocData.Frequency.ConvertToFlooredNumberOfSamples(subBloc.Window.End));

            if (trialsToUse.Count > 1)
            {
                Core.Data.ChannelSubTrial[] channelSubTrials = trialsToUse.Select(t => t.ChannelSubTrialBySubBloc[subBloc]).ToArray();

                float[][] series = channelSubTrials.Select(channelSubTrial => channelSubTrial.Values).ToArray();
                Core.Data.StreamingStatistics.Calculate(series, Core.Data.DataManager.DefaultAveraging, out float[] values, out float[] standardDeviations);

                result = ShapedCurveData.CreateRegular(values, standardDeviations, start, end, color);
            }
            else if (trialsToUse.Count == 1)
            {
                Core.Data.ChannelSubTrial channelSubTrial = trialsToUse[0].ChannelSubTrialBySubBloc[subBloc];
                float[] values = channelSubTrial.Values;

                result = CurveData.CreateRegular(values, start, end, color);
            }

            return result;
        }

        CurveData GetNonEpochedCurveData(Column column, ChannelStruct channel)
        {
            CurveData result = null;
            Core.Data.PatientDataInfo dataInfo = null;
            if (column.Data is MEGData megDataStruct)
            {
                dataInfo = megDataStruct.Dataset.GetMEGDataInfos().OfType<Core.Data.MEGcDataInfo>().FirstOrDefault(d => (d.Patient == channel.Patient && d.Name == megDataStruct.Name));
                if (dataInfo == null) return null;
            }

            Core.Data.MEGcData megData = Core.Data.DataManager.GetData(dataInfo) as Core.Data.MEGcData;
            Color color = PersistentDataManager.UserPreferences.Visualization.Graph.SiteColors.GetColor(Array.IndexOf(m_Channels, channel), Array.IndexOf(m_Columns, column));
            if (megData == null)
                return null;

            float start = 0;
            float end = 0;
            if (megData.ValuesByChannel.TryGetValue(channel.Channel, out float[] values))
            {
                end = megData.Frequency.ConvertNumberOfSamplesToMilliseconds(values.Length);

                result = CurveData.CreateRegular(values, start, end, color);
            }

            return result;
        }

        void AddValues(Graph.Curve curve, ref Core.Data.RunningStatistics statistics)
        {
            if (curve.Data != null)
            {
                int length = curve.Data.Count;
                for (int i = 0; i < length; i++)
                    statistics.Add(curve.Data.GetOrdinate(i));
            }

            foreach (var subCurve in curve.SubCurves)
                AddValues(subCurve, ref statistics);
        }
    }
}
