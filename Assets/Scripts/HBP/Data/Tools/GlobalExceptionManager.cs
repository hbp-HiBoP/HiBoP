using UnityEngine;
using HBP.UI.Tools;
using HBP.Core.Tools;
using System.Transactions;

namespace HBP.Data.Tools
{
    public class GlobalExceptionManager : Manager<GlobalExceptionManager>
    {
        #region Private Methods
        private string m_LastException;

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
                string exception = condition + "\n" + stackTrace;
                if (!string.Equals(exception, m_LastException, System.StringComparison.Ordinal))
                {
                    WindowsManager.Open("Bug Reporter window", null);
                    m_LastException = exception;
                }
            }
        }
        #endregion
    }
}