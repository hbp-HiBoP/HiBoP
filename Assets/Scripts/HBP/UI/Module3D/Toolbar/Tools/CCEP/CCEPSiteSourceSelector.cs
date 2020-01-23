using HBP.Module3D;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class CCEPSiteSourceSelector : Tool
    {
        #region Properties
        [SerializeField] private Text m_Text;
        [SerializeField] private Button m_SelectSource;
        [SerializeField] private Button m_UnselectSource;
        #endregion

        #region Public Methods
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

        public override void DefaultState()
        {
            m_Text.text = "No source selected";
        }

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