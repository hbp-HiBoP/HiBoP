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
        public static bool TryRun(Base3DScene scene, V2SiteFilterRequest request, Action fallback)
        {
            if (scene == null || !V2SiteFilterRequestRouter.TryGetHandler(scene, out Func<V2SiteFilterRequest, CancellationToken, System.Threading.Tasks.Task<bool>> handler))
                return false;

            RunAsync(request, handler, fallback).Forget();
            return true;
        }

        private static async UniTask RunAsync(V2SiteFilterRequest request, Func<V2SiteFilterRequest, CancellationToken, System.Threading.Tasks.Task<bool>> handler, Action fallback)
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
