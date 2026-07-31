using HBP.Core.Data;
using HBP.Core.Tools;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using HBP.UI.Tools;
using Cysharp.Threading.Tasks;

namespace HBP.UI.Main
{
    public class VersionWindow : DialogWindow
    {
        [SerializeField] Text m_CurrentText;
        [SerializeField] Text m_LatestText;
        [SerializeField] Text m_LatestDescription;
        [SerializeField] Button m_GithubButton;

        protected override void SetFields()
        {
            base.SetFields();
            m_CurrentText.text = Application.version;
            FetchLatestVersionAsync().Forget();
        }

        private async UniTaskVoid FetchLatestVersionAsync()
        {
            using UnityWebRequest request = UnityWebRequest.Get("https://api.github.com/repos/hbp-HiBoP/HiBoP/releases/latest");
            request.SetRequestHeader("User-Agent", "Other");

            var operation = request.SendWebRequest();
            await UniTask.WaitUntil(() => operation.isDone);

            try
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonString = request.downloadHandler.text;
                    var versionInfo = ClassLoaderSaver.LoadFromJsonString<GithubVersionInfo>(jsonString);
                    m_LatestText.text = versionInfo.VersionNumber;
                    m_LatestDescription.text = versionInfo.Description;
                    m_GithubButton.onClick.AddListener(() => Application.OpenURL(versionInfo.URL));
                }
                else
                {
                    throw new Exception($"Request failed: {request.error}");
                }
            }
            catch (Exception e)
            {
                LogVersionFetchFailure(e.ToString());
                m_LatestText.text = "Unknown";
                m_LatestDescription.text = "Unknown";
                m_GithubButton.onClick.RemoveAllListeners();
                m_GithubButton.onClick.AddListener(() => Application.OpenURL("https://github.com/hbp-HiBoP/HiBoP"));
            }
        }

        private static void LogVersionFetchFailure(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"Latest version fetch failed: {message}");
#endif
        }
    }
}
