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
            m_Instance.m_LoadingCircle.Open();
            method.OnUpdateProgress.AddListener((progress, duration, message) => m_Instance.m_LoadingCircle.ChangePercentage(progress, 0, message));
            try
            {
                return await method.ExecuteAsync();
            }
            catch (Exception e)
            {
                DialogBoxManager.Open(DialogBoxManager.AlertType.Error, e.ToString(), e.Message);
                return default;
            }
            finally
            {
                m_Instance.m_LoadingCircle.Close();
            }
        }
        public static async UniTask LoadAsync(Func<Action<float, float, LoadingText>, UniTask> taskToExecute)
        {
            AsyncMethod method = new(taskToExecute);
            m_Instance.m_LoadingCircle.Open();
            method.OnUpdateProgress.AddListener((progress, duration, message) => m_Instance.m_LoadingCircle.ChangePercentage(progress, 0, message));
            try
            {
                await method.ExecuteAsync();
            }
            catch (Exception e)
            {
                DialogBoxManager.Open(DialogBoxManager.AlertType.Error, e.ToString(), e.Message);
            }
            finally
            {
                m_Instance.m_LoadingCircle.Close();
            }
        }
        public static async void Load(Func<Action<float, float, LoadingText>, UniTask> taskToExecute)
        {
            AsyncMethod method = new AsyncMethod(taskToExecute);
            m_Instance.m_LoadingCircle.Open();
            method.OnUpdateProgress.AddListener((progress, duration, message) => m_Instance.m_LoadingCircle.ChangePercentage(progress, 0, message));
            try
            {
                await method.ExecuteAsync();
            }
            catch (Exception e)
            {
                DialogBoxManager.Open(DialogBoxManager.AlertType.Error, e.ToString(), e.Message);
            }
            m_Instance.m_LoadingCircle.Close();
        }
        #endregion
    }
}