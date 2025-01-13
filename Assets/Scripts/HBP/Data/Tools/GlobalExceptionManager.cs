using UnityEngine;
using HBP.UI.Tools;
using HBP.Core.Tools;

namespace HBP.Data.Tools
{
    public class GlobalExceptionManager : Manager<GlobalExceptionManager>
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
                WindowsManager.Open("Bug Reporter window");
            }
        }
        #endregion
    }
}