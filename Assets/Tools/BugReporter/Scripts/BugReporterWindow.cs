using HBP.UI;
using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity
{
    public class BugReporterWindow : SavableWindow
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] InputField m_EmailInputField;
        [SerializeField] InputField m_DescriptionInputField;
        #endregion

        #region Public Methods
        public override void Save()
        {
            try
            {
                SendMail();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (e is SmtpException)
                {
                    ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "The report could not be sent", "Please check your internet connection and try again.");
                }
                else
                {
                    ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, e.Source, e.Message);
                }
            }
            base.Save();
        }
        #endregion

        #region Private Methods
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
            bodyBuilder.AppendLine(m_NameInputField.text);
            bodyBuilder.AppendLine(m_EmailInputField.text);
            bodyBuilder.AppendLine(" ");
            bodyBuilder.AppendLine(m_DescriptionInputField.text);
            mail.Body = bodyBuilder.ToString();

            string logFile = "";
            switch (Application.platform)
            {
                case RuntimePlatform.OSXPlayer:
                    logFile = "~/Library/Logs/Unity/Player.log";
                    break;
                case RuntimePlatform.WindowsPlayer:
                    logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"LocalLow/HiBoP_Data/output_log.txt");
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
            ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Informational, "Bug report successfully sent.", "The issue will be adressed as soon as possible. If you've entered your contact information, we may contact you for further information concerning the bug you encountered.");
        }
        #endregion
    }
}