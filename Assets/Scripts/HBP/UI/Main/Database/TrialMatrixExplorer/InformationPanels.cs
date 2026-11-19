using HBP.Core.Data;
using HBP.Data.Informations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using State = HBP.Theme.State;
using ThemeElement = HBP.Theme.ThemeElement;

namespace HBP.UI.Database
{
    public class InformationPanels : MonoBehaviour
    {
        #region Properties

        [SerializeField] private Text m_PatientInformationText;
        [SerializeField] private TagDisplaySettingsContextMenu m_PatientTagDisplaySettingsContextMenu;
        [SerializeField] private Text m_SiteInformationText;
        [SerializeField] private TagDisplaySettingsContextMenu m_SiteTagDisplaySettingsContextMenu;
        [SerializeField] private GameObject m_TagRowTemplate;
        [SerializeField] private State m_TagHeaderState, m_AlternateTagRowState;

        private ChannelStruct m_ChannelStruct;
        private readonly List<(ThemeElement Background, Text Tag, Text Value)> m_PatientRows = new();
        private readonly List<(ThemeElement Background, Text Tag, Text Value)> m_SiteRows = new();

        #endregion

        #region Public Methods

        public void Set(ChannelStruct channelStruct)
        {
            m_ChannelStruct = channelStruct;
            Refresh();
        }

        public void Refresh()
        {
            if (!isActiveAndEnabled) return;

            var patient = m_ChannelStruct?.Patient;
            BindTags(patient?.Tags, m_PatientTagDisplaySettingsContextMenu, m_PatientInformationText, "No patient information available.", m_PatientRows);
            Site site = patient?.Sites.FirstOrDefault(s => s.Name == m_ChannelStruct.Channel);
            BindTags(site?.Tags, m_SiteTagDisplaySettingsContextMenu, m_SiteInformationText, "No site information available.", m_SiteRows);
        }

        #endregion

        #region Private Methods

        // The explorer can select a channel before showing this panel. Bind once the
        // rows can initialize their theme components before applying their styles.
        private void OnEnable() => Refresh();

        private void BindTags(IEnumerable<BaseTagValue> tags, TagDisplaySettingsContextMenu menu, Text empty, string emptyMessage, List<(ThemeElement Background, Text Tag, Text Value)> rows)
        {
            var visible = tags?.Where(value => menu.IsDisplayed(value.Tag)).ToList();
            int count = visible?.Count ?? 0;
            empty.text = emptyMessage;
            empty.gameObject.SetActive(count == 0);

            int rowCount = count == 0 ? 0 : count + 1;
            while (rows.Count < rowCount)
            {
                var row = Instantiate(m_TagRowTemplate, empty.transform.parent, false);
                var cells = row.GetComponentsInChildren<Text>(true);
                rows.Add((row.GetComponent<ThemeElement>(), cells[0], cells[1]));
            }

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                row.Background.gameObject.SetActive(i < rowCount);
                if (i >= rowCount) continue;
                row.Background.Set(i == 0 ? m_TagHeaderState : i % 2 == 1 ? m_AlternateTagRowState : row.Background.Element.DefaultState);
                row.Tag.fontStyle = FontStyle.Bold;
                if (i == 0)
                {
                    row.Tag.text = "Tag";
                    row.Value.text = "Value";
                    row.Value.fontStyle = FontStyle.Bold;
                }
                else
                {
                    row.Tag.text = visible[i - 1].Tag.Name;
                    row.Value.text = visible[i - 1].DisplayableValue;
                }
            }
        }

        #endregion
    }
}
