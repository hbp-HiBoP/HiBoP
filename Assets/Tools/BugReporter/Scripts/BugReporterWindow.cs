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
    public class BugReporterWindow : DialogWindow
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] InputField m_EmailInputField;
        [SerializeField] InputField m_DescriptionInputField;
        #endregion

        #region Public Methods
        public override void OK()
        {
            try
            {
                if (string.IsNullOrEmpty(m_DescriptionInputField.text))
                {
                    ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.WarningMultiOptions, "Empty description", "The description field is empty; we might not be able to help you properly.\nDo you still want to send the bug report without any description ?",
                        () => { SendMail(); base.OK(); }, "Send",
                        () => { }, "Cancel"
                        );
                }
                else
                {
                    SendMail();
                    base.OK();
                }
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
                base.OK();
            }
        }
        #endregion

        #region Private Methods
        private void SendMail()
        {
            using (SmtpClient smtpServer = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("hibophelp@gmail.com", "hibop2017") as ICredentialsByHost,
                EnableSsl = true
            })
            {
                ServicePointManager.ServerCertificateValidationCallback = delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };
                using (MailMessage mail = new MailMessage())
                {
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
                            logFile = Path.Combine("~", "Library", "Logs", Application.companyName, Application.productName, "Player.log");
                            break;
                        case RuntimePlatform.WindowsPlayer:
                            logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "..", "LocalLow", Application.companyName, Application.productName, "Player.log");
                            break;
                        case RuntimePlatform.LinuxPlayer:
                            logFile = Path.Combine("~", ".config", "unity3d", Application.companyName, Application.productName, "Player.log");
                            break;
                        default:
                            logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "..", "LocalLow", Application.companyName, Application.productName, "Player.log");
                            break;
                    }
                    if (File.Exists(logFile))
                    {
                        string copiedLogFile = Path.Combine(Application.dataPath, "error_log.txt");
                        File.Copy(logFile, copiedLogFile, true);
                        using (Attachment log = new Attachment(copiedLogFile))
                        {
                            mail.Attachments.Add(log);
                            smtpServer.Send(mail);
                        }
                    }
                    else
                    {
                        smtpServer.Send(mail);
                    }

                    ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Informational, "Bug report successfully sent.", "The issue will be adressed as soon as possible. If you've entered your contact information, we may contact you for further information concerning the bug you encountered.");
                }
            }
        }
        #endregion
    }
}