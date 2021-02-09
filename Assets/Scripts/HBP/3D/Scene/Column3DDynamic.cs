using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using HBP.Module3D.DLL;
using HBP.Data.Enums;
using HBP.Data.Visualization;

namespace HBP.Module3D
{
    /// <summary>
    /// Base class for the columns containing temporal dynamic data (iEEG, CCEP)
    /// </summary>
    public abstract class Column3DDynamic : Column3D
    {
        #region Properties
        /// <summary>
        /// Timeline of this column (contains information about the length, the number of samples, the events etc.)
        /// </summary>
        public abstract Timeline Timeline { get; }

        /// <summary>
        /// Parameters on how to display the activity on the column
        /// </summary>
        public DynamicDataParameters DynamicParameters { get; } = new DynamicDataParameters();
        
        /// <summary>
        /// Values of the signal in a 1D array (used for the DLL)
        /// </summary>
        public float[] ActivityValues { get; protected set; } = new float[0];
        /// <summary>
        /// Values of the signal of the sites that are not masked (and have correct values)
        /// </summary>
        public float[] ActivityValuesOfUnmaskedSites { get; protected set; } = new float[0];
        /// <summary>
        /// Signal values by site global ID
        /// </summary>
        public float[][] ActivityValuesBySiteID { get; protected set; } = new float[0][];
        /// <summary>
        /// Units of the signal values of each site
        /// </summary>
        public string[] ActivityUnitsBySiteID { get; protected set; } = new string[0];

        /// <summary>
        /// Size of each site depending on its activity
        /// </summary>
        protected List<Vector3> m_ElectrodesSizeScale = new List<Vector3>();
        /// <summary>
        /// Does the site have a positive activity value ?
        /// </summary>
        protected List<bool> m_ElectrodesPositiveColor = new List<bool>();
        #endregion

        #region Events
        /// <summary>
        /// Event called when updating the current timeline ID
        /// </summary>
        [HideInInspector] public UnityEvent OnUpdateCurrentTimelineID = new UnityEvent();
        #endregion

