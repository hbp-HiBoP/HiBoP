using HBP.Core.Data;
using System.Net;
using System;
using ThirdParty.CielaSpike;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using HBP.UI.Tools;

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
            this.StartCoroutineAsync(c_CheckVersion());
        }
        private IEnumerator c_CheckVersion()
        {
            yield return Ninja.JumpToUnity;
            string version = Application.version;
            yield return Ninja.JumpBack;
            using (WebClient wc = new WebClient())
            {
                try
                {
                    wc.Headers.Add("User-Agent: Other");
                    string jsonString = wc.DownloadString("https://api.github.com/repos/hbp-HiBoP/HiBoP/releases/latest");
                    var versionInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<VersionInfo>(jsonString);
                    version = versionInfo.VersionNumber;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            yield return Ninja.JumpToUnity;
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