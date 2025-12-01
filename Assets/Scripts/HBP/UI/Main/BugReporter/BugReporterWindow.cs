using HBP.UI.Tools;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class BugReporterWindow : DialogWindow
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] InputField m_EmailInputField;
        [SerializeField] InputField m_DescriptionInputField;

        // GitHub configuration - Fine-grained token with only Issues:Write permission on hbp-HiBoP/HiBoP-Issues repository
        private const string GITHUB_TOKEN = "";
        private const string GITHUB_REPO_OWNER = "hbp-HiBoP";
        private const string GITHUB_REPO_NAME = "HiBoP-Issues";
        #endregion

        #region Public Methods
        public override async void OK()
        {
            try
            {
                if (string.IsNullOrEmpty(m_DescriptionInputField.text))
                {
                    int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Empty description", "The description field is empty; we might not be able to help you properly.\nDo you still want to send the bug report without any description ?", "Send", "Cancel");
                    if (result == 0)
                    {
                        await CreateGitHubIssue();
                        base.OK();
                    }
                }
                else
                {
                    await CreateGitHubIssue();
                    base.OK();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "The report could not be sent", "Please check your internet connection and try again.\n\nError: " + e.Message).Forget();
                base.OK();
            }
        }
        #endregion

        #region Private Methods
        private void Start()
        {
            transform.parent = transform.parent.parent;
            transform.SetAsLastSibling();
        }

        private async Task CreateGitHubIssue()
        {
            string issueTitle = $"[Bug Report] {DateTime.Now:yyyy-MM-dd HH:mm}";
            string issueBody = BuildIssueBody();

            string url = $"https://api.github.com/repos/{GITHUB_REPO_OWNER}/{GITHUB_REPO_NAME}/issues";
            
            var issueData = new GitHubIssueRequest
            {
                title = issueTitle,
                body = issueBody,
                labels = new string[] { "bug", "user-report" }
            };

            string jsonPayload = JsonConvert.SerializeObject(issueData);

            using UnityWebRequest request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {GITHUB_TOKEN}");
            request.SetRequestHeader("User-Agent", "HiBoP-BugReporter");
            request.SetRequestHeader("Accept", "application/vnd.github+json");
            request.SetRequestHeader("X-GitHub-Api-Version", "2022-11-28");

            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonConvert.DeserializeObject<GitHubIssueResponse>(request.downloadHandler.text);
                DialogBoxManager.Open(
                    Core.Enums.DialogBoxType.Informational,
                    "Bug report successfully sent",
                    $"Thank you for your report! The issue has been created and will be addressed as soon as possible."
                ).Forget();
            }
            else
            {
                throw new Exception($"GitHub API error: {request.responseCode} - {request.downloadHandler.text}");
            }
        }

        private string BuildIssueBody()
        {
            StringBuilder bodyBuilder = new StringBuilder();

            // Contact information
            if (!string.IsNullOrEmpty(m_NameInputField.text) || !string.IsNullOrEmpty(m_EmailInputField.text))
            {
                bodyBuilder.AppendLine("## Contact Information");
                if (!string.IsNullOrEmpty(m_NameInputField.text))
                    bodyBuilder.AppendLine($"- **Name:** {m_NameInputField.text}");
                if (!string.IsNullOrEmpty(m_EmailInputField.text))
                    bodyBuilder.AppendLine($"- **Email:** {m_EmailInputField.text}");
                bodyBuilder.AppendLine();
            }

            // Description
            bodyBuilder.AppendLine("## Description");
            bodyBuilder.AppendLine(string.IsNullOrEmpty(m_DescriptionInputField.text) ? "_No description provided_" : m_DescriptionInputField.text);
            bodyBuilder.AppendLine();

            // System Information
            bodyBuilder.AppendLine("## System Information");
            bodyBuilder.AppendLine("<details>");
            bodyBuilder.AppendLine("<summary>Click to expand</summary>");
            bodyBuilder.AppendLine();
            bodyBuilder.AppendLine("| Property | Value |");
            bodyBuilder.AppendLine("|----------|-------|");
            bodyBuilder.AppendLine($"| **HiBoP Version** | {Application.version} |");
            bodyBuilder.AppendLine($"| **Unity Version** | {Application.unityVersion} |");
            bodyBuilder.AppendLine($"| **Platform** | {Application.platform} |");
            bodyBuilder.AppendLine($"| **OS** | {SystemInfo.operatingSystem} |");
            bodyBuilder.AppendLine($"| **Device Model** | {SystemInfo.deviceModel} |");
            bodyBuilder.AppendLine($"| **Device Type** | {SystemInfo.deviceType} |");
            bodyBuilder.AppendLine($"| **Processor** | {SystemInfo.processorType} ({SystemInfo.processorCount} cores) |");
            bodyBuilder.AppendLine($"| **System Memory** | {SystemInfo.systemMemorySize} MB |");
            bodyBuilder.AppendLine($"| **Graphics Device** | {SystemInfo.graphicsDeviceName} |");
            bodyBuilder.AppendLine($"| **Graphics Vendor** | {SystemInfo.graphicsDeviceVendor} |");
            bodyBuilder.AppendLine($"| **Graphics Memory** | {SystemInfo.graphicsMemorySize} MB |");
            bodyBuilder.AppendLine($"| **Max Texture Size** | {SystemInfo.maxTextureSize} |");
            bodyBuilder.AppendLine($"| **Screen Resolution** | {Screen.currentResolution.width}x{Screen.currentResolution.height} @ {Screen.dpi} DPI |");
            bodyBuilder.AppendLine($"| **Fullscreen** | {Screen.fullScreen} |");
            bodyBuilder.AppendLine();
            bodyBuilder.AppendLine("</details>");
            bodyBuilder.AppendLine();

            // Log file excerpt
            string logContent = GetRecentLogContent();
            if (!string.IsNullOrEmpty(logContent))
            {
                bodyBuilder.AppendLine("## Recent Log");
                bodyBuilder.AppendLine("<details>");
                bodyBuilder.AppendLine("<summary>Click to expand (last 100 lines)</summary>");
                bodyBuilder.AppendLine();
                bodyBuilder.AppendLine("```");
                bodyBuilder.AppendLine(logContent);
                bodyBuilder.AppendLine("```");
                bodyBuilder.AppendLine();
                bodyBuilder.AppendLine("</details>");
            }

            return bodyBuilder.ToString();
        }

        private string GetRecentLogContent()
        {
            string logFile = GetLogFilePath();
            
            if (!File.Exists(logFile))
                return null;

            try
            {
                // Copy the file first to avoid file lock issues
                string copiedLogFile = Path.Combine(Application.persistentDataPath, "error_log_temp.txt");
                File.Copy(logFile, copiedLogFile, true);

                string[] lines = File.ReadAllLines(copiedLogFile);
                
                // Get last 1000 lines
                int startIndex = Math.Max(0, lines.Length - 1000);
                StringBuilder logBuilder = new StringBuilder();
                
                for (int i = startIndex; i < lines.Length; i++)
                {
                    logBuilder.AppendLine(lines[i]);
                }

                // Clean up temp file
                try { File.Delete(copiedLogFile); } catch { }

                return logBuilder.ToString();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not read log file: {e.Message}");
                return null;
            }
        }

        private string GetLogFilePath()
        {
            return Application.platform switch
            {
                RuntimePlatform.OSXPlayer => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Logs", Application.companyName, Application.productName, "Player.log"),
                RuntimePlatform.WindowsPlayer => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "..", "LocalLow", Application.companyName, Application.productName, "Player.log"),
                RuntimePlatform.LinuxPlayer => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "unity3d", Application.companyName, Application.productName, "Player.log"),
                _ => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "..", "LocalLow", Application.companyName, Application.productName, "Player.log"),
            };
        }
        #endregion

        #region Helper Classes
        [JsonObject(MemberSerialization.Fields), Preserve]
        private class GitHubIssueRequest
        {
            public string title;
            public string body;
            public string[] labels;
        }

        [JsonObject(MemberSerialization.Fields), Preserve]
        private class GitHubIssueResponse
        {
            public string html_url;
            public int number;
        }
        #endregion
    }
}