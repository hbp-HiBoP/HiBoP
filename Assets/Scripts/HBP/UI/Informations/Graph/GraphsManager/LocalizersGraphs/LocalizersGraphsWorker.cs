using Cysharp.Threading.Tasks;
using HBP.Core.Exceptions;
using HBP.Core.Object3D;
using HBP.Core.Tools;
using HBP.Data.Informations;
using HBP.Data.Informations.Graphs;
using HBP.Data.Module3D;
using HBP.Data.Preferences;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HBP.UI.Informations
{
    public class LocalizerCurveData
    {
        public string DataType;
        public string ProtocolName;
        public string BlocName;
        public Vector2[] Points;

        public LocalizerCurveData(string dataType, string protocolName, string blocName, Vector2[] points)
        {
            DataType = dataType;
            ProtocolName = protocolName;
            BlocName = blocName;
            Points = points;
        }
    }

    public class LocalizersGraphsWorker
    {
        #region Public Methods
        public async UniTask<Dictionary<ChannelStruct, List<LocalizerCurveData>>> GenerateLocalizersGraphsVoxelAsync(string dataType, List<ProtocolItem> protocolItems, Action<float, float, LoadingText> progress)
        {
            Dictionary<ChannelStruct, List<LocalizerCurveData>> result = new Dictionary<ChannelStruct, List<LocalizerCurveData>>();
            var sites = GetSceneSites();
            
            // Dictionary to track which blocs we loaded ourselves and need to unload
            Dictionary<string, Dictionary<string, List<string>>> protocolBlocsToUnload = new Dictionary<string, Dictionary<string, List<string>>>();
            
            foreach (var protocolItem in protocolItems.Where(p => p.IsSelected))
            {
                try
                {
                    var protocolWasLoadedExternally = Object3DManager.Localizers.Protocols.Any(p => p.Name == protocolItem.Name);
                    var selectedBlocNames = protocolItem.SelectedBlocs.Select(b => b.Name).ToList();
                    
                    if (selectedBlocNames.Count == 0)
                        continue;

                    // Load only the specific blocs we need
                    var newlyLoadedBlocs = await Object3DManager.Localizers.LoadSpecificBlocsAsync(protocolItem.Name, dataType, selectedBlocNames);
                    
                    // Track which blocs we loaded so we can unload them later if protocol wasn't loaded externally
                    if (!protocolWasLoadedExternally && newlyLoadedBlocs.Count > 0)
                    {
                        if (!protocolBlocsToUnload.ContainsKey(protocolItem.Name))
                            protocolBlocsToUnload[protocolItem.Name] = new Dictionary<string, List<string>>();
                        
                        if (!protocolBlocsToUnload[protocolItem.Name].ContainsKey(dataType))
                            protocolBlocsToUnload[protocolItem.Name][dataType] = new List<string>();
                            
                        protocolBlocsToUnload[protocolItem.Name][dataType].AddRange(newlyLoadedBlocs);
                    }

                    var protocol = Object3DManager.Localizers.Protocols.FirstOrDefault(p => p.Name == protocolItem.Name) ?? throw new HBPException("Protocol not found", $"The protocol '{protocolItem.Name}' could not be loaded.\n\nPlease check your Localizers Atlas installation.");
                    var data = protocol.Datas.FirstOrDefault(d => d.Name == dataType) ?? throw new HBPException("Data not found", $"The data '{dataType}' was not found in the protocol '{protocolItem.Name}'.\n\nPlease check your Localizers Atlas installation.");
                    foreach (var blocItem in protocolItem.SelectedBlocs)
                    {
                        var bloc = data.Blocs.FirstOrDefault(b => b.Name == blocItem.Name) ?? throw new HBPException("Bloc not found", $"The bloc '{blocItem.Name}' was not found in the protocol '{protocolItem.Name}'.\n\nPlease check your Localizers Atlas installation.");
                        var times = GetTimes(bloc.FMRI);
                        foreach (var site in sites)
                        {
                            var channelStruct = new ChannelStruct(site.Information.Name, site.Information.Patient);
                            if (!result.TryGetValue(channelStruct, out List<LocalizerCurveData> curves))
                            {
                                curves = new List<LocalizerCurveData>();
                                result[channelStruct] = curves;
                            }
                            var values = GetVoxelData(site.Information.DefaultPosition, bloc.FMRI);
                            var points = new List<Vector2>();
                            for (int i = 0; i < times.Length; i++)
                            {
                                points.Add(new Vector2(times[i], values[i]));
                            }
                            curves.Add(new LocalizerCurveData(dataType, protocolItem.Name, blocItem.Name, points.ToArray()));
                        }
                    }
                }
                catch (Exception)
                {
                    // If something went wrong, we should still clean up any loaded blocs for this protocol
                    if (protocolBlocsToUnload.ContainsKey(protocolItem.Name))
                    {
                        foreach (var dataEntry in protocolBlocsToUnload[protocolItem.Name])
                        {
                            Object3DManager.Localizers.UnloadSpecificBlocs(protocolItem.Name, dataEntry.Key, dataEntry.Value);
                        }
                        protocolBlocsToUnload.Remove(protocolItem.Name);
                    }
                    throw; // Re-throw the exception
                }
            }

            // Clean up blocs that we loaded and that weren't loaded externally
            foreach (var protocolEntry in protocolBlocsToUnload)
            {
                foreach (var dataEntry in protocolEntry.Value)
                {
                    Object3DManager.Localizers.UnloadSpecificBlocs(protocolEntry.Key, dataEntry.Key, dataEntry.Value);
                }
            }

            return result;
        }
        public async UniTask<Dictionary<ChannelStruct, List<LocalizerCurveData>>> GenerateLocalizersGraphsRegionAsync(int precision, string dataType, List<ProtocolItem> protocolItems, Action<float, float, LoadingText> progress)
        {
            Dictionary<ChannelStruct, List<LocalizerCurveData>> result = new Dictionary<ChannelStruct, List<LocalizerCurveData>>();
            return result;
        }
        public async UniTask<Dictionary<ChannelStruct, List<LocalizerCurveData>>> GenerateLocalizersGraphsAtlasAsync(LocalizersGraphsAtlas atlas, string dataType, List<ProtocolItem> protocolItems, Action<float, float, LoadingText> progress)
        {
            Dictionary<ChannelStruct, List<LocalizerCurveData>> result = new Dictionary<ChannelStruct, List<LocalizerCurveData>>();
            return result;
        }
        #endregion

        #region Private Methods
        public List<Site> GetSceneSites()
        {
            return Module3DMain.SelectedScene.Columns.SelectMany(c => c.Sites).Where(s => !s.State.IsMasked).GroupBy(s => s.Information.FullID).Select(g => g.First()).ToList();
        }
        public float[] GetTimes(FMRI fmri)
        {
            float[] values = new float[fmri.Volumes.Count];
            for (int i = 0; i < fmri.Volumes.Count; i++)
            {
                values[i] = fmri.StartTime + i * fmri.TimeStep;
            }
            return values;
        }
        public float[] GetVoxelData(Vector3 voxel, FMRI fmri)
        {
            float[] values = new float[fmri.Volumes.Count];
            for (int i = 0; i < fmri.Volumes.Count; i++)
            {
                values[i] = fmri.Volumes[i].GetValueFromPosition(voxel);
            }
            return values;
        }
        #endregion
    }
}