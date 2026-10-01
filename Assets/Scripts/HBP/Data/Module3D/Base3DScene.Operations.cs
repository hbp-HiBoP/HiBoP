using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;
using UnityEngine;

namespace HBP.Data.Module3D
{
    public enum SitePositionCommand : byte
    {
        MoveLeft = 1,
        MoveRight = 2,
        Reset = 3
    }

    public partial class Base3DScene
    {
        /// <summary>Reports a completed command that changes the positions of scene sites.</summary>
        public event Action<SitePositionCommand> SitePositionCommandExecuted;

        /// <summary>Resolve a target independently of Desktop selection. Global means the same modality.</summary>
        public List<Column3D> GetColumnGroup(Column3D target, bool allOfSameType)
        {
            RequireColumn(target);
            return allOfSameType ? Columns.Where(column => column.GetType() == target.GetType()).ToList() : new List<Column3D> { target };
        }

        private void RequireColumn(Column3D column)
        {
            if (m_DestroyRequested) throw new ObjectDisposedException(nameof(Base3DScene));
            if (!column || !Columns.Contains(column)) throw new ArgumentException("The target column must belong to this scene.", nameof(column));
        }

        public void SelectColumn(Column3D column)
        {
            RequireColumn(column);
            column.IsSelected = true;
        }

        public void SelectSite(Column3D column, Core.Object3D.Site site)
        {
            RequireColumn(column);
            if (site && (!column.Sites.Contains(site) || site.State.IsMasked))
                throw new ArgumentException("Select an unmasked site belonging to the target column.", nameof(site));
            column.IsSelected = true;
            column.UnselectSite();
            if (site) site.IsSelected = true;
        }

        /// <summary>Apply a prepared scientific selection while derived site masks may still reflect the previous state.</summary>
        public void SelectSiteForSynchronization(Column3D column, Core.Object3D.Site site)
        {
            RequireColumn(column);
            if (site && !column.Sites.Contains(site))
                throw new ArgumentException("Select a site belonging to the target column.", nameof(site));
            column.IsSelected = true;
            column.UnselectSite();
            if (site) site.IsSelected = true;
        }

        public void SetTimelineIndex(Column3D target, int index, bool allOfSameType = false)
        {
            foreach (var timeline in GetTimelines(target, allOfSameType)) timeline.CurrentIndex = index;
        }

        public void AdvanceTimeline(Column3D target, int direction, bool allOfSameType = false)
        {
            foreach (var timeline in GetTimelines(target, allOfSameType)) timeline.CurrentIndex += direction * timeline.Step;
        }

        public void SetTimelinePlaying(Column3D target, bool playing, bool allOfSameType = false)
        {
            foreach (var timeline in GetTimelines(target, allOfSameType)) timeline.IsPlaying = playing;
            OnSharedStateChanged.Invoke();
        }

        public void SetTimelineLooping(Column3D target, bool looping, bool allOfSameType = false)
        {
            foreach (var timeline in GetTimelines(target, allOfSameType)) timeline.IsLooping = looping;
            OnSharedStateChanged.Invoke();
        }

        public void SetTimelineStep(Column3D target, int step, bool allOfSameType = false)
        {
            foreach (var timeline in GetTimelines(target, allOfSameType)) timeline.Step = Math.Max(1, step);
            OnSharedStateChanged.Invoke();
        }

        private IEnumerable<BasicTimeline> GetTimelines(Column3D target, bool allOfSameType)
        {
            // Snapshot the group before callbacks can change the selected column or scene.
            return GetColumnGroup(target, allOfSameType).Select(column => column.NavigationTimeline).Where(timeline => timeline != null && timeline.Length > 0).Distinct().ToArray();
        }

        public void MoveSitesToHemisphere(bool right)
        {
            Vector3 normal = MRIManager.SelectedMRI.Volume.GetOrientationVector(CutOrientation.Sagittal, right);
            Vector3 center = MeshManager.MeshCenter;
            foreach (var column in Columns) column.MoveAllSitesToTheSameSideOfAPlane(center, normal);
            OnSharedStateChanged.Invoke();
            SitePositionCommandExecuted?.Invoke(right ? SitePositionCommand.MoveRight : SitePositionCommand.MoveLeft);
        }

        public void ResetSitesPositions()
        {
            foreach (var column in Columns) column.ResetSitesPositions();
            OnSharedStateChanged.Invoke();
            SitePositionCommandExecuted?.Invoke(SitePositionCommand.Reset);
        }

        public void ResetSiteFilters(bool included = true)
        {
            ++m_FilterRequest;
            foreach (var column in Columns)
            foreach (var site in column.Sites)
                site.State.IsFiltered = included;
            Module3DMain.OnRequestUpdateInSiteList.Invoke();
        }

        private int m_FilterRequest;

