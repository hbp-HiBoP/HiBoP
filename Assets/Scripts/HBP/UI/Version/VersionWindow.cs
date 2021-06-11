using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI
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
            using (WebClient wc = new WebClient())
            {
                try
                {
                    wc.Headers.Add("User-Agent: Other");
                    string jsonString = wc.DownloadString("https://api.github.com/repos/hbp-HiBoP/HiBoP/releases/latest");
                    var versionInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<VersionInfo>(jsonString);
                    m_LatestText.text = versionInfo.VersionNumber;
                    m_LatestDescription.text = versionInfo.Description;
                    m_GithubButton.onClick.AddListener(() => Application.OpenURL(versionInfo.URL));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    m_LatestText.text = "Unknown";
                    m_LatestDescription.text = "Unknown";
                    m_GithubButton.onClick.RemoveAllListeners();
                    m_GithubButton.onClick.AddListener(() => Application.OpenURL("https://github.com/hbp-HiBoP/HiBoP"));
                }
            }
        }
    }
}