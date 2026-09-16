using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace HBP.Transfer.Scene
{
    internal static class NativePreparation
    {
        // Cancellation never detaches a synchronous native call. Its private result is
        // either handed to Unity, or destroyed after the worker has actually returned.
        internal static async UniTask<T> RunAsync<T>(Func<T> create, Action<T> release, CancellationToken token)
        {
            T result = await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                return create();
            });
            try
            {
                await UniTask.SwitchToMainThread();
                token.ThrowIfCancellationRequested();
                return result;
            }
            catch
            {
                release(result);
                throw;
            }
        }
    }
}
