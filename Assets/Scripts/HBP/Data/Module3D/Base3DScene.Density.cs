using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Core.Preferences;

namespace HBP.Data.Module3D
{
    public partial class Base3DScene
    {
        private bool m_PreparingDensityReferences;
        private UniTask m_DensityReferenceWork = UniTask.CompletedTask;

        private bool CanPrepareDensityReferences() => !m_DestroyRequested && !m_UpdatingGenerators && m_SensitiveActivityOperationCount == 0 && m_CoordinatedActivityProjectionBusyCount == 0 && !IsSurfaceRepresentationPreparing && !m_ConfiguredGeometryPending && m_ActivityProjectionGrid != null && !SceneInformation.GeometryNeedsUpdate && !SceneInformation.ProjectionGridNeedsUpdate && !SceneInformation.SurfaceProjectionNeedsUpdate;

        private bool NeedsDensityReferencePreparation() => m_ActivityProjectionGrid != null && Columns.OfType<Column3DAnatomy>().Any(column => !column.HasDensityReference(PersistentDataManager.UserPreferences.Visualization._3D.SiteInfluenceByDistance));

        private UniTask PrepareDensityReferencesAsync(CancellationToken token)
        {
            if (!m_DensityReferenceWork.Status.IsCompleted()) return m_DensityReferenceWork;
            m_DensityReferenceWork = PrepareDensityReferencesCoreAsync(token).ToAsyncLazy().Task;
            return m_DensityReferenceWork;
        }

        private async UniTask PrepareDensityReferencesCoreAsync(CancellationToken token)
        {
            while (NeedsDensityReferencePreparation())
            {
                token.ThrowIfCancellationRequested();
                if (m_DestroyRequested) throw new ObjectDisposedException(Name);
                if (!CanPrepareDensityReferences())
                {
                    await UniTask.Yield();
                    continue;
                }

                var rule = PersistentDataManager.UserPreferences.Visualization._3D.SiteInfluenceByDistance;
                var work = Columns.OfType<Column3DAnatomy>().Where(column => !column.HasDensityReference(rule)).Select(column => (Column: column, Work: column.PrepareDensityReferenceComputation(rule))).ToArray();
                var completion = new UniTaskCompletionSource();
                // Reserve borrowed generators, sites and grid before dispatching any native call.
                m_PreparingDensityReferences = true;
                m_UpdatingGenerators = true;
                m_GeneratorWork = completion.Task;
                foreach (var column in Columns) column.GeneratorWork = completion.Task;
                try
                {
                    UpdateActivityProjectionBusyState();
                    OnUpdatingGenerators.Invoke(true);
                    foreach (var item in work)
                    {
                        await UniTask.RunOnThreadPool(item.Work.Compute);
                        await UniTask.SwitchToMainThread();
                        token.ThrowIfCancellationRequested();
                        if (!m_DestroyRequested && item.Column && Columns.Contains(item.Column) && !SceneInformation.ProjectionGridNeedsUpdate && rule == PersistentDataManager.UserPreferences.Visualization._3D.SiteInfluenceByDistance)
                            item.Work.Publish();
                    }
                }
                catch (Exception exception)
                {
                    if (exception is not OperationCanceledException) m_PreparationError = exception;
                    throw;
                }
                finally
                {
                    await UniTask.SwitchToMainThread();
                    try
                    {
                        ApplyPendingGeneratorUpdates();
                    }
                    catch (Exception exception)
                    {
                        m_PreparationError = exception;
                        throw;
                    }
                    finally
                    {
                        m_PreparingDensityReferences = false;
                        CompleteGeneratorWork(completion);
                    }
                }

                Module3DMain.OnRequestUpdateInToolbar.Invoke();
                // If a structural input changed while computing, wait for its rebuild and try again.
            }
        }

        private void ApplyPendingGeneratorUpdates()
        {
            if (m_DestroyRequested) return;
            for (int i = 0; i < m_PendingGeneratorUpdates.Count; ++i) m_PendingGeneratorUpdates[i]();
        }

        private void CompleteGeneratorWork(UniTaskCompletionSource completion)
        {
            m_PendingGeneratorUpdates.Clear();
            m_PendingGeneratorUpdateIndexes.Clear();
            m_UpdatingGenerators = false;
            UpdateActivityProjectionBusyState();
            completion.TrySetResult();
            if (!m_DestroyRequested) OnUpdatingGenerators.Invoke(false);
        }
    }
}
