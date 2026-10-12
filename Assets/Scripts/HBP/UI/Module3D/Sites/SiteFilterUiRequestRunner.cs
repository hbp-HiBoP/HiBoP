using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Data.Module3D;
using HBP.Sync.Scene;
using HBP.UI.Tools;

namespace HBP.UI.Module3D
{
    internal static class SiteFilterUiRequestRunner
    {
        public static bool TryRun(Base3DScene scene, SiteFilterRequest request, Action fallback)
        {
            if (scene == null || !SiteFilterRequestRouter.TryGetHandler(scene, out Func<SiteFilterRequest, CancellationToken, System.Threading.Tasks.Task<bool>> handler))
                return false;

            RunAsync(request, handler, fallback).Forget();
            return true;
        }

        private static async UniTask RunAsync(SiteFilterRequest request, Func<SiteFilterRequest, CancellationToken, System.Threading.Tasks.Task<bool>> handler, Action fallback)
        {
            try
            {
                await LoadingManager.LoadDelayedAsync(async (_, token) =>
                {
                    bool handled = await handler(request, token);
                    if (!handled)
                    {
                        await UniTask.SwitchToMainThread(token);
                        fallback();
                    }
                }, CancellationToken.None, showInformations: false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
                // LoadingManager already displayed the failure dialog.
            }
        }
    }
}
