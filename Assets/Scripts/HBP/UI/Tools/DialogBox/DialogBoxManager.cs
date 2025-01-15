using HBP.Core.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI.Tools
{
    public class DialogBoxManager : Manager<DialogBoxManager>
    {
        #region Properties
        [SerializeField] private GameObject m_InformationAlertPrefab;
        [SerializeField] private GameObject m_WarningAlertPrefab;
        [SerializeField] private GameObject m_ErrorAlertPrefab;
        [SerializeField] private GameObject m_WarningAlertMultiOptionsPrefab;
        [SerializeField] private Canvas m_Canvas;

        public enum AlertType { Informational, Warning, Error, WarningMultiOptions }
        #endregion

        #region Public Methods
        public static void Open(AlertType type, string title, string message, UnityAction button1action = null, string button1name = "", UnityAction button2action = null, string button2name = "")
        {
            GameObject dialogBox = type switch
            {
                AlertType.Informational => Instantiate(m_Instance.m_InformationAlertPrefab, m_Instance.m_Canvas.transform),
                AlertType.Warning => Instantiate(m_Instance.m_WarningAlertPrefab, m_Instance.m_Canvas.transform),
                AlertType.Error => Instantiate(m_Instance.m_ErrorAlertPrefab, m_Instance.m_Canvas.transform),
                AlertType.WarningMultiOptions => Instantiate(m_Instance.m_WarningAlertMultiOptionsPrefab, m_Instance.m_Canvas.transform),
                _ => Instantiate(m_Instance.m_InformationAlertPrefab, m_Instance.m_Canvas.transform),
            };
            dialogBox.transform.SetAsLastSibling();
            if (type == AlertType.WarningMultiOptions)
            {
                dialogBox.GetComponent<MultiOptionsDialogBox>().Open(title, message, button1action, button1name, button2action, button2name);
            }
            else
            {
                dialogBox.GetComponent<DialogBox>().Open(title, message);
            }
        }
        #endregion
    }
}