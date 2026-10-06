using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Exceptions;
using HBP.Core.Tools;
using HBP.Sync.Scene;
using UnityEngine;

namespace HBP.UI.Tools
{
    /// <summary>Wraps only surface preparation; transitions and publication run after the loading operation ends.</summary>
    public static class SurfaceInflationLoading
    {
        public static async UniTask PrepareAsync(V2SurfaceInflationCoordinator coordinator, Func<CancellationToken, UniTask> prepare, CancellationToken cancellationToken)
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
                Task preparation = prepare(token).AsTask();
                try
                {
                    while (!preparation.IsCompleted)
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
