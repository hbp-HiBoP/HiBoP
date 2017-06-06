using System;
using System.Collections;
using UnityEngine;
using CielaSpike;

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
        private Canvas m_Canvas;
        public Canvas Canvas
        {
            get { return m_Canvas; }
            set { m_Canvas = value; }
        }

        public enum AlertType { Informational, Warning, Error }
        #endregion

        #region Public Methods
        public void Open(AlertType type,string title, string message)
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
                default:
                    dialogBox = Instantiate(InformationAlertPrefab, Canvas.transform);
                    break;
            }
            dialogBox.transform.SetAsLastSibling();
            dialogBox.GetComponent<DialogBox>().Open(title, message);
        }
        #endregion
    }
}