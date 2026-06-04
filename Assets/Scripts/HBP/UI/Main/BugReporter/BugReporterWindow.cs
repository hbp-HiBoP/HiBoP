using Cysharp.Threading.Tasks;
using HBP.Core.Tools;
using HBP.UI.Tools;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
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

        private const string DISCORD_WEBHOOK_URL = "https://discord.com/api/webhooks/1445005361508647075/DwuGjWbEQPHAAqTOWEx_tist_ZcntvmLLdJSUPTnz8wJQpehSI2-naappwKV-IUlwbnG";
        private const int MAX_PASTE_SIZE = 400000; // 400 KB limit
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
                        await LoadingManager.LoadAsync(SendBugReportToDiscord);
                        base.OK();
                    }
                }
                else
                {
                    await LoadingManager.LoadAsync(SendBugReportToDiscord);
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
        private async UniTask SendBugReportToDiscord(Action<float, float, LoadingText> updateProgress)
        {
            await UniTask.SwitchToMainThread();
            // Upload full log to a paste service first
            updateProgress?.Invoke(0f, 0, new LoadingText("Uploading log file"));
            string logUrl = await UploadLogToPasteService();

            var webhookData = new DiscordWebhookPayload
            {
                embeds = new DiscordEmbed[]
                {
                    new() {
                        title = $"[Bug Report] {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                        color = 15158332, // Red color
                        timestamp = DateTime.UtcNow.ToString("o"),
                        fields = BuildDiscordFields(logUrl)
                    }
                }
            };

            string jsonPayload = JsonConvert.SerializeObject(webhookData);

            using UnityWebRequest request = new(DISCORD_WEBHOOK_URL, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            updateProgress?.Invoke(0.5f, 0, new LoadingText("Sending report"));
            var operation = await request.SendWebRequest();
            await UniTask.WaitUntil(() => operation.isDone);

            updateProgress?.Invoke(1f, 0, new LoadingText("Finalization"));
            if (request.result == UnityWebRequest.Result.Success)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Informational, "Bug report successfully sent", "Thank you for your report! The issue will be addressed as soon as possible. If you've entered your contact information, we may reach out for further details.").Forget();
            }
            else
            {
                throw new Exception($"Discord webhook error: {request.responseCode} - {request.downloadHandler.text}");
            }
        }
        private DiscordField[] BuildDiscordFields(string logUrl)
        {
            var fields = new System.Collections.Generic.List<DiscordField>();

            // Contact Information
            if (!string.IsNullOrEmpty(m_NameInputField.text))
            {
                fields.Add(new DiscordField
                {
                    name = "👤 Name",
                    value = m_NameInputField.text,
                    inline = true
                });
            }

            if (!string.IsNullOrEmpty(m_EmailInputField.text))
            {
                fields.Add(new DiscordField
                {
                    name = "📧 Email",
                    value = m_EmailInputField.text,
                    inline = true
                });
            }

            // Description
            fields.Add(new DiscordField
            {
                name = "📝 Description",
                value = string.IsNullOrEmpty(m_DescriptionInputField.text) ? "_No description provided_" : TruncateForDiscord(m_DescriptionInputField.text, 1024),
                inline = false
            });

            // System Information
            StringBuilder sysInfo = new();
            sysInfo.AppendLine($"**HiBoP:** {Application.version}");
            sysInfo.AppendLine($"**Unity:** {Application.unityVersion}");
            sysInfo.AppendLine($"**Platform:** {Application.platform}");
            sysInfo.AppendLine($"**OS:** {SystemInfo.operatingSystem}");
            
            fields.Add(new DiscordField
            {
                name = "🖥️ System",
                value = sysInfo.ToString(),
                inline = true
            });

            // Hardware Information
            StringBuilder hwInfo = new();
            hwInfo.AppendLine($"**CPU:** {SystemInfo.processorType}");
            hwInfo.AppendLine($"**Cores:** {SystemInfo.processorCount}");
            hwInfo.AppendLine($"**RAM:** {SystemInfo.systemMemorySize} MB");
            hwInfo.AppendLine($"**GPU:** {SystemInfo.graphicsDeviceName}");
            hwInfo.AppendLine($"**VRAM:** {SystemInfo.graphicsMemorySize} MB");
            
            fields.Add(new DiscordField
            {
                name = "⚙️ Hardware",
                value = hwInfo.ToString(),
                inline = true
            });

            // Display Information
            StringBuilder displayInfo = new();
            displayInfo.AppendLine($"**Resolution:** {Screen.currentResolution.width}x{Screen.currentResolution.height}");
            displayInfo.AppendLine($"**DPI:** {Screen.dpi}");
            displayInfo.AppendLine($"**Fullscreen:** {(Screen.fullScreen ? "Yes" : "No")}");
            
            fields.Add(new DiscordField
            {
                name = "🖼️ Display",
                value = displayInfo.ToString(),
                inline = true
            });

            // Full Log Link
            if (!string.IsNullOrEmpty(logUrl))
            {
                fields.Add(new DiscordField
                {
                    name = "📋 Full Log File",
                    value = $"[Click here to view complete log]({logUrl})",
                    inline = false
                });
            }
            else
            {
                // Fallback: show recent log excerpt if upload failed
                string logContent = GetRecentLogContent(100);
                if (!string.IsNullOrEmpty(logContent))
                {
                    fields.Add(new DiscordField
                    {
                        name = "📋 Recent Log (last 100 lines)",
                        value = "```\n" + TruncateForDiscord(logContent, 900) + "\n```",
                        inline = false
                    });
                }
            }

            return fields.ToArray();
        }
        private string TruncateForDiscord(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;

            return text[..(maxLength - 3)] + "...";
        }
        private async UniTask<string> UploadLogToPasteService()
        {
            try
            {
                string logFile = GetLogFilePath();
                
                if (!File.Exists(logFile))
                    return null;

                // Copy the file first to avoid file lock issues
                string copiedLogFile = Path.Combine(Application.persistentDataPath, "error_log_temp.txt");
                File.Copy(logFile, copiedLogFile, true);

                string logContent = File.ReadAllText(copiedLogFile);
                
                // Clean up temp file
                try { File.Delete(copiedLogFile); } catch { }

                // Check size and truncate if necessary
                bool wasTruncated = false;
                if (Encoding.UTF8.GetByteCount(logContent) > MAX_PASTE_SIZE)
                {
                    // Get last 2000 lines instead of full file
                    string[] lines = logContent.Split('\n');
                    int startIndex = Math.Max(0, lines.Length - 2000);
                    logContent = string.Join("\n", lines, startIndex, lines.Length - startIndex);
                    wasTruncated = true;
                }

                // Add header with system info
                StringBuilder fullLog = new();
                fullLog.AppendLine("=" + new string('=', 78));
                fullLog.AppendLine($"  HiBoP Bug Report - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                fullLog.AppendLine("=" + new string('=', 78));
                fullLog.AppendLine();
                fullLog.AppendLine($"HiBoP Version: {Application.version}");
                fullLog.AppendLine($"Unity Version: {Application.unityVersion}");
                fullLog.AppendLine($"Platform: {Application.platform}");
                fullLog.AppendLine($"OS: {SystemInfo.operatingSystem}");
                fullLog.AppendLine($"Device: {SystemInfo.deviceModel}");
                fullLog.AppendLine($"Processor: {SystemInfo.processorType} ({SystemInfo.processorCount} cores)");
                fullLog.AppendLine($"Memory: {SystemInfo.systemMemorySize} MB");
                fullLog.AppendLine($"GPU: {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsMemorySize} MB)");
                fullLog.AppendLine($"Resolution: {Screen.currentResolution.width}x{Screen.currentResolution.height} @ {Screen.dpi} DPI");
                fullLog.AppendLine();
                if (wasTruncated)
                {
                    fullLog.AppendLine("⚠️  WARNING: Log file was too large. Showing last 2000 lines only.");
                    fullLog.AppendLine();
                }
                fullLog.AppendLine("=" + new string('=', 78));
                fullLog.AppendLine("  LOG CONTENT");
                fullLog.AppendLine("=" + new string('=', 78));
                fullLog.AppendLine();
                fullLog.Append(logContent);

                string finalContent = fullLog.ToString();

                // Try multiple paste services with fallback
                string url = await TryUploadToDpaste(finalContent);
                if (url != null) return url;

                url = await TryUploadToPasteEe(finalContent);
                if (url != null) return url;

                url = await TryUploadToRentry(finalContent);
                if (url != null) return url;

                Debug.LogWarning("Failed to upload log to any paste service");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error uploading log to paste service: {e.Message}");
                return null;
            }
        }
        private async UniTask<string> TryUploadToPasteEe(string content)
        {
            try
            {
                var formData = new WWWForm();
                formData.AddField("key", "");
                formData.AddField("description", "HiBoP Bug Report Log");
                formData.AddField("paste", content);
                formData.AddField("format", "text");
                formData.AddField("expiration", "1209600"); // 2 weeks
                formData.AddField("encrypted", "0");

                using UnityWebRequest request = UnityWebRequest.Post("https://api.paste.ee/v1/pastes", formData);
                
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    await UniTask.Yield();
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonConvert.DeserializeObject<PasteEeResponse>(request.downloadHandler.text);
                    if (response != null && !string.IsNullOrEmpty(response.link))
                    {
                        Debug.Log("Successfully uploaded to Paste.ee");
                        return response.link;
                    }
                }

                Debug.LogWarning($"Paste.ee upload failed: {request.responseCode}");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Paste.ee error: {e.Message}");
                return null;
            }
        }
        private async UniTask<string> TryUploadToDpaste(string content)
        {
            try
            {
                var formData = new WWWForm();
                formData.AddField("content", content);
                formData.AddField("title", "HiBoP Bug Report Log");
                formData.AddField("syntax", "text");
                formData.AddField("expiry_days", "7");

                using UnityWebRequest request = UnityWebRequest.Post("https://dpaste.com/api/", formData);
                request.SetRequestHeader("Accept", "application/json");
                
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    await UniTask.Yield();
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    // dpaste returns the URL directly in the response
                    string url = request.downloadHandler.text.Trim();
                    if (!string.IsNullOrEmpty(url) && url.StartsWith("http"))
                    {
                        Debug.Log("Successfully uploaded to dpaste.com");
                        return url;
                    }
                }

                Debug.LogWarning($"dpaste.com upload failed: {request.responseCode}");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"dpaste.com error: {e.Message}");
                return null;
            }
        }
        private async UniTask<string> TryUploadToRentry(string content)
        {
            try
            {
                var formData = new WWWForm();
                formData.AddField("text", content);

                using UnityWebRequest request = UnityWebRequest.Post("https://rentry.co/api/new", formData);
                
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    await UniTask.Yield();
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonConvert.DeserializeObject<RentryResponse>(request.downloadHandler.text);
                    if (response != null && !string.IsNullOrEmpty(response.url))
                    {
                        Debug.Log("Successfully uploaded to Rentry.co");
                        return response.url;
                    }
                }

                Debug.LogWarning($"Rentry.co upload failed: {request.responseCode}");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Rentry.co error: {e.Message}");
                return null;
            }
        }
        private string GetRecentLogContent(int lineCount = 50)
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
                
                // Get last N lines
                int startIndex = Math.Max(0, lines.Length - lineCount);
                StringBuilder logBuilder = new();
                
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
        private class DiscordWebhookPayload
        {
            public string username;
            public string avatar_url;
            public DiscordEmbed[] embeds;
        }

        [JsonObject(MemberSerialization.Fields), Preserve]
        private class DiscordEmbed
        {
            public string title;
            public int color;
            public string timestamp;
            public DiscordField[] fields;
        }

        [JsonObject(MemberSerialization.Fields), Preserve]
        private class DiscordField
        {
            public string name;
            public string value;
            public bool inline;
        }

        [JsonObject(MemberSerialization.Fields), Preserve]
        private class PasteEeResponse
        {
            public string id;
            public string link;
        }

        [JsonObject(MemberSerialization.Fields), Preserve]
        private class RentryResponse
        {
            public string url;
            public string edit_code;
        }
        #endregion
    }
}