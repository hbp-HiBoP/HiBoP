using HBP.UI.Tools;
using System.Globalization;
using UnityEngine;

namespace HBP.UI.Main
{
    public class AboutWindow : DialogWindow
    {
        #region Properties
        [SerializeField] TMPro.TMP_Text m_VersionText;
        [SerializeField] TMPro.TMP_Text m_BuildInfoText;
        [SerializeField] TMPro.TMP_Text m_LicenseText;
        [SerializeField] TMPro.TMP_Text m_HBPText;
        [SerializeField] TMPro.TMP_Text m_GithubText;
        #endregion

        #region Private Methods
        protected override void SetFields()
        {
            TextAsset buildInfo = Resources.Load<TextAsset>("BuildInfo");
            if (buildInfo != null)
            {
                Core.Data.BuildInfo info = Newtonsoft.Json.JsonConvert.DeserializeObject<Core.Data.BuildInfo>(buildInfo.text);
                m_VersionText.text = m_VersionText.text.Replace("{VERSION}", info.Version);
                m_BuildInfoText.text = m_BuildInfoText.text.Replace("{DATE}", info.BuildDate.ToString("MMM dd yyyy HH:mm:ss", CultureInfo.InvariantCulture)).Replace("{UNITY_VERSION}", info.UnityVersion);
            }
            else
            {
                Close();
            }
            base.SetFields();
        }
        #endregion
    }
}