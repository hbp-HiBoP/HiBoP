using ThirdParty.CielaSpike;
using System;
using System.Collections;
using Tools.Unity;
using UnityEngine;
using UnityEngine.Events;
using HBP.Core.Tools;

namespace HBP.UI
{
    public class LoadingManager : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        private Canvas m_Canvas;
        public Canvas Canvas
        {
            get { return m_Canvas; }
            set { m_Canvas = value; }
        }

        [SerializeField]
        GameObject LoadingCirclePrefab;
        #endregion

        #region Public Methods
        public LoadingCircle Open()
        {
            GameObject loadingCircleGameObject = Instantiate(LoadingCirclePrefab, Canvas.transform);
            LoadingCircle loadingCircle = loadingCircleGameObject.GetComponent<LoadingCircle>();
            return loadingCircle;
        }
        public void Load(IEnumerator action, GenericEvent<float, float, LoadingText> onChangeProgress, Action<TaskState> callBack = null)
        {
            StartCoroutine(c_Load(action, onChangeProgress, callBack));
        }
        public IEnumerator c_Load(IEnumerator action, GenericEvent<float, float, LoadingText> onChangeProgress, Action<TaskState> callBack = null)
        {
            LoadingCircle loadingCircle = Open();
            onChangeProgress.AddListener((progress, time, message) => loadingCircle.ChangePercentage(progress, time, message));
            yield return this.StartCoroutineAsync(action, out Task task);
            switch (task.State)
            {
                case TaskState.Done:
                    yield return new WaitForSeconds(0.2f);
                    break;
                case TaskState.Error:
                    Exception exception = task.Exception;
                    ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, exception.ToString(), exception.Message);
                    break;
            }
            loadingCircle.Close();
            if (callBack != null) callBack.Invoke(task.State);
        }
        #endregion
    }
}