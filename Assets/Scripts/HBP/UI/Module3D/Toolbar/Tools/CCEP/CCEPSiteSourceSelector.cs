using HBP.Module3D;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class CCEPSiteSourceSelector : Tool
    {
        #region Properties
        /// <summary>
        /// Text to display the name of the selected source
        /// </summary>
        [SerializeField] private Text m_Text;
        /// <summary>
        /// Set the currently selected site as source
        /// </summary>
        [SerializeField] private Button m_SelectSource;
        /// <summary>
        /// Unselect the current source
        /// </summary>
        [SerializeField] private Button m_UnselectSource;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_SelectSource.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                ((Column3DCCEP)SelectedColumn).SelectedSourceSite = SelectedColumn.SelectedSite;
            });
            m_UnselectSource.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                ((Column3DCCEP)SelectedColumn).SelectedSourceSite = null;
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Text.text = "No source selected";
            m_SelectSource.interactable = false;
            m_UnselectSource.interactable = false;
            gameObject.SetActive(true);
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            if (SelectedColumn is Column3DCCEP ccepColumn && ccepColumn.Mode == Column3DCCEP.CCEPMode.Site)
            {
                bool isSourceSelected = ccepColumn.IsSourceSiteSelected;
                bool isSelectedSiteASource = ccepColumn.Sources.Contains(ccepColumn.SelectedSite);

                m_SelectSource.interactable = isSelectedSiteASource;
                m_UnselectSource.interactable = isSourceSelected;
            }
            else
            {
                m_SelectSource.interactable = false;
                m_UnselectSource.interactable = false;
            }
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            if (SelectedColumn is Column3DCCEP ccepColumn)
            {
                if (ccepColumn.IsSourceSiteSelected)
                {
                    m_Text.text = string.Format("{0} ({1})", ccepColumn.SelectedSourceSite.Information.Name, ccepColumn.SelectedSourceSite.Information.Patient.Name);
                }
                else
                {
                    m_Text.text = "No source selected";
                }
            }
            else
            {
                m_Text.text = "Selected column is not CCEP";
            }
        }
        #endregion
    }
}