using System;
using UnityEngine;
using UnityEngine.Events;
using HBP.Core.Tools;
using Cysharp.Threading.Tasks;
using System.Threading;
using HBP.Core.Exceptions;

namespace HBP.UI.Tools
{
    public class LoadingManager : Manager<LoadingManager>
    {
        #region Properties

        [SerializeField] private MonoBehaviour m_LoadingCircle;
        private ILoadingPresenter Presenter => (ILoadingPresenter)m_LoadingCircle;
        private int m_PresentationUsers;
        private int m_InformationUsers, m_CancelableUsers;
        private readonly object m_PresentationLock = new();

        private void OpenPresentation(bool showInformations, bool cancelable = false)
        {
            lock (m_PresentationLock)
            {
                if (showInformations) m_InformationUsers++;
                if (cancelable) m_CancelableUsers++;
                if (++m_PresentationUsers == 1) Presenter.Open(m_InformationUsers != 0, m_CancelableUsers != 0);
                else Presenter.SetOptions(m_InformationUsers != 0, m_CancelableUsers != 0);
            }
        }

        private async UniTask ClosePresentation(bool showInformations, bool cancelable = false)
        {
            await UniTask.SwitchToMainThread();
            lock (m_PresentationLock)
            {
                if (showInformations) m_InformationUsers--;
                if (cancelable) m_CancelableUsers--;
                if (--m_PresentationUsers == 0) Presenter.Close();
                else Presenter.SetOptions(m_InformationUsers != 0, m_CancelableUsers != 0);
            }
        }

        #endregion

        #region Private Methods

        protected override void Initialization()
        {
            base.Initialization();
            Presenter.Initialize();
        }

        #endregion

        #region Public Methods

        public static async UniTask<T> LoadAsync<T>(Func<Action<float, float, LoadingText>, UniTask<T>> taskToExecute, bool showInformations = true)
        {
            AsyncMethod<T> method = new(taskToExecute);
            m_Instance.OpenPresentation(showInformations);
            method.OnUpdateProgress.AddListener((progress, duration, message) => m_Instance.Presenter.ChangePercentage(progress, duration, message));
            try
            {
                return await method.ExecuteAsync();
            }
            catch (HBPException e)
            {
                Debug.LogError(e.ToString());
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, e.Title, e.Message).Forget();
                throw e;
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                DialogBoxManager.OpenScrollable(Core.Enums.DialogBoxType.Error, "Unknown error", e.ToString()).Forget();
                throw e;
            }
            finally
            {
                await m_Instance.ClosePresentation(showInformations);
            }
        }

        public static async UniTask LoadAsync(Func<Action<float, float, LoadingText>, UniTask> taskToExecute, bool showInformations = true)
        {
            AsyncMethod method = new(taskToExecute);
            m_Instance.OpenPresentation(showInformations);
            method.OnUpdateProgress.AddListener((progress, duration, message) => m_Instance.Presenter.ChangePercentage(progress, duration, message));
            try
            {
                await method.ExecuteAsync();
            }
            catch (HBPException e)
            {
                Debug.LogError(e.ToString());
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, e.Title, e.Message).Forget();
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                DialogBoxManager.OpenScrollable(Core.Enums.DialogBoxType.Error, "Unknown error", e.ToString()).Forget();
            }

