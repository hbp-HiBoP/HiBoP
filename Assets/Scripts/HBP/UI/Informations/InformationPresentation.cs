using System;
using System.Collections.Generic;
using System.Linq;
using HBP.Core.Tools;
using HBP.Data.Informations;
using HBP.Data.Informations.Graphs;
using HBP.Data.Module3D;
using HBP.UI.Informations.Graphs;
using UnityEngine;
using MatrixGrid = HBP.Data.Informations.TrialMatrix.TrialMatrixGrid;

namespace HBP.UI.Informations
{
    /// <summary>Scene-owned scientific presentation. Views never own the trial selection.</summary>
    internal sealed class InformationPresentation : IDisposable
    {
        private static readonly Dictionary<Base3DScene, InformationsWrapper> Sources = new();
        private readonly Dictionary<(Core.Data.Dataset, string, string, string), Vector2> limits = new();
        private readonly List<HBP.Data.Informations.TrialMatrix.ChannelBloc> selectionSources = new();
        private readonly Dictionary<string, bool> enabled = new();
        private Dictionary<ChannelStruct, List<LocalizerCurveData>> localizers = new();
        private readonly InformationGraphBuilder builder = new();
        internal MatrixGrid Matrices { get; private set; }
        internal Tuple<Graph.Curve[], TimeWindow, bool>[] Graphs { get; private set; } = Array.Empty<Tuple<Graph.Curve[], TimeWindow, bool>>();
        internal Tuple<Tuple<Core.Data.Bloc, Core.Data.SubBloc>[], TimeWindow>[] SubBlocsByColumn => builder.m_SubBlocsAndWindowByColumn ?? Array.Empty<Tuple<Tuple<Core.Data.Bloc, Core.Data.SubBloc>[], TimeWindow>>();
        internal long Revision { get; private set; }
        internal long CurvesRevision { get; private set; }
        internal long ContentRevision { get; private set; }
        internal long SamplesRevision { get; private set; }
        internal int ActiveGraph { get; set; }
        internal bool UseDefaultRange = true;
        internal Vector2 OrdinateRange;
        internal Vector2[] AbscissaRanges = Array.Empty<Vector2>();
        internal Column[] Columns { get; private set; } = Array.Empty<Column>();
        internal ChannelStruct[] Channels { get; private set; } = Array.Empty<ChannelStruct>();
        internal event Action Changed;
        private readonly Base3DScene scene;

        internal InformationPresentation(Base3DScene scene, InformationsWrapper owner)
        {
            this.scene = scene;
            Matrices = new MatrixGrid(Array.Empty<ChannelStruct>(), Array.Empty<MatrixGrid.TrialMatrixData>());
            if (scene != null) Sources[scene] = owner;
        }

        internal static InformationsWrapper Find(Base3DScene scene) => scene != null && Sources.TryGetValue(scene, out var owner) ? owner : null;

        internal void Touch()
        {
            Revision++;
            Changed?.Invoke();
        }

        internal Vector2 GetMatrixLimits(HBP.Data.Informations.TrialMatrix.Data data) => limits.TryGetValue(MatrixKey(data), out var value) ? value : data.Limits;

        internal void SetMatrixLimits(HBP.Data.Informations.TrialMatrix.Data data, Vector2 value)
        {
            if (value == data.Limits)
            {
                if (limits.Remove(MatrixKey(data))) Touch();
                return;
            }

            if (limits.TryGetValue(MatrixKey(data), out var existing) && existing == value) return;
            limits[MatrixKey(data)] = value;
            Touch();
        }

        private static (Core.Data.Dataset, string, string, string) MatrixKey(HBP.Data.Informations.TrialMatrix.Data data)
        {
            var source = (data.DataStruct as MatrixGrid.CCEPTrialMatrixData)?.Source;
            return (data.DataStruct.Dataset, data.DataStruct.Name, source?.Patient.ID ?? "", source?.Channel ?? "");
        }

        internal void AttachMatrices(MatrixGrid grid)
        {
            foreach (var channel in selectionSources) channel.OnChangeTrialSelected.RemoveListener(OnTrialSelection);
            selectionSources.Clear();
            Matrices = grid;
            foreach (var data in Matrices.Data)
            foreach (var bloc in data.Blocs)
            foreach (var channel in bloc.ChannelBlocs)
            {
                channel.OnChangeTrialSelected.AddListener(OnTrialSelection);
                selectionSources.Add(channel);
            }
        }

        private void OnTrialSelection()
        {
            CurvesRevision++;
            Touch();
        }

        internal void SetEnabled(string id, bool value)
        {
            enabled[id] = value;
            Touch();
        }

        internal void SetLocalizers(Dictionary<ChannelStruct, List<LocalizerCurveData>> value) => localizers = value;

        internal void Rebuild(ChannelStruct[] channels, Column[] columns, bool rebuildMatrices = true)
        {
            Channels = channels ?? Array.Empty<ChannelStruct>();
            Columns = columns ?? Array.Empty<Column>();
            if (rebuildMatrices || Matrices == null)
            {
                AttachMatrices(TrialMatrixZone.BuildData(Channels, Columns.Select(c => c.Data).ToArray()));
                SamplesRevision++;
            }

            RebuildGraphs();
        }

        private void RebuildGraphs()
        {
            builder.ClearListeners();
            ReleaseCurves();
            builder.m_Columns = Columns;
            builder.m_Channels = Channels;
            builder.m_LocalizersCurves = localizers;
            builder.Matrices = Matrices;
            Graphs = Columns.Length == 0 ? Array.Empty<Tuple<Graph.Curve[], TimeWindow, bool>>() : builder.GenerateDataCurve(Columns, Channels);
            ActiveGraph = Mathf.Clamp(ActiveGraph, 0, Math.Max(0, Graphs.Length - 1));
            foreach (var curve in AllCurves())
            {
                if (enabled.TryGetValue(curve.ID, out bool state)) curve.Enabled = state;
                curve.OnChangeIsActive.AddListener(state => SetEnabled(curve.ID, state));
            }

            CurvesRevision++;
            ContentRevision++;
            Touch();
        }

        internal IEnumerable<Graph.Curve> AllCurves() => Graphs.SelectMany(g => Flatten(g.Item1));

        private static IEnumerable<Graph.Curve> Flatten(IEnumerable<Graph.Curve> roots)
        {
            foreach (var root in roots)
            {
                yield return root;
                foreach (var child in Flatten(root.SubCurves)) yield return child;
            }
        }

        internal static void AddValues(Graph.Curve curve, ref Core.Data.RunningStatistics statistics)
        {
            if (curve.Data != null)
                for (int i = 0; i < curve.Data.Count; i++)
                    statistics.Add(curve.Data.GetOrdinate(i));
            foreach (var child in curve.SubCurves) AddValues(child, ref statistics);
        }

        private void ReleaseCurves()
        {
            foreach (var data in AllCurves().Select(c => c.Data).Where(d => d != null).Distinct())
                if (Application.isPlaying) UnityEngine.Object.Destroy(data);
                else UnityEngine.Object.DestroyImmediate(data);
        }

        public void Dispose()
        {
            if (!ReferenceEquals(scene, null)) Sources.Remove(scene);
            builder.ClearListeners();
            foreach (var channel in selectionSources) channel.OnChangeTrialSelected.RemoveListener(OnTrialSelection);
            selectionSources.Clear();
            ReleaseCurves();
            Graphs = Array.Empty<Tuple<Graph.Curve[], TimeWindow, bool>>();
            Matrices = null;
        }
    }
}
