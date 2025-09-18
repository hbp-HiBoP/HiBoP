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
        public CurveData Curve;

        public LocalizerCurveData(string dataType, string protocolName, string blocName, CurveData curve)
        {
            DataType = dataType;
            ProtocolName = protocolName;
            BlocName = blocName;
            Curve = curve;
        }
    }

    public class LocalizersGraphsWorker
    {
        #region Public Methods
        public async UniTask<Dictionary<ChannelStruct, List<LocalizerCurveData>>> GenerateLocalizersGraphsVoxelAsync(string dataType, List<ProtocolItem> protocolItems, Action<float, float, LoadingText> progress)
        {
            Dictionary<ChannelStruct, List<LocalizerCurveData>> result = new Dictionary<ChannelStruct, List<LocalizerCurveData>>();
            var sites = GetSceneSites();
            foreach (var protocolItem in protocolItems.Where(p => p.IsSelected))
            {
                var loadedExternally = false;
                var protocol = Object3DManager.Localizers.Protocols.FirstOrDefault(p => p.Name == protocolItem.Name);
                if (protocol == null)
                {
                    Object3DManager.Localizers.Load(protocolItem.Name);
                    protocol = Object3DManager.Localizers.Protocols.FirstOrDefault(p => p.Name == protocolItem.Name);
                }
                else
                {
                    loadedExternally = true;
                }
                await UniTask.WaitUntil(() => protocol.Loaded);

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
                        var curve = CurveData.CreateInstance(points, PersistentDataManager.UserPreferences.Visualization.Graph.LocalizersColors.GetColor(0, data.Blocs.IndexOf(bloc)));
                        curves.Add(new LocalizerCurveData(dataType, protocolItem.Name, blocItem.Name, curve));
                    }
                }

                if (!loadedExternally)
                {
                    Object3DManager.Localizers.Unload(protocolItem.Name);
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