using UnityEngine;

namespace HBP.UI.Module3D
{
    public class SiteToolbar : Toolbar
    {
        #region Properties
        /// <summary>
        /// Display the name of the selected site
        /// </summary>
        [SerializeField] private Tools.SelectedSite m_SelectedSite;
        /// <summary>
        /// Compare two sites
        /// </summary>
        [SerializeField] private Tools.CompareSite m_CompareSite;
        /// <summary>
        /// Show all sites
        /// </summary>
        [SerializeField] private Tools.ShowAllSites m_ShowAllSites;
        /// <summary>
        /// Load a single patient scene from a multi patient scene
        /// </summary>
        [SerializeField] private Tools.LoadPatient m_LoadPatient;
        /// <summary>
        /// Show or hide blacklisted sites
        /// </summary>
        [SerializeField] private Tools.BlacklistedSitesDisplay m_BlacklistedSitesDisplay;
        /// <summary>
        /// Copy the state of the sites to other columns
        /// </summary>
        [SerializeField] private Tools.SiteStateCopy m_SiteStateCopy;
        /// <summary>
        /// Copy the state of the sites to other columns
        /// </summary>
        [SerializeField] private Tools.SiteStateExport m_SiteStateExport;
        /// <summary>
        /// Cut the mesh around the selected site
        /// </summary>
        [SerializeField] private Tools.CutAroundSite m_CutAroundSite;
        /// <summary>
        /// Open the Interactive Viewer from HBP
        /// </summary>
        [SerializeField] private Tools.OpenInteractiveViewer m_OpenInteractiveViewer;
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
            m_Tools.Add(m_LoadPatient);
            m_Tools.Add(m_BlacklistedSitesDisplay);
            m_Tools.Add(m_SiteStateCopy);
            m_Tools.Add(m_SiteStateExport);
            m_Tools.Add(m_CutAroundSite);
            m_Tools.Add(m_OpenInteractiveViewer);
        }
        #endregion
    }
}