using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI.Tools
{
    public class DialogBoxManager : MonoBehaviour
    {
        #region Properties
        private static DialogBoxManager m_Instance;

        [SerializeField] private GameObject m_InformationAlertPrefab;
        [SerializeField] private GameObject m_WarningAlertPrefab;
        [SerializeField] private GameObject m_ErrorAlertPrefab;
        [SerializeField] private GameObject m_WarningAlertMultiOptionsPrefab;
        [SerializeField] private Canvas m_Canvas;

        public enum AlertType { Informational, Warning, Error, WarningMultiOptions }
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
        public static void Open(AlertType type, string title, string message, UnityAction button1action = null, string button1name = "", UnityAction button2action = null, string button2name = "")
        {
            GameObject dialogBox;
            switch (type)
            {
                case AlertType.Informational:
                    dialogBox = Instantiate(m_Instance.m_InformationAlertPrefab, m_Instance.m_Canvas.transform);
                    break;
                case AlertType.Warning:
                    dialogBox = Instantiate(m_Instance.m_WarningAlertPrefab, m_Instance.m_Canvas.transform);
                    break;
                case AlertType.Error:
                    dialogBox = Instantiate(m_Instance.m_ErrorAlertPrefab, m_Instance.m_Canvas.transform);
                    break;
                case AlertType.WarningMultiOptions:
                    dialogBox = Instantiate(m_Instance.m_WarningAlertMultiOptionsPrefab, m_Instance.m_Canvas.transform);
                    break;
                default:
                    dialogBox = Instantiate(m_Instance.m_InformationAlertPrefab, m_Instance.m_Canvas.transform);
                    break;
            }
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