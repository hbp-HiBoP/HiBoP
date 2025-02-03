using System;
using UnityEngine;
using HBP.Core.Tools;
using Cysharp.Threading.Tasks;

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
            method.OnUpdateProgress.AddListener((progress, duration, message) => m_Instance.m_LoadingCircle.ChangePercentage(progress, 0, message));
            try
            {
                return await method.ExecuteAsync();
            }
            catch (Exception e)
            {
                await UniTask.SwitchToMainThread();
                DialogBoxManager.Open(DialogBoxManager.AlertType.Error, e.ToString(), e.Message);
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
            method.OnUpdateProgress.AddListener((progress, duration, message) => m_Instance.m_LoadingCircle.ChangePercentage(progress, 0, message));
            try
            {
                await method.ExecuteAsync();
            }
            catch (Exception e)
            {
                await UniTask.SwitchToMainThread();
                DialogBoxManager.Open(DialogBoxManager.AlertType.Error, e.ToString(), e.Message);
            }
            await UniTask.SwitchToMainThread();
            m_Instance.m_LoadingCircle.Close();
        }
        public static void Load(Func<Action<float, float, LoadingText>, UniTask> taskToExecute)
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
            method.OnUpdateProgress.AddListener((progress, duration, message) => m_Instance.m_LoadingCircle.ChangePercentage(progress, 0, message));
            try
            {
                await method.ExecuteAsync();
            }
            catch (Exception e)
            {
                await UniTask.SwitchToMainThread();
                DialogBoxManager.Open(DialogBoxManager.AlertType.Error, e.ToString(), e.Message);
            }
            m_Instance.m_LoadingCircle.Close();
        }
        #endregion
    }
}