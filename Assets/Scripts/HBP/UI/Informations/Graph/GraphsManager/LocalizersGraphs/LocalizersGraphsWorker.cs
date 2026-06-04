using Cysharp.Threading.Tasks;
using HBP.Core.DLL;
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
using System.Threading;
using UnityEngine;

namespace HBP.UI.Informations
{
    public class LocalizerCurveData
    {
        public string DataType;
        public string ProtocolName;
        public string BlocName;
        public Vector2[] Points;
        public float[] SEM;

        public LocalizerCurveData(string dataType, string protocolName, string blocName, Vector2[] points, float[] sem = null)
        {
            DataType = dataType;
            ProtocolName = protocolName;
            BlocName = blocName;
            Points = points;
            SEM = sem;
        }
    }

    public struct RescalingParameters
    {
        public bool EnableRescaling;
        public float BaselineValue;
        public float GainFactor;
        public float Offset;

        public RescalingParameters(bool enableRescaling, float baselineValue, float gainFactor, float offset)
        {
            EnableRescaling = enableRescaling;
            BaselineValue = baselineValue;
            GainFactor = gainFactor;
            Offset = offset;
        }
    }

    public class LocalizersGraphsWorker
    {
        #region Public Methods
        public async UniTask<Dictionary<ChannelStruct, List<LocalizerCurveData>>> GenerateLocalizersGraphsVoxelAsync(string dataType, List<ProtocolItem> protocolItems, RescalingParameters rescalingParams, Action<float, float, LoadingText> progress, CancellationToken token)
        {
            Dictionary<ChannelStruct, List<LocalizerCurveData>> result = new();
            var sites = GetSceneSites();
            
            // Calculate total work units for progress tracking
            var selectedProtocols = protocolItems.Where(p => p.IsSelected).ToList();
            var totalBlocs = selectedProtocols.Sum(p => p.SelectedBlocs.Count);
            
            if (totalBlocs == 0)
            {
                return result;
            }

            progress?.Invoke(0.0f, 0, new LoadingText("Initializing localizer graphs generation"));

            int processedBlocs = 0;
            int blocIndex = 0;
            int totalNumberOfBlocs = selectedProtocols.Sum(p => p.SelectedBlocs.Count);

            foreach (var protocolItem in selectedProtocols)
            {
                var selectedBlocNames = protocolItem.SelectedBlocs.Select(b => b.Name).ToList();
                
                if (selectedBlocNames.Count == 0)
                    continue;

                var protocolWasLoadedExternally = Object3DManager.Localizers.Protocols.Any(p => p.Name == protocolItem.Name);
                var protocol = Object3DManager.Localizers.Protocols.FirstOrDefault(p => p.Name == protocolItem.Name);
                var data = protocol?.Datas.FirstOrDefault(d => d.Name == dataType);

                foreach (var blocItem in protocolItem.SelectedBlocs)
                {
                    token.ThrowIfCancellationRequested();
                    blocIndex++;
                    
                    // Progress for loading phase
                    float loadingProgress = (float)(processedBlocs + 1) / totalBlocs;
                    progress?.Invoke(loadingProgress, 2f, new LoadingText("Loading bloc ", $"{blocItem.Name} from {protocolItem.Name}", $" [{blocIndex}/{totalNumberOfBlocs}]"));

                    // Check if bloc is already loaded
                    var existingBloc = data?.Blocs.FirstOrDefault(b => b.Name == blocItem.Name);
                    bool blocWasLoadedExternally = existingBloc?.Loaded ?? false;
                        
                    // Load only this specific bloc
                    var newlyLoadedBlocs = await Object3DManager.Localizers.LoadSpecificBlocsAsync(protocolItem.Name, dataType, new[] { blocItem.Name });
                        
                    // Get the protocol and data after loading
                    protocol = Object3DManager.Localizers.Protocols.FirstOrDefault(p => p.Name == protocolItem.Name);
                    if (protocol == null)
                        throw new HBPException("Protocol not found", $"The protocol '{protocolItem.Name}' could not be loaded.\n\nPlease check your Localizers Atlas installation.");

                    data = protocol.Datas.FirstOrDefault(d => d.Name == dataType);
                    if (data == null)
                        throw new HBPException("Data not found", $"The data '{dataType}' was not found in the protocol '{protocolItem.Name}'.\n\nPlease check your Localizers Atlas installation.");

                    var bloc = data.Blocs.FirstOrDefault(b => b.Name == blocItem.Name);
                    if (bloc == null)
                        throw new HBPException("Bloc not found", $"The bloc '{blocItem.Name}' was not found in the protocol '{protocolItem.Name}'.\n\nPlease check your Localizers Atlas installation.");

                    try
                    {
                        // Extract all the data we need from this bloc
                        await UniTask.SwitchToThreadPool();
                        var times = GetTimes(bloc.FMRI);
                        foreach (var site in sites)
                        {
                            var channelStruct = new ChannelStruct(site.Information.Name, site.Information.Patient);
                            if (!result.TryGetValue(channelStruct, out List<LocalizerCurveData> curves))
                            {
                                curves = new List<LocalizerCurveData>();
                                result[channelStruct] = curves;
                            }

                            float maskValue = bloc.FMRI.MaskVolume.GetValueFromPosition(site.Information.DefaultPosition);
                            if (maskValue <= 0) continue; // Skip if outside mask

                            var values = GetVoxelData(site.Information.DefaultPosition, bloc.FMRI);
                            
                            // Apply rescaling if enabled
                            if (rescalingParams.EnableRescaling)
                            {
                                ApplyRescaling(values, rescalingParams);
                            }
                            
                            var points = new List<Vector2>();
                            for (int i = 0; i < times.Length; i++)
                            {
                                points.Add(new Vector2(times[i], values[i]));
                            }
                            curves.Add(new LocalizerCurveData(dataType, protocolItem.Name, blocItem.Name, points.ToArray()));
                        }
                        await UniTask.SwitchToMainThread();
                    }
                    finally
                    {
                        // Clean up this bloc immediately if we loaded it and protocol wasn't loaded externally
                        if (!protocolWasLoadedExternally && !blocWasLoadedExternally && newlyLoadedBlocs.Contains(blocItem.Name))
                        {
                            Object3DManager.Localizers.UnloadSpecificBlocs(protocolItem.Name, dataType, new[] { blocItem.Name });
                        }
                    }

                    processedBlocs++;
                }
            }

            // Final progress update
            progress?.Invoke(1.0f, 0, new LoadingText($"Completed: generated curves for {processedBlocs} blocs and {sites.Count} sites"));

            return result;
        }
        public async UniTask<Dictionary<ChannelStruct, List<LocalizerCurveData>>> GenerateLocalizersGraphsRegionAsync(int precision, string dataType, List<ProtocolItem> protocolItems, RescalingParameters rescalingParams, Action<float, float, LoadingText> progress, CancellationToken token)
        {
            Dictionary<ChannelStruct, List<LocalizerCurveData>> result = new();
            var sites = GetSceneSites();

            // Calculate total work units for progress tracking
            var selectedProtocols = protocolItems.Where(p => p.IsSelected).ToList();
            var totalBlocs = selectedProtocols.Sum(p => p.SelectedBlocs.Count);

            if (totalBlocs == 0)
            {
                return result;
            }

            progress?.Invoke(0.0f, 0, new LoadingText("Initializing localizer graphs generation"));

            int processedBlocs = 0;
            int blocIndex = 0;
            int totalNumberOfBlocs = selectedProtocols.Sum(p => p.SelectedBlocs.Count);

            foreach (var protocolItem in selectedProtocols)
            {
                var selectedBlocNames = protocolItem.SelectedBlocs.Select(b => b.Name).ToList();

                if (selectedBlocNames.Count == 0)
                    continue;

                var protocolWasLoadedExternally = Object3DManager.Localizers.Protocols.Any(p => p.Name == protocolItem.Name);
                var protocol = Object3DManager.Localizers.Protocols.FirstOrDefault(p => p.Name == protocolItem.Name);
                var data = protocol?.Datas.FirstOrDefault(d => d.Name == dataType);

                foreach (var blocItem in protocolItem.SelectedBlocs)
                {
                    token.ThrowIfCancellationRequested();
                    blocIndex++;

                    // Progress for loading phase
                    float loadingProgress = (float)(processedBlocs + 1) / totalBlocs;
                    progress?.Invoke(loadingProgress, 2f, new LoadingText("Loading bloc ", $"{blocItem.Name} from {protocolItem.Name}", $" [{blocIndex}/{totalNumberOfBlocs}]"));

                    // Check if bloc is already loaded
                    var existingBloc = data?.Blocs.FirstOrDefault(b => b.Name == blocItem.Name);
                    bool blocWasLoadedExternally = existingBloc?.Loaded ?? false;

                    // Load only this specific bloc
                    var newlyLoadedBlocs = await Object3DManager.Localizers.LoadSpecificBlocsAsync(protocolItem.Name, dataType, new[] { blocItem.Name });

                    // Get the protocol and data after loading
                    protocol = Object3DManager.Localizers.Protocols.FirstOrDefault(p => p.Name == protocolItem.Name);
                    if (protocol == null)
                        throw new HBPException("Protocol not found", $"The protocol '{protocolItem.Name}' could not be loaded.\n\nPlease check your Localizers Atlas installation.");

                    data = protocol.Datas.FirstOrDefault(d => d.Name == dataType);
                    if (data == null)
                        throw new HBPException("Data not found", $"The data '{dataType}' was not found in the protocol '{protocolItem.Name}'.\n\nPlease check your Localizers Atlas installation.");

                    var bloc = data.Blocs.FirstOrDefault(b => b.Name == blocItem.Name);
                    if (bloc == null)
                        throw new HBPException("Bloc not found", $"The bloc '{blocItem.Name}' was not found in the protocol '{protocolItem.Name}'.\n\nPlease check your Localizers Atlas installation.");

                    try
                    {
                        // Extract all the data we need from this bloc
                        await UniTask.SwitchToThreadPool();
                        var times = GetTimes(bloc.FMRI);
                        foreach (var site in sites)
                        {
                            var channelStruct = new ChannelStruct(site.Information.Name, site.Information.Patient);
                            if (!result.TryGetValue(channelStruct, out List<LocalizerCurveData> curves))
                            {
                                curves = new List<LocalizerCurveData>();
                                result[channelStruct] = curves;
                            }
                            var (values, rawValues) = GetRegionData(site.Information.DefaultPosition, bloc.FMRI, precision);

                            // Apply rescaling if enabled
                            if (rescalingParams.EnableRescaling)
                            {
                                ApplyRescaling(values, rescalingParams);
                                for (int i = 0; i < rawValues.Length; i++)
                                {
                                    ApplyRescaling(rawValues[i], rescalingParams);
                                }
                            }
                            
                            var sem = rawValues.Select(rv => rv.SEM()).ToArray();

                            var points = new List<Vector2>();
                            for (int i = 0; i < times.Length; i++)
                            {
                                points.Add(new Vector2(times[i], values[i]));
                            }
                            curves.Add(new LocalizerCurveData(dataType, protocolItem.Name, blocItem.Name, points.ToArray(), sem.ToArray()));
                        }
                        await UniTask.SwitchToMainThread();
                    }
                    finally
                    {
                        // Clean up this bloc immediately if we loaded it and protocol wasn't loaded externally
                        if (!protocolWasLoadedExternally && !blocWasLoadedExternally && newlyLoadedBlocs.Contains(blocItem.Name))
                        {
                            Object3DManager.Localizers.UnloadSpecificBlocs(protocolItem.Name, dataType, new[] { blocItem.Name });
                        }
                    }

                    processedBlocs++;
                }
            }

            // Final progress update
            progress?.Invoke(1.0f, 0, new LoadingText($"Completed: generated curves for {processedBlocs} blocs and {sites.Count} sites"));

            return result;
        }
        public async UniTask<Dictionary<ChannelStruct, List<LocalizerCurveData>>> GenerateLocalizersGraphsAtlasAsync(LocalizersGraphsAtlas atlas, string dataType, List<ProtocolItem> protocolItems, RescalingParameters rescalingParams, Action<float, float, LoadingText> progress, CancellationToken token)
        {
            Dictionary<ChannelStruct, List<LocalizerCurveData>> result = new();
            BrainAtlas selectedAtlas = atlas switch
            {
                LocalizersGraphsAtlas.MarsAtlas => Object3DManager.MarsAtlas,
                LocalizersGraphsAtlas.Jubrain => Object3DManager.JuBrain,
                _ => null,
            };
            if (selectedAtlas == null) throw new HBPException("Atlas not found", $"The atlas '{atlas}' is not available.");

            if (!selectedAtlas.Loaded) selectedAtlas.Load();

            var sites = GetSceneSites();

            // Calculate total work units for progress tracking
            var selectedProtocols = protocolItems.Where(p => p.IsSelected).ToList();
            var totalBlocs = selectedProtocols.Sum(p => p.SelectedBlocs.Count);

            if (totalBlocs == 0)
            {
                return result;
            }

            progress?.Invoke(0.0f, 0, new LoadingText("Initializing atlas-based localizer graphs generation"));

            // Step 1: Get the list of regions we need to retrieve data for
            var regionIndices = new HashSet<int>();
            foreach (var site in sites)
            {
                int regionIndex = selectedAtlas.GetClosestAreaIndex(site.Information.DefaultPosition, 1);
                if (regionIndex > 0)
                {
                    regionIndices.Add(regionIndex);
                }
            }

            if (regionIndices.Count == 0)
            {
                progress?.Invoke(1.0f, 0, new LoadingText("No valid atlas regions found for any sites"));
                return result;
            }

            // Step 2: Get voxels for each region
            var regionVoxels = new Dictionary<int, Vector3[]>();
            int processedRegions = 0;
            foreach (var regionIndex in regionIndices)
            {
                token.ThrowIfCancellationRequested();
                progress?.Invoke(0.2f, 1f, new LoadingText($"Getting coordinates of atlas areas"));
                
                var coordinates = selectedAtlas.GetAreaCoordinates(regionIndex);
                if (coordinates.Length > 0)
                {
                    regionVoxels[regionIndex] = coordinates;
                }
                processedRegions++;
            }

            int processedBlocs = 0;
            int blocIndex = 0;
            int totalNumberOfBlocs = selectedProtocols.Sum(p => p.SelectedBlocs.Count);

            foreach (var protocolItem in selectedProtocols)
            {
                var selectedBlocNames = protocolItem.SelectedBlocs.Select(b => b.Name).ToList();

                if (selectedBlocNames.Count == 0)
                    continue;

                var protocolWasLoadedExternally = Object3DManager.Localizers.Protocols.Any(p => p.Name == protocolItem.Name);
                var protocol = Object3DManager.Localizers.Protocols.FirstOrDefault(p => p.Name == protocolItem.Name);
                var data = protocol?.Datas.FirstOrDefault(d => d.Name == dataType);

                foreach (var blocItem in protocolItem.SelectedBlocs)
                {
                    token.ThrowIfCancellationRequested();
                    blocIndex++;

                    // Progress for loading phase
                    float baseProgress = 0.2f + 0.8f * (processedBlocs + 1) / totalBlocs;
                    progress?.Invoke(baseProgress, 2f, new LoadingText("Loading bloc ", $"{blocItem.Name} from {protocolItem.Name}", $" [{blocIndex}/{totalNumberOfBlocs}]"));

                    // Check if bloc is already loaded
                    var existingBloc = data?.Blocs.FirstOrDefault(b => b.Name == blocItem.Name);
                    bool blocWasLoadedExternally = existingBloc?.Loaded ?? false;

                    // Load only this specific bloc
                    var newlyLoadedBlocs = await Object3DManager.Localizers.LoadSpecificBlocsAsync(protocolItem.Name, dataType, new[] { blocItem.Name });

                    // Get the protocol and data after loading
                    protocol = Object3DManager.Localizers.Protocols.FirstOrDefault(p => p.Name == protocolItem.Name);
                    if (protocol == null)
                        throw new HBPException("Protocol not found", $"The protocol '{protocolItem.Name}' could not be loaded.\n\nPlease check your Localizers Atlas installation.");

                    data = protocol.Datas.FirstOrDefault(d => d.Name == dataType);
                    if (data == null)
                        throw new HBPException("Data not found", $"The data '{dataType}' was not found in the protocol '{protocolItem.Name}'.\n\nPlease check your Localizers Atlas installation.");

                    var bloc = data.Blocs.FirstOrDefault(b => b.Name == blocItem.Name);
                    if (bloc == null)
                        throw new HBPException("Bloc not found", $"The bloc '{blocItem.Name}' was not found in the protocol '{protocolItem.Name}'.\n\nPlease check your Localizers Atlas installation.");

                    try
                    {
                        // Extract all the data we need from this bloc
                        await UniTask.SwitchToThreadPool();
                        var times = GetTimes(bloc.FMRI);
                        
                        // Step 3 & 4: Get values for each region and calculate mean and SEM
                        var regionData = new Dictionary<int, (float[] meanValues, float[] semValues)>();
                        
                        foreach (var kvp in regionVoxels)
                        {
                            int regionIndex = kvp.Key;
                            Vector3[] voxels = kvp.Value;
                            
                            float[] meanValues = new float[bloc.FMRI.Volumes.Count];
                            float[] semValues = new float[bloc.FMRI.Volumes.Count];
                            
                            // For each volume, get all voxel values in this region
                            for (int volumeIndex = 0; volumeIndex < bloc.FMRI.Volumes.Count; volumeIndex++)
                            {
                                var voxelValues = new List<float>();
                                
                                foreach (var voxel in voxels)
                                {
                                    float value = bloc.FMRI.Volumes[volumeIndex].GetValueFromPosition(voxel);
                                    float maskValue = bloc.FMRI.MaskVolume.GetValueFromPosition(voxel);
                                    if (maskValue > 0)
                                    {
                                        voxelValues.Add(value);
                                    }
                                }

                                float[] finalValues = voxelValues.ToArray();

                                // Apply rescaling if enabled
                                if (rescalingParams.EnableRescaling)
                                {
                                    ApplyRescaling(finalValues, rescalingParams);
                                }

                                if (voxelValues.Count > 0)
                                {
                                    meanValues[volumeIndex] = finalValues.Mean();
                                    semValues[volumeIndex] = finalValues.SEM();
                                }
                                else
                                {
                                    meanValues[volumeIndex] = 0f;
                                    semValues[volumeIndex] = 0f;
                                }
                            }
                            
                            
                            regionData[regionIndex] = (meanValues, semValues);
                        }
                        
                        // Step 5: Assign data to sites based on their region
                        foreach (var site in sites)
                        {
                            var channelStruct = new ChannelStruct(site.Information.Name, site.Information.Patient);
                            if (!result.TryGetValue(channelStruct, out List<LocalizerCurveData> curves))
                            {
                                curves = new List<LocalizerCurveData>();
                                result[channelStruct] = curves;
                            }
                            
                            int siteRegionIndex = selectedAtlas.GetClosestAreaIndex(site.Information.DefaultPosition, 1);
                            if (siteRegionIndex >= 0 && regionData.TryGetValue(siteRegionIndex, out var siteRegionData))
                            {
                                var points = new List<Vector2>();
                                for (int i = 0; i < times.Length; i++)
                                {
                                    points.Add(new Vector2(times[i], siteRegionData.meanValues[i]));
                                }
                                curves.Add(new LocalizerCurveData(dataType, protocolItem.Name, blocItem.Name, points.ToArray(), siteRegionData.semValues));
                            }
                        }
                        
                        await UniTask.SwitchToMainThread();
                    }
                    finally
                    {
                        // Clean up this bloc immediately if we loaded it and protocol wasn't loaded externally
                        if (!protocolWasLoadedExternally && !blocWasLoadedExternally && newlyLoadedBlocs.Contains(blocItem.Name))
                        {
                            Object3DManager.Localizers.UnloadSpecificBlocs(protocolItem.Name, dataType, new[] { blocItem.Name });
                        }
                    }

                    processedBlocs++;
                }
            }

            // Final progress update
            progress?.Invoke(1.0f, 0, new LoadingText($"Completed: generated atlas-based curves for {processedBlocs} blocs and {sites.Count} sites using {regionIndices.Count} regions"));

            return result;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Apply rescaling to an array of values using the formula: newValue = (oldValue - baseline) * gain + baseline + offset
        /// </summary>
        /// <param name="values">Array of values to rescale (modified in place)</param>
        /// <param name="rescalingParams">Rescaling parameters</param>
        private void ApplyRescaling(float[] values, RescalingParameters rescalingParams)
        {
            if (!rescalingParams.EnableRescaling || values == null || values.Length == 0)
                return;

            for (int i = 0; i < values.Length; i++)
            {
                // Apply rescaling formula: newValue = (oldValue - baseline) * gain + baseline + offset
                values[i] = (values[i] - rescalingParams.BaselineValue) * rescalingParams.GainFactor + rescalingParams.BaselineValue + rescalingParams.Offset;
            }
        }
        private List<Site> GetSceneSites()
        {
            return Module3DMain.SelectedScene.Columns.SelectMany(c => c.Sites).Where(s => !s.State.IsMasked).GroupBy(s => s.Information.FullID).Select(g => g.First()).ToList();
        }
        private float[] GetTimes(FMRI fmri)
        {
            float[] values = new float[fmri.Volumes.Count];
            for (int i = 0; i < fmri.Volumes.Count; i++)
            {
                values[i] = fmri.StartTime + i * fmri.TimeStep;
            }
            return values;
        }
        private float[] GetVoxelData(Vector3 voxel, FMRI fmri)
        {
            float[] values = new float[fmri.Volumes.Count];
            for (int i = 0; i < fmri.Volumes.Count; i++)
            {
                values[i] = fmri.Volumes[i].GetValueFromPosition(voxel);
            }
            return values;
        }
        private (float[], float[][]) GetRegionData(Vector3 voxel, FMRI fmri, int precision)
        {
            int numberOfVoxels = (int)Math.Pow((2 * precision + 1), 3);
            float[] values = new float[fmri.Volumes.Count];
            float[][] rawValues = new float[fmri.Volumes.Count][];
            for (int i = 0; i < fmri.Volumes.Count; i++)
            {
                rawValues[i] = new float[numberOfVoxels];
            }
            for (int i = 0; i < fmri.Volumes.Count; i++)
            {
                int actualLength = 0;
                values[i] = fmri.Volumes[i].GetAverageValueAroundPositionWithMask(voxel, precision, fmri.MaskVolume, ref rawValues[i], ref actualLength);
                if (actualLength > 0 && actualLength < numberOfVoxels)
                {
                    Array.Resize(ref rawValues[i], actualLength);
                }
            }
            return (values, rawValues);
        }
        #endregion
    }
}