using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Tools.Unity
{
    public class BugReporterWindow : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        private Button m_SubmitButton;
        [SerializeField]
        private Button m_CancelButton;
        [SerializeField]
        private Button m_CloseButton;
        [SerializeField]
        private InputField m_ReporterName;
        [SerializeField]
        private InputField m_Description;
        [SerializeField]
        private InputField m_MailAdress;
        #endregion

        #region Events
        public GenericEvent<bool> OnCloseWindow = new GenericEvent<bool>();
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_SubmitButton.onClick.AddListener(() =>
            {
                SendMail();
                OnCloseWindow.Invoke(true);
            });
            m_CancelButton.onClick.AddListener(() =>
            {
                OnCloseWindow.Invoke(false);
            });
            m_CloseButton.onClick.AddListener(() =>
            {
                OnCloseWindow.Invoke(false);
            });
        }
        private void SendMail()
        {
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("hibophelp@gmail.com", "Bug Reporter");
            mail.To.Add("hibophelp@gmail.com");
            mail.Subject = "BUGREPORT " + DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            mail.Body = m_ReporterName.text + " " + m_MailAdress.text + "\n\n" + m_Description.text;
            
            string logFile = "./HiBoP_Data/output_log.txt";
            if (File.Exists(logFile)) // windows only; TODO : multi support
            {
                string copiedLogFile = "./HiBoP_Data/log.txt";
                File.Copy(logFile, copiedLogFile, true);
                mail.Attachments.Add(new Attachment(copiedLogFile));
            }

            SmtpClient smtpServer = new SmtpClient("smtp.gmail.com");
            smtpServer.Port = 587;
            smtpServer.Credentials = new NetworkCredential("hibophelp@gmail.com", "hibop2017") as ICredentialsByHost;
            smtpServer.EnableSsl = true;
            ServicePointManager.ServerCertificateValidationCallback = delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };
            smtpServer.Send(mail);
        }
        #endregion
    }
}