using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using HBP.Core.DLL;
using HBP.Core.Object3D;
using HBP.Core.Tools;
using UnityEngine;
using AtlasRegionKey = System.ValueTuple<HBP.Data.Module3D.TriangleEraser.AtlasSource, string, int>;

namespace HBP.Data.Module3D
{
    public sealed partial class TriangleEraser
    {
        private Dictionary<AtlasRegionKey, FaceChanges> m_AtlasRegions = new();

        private Dictionary<AtlasRegionKey, FaceChanges> ClearAtlasRegions()
        {
            if (m_AtlasRegions.Count == 0) return null;
            var previous = m_AtlasRegions;
            m_AtlasRegions = new();
            return previous;
        }

        public bool IsAtlasRegionSelected(AtlasSource source, string version, int id) => m_AtlasRegions.ContainsKey(new AtlasRegionKey(source, version, id));

        public enum AtlasSource
        {
            MarsAtlas,
            JuBrain,
            DiFuMo
        }

        public readonly struct AtlasRegion
        {
            public int Id { get; }
            public string Name { get; }

            public AtlasRegion(int id, string name)
            {
                Id = id;
                Name = name;
            }
        }

        internal UniTask SelectionWork { get; private set; } = UniTask.CompletedTask;
        public bool IsComputingSelection { get; private set; }
        public bool CanSelectActivity => m_Scene.HasSurfaceActivityProjection && !m_Scene.IsActivityProjectionBusy && SurfaceGenerator.SupportsReadActivity;

        public bool IsAtlasAvailable(AtlasSource source)
        {
            if (m_Scene.MeshManager.ReferenceSurface == null || m_Scene.MeshManager.Meshes.Count == 0) return false;
            var mesh = m_Scene.MeshManager.SelectedMesh;
            return source switch
            {
                AtlasSource.MarsAtlas => mesh.SupportsMarsAtlas && Object3DManager.MarsAtlas.Loaded,
                AtlasSource.JuBrain => mesh.SupportsMNIResources && Object3DManager.JuBrain.Loaded,
                AtlasSource.DiFuMo => mesh.SupportsMNIResources && Object3DManager.DiFuMo.FMRIs.Keys.Any(Object3DManager.DiFuMo.IsLoaded),
                _ => false
            };
        }

        public string[] GetLoadedDiFuMoVersions() => Object3DManager.DiFuMo.FMRIs.Keys.Where(Object3DManager.DiFuMo.IsLoaded).OrderBy(version => int.Parse(version)).ToArray();

        private bool CanReadAtlas(AtlasSource source, string version) => IsAtlasAvailable(source) && (source != AtlasSource.DiFuMo || version != null && Object3DManager.DiFuMo.IsLoaded(version));

        public AtlasRegion[] GetAtlasRegions(AtlasSource source, string version)
        {
            if (!CanReadAtlas(source, version)) return Array.Empty<AtlasRegion>();
            if (source == AtlasSource.DiFuMo)
                return Object3DManager.DiFuMo.Information[version].AllLabels.Where(label => !string.IsNullOrWhiteSpace(label.Name)).Select(label => new AtlasRegion(label.Component, label.Name)).OrderBy(region => region.Name).ToArray();

            BrainAtlas atlas = GetAtlas(source);
            return GetAtlasLabels(atlas).Where(label => label > 0).Distinct().Select(label => new AtlasRegion(label, atlas.GetAreaName(label))).Where(region => !string.IsNullOrWhiteSpace(region.Name)).OrderBy(region => region.Name).ToArray();
        }

        private BrainAtlas m_LabelAtlas;
        private Surface m_LabelSurface;
        private long m_LabelGeometryVersion;
        private int[] m_AtlasLabels;

        private int[] GetAtlasLabels(BrainAtlas atlas)
        {
            Surface reference = m_Scene.MeshManager.ReferenceSurface;
            if (!ReferenceEquals(atlas, m_LabelAtlas) || !ReferenceEquals(reference, m_LabelSurface) || m_LabelGeometryVersion != reference.GeometryVersion)
            {
                m_AtlasLabels = atlas.GetSurfaceAreaLabels(reference);
                m_LabelAtlas = atlas;
                m_LabelSurface = reference;
                m_LabelGeometryVersion = reference.GeometryVersion;
            }

            return m_AtlasLabels;
        }

        private static BrainAtlas GetAtlas(AtlasSource source) => source == AtlasSource.MarsAtlas ? Object3DManager.MarsAtlas : Object3DManager.JuBrain;

