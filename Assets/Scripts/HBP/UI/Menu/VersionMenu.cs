using NewTheme.Components;
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;
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
            using (WebClient wc = new WebClient())
            {
                try
                {
                    wc.Headers.Add("User-Agent: Other");
                    string jsonString = wc.DownloadString("https://api.github.com/repos/hbp-HiBoP/HiBoP/releases/latest");
                    var versionInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<VersionInfo>(jsonString);
                    m_Image.gameObject.SetActive(string.Compare(versionInfo.VersionNumber, Application.version) > 0);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public void OpenVersionWindow()
        {
            ApplicationState.WindowsManager.Open("Version Window");
        }
    }
}