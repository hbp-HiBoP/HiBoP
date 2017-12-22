using System;
using System.Collections;
using UnityEngine;
using CielaSpike;
using UnityEngine.Events;

namespace Tools.Unity
{
    public class DialogBoxManager : MonoBehaviour
    {
        #region Properties
        [SerializeField, Candlelight.PropertyBackingField]
        private GameObject m_InformationAlertPrefab;
        public GameObject InformationAlertPrefab
        {
            get { return m_InformationAlertPrefab; }
            set { m_InformationAlertPrefab = value; }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private GameObject m_WarningAlertPrefab;
        public GameObject WarningAlertPrefab
        {
            get { return m_WarningAlertPrefab; }
            set { m_WarningAlertPrefab = value; }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private GameObject m_ErrorAlertPrefab;
        public GameObject ErrorAlertPrefab
        {
            get { return m_ErrorAlertPrefab; }
            set { m_ErrorAlertPrefab = value; }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private GameObject m_WarningAlertMultiOptionsPrefab;
        public GameObject WarningAlertMultiOptionsPrefab
        {
            get { return m_WarningAlertMultiOptionsPrefab; }
            set { m_WarningAlertMultiOptionsPrefab = value; }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private Canvas m_Canvas;
        public Canvas Canvas
        {
            get { return m_Canvas; }
            set { m_Canvas = value; }
        }

        public enum AlertType { Informational, Warning, Error, WarningMultiOptions }
        #endregion

        #region Public Methods
        public void Open(AlertType type, string title, string message, UnityAction button1action = null, string button1name = "", UnityAction button2action = null, string button2name = "")
        {
            GameObject dialogBox;
            switch (type)
            {
                case AlertType.Informational:
                    dialogBox = Instantiate(InformationAlertPrefab, Canvas.transform);
                    break;
                case AlertType.Warning:
                    dialogBox = Instantiate(WarningAlertPrefab, Canvas.transform);
                    break;
                case AlertType.Error:
                    dialogBox = Instantiate(ErrorAlertPrefab, Canvas.transform);
                    break;
                case AlertType.WarningMultiOptions:
                    dialogBox = Instantiate(WarningAlertMultiOptionsPrefab, Canvas.transform);
                    break;
                default:
                    dialogBox = Instantiate(InformationAlertPrefab, Canvas.transform);
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