        public void SetAtlasRegionSelected(AtlasSource source, string version, int id, bool selected)
        {
            var key = new AtlasRegionKey(source, version, id);
            if (!IsEnabled || IsComputingSelection || !CanReadAtlas(source, version) || m_AtlasRegions.TryGetValue(key, out var region) == selected) return;
            if (!selected)
            {
                var previous = m_AtlasRegions;
                m_AtlasRegions = new(previous);
                m_AtlasRegions.Remove(key);
                var retained = new BitArray(m_Visibility.Length);
                foreach (var other in m_AtlasRegions.Values)
                foreach (int face in other.Faces())
                    retained[face] = true;
                Retouch(region.Faces().Where(face => !retained[face]), remove: true, previousRegions: previous);
                return;
            }

            int[] triangles = Geometry.Triangles;
            if (source != AtlasSource.DiFuMo)
            {
                int[] labels = GetAtlasLabels(GetAtlas(source));
                AddAtlasRegion(key, Enumerable.Range(0, triangles.Length / 3).Where(MatchesLabel).ToList());
                return;

                bool MatchesLabel(int face)
                {
                    int a = labels[triangles[face * 3]];
                    int b = labels[triangles[face * 3 + 1]];
                    int c = labels[triangles[face * 3 + 2]];
                    return (a == id && (b == id || c == id)) || (b == id && c == id);
                }
            }

            var volumes = Object3DManager.DiFuMo.FMRIs[version].Volumes;
            if ((uint)id >= (uint)volumes.Count) return;
            Volume component = volumes[id];
            Surface reference = m_Scene.MeshManager.ReferenceSurface;
            float threshold = Mathf.Clamp01(DiFuMoThreshold);
            int revision = m_EditRevision;
            StartSelection(ReadDiFuMo);

            async UniTask ReadDiFuMo()
            {
                using var lease = ResourceRetention.Retain("difumo:" + version);
                using var snapshot = (Surface)reference.Clone();
                await UniTask.SwitchToThreadPool();
                float[] values = component.GetVerticesValues(snapshot);
                var faces = new List<int>();
                int count = triangles.Length / 3;
                float cutoff = threshold * component.ExtremeValues.Max;
                for (int face = 0; face < count; face++)
                {
                    float value = Mean(values, triangles, face);
                    if (float.IsFinite(value) && value > 0f && value >= cutoff) faces.Add(face);
                }

                await UniTask.SwitchToMainThread();
                if (CanPublish(reference, revision) && CanReadAtlas(source, version))
                    AddAtlasRegion(key, faces);
            }
        }

        private void AddAtlasRegion(AtlasRegionKey key, List<int> faces)
        {
            var previous = m_AtlasRegions;
            m_AtlasRegions = new(previous) { [key] = new FaceChanges(faces, m_Visibility.Length) };
            Retouch(faces, previousRegions: previous);
        }

        private void SelectActivity(Column3D column, int seed)
        {
            ActivityGenerator activity = column ? column.ActivityGenerator : null;
            if (!CanSelectActivity || activity?.ProjectionGrid == null || !m_Scene.TryBeginSensitiveActivityOperation(out IDisposable lease)) return;
            int sample = column switch
            {
                Column3DDynamic dynamic => dynamic.CurrentProjectionSample.Index,
                Column3DFMRI fmri => fmri.SelectedVolumeIndex,
                Column3DMEG meg => meg.SelectedVolumeIndex,
                Column3DStatic stat => stat.SelectedLabelIndex,
                _ => 0
            };
            Surface reference = m_Scene.MeshManager.ReferenceSurface;
            int[] triangles = Geometry.Triangles;
            float tolerance = Mathf.Clamp(ActivityTolerance, 0f, 100f) / 100f;
            int revision = m_EditRevision;
            ulong activityGeneration = m_Scene.ActivityInputGeneration;
            StartSelection(ReadActivity);

            async UniTask ReadActivity()
            {
                using (lease)
                using (var reader = new SurfaceGenerator())
                {
                    using var snapshot = (Surface)reference.Clone();
                    reader.Initialize(activity, snapshot);
                    float[] values;
                    byte[] valid;
                    try
                    {
                        await UniTask.SwitchToThreadPool();
                        (values, valid) = reader.ReadActivity(sample);
                    }
                    finally
                    {
                        await UniTask.SwitchToMainThread();
                    }

                    if (!CanPublish(reference, revision) || !CanSelectActivity || !column || column.ActivityGenerator != activity || m_Scene.ActivityInputGeneration != activityGeneration) return;
                    if (!IsValid(seed)) return;

                    float initial = Mean(values, triangles, seed);
                    float range = Math.Abs(initial) * tolerance;
                    Retouch(Propagate(seed, face => IsValid(face) && Math.Abs(Mean(values, triangles, face) - initial) <= range));

                    bool IsValid(int face) => valid[triangles[face * 3]] != 0 && valid[triangles[face * 3 + 1]] != 0 && valid[triangles[face * 3 + 2]] != 0;
                }
            }
        }

        private bool CanPublish(Surface reference, int revision) => IsEnabled && revision == m_EditRevision && !m_Scene.IsClosing && !m_Scene.SceneInformation.GeometryNeedsUpdate && ReferenceEquals(reference, m_Scene.MeshManager.ReferenceSurface);

        private static float Mean(float[] values, int[] triangles, int face) => (float)(((double)values[triangles[face * 3]] + values[triangles[face * 3 + 1]] + values[triangles[face * 3 + 2]]) / 3d);

        private void StartSelection(Func<UniTask> work)
        {
            IsComputingSelection = true;
            SelectionWork = RunSelection(work).ToAsyncLazy().Task;
            Module3DMain.OnRequestUpdateInToolbar.Invoke();
        }

        private async UniTask RunSelection(Func<UniTask> work)
        {
            try
            {
                await work();
            }
            catch (Exception exception)
            {
                await UniTask.SwitchToMainThread();
                Debug.LogException(exception);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                IsComputingSelection = false;
                if (!m_Scene.IsClosing) Module3DMain.OnRequestUpdateInToolbar.Invoke();
            }
        }
    }
}
