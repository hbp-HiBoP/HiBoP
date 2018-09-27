using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tools.Unity
{
    public class GlobalExceptionManager : MonoBehaviour // Maybe FIXME : integrate this to the windows system
    {
        #region Properties
        private bool m_WindowOpen = false;
        [SerializeField]
        private GameObject m_BugReporterWindowPrefab;
        #endregion

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
            if ((type == LogType.Error || type == LogType.Exception) && !m_WindowOpen)
            {
                OpenBugReporter();
            }
        }
        #endregion

        #region Public Methods
        public void OpenBugReporter()
        {
            BugReporterWindow window = Instantiate(m_BugReporterWindowPrefab, GameObject.Find("Windows").transform, false).GetComponent<BugReporterWindow>();
            m_WindowOpen = true;
            window.OnClose.AddListener(() => m_WindowOpen = false);
        }
        #endregion
    }
}