using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using HBP.Module3D.DLL;
using HBP.Data.Enums;
using System.Linq;

namespace HBP.Module3D
{
    /// <summary>
    /// Column 3D iEEG class (functional iEEG data)
    /// </summary>
    public class Column3DDynamic : Column3D
    {
        #region Properties
        /// <summary>
        /// Timeline of this column
        /// </summary>
        public virtual Data.Visualization.Timeline Timeline
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Shared minimum influence (for iEEG on the surface)
        /// </summary>
        public float SharedMinInf = 0f;
        /// <summary>
        /// Shared maximum influence (for iEEG of the surface)
        /// </summary>
        public float SharedMaxInf = 0f;

        /// <summary>
        /// IEEG Parameters
        /// </summary>
        public DynamicDataParameters DynamicParameters { get; } = new DynamicDataParameters();

        /// <summary>
        /// Dimensions of the EEG array (used for the DLL)
        /// </summary>
        public int[] EEGDimensions
        {
            get
            {
                return new int[] { Timeline.Length, 1, Sites.Count };
            }
        }
        /// <summary>
        /// Values of the iEEG signal in a 1D array (used for the DLL)
        /// </summary>
        public float[] IEEGValues = new float[0];
        /// <summary>
        /// Values of the iEEG signal of the sites that are not masked (and have correct values)
        /// </summary>
        public float[] IEEGValuesOfUnmaskedSites = new float[0];
        /// <summary>
        /// iEEG values by site global ID
        /// </summary>
        public float[][] IEEGValuesBySiteID;
        /// <summary>
        /// Units of the iEEG values of each site
        /// </summary>
        public string[] IEEGUnitsBySiteID = new string[0];

