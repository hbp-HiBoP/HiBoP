using HBP.Core.Data;
using HBP.Core.Tools;
using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using HBP.UI.Tools;
using Cysharp.Threading.Tasks;

namespace HBP.UI.Main
{
    public class VersionLabel : MonoBehaviour
    {
        #region Properties

        [SerializeField] Text m_Text;

        #endregion

        #region Private Methods

        private void Awake()
        {
            m_Text.text = string.Format("{0} {1}", Application.productName, Application.version);
            CheckVersion().Forget();
        }

        private async UniTaskVoid CheckVersion()
        {
            string version = Application.version;

            try
            {
                using UnityWebRequest request = UnityWebRequest.Get("https://api.github.com/repos/hbp-HiBoP/HiBoP/releases/latest");
                request.SetRequestHeader("User-Agent", "HiBoP-VersionCheck");
                var operation = request.SendWebRequest();
                await UniTask.WaitUntil(() => operation.isDone);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    LogVersionCheckFailure(request.error);
                    return;
                }

                string jsonString = request.downloadHandler.text;
                var versionInfo = ClassLoaderSaver.LoadFromJsonString<GithubVersionInfo>(jsonString);
                version = versionInfo.VersionNumber;
            }
            catch (Exception e)
            {
                LogVersionCheckFailure(e.ToString());
                return;
            }

            if (string.Compare(version, Application.version) > 0)
            {
                int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Informational, "New version available", "A new version of HiBoP is available. Please update to the latest version.", "Update now", "Remind me later");

                if (result == 0)
                    WindowsManager.Open("Version Window", null);
            }
        }

        private static void LogVersionCheckFailure(string message)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"Latest version check failed: {message}");
#endif
        }

        #endregion
    }
}
