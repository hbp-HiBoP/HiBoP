using ThirdParty.CielaSpike;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using HBP.Core.Tools;

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
        public static void Load(IEnumerator action, GenericEvent<float, float, LoadingText> onChangeProgress, Action<TaskState> callBack = null)
        {
            m_Instance.StartCoroutine(c_Load(action, onChangeProgress, callBack));
        }
        public static IEnumerator c_Load(IEnumerator action, GenericEvent<float, float, LoadingText> onChangeProgress, Action<TaskState> callBack = null)
        {
            m_Instance.m_LoadingCircle.Open();
            onChangeProgress.AddListener((progress, time, message) => m_Instance.m_LoadingCircle.ChangePercentage(progress, time, message));
            yield return m_Instance.StartCoroutineAsync(action, out Task task);
            switch (task.State)
            {
                case TaskState.Done:
                    yield return new WaitForSeconds(0.2f);
                    break;
                case TaskState.Error:
                    Exception exception = task.Exception;
                    DialogBoxManager.Open(DialogBoxManager.AlertType.Error, exception.ToString(), exception.Message);
                    break;
            }
            m_Instance.m_LoadingCircle.Close();
            callBack?.Invoke(task.State);
        }
        #endregion
    }
}