        /// <summary>
        /// Size of each site
        /// </summary>
        protected List<Vector3> m_ElectrodesSizeScale;
        /// <summary>
        /// Does the site have a positive value ?
        /// </summary>
        protected List<bool> m_ElectrodesPositiveColor;
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
        /// Set EEG Data for each site
        /// </summary>
        protected virtual void SetEEGData()
        {
        }
        /// <summary>
        /// Update sites sizes and colors arrays for iEEG (to be called before the rendering update)
        /// </summary>
        /// <param name="data">Information about the scene</param>
        protected virtual void UpdateSitesSizeAndColorForIEEG(SceneStatesInfo data)
        {
            UnityEngine.Profiling.Profiler.BeginSample("update_sites_size_and_color_arrays_for_IEEG");

            float diffMin = DynamicParameters.SpanMin - DynamicParameters.Middle;
            float diffMax = DynamicParameters.SpanMax - DynamicParameters.Middle;

            for (int ii = 0; ii < Sites.Count; ++ii)
            {
                if ((Sites[ii].State.IsOutOfROI && !data.ShowAllSites) || Sites[ii].State.IsMasked)
                    continue;

                float value = IEEGValuesBySiteID[ii][Timeline.CurrentIndex];
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
        /// <summary>
        /// Update the implantation for this column
        /// </summary>
        /// <param name="sites">List of the sites (DLL)</param>
        /// <param name="sitesPatientParent">List of the gameobjects for the sites corresponding to the patients</param>
        /// <param name="siteList">List of the sites gameobjects</param>
        public override void UpdateSites(PatientElectrodesList sites, List<GameObject> sceneSitePatientParent)
        {
            base.UpdateSites(sites, sceneSitePatientParent);
            
            m_ElectrodesSizeScale = new List<Vector3>(m_RawElectrodes.NumberOfSites);
            m_ElectrodesPositiveColor = new List<bool>(m_RawElectrodes.NumberOfSites);
            
            for (int ii = 0; ii < m_RawElectrodes.NumberOfSites; ii++)
            {
                m_ElectrodesSizeScale.Add(new Vector3(1, 1, 1));
                m_ElectrodesPositiveColor.Add(true);
            }

            SetEEGData();
        }
        /// <summary>
        /// Specify a column data for this column
        /// </summary>
        /// <param name="columnData">Column data to use</param>
        public virtual void ComputeEEGData()
        {
            Timeline.OnUpdateCurrentIndex.AddListener(() =>
            {
                OnUpdateCurrentTimelineID.Invoke();
                if (IsSelected)
                {
                    ApplicationState.Module3D.OnUpdateSelectedColumnTimeLineIndex.Invoke();
                }
            });
            SetEEGData();
        }
        /// <summary>
        /// Update the shape and color of the sites of this column
        /// </summary>
        /// <param name="data">Information about the scene</param>
        /// <param name="latenciesFile">CCEP files</param>
        public override void UpdateSitesRendering(SceneStatesInfo data, Latencies latenciesFile)
        {
            UpdateSitesSizeAndColorForIEEG(data);

            if (data.DisplayCCEPMode) // CCEP
            {
                for (int i = 0; i < Sites.Count; ++i)
                {
                    Site site = Sites[i];
                    bool activity = site.IsActive;
                    SiteType siteType;
                    float alpha = -1.0f;
                    if (!site.State.IsFiltered)
                    {
                        if (activity) site.IsActive = false;
                        continue;
                    }
                    else if (site.State.IsBlackListed)
                    {
                        site.transform.localScale = Vector3.one;
                        siteType = SiteType.BlackListed;
                        if (data.HideBlacklistedSites)
                        {
                            if (activity) site.IsActive = false;
                            continue;
                        }
                    }
                    else if (latenciesFile != null)
                    {
                        if (SelectedSiteID == -1)
                        {
                            site.transform.localScale = Vector3.one;
                            siteType = latenciesFile.IsSiteASource(i) ? SiteType.Source : SiteType.NotASource;
                        }
                        else
                        {
                            if (i == SelectedSiteID)
                            {
                                site.transform.localScale = Vector3.one;
                                siteType = SiteType.Source;
                            }
                            else if (latenciesFile.IsSiteResponsiveForSource(i, SelectedSiteID))
                            {
                                siteType = latenciesFile.PositiveHeight[SelectedSiteID][i] ? SiteType.Positive : SiteType.Negative;
                                alpha = site.State.IsHighlighted ? 1.0f : latenciesFile.Transparencies[SelectedSiteID][i];
                                site.transform.localScale = Vector3.one * latenciesFile.Sizes[SelectedSiteID][i];
                            }
                            else
                            {
                                if (activity) site.IsActive = false;
                                continue;
                            }
                        }
                    }
                    else
                    {
                        site.transform.localScale = Vector3.one;
                        siteType = SiteType.Normal;
                    }
                    if (!activity) site.IsActive = true;
                    Material siteMaterial = SharedMaterials.SiteSharedMaterial(site.State.IsHighlighted, siteType, site.State.Color);
                    if (alpha > 0.0f)
                    {
                        Color materialColor = siteMaterial.color;
                        materialColor.a = alpha;
                        siteMaterial.color = materialColor;
                    }
                    site.GetComponent<MeshRenderer>().sharedMaterial = siteMaterial;
                    site.transform.localScale *= DynamicParameters.Gain;
                }
            }
            else // iEEG
            {
                for (int i = 0; i < Sites.Count; ++i)
                {
                    Site site = Sites[i];
                    bool activity = site.IsActive;
                    SiteType siteType;
                    if (site.State.IsMasked || (site.State.IsOutOfROI && !data.ShowAllSites) || !site.State.IsFiltered)
                    {
                        if (activity) site.IsActive = false;
                        continue;
                    }
                    else if (site.State.IsBlackListed)
                    {
                        site.transform.localScale = Vector3.one;
                        siteType = SiteType.BlackListed;
                        if (data.HideBlacklistedSites)
                        {
                            if (activity) site.IsActive = false;
                            continue;
                        }
                    }
                    else if (data.IsGeneratorUpToDate)
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
                    site.transform.localScale *= DynamicParameters.Gain;
                }
            }
        }
        #endregion
    }
}