            await m_Instance.ClosePresentation(showInformations);
        }

        public static void Load(Func<Action<float, float, LoadingText>, UniTask> taskToExecute, bool showInformations = true)
        {
            LoadVoid(taskToExecute, showInformations).Forget();
        }

        public static async UniTask<T> LoadAsync<T>(Func<Action<float, float, LoadingText>, CancellationToken, UniTask<T>> taskToExecute, bool showInformations = true)
        {
            CancelableAsyncMethod<T> method = new(taskToExecute);
            m_Instance.OpenPresentation(showInformations, true);
            method.OnUpdateProgress.AddListener((progress, duration, message) => m_Instance.Presenter.ChangePercentage(progress, duration, message));
            m_Instance.Presenter.OnCancel.AddListener(method.Cancel);
            try
            {
                return await method.ExecuteAsync();
            }
            catch (OperationCanceledException e)
            {
                throw e;
            }
            catch (HBPException e)
            {
                Debug.LogError(e.ToString());
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, e.Title, e.Message).Forget();
                throw e;
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                DialogBoxManager.OpenScrollable(Core.Enums.DialogBoxType.Error, "Unknown error", e.ToString()).Forget();
                throw e;
            }
            finally
            {
                await m_Instance.ClosePresentation(showInformations, true);
                m_Instance.Presenter.OnCancel.RemoveListener(method.Cancel);
            }
        }

        public static async UniTask LoadAsync(Func<Action<float, float, LoadingText>, CancellationToken, UniTask> taskToExecute, bool showInformations = true)
        {
            CancelableAsyncMethod method = new(taskToExecute);
            m_Instance.OpenPresentation(showInformations, true);
            method.OnUpdateProgress.AddListener((progress, duration, message) => m_Instance.Presenter.ChangePercentage(progress, duration, message));
            m_Instance.Presenter.OnCancel.AddListener(method.Cancel);
            try
            {
                await method.ExecuteAsync();
            }
            catch (OperationCanceledException e)
            {
                throw e;
            }
            catch (HBPException e)
            {
                Debug.LogError(e.ToString());
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, e.Title, e.Message).Forget();
                throw e;
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                DialogBoxManager.OpenScrollable(Core.Enums.DialogBoxType.Error, "Unknown error", e.ToString()).Forget();
                throw e;
            }
            finally
            {
                await m_Instance.ClosePresentation(showInformations, true);
                m_Instance.Presenter.OnCancel.RemoveListener(method.Cancel);
            }
        }

        /// <summary>Runs a cancelable task and opens the loading visual only if it outlives the delay.</summary>
        public static async UniTask LoadDelayedAsync(Func<Action<float, float, LoadingText>, CancellationToken, UniTask> taskToExecute, CancellationToken cancellationToken, int delayMilliseconds = 200, bool showInformations = true)
        {
            if (taskToExecute == null) throw new ArgumentNullException(nameof(taskToExecute));
            if (delayMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(delayMilliseconds));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            bool visualOpened = false;
            Action<float, float, LoadingText> update = (progress, duration, message) =>
            {
                if (visualOpened && m_Instance != null) m_Instance.Presenter.ChangePercentage(progress, duration, message);
            };
            UnityAction cancel = linked.Cancel;
            // The race and the final drain share one operation, including concurrent waits.
            UniTask operation = taskToExecute(update, linked.Token).ToAsyncLazy().Task;
            try
            {
                UniTask delay = UniTask.Delay(TimeSpan.FromMilliseconds(delayMilliseconds), DelayType.Realtime);
                int completed = await UniTask.WhenAny(operation, delay);
                if (completed == 1 && !operation.Status.IsCompleted() && !linked.IsCancellationRequested)
                {
                    await UniTask.SwitchToMainThread();
                    if (m_Instance != null && !operation.Status.IsCompleted() && !linked.IsCancellationRequested)
                    {
                        m_Instance.OpenPresentation(showInformations, true);
                        m_Instance.Presenter.OnCancel.AddListener(cancel);
                        visualOpened = true;
                    }
                }

                await operation;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HBPException exception)
            {
                await UniTask.SwitchToMainThread();
                Debug.LogError(exception.ToString());
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, exception.Title, exception.Message).Forget();
                throw;
            }
            catch (Exception exception)
            {
                await UniTask.SwitchToMainThread();
                Debug.LogError(exception.ToString());
                DialogBoxManager.OpenScrollable(Core.Enums.DialogBoxType.Error, "Unknown error", exception.ToString()).Forget();
                throw;
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                if (visualOpened && m_Instance != null)
                {
                    m_Instance.Presenter.OnCancel.RemoveListener(cancel);
                    await m_Instance.ClosePresentation(showInformations, true);
                }
            }
        }

        public static void Load(Func<Action<float, float, LoadingText>, CancellationToken, UniTask> taskToExecute, bool showInformations = true)
        {
            LoadVoid(taskToExecute, showInformations).Forget();
        }

        #endregion

        #region Private Methods

        private static async UniTaskVoid LoadVoid(Func<Action<float, float, LoadingText>, UniTask> taskToExecute, bool showInformations)
        {
            AsyncMethod method = new(taskToExecute);
            m_Instance.OpenPresentation(showInformations);
            method.OnUpdateProgress.AddListener((progress, duration, message) => m_Instance.Presenter.ChangePercentage(progress, duration, message));
            try
            {
                await method.ExecuteAsync();
            }
            catch (HBPException e)
            {
                Debug.LogError(e.ToString());
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, e.Title, e.Message).Forget();
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                DialogBoxManager.OpenScrollable(Core.Enums.DialogBoxType.Error, "Unknown error", e.ToString()).Forget();
            }

            await m_Instance.ClosePresentation(showInformations);
        }

        private static async UniTaskVoid LoadVoid(Func<Action<float, float, LoadingText>, CancellationToken, UniTask> taskToExecute, bool showInformations)
        {
            CancelableAsyncMethod method = new(taskToExecute);
            m_Instance.OpenPresentation(showInformations, true);
            method.OnUpdateProgress.AddListener((progress, duration, message) => m_Instance.Presenter.ChangePercentage(progress, duration, message));
            m_Instance.Presenter.OnCancel.AddListener(method.Cancel);
            try
            {
                await method.ExecuteAsync();
            }
            catch (OperationCanceledException)
            {
            }
            catch (HBPException e)
            {
                Debug.LogError(e.ToString());
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, e.Title, e.Message).Forget();
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                DialogBoxManager.OpenScrollable(Core.Enums.DialogBoxType.Error, "Unknown error", e.ToString()).Forget();
            }
            finally
            {
                await m_Instance.ClosePresentation(showInformations, true);
                m_Instance.Presenter.OnCancel.RemoveListener(method.Cancel);
            }
        }

        #endregion
    }
}
