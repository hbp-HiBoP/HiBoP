using ThirdParty.CielaSpike;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using HBP.Core.Tools;

namespace HBP.UI
{
    public class LoadingManager : MonoBehaviour
    {
        #region Properties
        private static LoadingManager m_Instance;

        [SerializeField] private Canvas m_Canvas;
        [SerializeField] GameObject m_LoadingCirclePrefab;
        #endregion

        #region Private Methods
        private void Awake()
        {
            if (m_Instance == null)
            {
                m_Instance = this;
            }
            else
            {
                Destroy(this);
            }
        }
        #endregion

        #region Public Methods
        public static LoadingCircle Open()
        {
            GameObject loadingCircleGameObject = Instantiate(m_Instance.m_LoadingCirclePrefab, m_Instance.m_Canvas.transform);
            LoadingCircle loadingCircle = loadingCircleGameObject.GetComponent<LoadingCircle>();
            return loadingCircle;
        }
        public static void Load(IEnumerator action, GenericEvent<float, float, LoadingText> onChangeProgress, Action<TaskState> callBack = null)
        {
            m_Instance.StartCoroutine(c_Load(action, onChangeProgress, callBack));
        }
        public static IEnumerator c_Load(IEnumerator action, GenericEvent<float, float, LoadingText> onChangeProgress, Action<TaskState> callBack = null)
        {
            LoadingCircle loadingCircle = Open();
            onChangeProgress.AddListener((progress, time, message) => loadingCircle.ChangePercentage(progress, time, message));
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
            loadingCircle.Close();
            if (callBack != null) callBack.Invoke(task.State);
        }
        #endregion
    }
}