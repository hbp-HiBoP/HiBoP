using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
                try
                {
                    SendMail();
                    OnCloseWindow.Invoke(true);
                }
                catch (Exception e)
                {
                    OnCloseWindow.Invoke(false);
                    if (e is SmtpException)
                    {
                        ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "The report could not be sent", "Please check your internet connection and try again.");
                    }
                    else
                    {
                        ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, e.Source, e.Message);
                    }
                }
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

            StringBuilder bodyBuilder = new StringBuilder();
            bodyBuilder.AppendFormat
            (
                "{0} {1} {2} {3}\n{4}, {5}, {6}x {7}\n{8}x{9} {10}dpi FullScreen {11}, {12}, {13} vmem: {14} Max Texture: {15}\n",
                SystemInfo.deviceModel,
                SystemInfo.deviceName,
                SystemInfo.deviceType,
                SystemInfo.deviceUniqueIdentifier,

                SystemInfo.operatingSystem,
                SystemInfo.systemMemorySize,
                SystemInfo.processorCount,
                SystemInfo.processorType,

                Screen.currentResolution.width,
                Screen.currentResolution.height,
                Screen.dpi,
                Screen.fullScreen,
                SystemInfo.graphicsDeviceName,
                SystemInfo.graphicsDeviceVendor,
                SystemInfo.graphicsMemorySize,
                SystemInfo.maxTextureSize
            );
            bodyBuilder.AppendLine(" ");
            bodyBuilder.AppendLine(m_ReporterName.text);
            bodyBuilder.AppendLine(m_MailAdress.text);
            bodyBuilder.AppendLine(" ");
            bodyBuilder.AppendLine(m_Description.text);
            mail.Body = bodyBuilder.ToString();

            string logFile = "";
            switch (Application.platform)
            {
                case RuntimePlatform.OSXPlayer:
                    logFile = "~/Library/Logs/Unity/Player.log";
                    break;
                case RuntimePlatform.WindowsPlayer:
                    logFile = "./HiBoP_Data/output_log.txt";
                    break;
                case RuntimePlatform.LinuxPlayer:
                    logFile = "~/.config/unity3d/CRNL/HiBoP/Player.log";
                    break;
                default:
                    logFile = "./HiBoP_Data/output_log.txt";
                    break;
            }
            if (File.Exists(logFile))
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