        #region Private Methods
        protected virtual void Update()
        {
            if (Timeline != null)
            {
                Timeline.Play();
            }
        }
        /// <summary>
        /// Set activity data for each site
        /// </summary>
        protected abstract void SetActivityData();
        /// <summary>
        /// Update sites sizes and colors arrays depending on the activity (to be called before the rendering update)
        /// </summary>
        /// <param name="showAllSites">Display sites that are not in a ROI</param>
        protected virtual void UpdateSitesSizeAndColorOfSites(bool showAllSites)
        {
            UnityEngine.Profiling.Profiler.BeginSample("update_sites_size_and_color_arrays");

            float diffMin = DynamicParameters.SpanMin - DynamicParameters.Middle;
            float diffMax = DynamicParameters.SpanMax - DynamicParameters.Middle;

            for (int ii = 0; ii < Sites.Count; ++ii)
            {
                if ((Sites[ii].State.IsOutOfROI && !showAllSites) || Sites[ii].State.IsMasked)
                    continue;

                float value = ActivityValuesBySiteID[ii][Timeline.CurrentIndex];
                if (value < DynamicParameters.SpanMin)
                    value = DynamicParameters.SpanMin;
                if (value > DynamicParameters.SpanMax)
                    value = DynamicParameters.SpanMax;

                value -= DynamicParameters.Middle;

                if (value < 0)
                {
                    m_ElectrodesPositiveColor[ii] = false;
                    value = 0.5f + 2 * (value / diffMin);
                }
                else if (value > 0)
                {
                    m_ElectrodesPositiveColor[ii] = true;
                    value = 0.5f + 2 * (value / diffMax);
                }
                else
                {
                    m_ElectrodesPositiveColor[ii] = false;
                    value = 0.5f;
                }

                m_ElectrodesSizeScale[ii] = new Vector3(value, value, value);
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }
        #endregion

        #region Public Methods
        public override void Initialize(int idColumn, Column baseColumn, Implantation3D implantation, List<GameObject> sceneSitePatientParent)
        {
            base.Initialize(idColumn, baseColumn, implantation, sceneSitePatientParent);

            ActivityGenerator = new IEEGGenerator();
        }
        /// <summary>
        /// Update the sites of this column (when changing the implantation of the scene)
        /// </summary>
        /// <param name="implantation">Selected implantation</param>
        /// <param name="sceneSitePatientParent">List of the patient parent of the sites as instantiated in the scene</param>
        public override void UpdateSites(Implantation3D implantation, List<GameObject> sceneSitePatientParent)
        {
            base.UpdateSites(implantation, sceneSitePatientParent);
            
            m_ElectrodesSizeScale = new List<Vector3>(RawElectrodes.NumberOfSites);
            m_ElectrodesPositiveColor = new List<bool>(RawElectrodes.NumberOfSites);
            
            for (int ii = 0; ii < RawElectrodes.NumberOfSites; ii++)
            {
                m_ElectrodesSizeScale.Add(new Vector3(1, 1, 1));
                m_ElectrodesPositiveColor.Add(true);
            }

            SetActivityData();
        }
        /// <summary>
        /// Method called when initializing the activity on the column
        /// </summary>
        public virtual void ComputeActivityData()
        {
            Timeline.OnUpdateCurrentIndex.AddListener(() =>
            {
                OnUpdateCurrentTimelineID.Invoke();
                if (IsSelected)
                {
                    ApplicationState.Module3D.OnUpdateSelectedColumnTimeLineIndex.Invoke();
                }
            });
            SetActivityData();
        }
        /// <summary>
        /// Update the visibility, the size and the color of the sites depending on their state
        /// </summary>
        /// <param name="showAllSites">Do we show sites that are not in a ROI ?</param>
        /// <param name="hideBlacklistedSites">Do we hide blacklisted sites ?</param>
        /// <param name="isGeneratorUpToDate">Is the activity generator up to date ?</param>
        public override void UpdateSitesRendering(bool showAllSites, bool hideBlacklistedSites, bool isGeneratorUpToDate, float gain)
        {
            UpdateSitesSizeAndColorOfSites(showAllSites);

            for (int i = 0; i < Sites.Count; ++i)
            {
                Site site = Sites[i];
                bool activity = site.IsActive;
                SiteType siteType;
                if (site.State.IsMasked || (site.State.IsOutOfROI && !showAllSites) || !site.State.IsFiltered)
                {
                    if (activity) site.IsActive = false;
                    continue;
                }
                else if (site.State.IsBlackListed)
                {
                    site.transform.localScale = Vector3.one;
                    siteType = SiteType.BlackListed;
                    if (hideBlacklistedSites)
                    {
                        if (activity) site.IsActive = false;
                        continue;
                    }
                }
                else if (isGeneratorUpToDate)
                {
                    site.transform.localScale = m_ElectrodesSizeScale[i];
                    siteType = m_ElectrodesPositiveColor[i] ? SiteType.Positive : SiteType.Negative;
                }
                else
                {
                    site.transform.localScale = Vector3.one;
                    siteType = SiteType.Normal;
                }
                if (!activity) site.IsActive = true;
                site.GetComponent<MeshRenderer>().sharedMaterial = SharedMaterials.SiteSharedMaterial(site.State.IsHighlighted, siteType, site.State.Color);
                site.transform.localScale *= gain;
            }
        }
        /// <summary>
        /// Compute the UVs of the meshes for the brain activity
        /// </summary>
        /// <param name="brainSurface">Surface of the brain</param>
        public override void ComputeSurfaceBrainUVWithActivity()
        {
            SurfaceGenerator.ComputeActivityUV(Timeline.CurrentIndex, ActivityAlpha);
        }
        #endregion
    }
}
