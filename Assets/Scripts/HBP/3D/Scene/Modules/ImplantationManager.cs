using System.Collections.Generic;
using UnityEngine;
using HBP.Core.Data.Enums;

namespace HBP.Module3D
{
    /// <summary>
    /// Class responsible for managing the implantations of the scene
    /// </summary>
    /// <remarks>
    /// This class can load and store implantations for the corresponding scene.
    /// It is also used to select which implantation to display on the scene and to compare sites.
    /// </remarks>
    public class ImplantationManager : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Parent scene of the manager
        /// </summary>
        [SerializeField] private Base3DScene m_Scene;
        /// <summary>
        /// Component containing references to GameObjects of the 3D scene
        /// </summary>
        [SerializeField] private DisplayedObjects m_DisplayedObjects;

        /// <summary>
        /// List of the implantation3Ds of the scene
        /// </summary>
        public List<Core.Object3D.Implantation3D> Implantations { get; } = new List<Core.Object3D.Implantation3D>();
        /// <summary>
        /// Selected implantation3D ID
        /// </summary>
        public int SelectedImplantationID { get; set; }
        /// <summary>
        /// Selected implantation3D
        /// </summary>
        public Core.Object3D.Implantation3D SelectedImplantation
        {
            get
            {
                return Implantations.Count > 0 ? Implantations[SelectedImplantationID] : null;
            }
        }

        /// <summary>
        /// Site to compare with when using the comparing site feature
        /// </summary>
        public Core.Object3D.Site SiteToCompare { get; private set; }
        private bool m_ComparingSites;
        /// <summary>
        /// Are we comparing sites ?
        /// </summary>
        public bool ComparingSites
        {
            get
            {
                return m_ComparingSites;
            }
            set
            {
                m_ComparingSites = value;
                if (m_ComparingSites)
                {
                    SiteToCompare = m_Scene.SelectedColumn.SelectedSite;
                }
                else
                {
                    SiteToCompare = null;
                }
            }
        }
        #endregion

        #region Private Methods
        private void OnDestroy()
        {
            foreach (var implantation in Implantations)
            {
                implantation.Clean();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Add a new implantation to the manager
        /// </summary>
        /// <param name="name">Name of the implantation</param>
        /// <param name="pts">List of pts files (one by patient)</param>
        /// <param name="marsAtlas">List of Mars Atlas files (one by patient)</param>
        /// <param name="patientIDs">List of patients IDs</param>
        public void Add(string name, List<Core.Object3D.Implantation3D.SiteInfo> siteInfos, IEnumerable<Core.Data.Patient> patients)
        {
            Core.Object3D.Implantation3D implantation3D = new Core.Object3D.Implantation3D(name, siteInfos, patients);
            if (implantation3D.IsLoaded)
            {
                Implantations.Add(implantation3D);
            }
            else
            {
                throw new CanNotLoadImplantation(name);
            }
        }
        /// <summary>
        /// Set the implantation to be used
        /// </summary>
        /// <param name="implantationName">Name of the implantation to use</param>
        public void Select(string implantationName)
        {
            int implantationID = Implantations.FindIndex(i => i.Name == implantationName);
            SelectedImplantationID = implantationID > 0 ? implantationID : 0;
            m_DisplayedObjects.InstantiateImplantation(SelectedImplantation);
            
            // reset selected site
            for (int ii = 0; ii < m_Scene.Columns.Count; ++ii)
            {
                m_Scene.Columns[ii].UnselectSite();
            }

            m_Scene.ResetGenerators();
        }
        /// <summary>
        /// Display information about the site under the mouse
        /// </summary>
        /// <param name="canDisplay">Is a site under the mouse ?</param>
        /// <param name="column">Column on which the raycast is performed</param>
        /// <param name="hit">RaycastHit of the raycast</param>
        public void DisplaySiteInformation(bool canDisplay, Column3D column, RaycastHit hit)
        {
            if (canDisplay)
            {
                Core.Object3D.Site site = hit.collider.GetComponent<Core.Object3D.Site>();
                // Compute each required variable
                int siteID = site.Information.Index;
                string CCEPLatency = "none", CCEPAmplitude = "none";
                float iEEGActivity = -1;
                string iEEGUnit = "";
                // CCEP
                if (column is Column3DCCEP ccepColumn)
                {
                    CCEPLatency = ccepColumn.Latencies[siteID].ToString();
                    CCEPAmplitude = ccepColumn.Amplitudes[siteID].ToString();
                }
                // iEEG
                if (column is Column3DDynamic columnIEEG)
                {
                    iEEGUnit = columnIEEG.ActivityUnitsBySiteID[siteID];
                    iEEGActivity = columnIEEG.ActivityValuesBySiteID[siteID][columnIEEG.Timeline.CurrentIndex];
                }
                // Send Event
                SiteInformationDisplayMode displayMode;
                if (m_Scene.IsGeneratorUpToDate)
                {
                    if (column is Column3DCCEP)
                    {
                        displayMode = SiteInformationDisplayMode.CCEP;
                    }
                    else if (column is Column3DIEEG)
                    {
                        displayMode = SiteInformationDisplayMode.IEEG;
                    }
                    else
                    {
                        displayMode = SiteInformationDisplayMode.Anatomy;
                    }
                }
                else
                {
                    displayMode = SiteInformationDisplayMode.Anatomy;
                }
                ApplicationState.Module3D.OnDisplaySiteInformation.Invoke(new Core.Object3D.SiteInfo(site, true, Input.mousePosition, displayMode, iEEGActivity.ToString("0.00"), iEEGUnit, CCEPAmplitude, CCEPLatency));
            }
            else
            {
                ApplicationState.Module3D.OnDisplaySiteInformation.Invoke(new Core.Object3D.SiteInfo(null, false, Input.mousePosition));
            }
        }
        #endregion
    }
}