        public UniTask ComputeCorrelationsAsync(Action<float, float, Core.Tools.LoadingText> progress, CancellationToken cancellationToken = default)
        {
            if (m_DestroyRequested) throw new ObjectDisposedException(nameof(Base3DScene));
            if (!m_CorrelationWork.Status.IsCompleted()) throw new InvalidOperationException("Correlations are already computing.");
            m_CorrelationWork = ComputeSceneCorrelationsAsync(progress, cancellationToken).ToAsyncLazy().Task;
            return m_CorrelationWork;
        }

        private async UniTask ComputeSceneCorrelationsAsync(Action<float, float, Core.Tools.LoadingText> progress, CancellationToken cancellationToken)
        {
            IReadOnlyList<CorrelationResultData> results = await ComputeCorrelationResultsCoreAsync(progress, cancellationToken);
            await UniTask.SwitchToMainThread();
            cancellationToken.ThrowIfCancellationRequested();
            ApplyCorrelationResults(results);
            DisplayCorrelations = true;
            Module3DMain.OnRequestUpdateInToolbar.Invoke();
        }

        /// <summary>Computes every IEEG column into detached buffers and tracks the work with the scene lifetime.</summary>
        public UniTask<IReadOnlyList<CorrelationResultData>> ComputeCorrelationResultsAsync(Action<float, float, Core.Tools.LoadingText> progress, CancellationToken cancellationToken = default)
        {
            if (m_DestroyRequested) throw new ObjectDisposedException(nameof(Base3DScene));
            if (!m_CorrelationWork.Status.IsCompleted()) throw new InvalidOperationException("Correlations are already computing.");
            var result = ComputeCorrelationResultsCoreAsync(progress, cancellationToken).ToAsyncLazy();
            m_CorrelationWork = AwaitCorrelationResultsAsync(result.Task).ToAsyncLazy().Task;
            return result.Task;
        }

        private async UniTask<IReadOnlyList<CorrelationResultData>> ComputeCorrelationResultsCoreAsync(Action<float, float, Core.Tools.LoadingText> progress, CancellationToken cancellationToken)
        {
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_SurfaceRepresentationLifetime.Token);
            var columns = ColumnsIEEG.ToArray();
            var results = new CorrelationResultData[columns.Length];
            for (int i = 0; i < columns.Length; i++)
            {
                stop.Token.ThrowIfCancellationRequested();
                results[i] = await columns[i].ComputeCorrelationResultAsync((value, duration, text) => progress?.Invoke(columns.Length == 0 ? 1f : (i + value) / columns.Length, duration, text), stop.Token);
            }

            await UniTask.SwitchToMainThread();
            stop.Token.ThrowIfCancellationRequested();
            if (m_DestroyRequested || !ColumnsIEEG.SequenceEqual(columns)) throw new OperationCanceledException("The IEEG column roster changed during correlation computation.");
            return Array.AsReadOnly(results);
        }

        private static async UniTask AwaitCorrelationResultsAsync(UniTask<IReadOnlyList<CorrelationResultData>> results) => await results;

        /// <summary>Validates all columns first, then replaces every matrix and provenance record in one main-thread turn.</summary>
        public void ApplyCorrelationResults(IEnumerable<CorrelationResultData> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            if (m_DestroyRequested) throw new ObjectDisposedException(nameof(Base3DScene));
            var columns = ColumnsIEEG.ToDictionary(column => column.ColumnData.ID, StringComparer.Ordinal);
            CorrelationResultData[] staged = results.ToArray();
            if (staged.Length != columns.Count || staged.Select(result => result.ColumnId).Distinct(StringComparer.Ordinal).Count() != columns.Count || staged.Any(result => !columns.ContainsKey(result.ColumnId)))
                throw new InvalidOperationException("Correlation results do not match the prepared IEEG column roster.");

            foreach (CorrelationResultData result in staged)
            {
                Column3DIEEG column = columns[result.ColumnId];
                if (!StringComparer.Ordinal.Equals(result.Provenance.ColumnId, column.ColumnData.ID)) throw new InvalidOperationException("Correlation provenance does not match the prepared column.");
                var ownedSites = new HashSet<Core.Object3D.Site>(column.Sites);
                ValidateCorrelationMatrix(result.Correlations, ownedSites);
                ValidateCorrelationMatrix(result.Means, ownedSites);
            }

            var matrices = staged.Select(result => (Column: columns[result.ColumnId], Correlations: result.Correlations.ToDictionary(row => row.Key, row => row.Value.ToDictionary(pair => pair.Key, pair => pair.Value)), Means: result.Means.ToDictionary(row => row.Key, row => row.Value.ToDictionary(pair => pair.Key, pair => pair.Value)), result.Provenance)).ToArray();
            foreach (var matrix in matrices)
            {
                matrix.Column.CorrelationBySitePair = matrix.Correlations;
                matrix.Column.CorrelationMeanBySitePair = matrix.Means;
                matrix.Column.CorrelationProvenance = matrix.Provenance;
            }
        }

