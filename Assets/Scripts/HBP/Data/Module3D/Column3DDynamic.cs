using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using HBP.Core.Enums;
using HBP.Core.Data;
using HBP.Core.Preferences;

namespace HBP.Data.Module3D
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

        public abstract Timeline ProjectionTimeline { get; }
        public TemporalSamplingPolicy TemporalSampling => PersistentDataManager.UserPreferences.Data.EEG.TemporalSampling;
        public TemporalSample CurrentProjectionSample => Timeline.GetProjectionSample(ProjectionTimeline, Timeline.CurrentIndex, TemporalSampling);

        public SubTimeline CurrentProjectionSubtimeline
        {
            get
            {
                SubTimeline current = Timeline.CurrentSubtimeline;
                foreach (KeyValuePair<SubBloc, SubTimeline> pair in Timeline.SubTimelinesBySubBloc)
                {
                    if (ReferenceEquals(pair.Value, current) && ProjectionTimeline.SubTimelinesBySubBloc.TryGetValue(pair.Key, out SubTimeline projection))
                        return projection;
                }

                return ProjectionTimeline.CurrentSubtimeline;
            }
        }

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
        public RunningStatistics ActivityStatistics { get; protected set; }

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
        protected List<Vector3> m_ElectrodesSizeScale = new();

        /// <summary>
        /// Does the site have a positive activity value ?
        /// </summary>
        protected List<bool> m_ElectrodesPositiveColor = new();

        #endregion

        #region Events

        /// <summary>
        /// Event called when updating the current timeline ID
        /// </summary>
        [HideInInspector] public UnityEvent OnUpdateCurrentTimelineID = new();

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

            for (int ii = 0; ii < Sites.Count; ++ii)
            {
                if ((Sites[ii].State.IsOutOfROI && !showAllSites) || Sites[ii].State.IsMasked)
                    continue;

                var appearance = EvaluateSiteActivity(ii);
                m_ElectrodesPositiveColor[ii] = appearance.Type == SiteType.Positive;
                m_ElectrodesSizeScale[ii] = Vector3.one * appearance.Scale;
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        #endregion

        #region Public Methods

        public override (System.Action Compute, System.Action Publish) PrepareActivityComputation(bool roiActive, SiteInfluenceByDistanceType influenceRule, bool supportsMarsAtlas)
        {
            var updateMasks = PrepareSitesMaskUpdate(roiActive);
            var generator = (Core.DLL.IEEGGenerator)ActivityGenerator;
            var values = ActivityValues;
            int length = ProjectionTimeline.Length;
            var computeSignal = PrepareCalibratedSignalComputation(generator, values, length, influenceRule, supportsMarsAtlas);
            if (computeSignal == null) return (updateMasks, null);
            Core.DLL.IEEGComputeMetrics metrics = default;
            long valueCount = values.LongLength;
            return (() =>
            {
                updateMasks();
                metrics = computeSignal();
            }, () => UpdateProjectionMemoryAccounting(metrics, valueCount));
        }

        protected virtual System.Func<Core.DLL.IEEGComputeMetrics> PrepareCalibratedSignalComputation(Core.DLL.IEEGGenerator generator, float[] values, int length, SiteInfluenceByDistanceType influenceRule, bool supportsMarsAtlas)
        {
            var sites = RawElectrodes;
            int siteCount = sites.NumberOfSites;
            float distance = DynamicParameters.InfluenceDistance;
            float middle = DynamicParameters.Middle;
            float minimum = DynamicParameters.SpanMin;
            float maximum = DynamicParameters.SpanMax;
            return () => generator.ComputeCalibratedActivity(sites, distance, values, length, siteCount, influenceRule, middle, minimum, maximum);
        }

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
            Timeline.OnUpdateCurrentIndex.AddListener(() =>
            {
                OnUpdateCurrentTimelineID.Invoke();
                if (IsSelected)
                {
                    Module3DMain.OnUpdateSelectedColumnTimeLineIndex.Invoke();
                }
            });
            Timeline.OnStopTimelinePlay.AddListener(() => { Module3DMain.OnRequestUpdateInToolbar.Invoke(); });
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
            for (int i = 0; i < Sites.Count; ++i)
            {
                Core.Object3D.Site site = Sites[i];
                var appearance = EvaluateSiteAppearance(i, showAllSites, hideBlacklistedSites, isGeneratorUpToDate);
                if (!appearance.Visible)
                {
                    // Preserve the previous hidden-blacklist transform behavior.
                    if (!site.State.IsMasked && (!site.State.IsOutOfROI || showAllSites) && site.State.IsFiltered && site.State.IsBlackListed)
                        site.transform.localScale = Vector3.one;
                    if (site.IsActive) site.IsActive = false;
                    continue;
                }

                if (!site.IsActive) site.IsActive = true;
                site.GetComponent<MeshRenderer>().sharedMaterial = Module3DMain.SharedMaterials.Site.GetSharedMaterial(site.State.IsHighlighted, appearance.Type, site.State.Color);
                site.transform.localScale = Vector3.one * appearance.Scale * gain;
            }
        }

        private Core.Object3D.SiteAppearance EvaluateSiteActivity(int index)
        {
            float value = CurrentProjectionSample.Evaluate(ActivityValuesBySiteID[index]);
            return Core.Object3D.SiteAppearance.FromActivity(value, DynamicParameters.SpanMin, DynamicParameters.Middle, DynamicParameters.SpanMax);
        }

        /// <summary>Prepared scientific result, independent of Desktop renderers and local selection.
        /// Call synchronously with the other prepared column reads on the Unity thread.</summary>
        public Core.Object3D.SiteAppearance EvaluateSiteAppearance(int index, bool showAllSites, bool hideBlacklistedSites, bool isGeneratorUpToDate)
        {
            var state = Sites[index].State;
            var activity = state.IsMasked || (state.IsOutOfROI && !showAllSites) ? default : EvaluateSiteActivity(index);
            return Core.Object3D.SiteAppearance.Resolve(state.IsMasked || (state.IsOutOfROI && !showAllSites) ? 1 : activity.Scale, activity.Type == SiteType.Positive, state.IsMasked, state.IsOutOfROI, state.IsFiltered, state.IsBlackListed, showAllSites, hideBlacklistedSites, isGeneratorUpToDate);
        }

        /// <summary>
        /// Compute the UVs of the meshes for the brain activity
        /// </summary>
        /// <param name="brainSurface">Surface of the brain</param>
        public override void ComputeSurfaceBrainUVWithActivity()
        {
            TemporalSample sample = CurrentProjectionSample;
            SurfaceGenerator.ComputeActivityUV(sample.Index, ActivityAlpha);
        }

        public int[] GetUnmaskedHistogramBins(float minimum, float maximum, int binCount)
        {
            if (binCount <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(binCount));
            int[] bins = new int[binCount];
            float difference = maximum - minimum;
            if (difference == 0f)
            {
                minimum -= 1f;
                maximum += 1f;
                difference = maximum - minimum;
            }

            for (int site = 0; site < Sites.Count; ++site)
            {
                if (Sites[site].State.IsMasked)
                    continue;
                float[] values = ActivityValuesBySiteID[site];
                for (int index = 0; index < values.Length; ++index)
                {
                    float coefficient = Mathf.Abs((values[index] - minimum) / difference);
                    int bin = Mathf.Clamp((int)(coefficient * (binCount - 1)), 0, binCount - 1);
                    bins[bin]++;
                }
            }

            return bins;
        }

        public void UpdateProjectionMemoryAccounting(Core.DLL.IEEGComputeMetrics metrics) => UpdateProjectionMemoryAccounting(metrics, ActivityValues?.LongLength ?? 0);

        private void UpdateProjectionMemoryAccounting(Core.DLL.IEEGComputeMetrics metrics, long valueCount)
        {
            long managedBytes = valueCount * sizeof(float);
            long nativeBytes = (metrics.storedValueCount + metrics.storedWeightCount) * sizeof(float) + metrics.spatialIndexCacheBytes;
            DataManager.RegisterMemoryUsage(this, MemoryCacheCategory.NativeProjection, managedBytes + nativeBytes, true);
        }

        #endregion
    }
}
