using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.UI.Tools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        private const string DISCORD_WEBHOOK_URL = "https://discord.com/api/webhooks/1445005361508647075/DwuGjWbEQPHAAqTOWEx_tist_ZcntvmLLdJSUPTnz8wJQpehSI2-naappwKV-IUlwbnG";
        private const int MAX_DIAGNOSTIC_LENGTH = 3200;
        private const int MAX_EMBED_TEXT_LENGTH = 5800;

        [SerializeField] private InputField m_NameInputField;
        [SerializeField] private InputField m_EmailInputField;
        [SerializeField] private InputField m_DescriptionInputField;

        private bool m_ReportSent;

        #endregion

        #region Public Methods

        public override async void OK()
        {
            try
            {
                await LoadingManager.LoadAsync(SendBugReportToDiscord);
                m_ReportSent = true;
                GlobalExceptionManager.CloseCurrentIncident();
                base.OK();
            }
            catch (Exception exception)
            {
                Debug.LogError($"Bug report sending failed: {exception}");
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "The report could not be sent", "Please check your internet connection and try again.\n\nError: " + exception.Message).Forget();
            }
        }

        public override void Close()
        {
            if (!m_ReportSent)
            {
                GlobalExceptionManager.CloseCurrentIncident();
            }

            base.Close();
        }

        #endregion

        #region Private Methods

        private void Start()
        {
            transform.SetParent(transform.parent.parent, false);
            transform.SetAsLastSibling();
        }

        private async UniTask SendBugReportToDiscord(Action<float, float, LoadingText> updateProgress)
        {
            await UniTask.SwitchToMainThread();
            string title = $"[Bug Report] {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            DiscordField[] fields = BuildDiscordFields();
            int fieldTextLength = title.Length;
            foreach (DiscordField field in fields)
            {
                fieldTextLength += field.name.Length + field.value.Length;
            }

            int diagnosticLength = Math.Max(0, Math.Min(MAX_DIAGNOSTIC_LENGTH, MAX_EMBED_TEXT_LENGTH - fieldTextLength));
            DiscordWebhookPayload webhookData = new()
            {
                allowed_mentions = new DiscordAllowedMentions(),
                embeds = new[]
                {
                    new DiscordEmbed
                    {
                        title = title,
                        description = BuildDiagnosticDescription(diagnosticLength),
                        color = 15158332,
                        timestamp = DateTime.UtcNow.ToString("o"),
                        fields = fields
                    }
                }
            };

            string jsonPayload = JsonConvert.SerializeObject(webhookData);
            using UnityWebRequest request = new(DISCORD_WEBHOOK_URL, "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonPayload));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            updateProgress?.Invoke(0.5f, 0, new LoadingText("Sending report"));
            await request.SendWebRequest();

            updateProgress?.Invoke(1f, 0, new LoadingText("Finalization"));
            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new Exception($"Discord webhook error: {request.responseCode} - {request.downloadHandler.text}");
            }

            DialogBoxManager.Open(Core.Enums.DialogBoxType.Informational, "Bug report successfully sent", "Thank you for your report! The issue will be addressed as soon as possible. If you've entered your contact information, we may reach out for further details.").Forget();
        }

        private string BuildDiagnosticDescription(int maxLength)
        {
            ExceptionIncidentSnapshot incident = GlobalExceptionManager.GetCurrentIncident();
            if (incident == null)
            {
                return PrepareFieldValue("_No automatic exception captured for this report._", maxLength);
            }

            if (maxLength < 8)
            {
                return string.Empty;
            }

            string diagnostic = IncidentDiscordFormatter.Format(incident, maxLength - 8);
            return $"```\n{diagnostic}\n```";
        }

        private DiscordField[] BuildDiscordFields()
        {
            List<DiscordField> fields = new();
            AddOptionalField(fields, "👤 Name", m_NameInputField.text, true, 256);
            AddOptionalField(fields, "📧 Email", m_EmailInputField.text, true, 256);

            fields.Add(new DiscordField
            {
                name = "📝 Description",
                value = string.IsNullOrWhiteSpace(m_DescriptionInputField.text) ? "_No description provided_" : PrepareFieldValue(m_DescriptionInputField.text, 1024),
                inline = false
            });

            StringBuilder system = new();
            system.AppendLine($"**HiBoP:** {GetBuildIdentifier()}");
            system.AppendLine($"**Unity:** {Application.unityVersion}");
            system.AppendLine($"**Platform:** {Application.platform}");
            system.AppendLine($"**OS:** {SystemInfo.operatingSystem}");
            fields.Add(new DiscordField { name = "🖥️ System", value = PrepareFieldValue(system.ToString(), 1024), inline = true });

            StringBuilder hardware = new();
            hardware.AppendLine($"**CPU:** {SystemInfo.processorType}");
            hardware.AppendLine($"**Cores:** {SystemInfo.processorCount}");
            hardware.AppendLine($"**RAM:** {SystemInfo.systemMemorySize} MB");
            hardware.AppendLine($"**GPU:** {SystemInfo.graphicsDeviceName}");
            hardware.AppendLine($"**VRAM:** {SystemInfo.graphicsMemorySize} MB");
            fields.Add(new DiscordField { name = "⚙️ Hardware", value = PrepareFieldValue(hardware.ToString(), 1024), inline = true });

            StringBuilder display = new();
            display.AppendLine($"**Resolution:** {Screen.currentResolution.width}x{Screen.currentResolution.height}");
            display.AppendLine($"**DPI:** {Screen.dpi}");
            display.AppendLine($"**Fullscreen:** {(Screen.fullScreen ? "Yes" : "No")}");
            fields.Add(new DiscordField { name = "🖼️ Display", value = PrepareFieldValue(display.ToString(), 1024), inline = true });

            return fields.ToArray();
        }

        private static void AddOptionalField(List<DiscordField> fields, string name, string value, bool inline, int maxLength)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                fields.Add(new DiscordField { name = name, value = PrepareFieldValue(value, maxLength), inline = inline });
            }
        }

        private static string PrepareFieldValue(string value, int maxLength)
        {
            if (maxLength <= 0)
            {
                return string.Empty;
            }

            string sanitized = IncidentDiscordFormatter.SanitizeForDiscord(value);
            if (string.IsNullOrEmpty(sanitized) || sanitized.Length <= maxLength)
            {
                return sanitized;
            }

            if (maxLength <= 3)
            {
                return sanitized[..maxLength];
            }

            return sanitized[..(maxLength - 3)] + "...";
        }

        private static string GetBuildIdentifier()
        {
            TextAsset buildInfoAsset = Resources.Load<TextAsset>("BuildInfo");
            if (buildInfoAsset == null)
            {
                return Application.version;
            }

            try
            {
                BuildInfo buildInfo = JsonConvert.DeserializeObject<BuildInfo>(buildInfoAsset.text);
                return buildInfo != null && !string.IsNullOrWhiteSpace(buildInfo.Commit) ? $"{Application.version}+{buildInfo.Commit}" : Application.version;
            }
            catch
            {
                return Application.version;
            }
        }

        #endregion

        #region Helper Classes

        [JsonObject(MemberSerialization.Fields), Preserve]
        private class DiscordWebhookPayload
        {
            public DiscordAllowedMentions allowed_mentions;
            public DiscordEmbed[] embeds;
        }

        [JsonObject(MemberSerialization.Fields), Preserve]
        private class DiscordAllowedMentions
        {
            public string[] parse = Array.Empty<string>();
        }

        [JsonObject(MemberSerialization.Fields), Preserve]
        private class DiscordEmbed
        {
            public string title;
            public string description;
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

        #endregion
    }
}
