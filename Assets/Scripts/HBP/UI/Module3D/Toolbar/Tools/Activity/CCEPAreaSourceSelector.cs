using HBP.Module3D;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using HBP.Core.Data;
using HBP.Core.Object3D;

namespace HBP.UI.Module3D.Tools
{
    public class CCEPAreaSourceSelector : Tool
    {
        #region Structs
        /// <summary>
        /// Struct to contain information about a MarsAtlas area
        /// </summary>
        private struct MarsAtlasArea
        {
            /// <summary>
            /// Label of the area
            /// </summary>
            public int Label { get; set; }
            /// <summary>
            /// Name of the area
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// Full name of the area
            /// </summary>
            public string FullName { get; set; }
        }
        #endregion

        #region Properties
        /// <summary>
        /// List of all MarsAtlas areas
        /// </summary>
        private List<MarsAtlasArea> m_MarsAtlasAreas = new List<MarsAtlasArea>();
        /// <summary>
        /// Dropdown to select the MarsAtlas area to consider as source
        /// </summary>
        [SerializeField] private Dropdown m_MarsAtlasDropdown;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_MarsAtlasDropdown.onValueChanged.AddListener(index =>
            {
                if (ListenerLock) return;

                ((Column3DCCEP)SelectedColumn).SelectedSourceMarsAtlasLabel = m_MarsAtlasAreas[index].Label;
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_MarsAtlasDropdown.value = 0;
            m_MarsAtlasDropdown.interactable = false;
            gameObject.SetActive(false);
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isColumnCCEPAndMarsAtlasModeEnabled = SelectedColumn is Column3DCCEP ccepColumn && ccepColumn.Mode == Column3DCCEP.CCEPMode.MarsAtlas;

            m_MarsAtlasDropdown.interactable = isColumnCCEPAndMarsAtlasModeEnabled;
            gameObject.SetActive(isColumnCCEPAndMarsAtlasModeEnabled);
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            m_MarsAtlasAreas.Clear();
            m_MarsAtlasDropdown.options.Clear();
            if (SelectedColumn is Column3DCCEP ccepColumn && ccepColumn.Mode == Column3DCCEP.CCEPMode.MarsAtlas)
            {
                int[] marsAtlasLabels = Object3DManager.MarsAtlas.Labels();
                Core.Data.StringTag marsAtlasTag = ApplicationState.ProjectLoaded.Preferences.Tags.FirstOrDefault(t => t.Name == "MarsAtlas") as Core.Data.StringTag;
                m_MarsAtlasAreas.Add(new MarsAtlasArea { Label = -1, Name = "None", FullName = "None" });
                foreach (var label in marsAtlasLabels)
                {
                    string labelName = string.Format("{0}_{1}", Object3DManager.MarsAtlas.Hemisphere(label), Object3DManager.MarsAtlas.Name(label));
                    if (ccepColumn.Sources.Any(s => (s.Information.SiteData.Tags.FirstOrDefault(t => t.Tag == marsAtlasTag) as Core.Data.StringTagValue)?.Value == labelName))
                    {
                        m_MarsAtlasAreas.Add(new MarsAtlasArea { Label = label, Name = labelName, FullName = Object3DManager.MarsAtlas.FullName(label) });
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