        private static void ValidateCorrelationMatrix(IReadOnlyDictionary<Core.Object3D.Site, IReadOnlyDictionary<Core.Object3D.Site, float>> matrix, HashSet<Core.Object3D.Site> ownedSites)
        {
            foreach (var row in matrix)
            {
                if (!row.Key || !ownedSites.Contains(row.Key) || row.Value == null) throw new InvalidOperationException("A correlation row is outside its owning column.");
                foreach (var pair in row.Value)
                    if (!pair.Key || !ownedSites.Contains(pair.Key) || float.IsNaN(pair.Value) || float.IsInfinity(pair.Value))
                        throw new InvalidOperationException("A correlation pair is invalid or outside its owning column.");
            }
        }

        public void ResetCorrelations()
        {
            foreach (var column in ColumnsIEEG)
            {
                column.CorrelationBySitePair.Clear();
                column.CorrelationMeanBySitePair.Clear();
                column.CorrelationProvenance = null;
            }

            Module3DMain.OnRequestUpdateInToolbar.Invoke();
        }

        public async UniTask FilterSitesAsync(IEnumerable<Core.Object3D.Site> sites, Func<Core.Object3D.Site, bool> predicate, Action<float> progress = null, CancellationToken cancellationToken = default)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            int request = ++m_FilterRequest;
            var targets = sites.ToArray();
            var ownedSites = new HashSet<Core.Object3D.Site>(Columns.SelectMany(column => column.Sites));
            if (targets.Any(site => !ownedSites.Contains(site))) throw new ArgumentException("Sites must belong to this scene.", nameof(sites));
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_SurfaceRepresentationLifetime.Token);
            bool[] matches = new bool[targets.Length];
            // Predicates read Unity objects and native geometry. Keep them on the owning thread,
            // yielding between small batches instead of racing a UI queue against a worker.
            for (int i = 0; i < targets.Length; i++)
            {
                stop.Token.ThrowIfCancellationRequested();
                if (request != m_FilterRequest || !targets[i]) throw new OperationCanceledException();
                matches[i] = predicate(targets[i]);
                if ((i + 1) % 32 == 0)
                {
                    progress?.Invoke((float)(i + 1) / targets.Length);
                    await UniTask.Yield(PlayerLoopTiming.Update, stop.Token);
                }
            }

            stop.Token.ThrowIfCancellationRequested();
            if (request != m_FilterRequest) throw new OperationCanceledException();
            ownedSites = new HashSet<Core.Object3D.Site>(Columns.SelectMany(column => column.Sites));
            if (targets.Any(site => !site || !ownedSites.Contains(site))) throw new OperationCanceledException("The implantation changed during filtering.");
            for (int i = 0; i < targets.Length; i++) targets[i].State.IsFiltered = matches[i];
            progress?.Invoke(1);
            Module3DMain.OnRequestUpdateInSiteList.Invoke();
        }

        private static bool? CheckSpecificSiteLocation(SpecificSiteLocationFilterCondition condition, Core.Object3D.Site site)
        {
            var column = site ? site.GetComponentInParent<Column3D>() : null;
            if (!column) return false;
            // Resolve ownership without depending on Desktop selection or a column back-reference.
            var scene = s_LiveScenes.FirstOrDefault(candidate => candidate && !candidate.IsClosing && candidate.Columns.Contains(column));
            return scene != null ? scene.EvaluateSiteLocation(condition, site) : false;
        }

        public bool? EvaluateSiteLocation(SpecificSiteLocationFilterCondition condition, Core.Object3D.Site site)
        {
            switch (condition.LocationType)
            {
                case SpecificSiteLocationFilterCondition.SpecificLocationType.BrainMesh:
                    Core.DLL.Surface surface = condition.MeshPart switch
                    {
                        MeshPart.Both => MeshManager.SelectedMesh.SimplifiedBoth,
                        MeshPart.Left => MeshManager.SelectedMesh is Core.Object3D.LeftRightMesh3D left ? left.SimplifiedLeft : null,
                        MeshPart.Right => MeshManager.SelectedMesh is Core.Object3D.LeftRightMesh3D right ? right.SimplifiedRight : null,
                        _ => null
                    };
                    return surface != null && surface.IsPointInside(site.Information.DefaultPosition);
                case SpecificSiteLocationFilterCondition.SpecificLocationType.CutPlane:
                    return ImplantationManager.SelectedImplantation != null && ImplantationManager.SelectedImplantation.RawSiteList.IsSiteOnAnyPlane(site, Cuts.Cast<Core.DLL.Plane>().ToList(), 1.0f);
                default:
                    return null;
            }
        }
    }
}
