using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace HBP.UI.Module3D.Tools
{
    public class CCEPAreaSourceSelector : Tool
    {
        #region Structs
        private struct MarsAtlasArea
        {
            public int Label { get; set; }
            public string Name { get; set; }
            public string FullName { get; set; }
        }
        #endregion

        #region Properties
        private List<MarsAtlasArea> m_MarsAtlasAreas = new List<MarsAtlasArea>();
        [SerializeField] private Dropdown m_MarsAtlasDropdown;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_MarsAtlasDropdown.onValueChanged.AddListener(index =>
            {
                if (ListenerLock) return;

                ((Column3DCCEP)SelectedColumn).SelectedSourceMarsAtlasLabel = m_MarsAtlasAreas[index].Label;
            });
        }

        public override void DefaultState()
        {
            m_MarsAtlasDropdown.value = 0;
        }

        public override void UpdateInteractable()
        {
            bool isColumnCCEPAndMarsAtlasModeEnabled = SelectedColumn is Column3DCCEP ccepColumn && ccepColumn.Mode == Column3DCCEP.CCEPMode.MarsAtlas;

            m_MarsAtlasDropdown.interactable = isColumnCCEPAndMarsAtlasModeEnabled;
        }

        public override void UpdateStatus()
        {
            m_MarsAtlasAreas.Clear();
            m_MarsAtlasDropdown.options.Clear();
            if (SelectedColumn is Column3DCCEP ccepColumn && ccepColumn.Mode == Column3DCCEP.CCEPMode.MarsAtlas)
            {
                int[] marsAtlasLabels = ApplicationState.Module3D.MarsAtlas.Labels();
                Data.StringTag marsAtlasTag = ApplicationState.ProjectLoaded.Preferences.Tags.FirstOrDefault(t => t.Name == "MarsAtlas") as Data.StringTag;
                m_MarsAtlasAreas.Add(new MarsAtlasArea { Label = -1, Name = "None", FullName = "None" });
                foreach (var label in marsAtlasLabels)
                {
                    string labelName = string.Format("{0}_{1}", ApplicationState.Module3D.MarsAtlas.Hemisphere(label), ApplicationState.Module3D.MarsAtlas.Name(label));
                    if (ccepColumn.Sources.Any(s => (s.Information.SiteData.Tags.FirstOrDefault(t => t.Tag == marsAtlasTag) as Data.StringTagValue)?.Value == labelName))
                    {
                        m_MarsAtlasAreas.Add(new MarsAtlasArea { Label = label, Name = labelName, FullName = ApplicationState.Module3D.MarsAtlas.FullName(label) });
                    }
                }
                foreach (var area in m_MarsAtlasAreas)
                {
                    m_MarsAtlasDropdown.options.Add(new Dropdown.OptionData(area.FullName));
                }
                m_MarsAtlasDropdown.value = m_MarsAtlasAreas.FindIndex(a => a.Label == ccepColumn.SelectedSourceMarsAtlasLabel);
                m_MarsAtlasDropdown.RefreshShownValue();
            }
            else
            {
                m_MarsAtlasDropdown.value = 0;
            }
        }
        #endregion
    }
}