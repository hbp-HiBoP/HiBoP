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
                BugReporterWindow window = Instantiate(m_BugReporterWindowPrefab, GameObject.Find("Windows").transform, false).GetComponent<BugReporterWindow>();
                m_WindowOpen = true;
                window.OnCloseWindow.AddListener((mailSent) =>
                {
                    Destroy(window.gameObject);
                    if (mailSent)
                    {
                        ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Informational, "Bug report successfully sent.", "The issue will be adressed as soon as possible. If you've entered your contact information, we may contact you for further information concerning the bug you encountered.");
                    }
                    m_WindowOpen = false;
                });
            }
        }
        #endregion

        #region Public Methods

        #endregion
    }
}