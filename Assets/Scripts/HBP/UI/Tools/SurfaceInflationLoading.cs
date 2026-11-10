using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Exceptions;
using HBP.Core.Tools;
using HBP.Sync.Scene;
using HBP.Core.DLL;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using UnityEngine;

namespace HBP.UI.Tools
{
    /// <summary>Wraps only surface preparation; transitions and publication run after the loading operation ends.</summary>
    public static class SurfaceInflationLoading
    {
        public static bool RequiresLoadingManager(Base3DScene scene, SurfaceRepresentation representation) => representation == SurfaceRepresentation.Inflated && !scene.MeshManager.SelectedMesh.HasInflatedRepresentation && scene.SurfaceRepresentationRequestHandler == null;

        /// <summary>Common Desktop/Quest UI boundary: only calculation owns a loading operation.</summary>
        public static async UniTask ChangeRepresentationAsync(Base3DScene scene, SurfaceRepresentation representation, CancellationToken cancellationToken = default)
        {
            try
            {
                if (RequiresLoadingManager(scene, representation))
                {
                    CancellationToken preparationCancellation = cancellationToken;
                    await LoadingManager.LoadDelayedAsync(async (update, token) =>
                    {
                        preparationCancellation = token;
                        IProgress<float> progress = new Progress<float>(value => update(value, 0, new LoadingText("Inflating surface")));
                        try
                        {
                            await UniTask.SwitchToMainThread(token);
                            await scene.PrepareSurfaceRepresentationAsync(representation, progress, token);
                        }
                        catch (SurfaceInflationException exception)
                        {
                            throw new HBPException("Surface inflation failed", BuildInflationError(exception));
                        }
                    }, cancellationToken);
                    preparationCancellation.ThrowIfCancellationRequested();
                }

                await scene.SetSurfaceRepresentationAsync(representation, cancellationToken: cancellationToken, animate: true);
            }
            catch (OperationCanceledException)
            {
            }
            catch (HBPException)
            {
            }
            catch (Exception exception)
            {
                ReportFailure(exception);
            }
        }

        public static void ShowCutsInformationIfNeeded(Base3DScene scene)
        {
            if (scene == null || scene.IsClosing || scene.Cuts.Count == 0 || scene.MeshManager.Meshes.Count == 0 || scene.MeshManager.SelectedMesh.Representation != SurfaceRepresentation.Inflated || !scene.TryMarkInflatedCutsInformationShown()) return;
            DialogBoxManager.Open(DialogBoxType.Informational, "Cuts in inflated mode", "In inflated mode, cuts remain defined in anatomical coordinates. They continue to support cut views and volumetric exploration, but do not visually clip the inflated surface.", "OK").Forget();
        }

        private static string BuildInflationError(SurfaceInflationException exception)
        {
            SurfaceInflationReport report = exception.Report;
            return $"{exception.Message}\n\nVertices: {report.VertexCount:N0}\nTriangles: {report.TriangleCount:N0}\nComponents: {report.ComponentCount:N0}\nBoundary edges: {report.BoundaryEdgeCount:N0}\nNon-manifold edges: {report.NonManifoldEdgeCount:N0}\nNon-manifold vertices: {report.NonManifoldVertexCount:N0}\nIterations: {report.IterationCount:N0}\nConverged: {(report.Converged ? "yes" : "no")}";
        }

        public static async UniTask PrepareAsync(SurfaceInflationCoordinator coordinator, Func<CancellationToken, UniTask> prepare, CancellationToken cancellationToken)
        {
            if (!LoadingManager.IsInitialized)
            {
                await prepare(cancellationToken);
                return;
            }

            CancellationToken preparationCancellation = cancellationToken;
            await LoadingManager.LoadDelayedAsync(async (update, token) =>
            {
                preparationCancellation = token;
                await UniTask.SwitchToMainThread();
                UniTask preparation = prepare(token);
                try
                {
                    while (!preparation.Status.IsCompleted())
                    {
                        update(coordinator.Progress, 0, new LoadingText("Inflating surface on both devices"));
                        await UniTask.Yield(PlayerLoopTiming.Update);
                    }

                    await preparation;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (HBPException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    // LoadingManager presents this once; completion observers ignore already presented failures.
                    throw new HBPException("Surface inflation failed", exception.ToString());
                }
            }, cancellationToken);
            preparationCancellation.ThrowIfCancellationRequested();
        }

        public static async UniTask ObserveAsync(Task completion)
        {
            try
            {
                await completion;
            }
            catch (OperationCanceledException)
            {
            }
            catch (HBPException)
            {
                /* Preparation failures were already presented by LoadingManager. */
            }
            catch (Exception exception)
            {
                ReportFailure(exception);
            }
        }

        public static void ReportFailure(Exception exception)
        {
            Debug.LogError(exception.ToString());
            DialogBoxManager.OpenScrollable(Core.Enums.DialogBoxType.Error, "Surface inflation failed", exception.ToString()).Forget();
        }
    }
}
