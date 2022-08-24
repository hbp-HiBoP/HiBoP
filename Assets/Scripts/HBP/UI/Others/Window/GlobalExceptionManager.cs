using UnityEngine;
using HBP.UI.Tools;

namespace HBP.Data.Tools
{
    public class GlobalExceptionManager : MonoBehaviour
    {
        #region Properties
        private static GlobalExceptionManager m_Instance;
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