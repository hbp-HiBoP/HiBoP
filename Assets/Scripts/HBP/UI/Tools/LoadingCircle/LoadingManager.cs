using System;
using UnityEngine;
using HBP.Core.Tools;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace HBP.UI.Tools
{
    public class LoadingManager : Manager<LoadingManager>
    {
        #region Properties
        [SerializeField] private LoadingCircle m_LoadingCircle;
        #endregion

        #region Private Methods
        protected override void Initialization()
        {
            base.Initialization();
            m_LoadingCircle.Initialize();
        }
        #endregion

        #region Public Methods
        public static async UniTask<T> LoadAsync<T>(Func<Action<float, float, LoadingText>, UniTask<T>> taskToExecute)
        {
            AsyncMethod<T> method = new(taskToExecute);
            await UniTask.SwitchToMainThread();
            m_Instance.m_LoadingCircle.Open();
            await UniTask.SwitchToThreadPool();
            method.OnUpdateProgress.AddListener((progress, duration, message) => m_Instance.m_LoadingCircle.ChangePercentage(progress, duration, message));
            try
            {
                return await method.ExecuteAsync();
            }
            catch (Exception e)
            {
                await UniTask.SwitchToMainThread();
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, e.ToString(), e.Message).Forget();
                return default;
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                m_Instance.m_LoadingCircle.Close();
            }
        }
        public static async UniTask LoadAsync(Func<Action<float, float, LoadingText>, UniTask> taskToExecute)
        {
            AsyncMethod method = new(taskToExecute);
            await UniTask.SwitchToMainThread();
            m_Instance.m_LoadingCircle.Open();
            await UniTask.SwitchToThreadPool();
            method.OnUpdateProgress.AddListener((progress, duration, message) => m_Instance.m_LoadingCircle.ChangePercentage(progress, duration, message));
            try
            {
                await method.ExecuteAsync();
            }
            catch (Exception e)
            {
                await UniTask.SwitchToMainThread();
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, e.ToString(), e.Message).Forget();
            }
            await UniTask.SwitchToMainThread();
            m_Instance.m_LoadingCircle.Close();
        }
        public static void Load(Func<Action<float, float, LoadingText>, UniTask> taskToExecute)
        {
            LoadVoid(taskToExecute).Forget();
        }

        public static async UniTask<T> LoadAsync<T>(Func<Action<float, float, LoadingText>, CancellationToken, UniTask<T>> taskToExecute)
        {
            CancelableAsyncMethod<T> method = new(taskToExecute);
            await UniTask.SwitchToMainThread();
            m_Instance.m_LoadingCircle.Open(true);
            await UniTask.SwitchToThreadPool();
            method.OnUpdateProgress.AddListener((progress, duration, message) => m_Instance.m_LoadingCircle.ChangePercentage(progress, duration, message));
            try
            {
                m_Instance.m_LoadingCircle.OnCancel.AddListener(method.Cancel);
                return await method.ExecuteAsync();
            }
            catch (Exception e)
            {
                await UniTask.SwitchToMainThread();
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, e.ToString(), e.Message).Forget();
                return default;
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                m_Instance.m_LoadingCircle.Close();
                m_Instance.m_LoadingCircle.OnCancel.RemoveListener(method.Cancel);
            }
        }
        public static async UniTask LoadAsync(Func<Action<float, float, LoadingText>, CancellationToken, UniTask> taskToExecute)
        {
            CancelableAsyncMethod method = new(taskToExecute);
            await UniTask.SwitchToMainThread();
            m_Instance.m_LoadingCircle.Open(true);
            await UniTask.SwitchToThreadPool();
            method.OnUpdateProgress.AddListener((progress, duration, message) => m_Instance.m_LoadingCircle.ChangePercentage(progress, duration, message));
            try
            {
                m_Instance.m_LoadingCircle.OnCancel.AddListener(method.Cancel);
                await method.ExecuteAsync();
            }
            catch (Exception e)
            {
                await UniTask.SwitchToMainThread();
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, e.ToString(), e.Message).Forget();
            }
            await UniTask.SwitchToMainThread();
            m_Instance.m_LoadingCircle.Close();
            m_Instance.m_LoadingCircle.OnCancel.RemoveListener(method.Cancel);
        }
        public static void Load(Func<Action<float, float, LoadingText>, CancellationToken, UniTask> taskToExecute)
        {
            LoadVoid(taskToExecute).Forget();
        }
        #endregion

        #region Private Methods
        private static async UniTaskVoid LoadVoid(Func<Action<float, float, LoadingText>, UniTask> taskToExecute)
        {
            AsyncMethod method = new(taskToExecute);
            await UniTask.SwitchToMainThread();
            m_Instance.m_LoadingCircle.Open();
            await UniTask.SwitchToThreadPool();
            method.OnUpdateProgress.AddListener((progress, duration, message) => m_Instance.m_LoadingCircle.ChangePercentage(progress, duration, message));
            try
            {
                await method.ExecuteAsync();
            }
            catch (Exception e)
            {
                await UniTask.SwitchToMainThread();
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, e.ToString(), e.Message).Forget();
            }
            await UniTask.SwitchToMainThread();
            m_Instance.m_LoadingCircle.Close();
        }
        private static async UniTaskVoid LoadVoid(Func<Action<float, float, LoadingText>, CancellationToken, UniTask> taskToExecute)
        {
            CancelableAsyncMethod method = new(taskToExecute);
            await UniTask.SwitchToMainThread();
            m_Instance.m_LoadingCircle.Open(true);
            await UniTask.SwitchToThreadPool();
            method.OnUpdateProgress.AddListener((progress, duration, message) => m_Instance.m_LoadingCircle.ChangePercentage(progress, duration, message));
            try
            {
                m_Instance.m_LoadingCircle.OnCancel.AddListener(method.Cancel);
                await method.ExecuteAsync();
            }
            catch (Exception e)
            {
                await UniTask.SwitchToMainThread();
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, e.ToString(), e.Message).Forget();
            }
            await UniTask.SwitchToMainThread();
            m_Instance.m_LoadingCircle.Close();
            m_Instance.m_LoadingCircle.OnCancel.RemoveListener(method.Cancel);
        }
        #endregion
    }
}