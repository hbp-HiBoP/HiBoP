using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tools.Unity
{
    public class GlobalExceptionManager : MonoBehaviour
    {
        #region Private Methods
        private void OnEnable()
        {
            Application.logMessageReceived += HandleException;
        }
        private void OnDisable()
        {
            Application.logMessageReceived -= HandleException;
        }
        private void HandleException(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception)
            {
                ApplicationState.WindowsManager.Open("Bug Reporter window");
            }
        }
        #endregion
    }
}