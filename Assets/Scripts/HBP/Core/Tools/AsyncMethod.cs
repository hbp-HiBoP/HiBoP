using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine.Events;

namespace HBP.Core.Tools
{
    public class AsyncMethod
    {
        #region Properties
        private readonly Func<Action<float, float, LoadingText>, UniTask> m_TaskToExecute;
        #endregion

        #region Events
        public GenericEvent<float, float, LoadingText> OnUpdateProgress = new();
        #endregion

        #region Constructors
        public AsyncMethod(Func<Action<float, float, LoadingText>, UniTask> taskToExecute)
        {
            m_TaskToExecute = taskToExecute;
        }
        #endregion

        #region Public Methods
        public async UniTask ExecuteAsync()
        {
            await m_TaskToExecute(UpdateProgressAsync);
        }
        #endregion

        #region Private Methods
        private void UpdateProgressAsync(float progress, float duration, LoadingText message)
        {
            OnUpdateProgress.Invoke(progress, duration, message);
        }
        #endregion
    }

    public class CancelableAsyncMethod
    {
        #region Properties
        private readonly Func<Action<float, float, LoadingText>, CancellationToken, UniTask> m_TaskToExecute;
        private readonly CancellationTokenSource m_CancellationTokenSource = new();
        #endregion

        #region Events
        public GenericEvent<float, float, LoadingText> OnUpdateProgress = new();
        #endregion

        #region Constructors
        public CancelableAsyncMethod(Func<Action<float, float, LoadingText>, CancellationToken, UniTask> taskToExecute)
        {
            m_TaskToExecute = taskToExecute;
        }
        #endregion

        #region Public Methods
        public async UniTask ExecuteAsync()
        {
            await m_TaskToExecute(UpdateProgressAsync, m_CancellationTokenSource.Token);
        }
        public void Cancel()
        {
            m_CancellationTokenSource.Cancel();
        }
        #endregion

        #region Private Methods
        private void UpdateProgressAsync(float progress, float duration, LoadingText message)
        {
            OnUpdateProgress.Invoke(progress, duration, message);
        }
        #endregion
    }

    public class AsyncMethod<T>
    {
        #region Properties
        private readonly Func<Action<float, float, LoadingText>, UniTask<T>> m_TaskToExecute;
        #endregion

        #region Events
        public GenericEvent<float, float, LoadingText> OnUpdateProgress = new();
        #endregion

        #region Constructors
        public AsyncMethod(Func<Action<float, float, LoadingText>, UniTask<T>> taskToExecute)
        {
            m_TaskToExecute = taskToExecute;
        }
        #endregion

        #region Public Methods
        public async UniTask<T> ExecuteAsync()
        {
            return await m_TaskToExecute(UpdateProgressAsync);
        }
        #endregion

        #region Private Methods
        private void UpdateProgressAsync(float progress, float duration, LoadingText message)
        {
            OnUpdateProgress.Invoke(progress, duration, message);
        }
        #endregion
    }

    public class CancelableAsyncMethod<T>
    {
        #region Properties
        private readonly Func<Action<float, float, LoadingText>, CancellationToken, UniTask<T>> m_TaskToExecute;
        private readonly CancellationTokenSource m_CancellationTokenSource = new();
        #endregion

        #region Events
        public GenericEvent<float, float, LoadingText> OnUpdateProgress = new();
        #endregion

        #region Constructors
        public CancelableAsyncMethod(Func<Action<float, float, LoadingText>, CancellationToken, UniTask<T>> taskToExecute)
        {
            m_TaskToExecute = taskToExecute;
        }
        #endregion

        #region Public Methods
        public async UniTask<T> ExecuteAsync()
        {
            return await m_TaskToExecute(UpdateProgressAsync, m_CancellationTokenSource.Token);
        }
        public void Cancel()
        {
            m_CancellationTokenSource.Cancel();
        }
        #endregion

        #region Private Methods
        private void UpdateProgressAsync(float progress, float duration, LoadingText message)
        {
            OnUpdateProgress.Invoke(progress, duration, message);
        }
        #endregion
    }
}