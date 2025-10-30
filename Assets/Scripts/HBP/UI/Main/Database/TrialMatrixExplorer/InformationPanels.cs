using HBP.Core.Data;
using HBP.Data.Informations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Database
{
    public class InformationPanels : MonoBehaviour
    {
        #region Properties
        [SerializeField] private Text m_PatientInformationText;
        [SerializeField] private TagDisplaySettingsContextMenu m_PatientTagDisplaySettingsContextMenu;
        [SerializeField] private Text m_SiteInformationText;
        [SerializeField] private TagDisplaySettingsContextMenu m_SiteTagDisplaySettingsContextMenu;

        private ChannelStruct m_ChannelStruct;
        #endregion

        #region Public Methods
        public void Set(ChannelStruct channelStruct)
        {
            m_ChannelStruct = channelStruct;

            string patientInfo = BuildTagsString(channelStruct.Patient.Tags.Where(t => m_PatientTagDisplaySettingsContextMenu.IsDisplayed(t.Tag)));
            m_PatientInformationText.text = string.IsNullOrEmpty(patientInfo) ? "No patient information available." : patientInfo;

            Site site = channelStruct.Patient.Sites.FirstOrDefault(s => s.Name == channelStruct.Channel);
            if (site != null)
            {
                string siteInfo = BuildTagsString(site.Tags.Where(t => m_SiteTagDisplaySettingsContextMenu.IsDisplayed(t.Tag)));
                m_SiteInformationText.text = string.IsNullOrEmpty(siteInfo) ? "No site information available." : siteInfo;
            }
            else
            {
                m_SiteInformationText.text = "No site information available.";
            }
        }
        public void Refresh()
        {
            Set(m_ChannelStruct);
        }
        #endregion

        #region Private Methods
        private string BuildTagsString(IEnumerable<BaseTagValue> tags)
        {
            StringBuilder sb = new();
            foreach (var tag in tags)
            {
                sb.AppendLine($"<b>• {tag.Tag.Name}:</b>");
                sb.AppendLine($"{tag.DisplayableValue}");
                sb.AppendLine();
            }
            return sb.ToString();
        }
        #endregion
    }
}