using HBP.Core.Data;
using System.Net;
using System;
using UnityEngine;
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
            await UniTask.SwitchToThreadPool();
            try
            {
                using WebClient wc = new();
                wc.Headers.Add("User-Agent: Other");
                string jsonString = wc.DownloadString("https://api.github.com/repos/hbp-HiBoP/HiBoP/releases/latest");
                var versionInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<GithubVersionInfo>(jsonString);
                version = versionInfo.VersionNumber;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            await UniTask.SwitchToMainThread();
            if (string.Compare(version, Application.version) > 0)
            {
                DialogBoxManager.Open(DialogBoxManager.AlertType.Informational, "New version available", "A new version of HiBoP is available. Please update to the latest version.", () =>
                {
                    WindowsManager.Open("Version Window");
                });
            }
        }
        #endregion
    }
}