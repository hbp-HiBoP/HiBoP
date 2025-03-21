using System;
using UnityEngine;
using HBP.Core.Tools;
using Cysharp.Threading.Tasks;
using System.Threading;
using HBP.Core.Exceptions;

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
        public static async UniTask<T> LoadAsync<T>(Func<Action<float, float, LoadingText>, UniTask<T>> taskToExecute, bool showInformations = true)
        {
            AsyncMethod<T> method = new(taskToExecute);
            m_Instance.m_LoadingCircle.Open(showInformations);
            method.OnUpdateProgress.AddListener((progress, duration, message) => m_Instance.m_LoadingCircle.ChangePercentage(progress, duration, message));
            try
            {
                return await method.ExecuteAsync();
            }
            catch (HBPException e)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, e.Title, e.Message).Forget();
                throw e;
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Unknown error", "An unknown error has occured. Please send a bug report.").Forget();
                throw e;
            }
            finally
            {
                m_Instance.m_LoadingCircle.Close();
            }
        }
        public static async UniTask LoadAsync(Func<Action<float, float, LoadingText>, UniTask> taskToExecute, bool showInformations = true)
        {
            AsyncMethod method = new(taskToExecute);
            m_Instance.m_LoadingCircle.Open(showInformations);
            method.OnUpdateProgress.AddListener((progress, duration, message) => m_Instance.m_LoadingCircle.ChangePercentage(progress, duration, message));
            try
            {
                await method.ExecuteAsync();
            }
            catch (HBPException e)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, e.Title, e.Message).Forget();
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Unknown error", "An unknown error has occured. Please send a bug report.").Forget();
            }
            m_Instance.m_LoadingCircle.Close();
        }
        public static void Load(Func<Action<float, float, LoadingText>, UniTask> taskToExecute, bool showInformations = true)
        {
            LoadVoid(taskToExecute, showInformations).Forget();
        }

        public static async UniTask<T> LoadAsync<T>(Func<Action<float, float, LoadingText>, CancellationToken, UniTask<T>> taskToExecute, bool showInformations = true)
        {
            CancelableAsyncMethod<T> method = new(taskToExecute);
            m_Instance.m_LoadingCircle.Open(showInformations, true);
            method.OnUpdateProgress.AddListener((progress, duration, message) => m_Instance.m_LoadingCircle.ChangePercentage(progress, duration, message));
            m_Instance.m_LoadingCircle.OnCancel.AddListener(method.Cancel);
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
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, e.Title, e.Message).Forget();
                throw e;
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Unknown error", "An unknown error has occured. Please send a bug report.").Forget();
                throw e;
            }
            finally
            {
                m_Instance.m_LoadingCircle.Close();
                m_Instance.m_LoadingCircle.OnCancel.RemoveListener(method.Cancel);
            }
        }
        public static async UniTask LoadAsync(Func<Action<float, float, LoadingText>, CancellationToken, UniTask> taskToExecute, bool showInformations = true)
        {
            CancelableAsyncMethod method = new(taskToExecute);
            m_Instance.m_LoadingCircle.Open(showInformations, true);
            method.OnUpdateProgress.AddListener((progress, duration, message) => m_Instance.m_LoadingCircle.ChangePercentage(progress, duration, message));
            m_Instance.m_LoadingCircle.OnCancel.AddListener(method.Cancel);
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
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, e.Title, e.Message).Forget();
                throw e;
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Unknown error", "An unknown error has occured. Please send a bug report.").Forget();
                throw e;
            }
            finally
            {
                m_Instance.m_LoadingCircle.Close();
                m_Instance.m_LoadingCircle.OnCancel.RemoveListener(method.Cancel);
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
            m_Instance.m_LoadingCircle.Open(showInformations);
            method.OnUpdateProgress.AddListener((progress, duration, message) => m_Instance.m_LoadingCircle.ChangePercentage(progress, duration, message));
            try
            {
                await method.ExecuteAsync();
            }
            catch (HBPException e)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, e.Title, e.Message).Forget();
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Unknown error", "An unknown error has occured. Please send a bug report.").Forget();
            }
            m_Instance.m_LoadingCircle.Close();
        }
        private static async UniTaskVoid LoadVoid(Func<Action<float, float, LoadingText>, CancellationToken, UniTask> taskToExecute, bool showInformations)
        {
            CancelableAsyncMethod method = new(taskToExecute);
            m_Instance.m_LoadingCircle.Open(showInformations, true);
            method.OnUpdateProgress.AddListener((progress, duration, message) => m_Instance.m_LoadingCircle.ChangePercentage(progress, duration, message));
            m_Instance.m_LoadingCircle.OnCancel.AddListener(method.Cancel);
            try
            {
                await method.ExecuteAsync();
            }
            catch (OperationCanceledException)
            {
            }
            catch (HBPException e)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, e.Title, e.Message).Forget();
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Unknown error", "An unknown error has occured. Please send a bug report.").Forget();
            }
            finally
            {
                m_Instance.m_LoadingCircle.Close();
                m_Instance.m_LoadingCircle.OnCancel.RemoveListener(method.Cancel);
            }
        }
        #endregion
    }
}