using UnityEngine;

namespace HBP.UI.Toolbar
{
    public class SiteToolbar : Toolbar
    {
        #region Properties
        /// <summary>
        /// Display the name of the selected site
        /// </summary>
        [SerializeField] private SelectedSite m_SelectedSite;
        /// <summary>
        /// Compare two sites
        /// </summary>
        [SerializeField] private CompareSite m_CompareSite;
        /// <summary>
        /// Show all sites
        /// </summary>
        [SerializeField] private ShowAllSites m_ShowAllSites;
        /// <summary>
        /// Change the gain of the sites
        /// </summary>
        [SerializeField] private SiteGain m_SiteGain;
        /// <summary>
        /// Load a single patient scene from a multi patient scene
        /// </summary>
        [SerializeField] private LoadPatient m_LoadPatient;
        /// <summary>
        /// Show or hide blacklisted sites
        /// </summary>
        [SerializeField] private BlacklistedSitesDisplay m_BlacklistedSitesDisplay;
        /// <summary>
        /// Copy the state of the sites to other columns
        /// </summary>
        [SerializeField] private SiteStateCopy m_SiteStateCopy;
        /// <summary>
        /// Copy the state of the sites to other columns
        /// </summary>
        [SerializeField] private SiteStateExport m_SiteStateExport;
        /// <summary>
        /// Cut the mesh around the selected site
        /// </summary>
        [SerializeField] private CutAroundSite m_CutAroundSite;
        /// <summary>
        /// Move all sites to one hemisphere
        /// </summary>
        [SerializeField] private MoveSites m_MoveSites;
        /// <summary>
        /// Open the Interactive Viewer from HBP
        /// </summary>
        [SerializeField] private OpenInteractiveViewer m_OpenInteractiveViewer;
        #endregion

        #region Private Methods
        /// <summary>
        /// Link elements to the toolbar
        /// </summary>
        protected override void AddTools()
        {
            m_Tools.Add(m_SelectedSite);
            m_Tools.Add(m_CompareSite);
            m_Tools.Add(m_ShowAllSites);
            m_Tools.Add(m_SiteGain);
            m_Tools.Add(m_LoadPatient);
            m_Tools.Add(m_BlacklistedSitesDisplay);
            m_Tools.Add(m_SiteStateCopy);
            m_Tools.Add(m_SiteStateExport);
            m_Tools.Add(m_CutAroundSite);
            m_Tools.Add(m_OpenInteractiveViewer);
            m_Tools.Add(m_MoveSites);
        }
        #endregion
    }
}