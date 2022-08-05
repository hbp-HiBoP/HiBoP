using ThirdParty.CielaSpike;
using System;
using System.Collections;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI
{
    public class VersionMenu : Menu
    {
        [SerializeField] Text m_Text;
        [SerializeField] Image m_Image;

        private void Awake()
        {
            m_Text.text = string.Format("{0} {1}", Application.productName, Application.version);
            this.StartCoroutineAsync(c_CheckVersion());
        }

        public void OpenVersionWindow()
        {
            WindowsManager.Open("Version Window");
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
            m_Image.gameObject.SetActive(string.Compare(version, Application.version) > 0);
        }
    }
}