using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using HBP.Core.Enums;
using HBP.Core.Data;
using System.Linq;

namespace HBP.Data.Module3D
{
    /// <summary>
    /// Base class for the columns containing temporal dynamic data (iEEG, CCEP)
    /// </summary>
    public class Column3DStatic : Column3D
    {
        #region Properties
        /// <summary>
        /// Static data of this column (contains information about what to display)
        /// </summary>
        public StaticColumn ColumnStaticData
        {
            get
            {
                return ColumnData as StaticColumn;
            }
        }

        /// <summary>
        /// Parameters on how to display the activity on the column
        /// </summary>
        public DynamicDataParameters StaticParameters { get; } = new DynamicDataParameters();

        public string[] Labels { get; private set; } = new string[0];
        private int m_SelectedLabelIndex = 0;
        public int SelectedLabelIndex
        {
            get
            {
                return m_SelectedLabelIndex;
            }
            set
            {
                m_SelectedLabelIndex = (value + Labels.Length) % Labels.Length;
                OnUpdateSelectedLabel.Invoke();
            }
        }

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
        public float[][] ActivityValuesByLabelIDBySiteID { get; protected set; } = new float[0][];

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
        public UnityEvent OnUpdateSelectedLabel = new UnityEvent();
        #endregion

        #region Private Methods
        /// <summary>
        /// Set activity data for each site
        /// </summary>
        protected void SetActivityData()
        {
            if (ColumnStaticData == null) return;

            Labels = ColumnStaticData.Data.ValueByChannelIDByLabel.Keys.ToArray();

            int labelsLength = Labels.Length;
            int sitesCount = Sites.Count;
            ActivityValuesByLabelIDBySiteID = new float[sitesCount][];
            for (int i = 0; i < Sites.Count; ++i) ActivityValuesByLabelIDBySiteID[i] = new float[labelsLength];

            for (int i = 0; i < Labels.Length; ++i)
            {
                string label = Labels[i];
                if (ColumnStaticData.Data.ValueByChannelIDByLabel.TryGetValue(label, out Dictionary<string, float> valuesBySiteID))
                {
                    foreach (Core.Object3D.Site site in Sites)
                    {
                        if (valuesBySiteID.TryGetValue(site.Information.FullID, out float value))
                        {
                            ActivityValuesByLabelIDBySiteID[site.Information.Index][i] = value;
                            site.State.IsMasked = false;
                        }
                        else
                        {
                            ActivityValuesByLabelIDBySiteID[site.Information.Index][i] = 0;
                            site.State.IsMasked = true;
                        }
                    }
                }
            }

            StaticParameters.MinimumAmplitude = float.MaxValue;
            StaticParameters.MaximumAmplitude = float.MinValue;

            int length = labelsLength * sitesCount;
            ActivityValues = new float[length];
            List<float> iEEGNotMasked = new List<float>();
            for (int s = 0; s < sitesCount; ++s)
            {
                for (int t = 0; t < labelsLength; ++t)
                {
                    float val = ActivityValuesByLabelIDBySiteID[s][t];
                    ActivityValues[t * sitesCount + s] = val;
                }
                if (!Sites[s].State.IsMasked)
                {
                    for (int t = 0; t < labelsLength; ++t)
                    {
                        float val = ActivityValuesByLabelIDBySiteID[s][t];
                        iEEGNotMasked.Add(val);

                        if (val > StaticParameters.MaximumAmplitude)
                            StaticParameters.MaximumAmplitude = val;
                        else if (val < StaticParameters.MinimumAmplitude)
                            StaticParameters.MinimumAmplitude = val;
                    }
                }
            }

            if (StaticParameters.MinimumAmplitude == float.MaxValue) StaticParameters.MinimumAmplitude = -1;
            if (StaticParameters.MaximumAmplitude == float.MinValue) StaticParameters.MaximumAmplitude = 1;
            ActivityValuesOfUnmaskedSites = iEEGNotMasked.ToArray();
        }
        /// <summary>
        /// Update sites sizes and colors arrays depending on the activity (to be called before the rendering update)
        /// </summary>
        /// <param name="showAllSites">Display sites that are not in a ROI</param>
        protected virtual void UpdateSitesSizeAndColorOfSites(bool showAllSites)
        {
            UnityEngine.Profiling.Profiler.BeginSample("update_sites_size_and_color_arrays");

            float diffMin = StaticParameters.SpanMin - StaticParameters.Middle;
            float diffMax = StaticParameters.SpanMax - StaticParameters.Middle;

            for (int ii = 0; ii < Sites.Count; ++ii)
            {
                if ((Sites[ii].State.IsOutOfROI && !showAllSites) || Sites[ii].State.IsMasked)
                    continue;

                float value = ActivityValuesByLabelIDBySiteID[ii][SelectedLabelIndex];
                if (value < StaticParameters.SpanMin)
                    value = StaticParameters.SpanMin;
                if (value > StaticParameters.SpanMax)
                    value = StaticParameters.SpanMax;

                value -= StaticParameters.Middle;

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
        public override void Initialize(int idColumn, Column baseColumn, Core.Object3D.Implantation3D implantation, List<GameObject> sceneSitePatientParent)
        {
            base.Initialize(idColumn, baseColumn, implantation, sceneSitePatientParent);

            ActivityGenerator = new Core.DLL.IEEGGenerator();
        }
        /// <summary>
        /// Update the sites of this column (when changing the implantation of the scene)
        /// </summary>
        /// <param name="implantation">Selected implantation</param>
        /// <param name="sceneSitePatientParent">List of the patient parent of the sites as instantiated in the scene</param>
        public override void UpdateSites(Core.Object3D.Implantation3D implantation, List<GameObject> sceneSitePatientParent)
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
        public override void ComputeActivityData()
        {
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
                Core.Object3D.Site site = Sites[i];
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
                site.GetComponent<MeshRenderer>().sharedMaterial = Core.Object3D.SharedMaterials.SiteSharedMaterial(site.State.IsHighlighted, siteType, site.State.Color);
                site.transform.localScale *= gain;
            }
        }
        /// <summary>
        /// Compute the UVs of the meshes for the brain activity
        /// </summary>
        /// <param name="brainSurface">Surface of the brain</param>
        public override void ComputeSurfaceBrainUVWithActivity()
        {
            SurfaceGenerator.ComputeActivityUV(SelectedLabelIndex, ActivityAlpha);
        }
        /// <summary>
        /// Load the column configuration from the column data
        /// </summary>
        /// <param name="firstCall">Has this method not been called by another load method ?</param>
        public override void LoadConfiguration(bool firstCall = true)
        {
            if (firstCall) ResetConfiguration();
            StaticParameters.InfluenceDistance = ColumnStaticData.StaticConfiguration.MaximumInfluence;
            StaticParameters.SetSpanValues(ColumnStaticData.StaticConfiguration.SpanMin, ColumnStaticData.StaticConfiguration.Middle, ColumnStaticData.StaticConfiguration.SpanMax);
            base.LoadConfiguration(false);
        }
        /// <summary>
        /// Save the configuration of this column to the data column
        /// </summary>
        public override void SaveConfiguration()
        {
            ColumnStaticData.StaticConfiguration.MaximumInfluence = StaticParameters.InfluenceDistance;
            ColumnStaticData.StaticConfiguration.SpanMin = StaticParameters.SpanMin;
            ColumnStaticData.StaticConfiguration.Middle = StaticParameters.Middle;
            ColumnStaticData.StaticConfiguration.SpanMax = StaticParameters.SpanMax;
            base.SaveConfiguration();
        }
        /// <summary>
        /// Reset the configuration of this column
        /// </summary>
        public override void ResetConfiguration()
        {
            StaticParameters.InfluenceDistance = 15.0f;
            StaticParameters.ResetSpanValues(this);
            base.ResetConfiguration();
        }
        #endregion
    }
}
