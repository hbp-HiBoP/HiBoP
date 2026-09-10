using HBP.Core.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Data.Module3D
{
    /// <summary>
    /// Class containing FMRI data for a column
    /// </summary>
    public class Column3DFMRI : Column3D
    {
        #region Properties

        public override Core.Data.BasicTimeline NavigationTimeline => Timeline;

        /// <summary>
        /// FMRI data of this column (contains information about what to display)
        /// </summary>
        public FMRIColumn ColumnFMRIData
        {
            get { return ColumnData as FMRIColumn; }
        }

        /// <summary>
        /// Parameters on how to display the activity on the column
        /// </summary>
        public FMRIDataParameters FMRIParameters { get; } = new FMRIDataParameters();

        private int m_SelectedFMRIIndex = 0;

        /// <summary>
        /// Currently selected FMRI
        /// </summary>
        public int SelectedFMRIIndex
        {
            get { return m_SelectedFMRIIndex; }
            set
            {
                m_SelectedFMRIIndex = value % ColumnFMRIData.Data.FMRIs.Count;
                Timeline.Update(SelectedFMRI);
                OnChangeSelectedFMRI.Invoke();
            }
        }

        public Core.Object3D.FMRI SelectedFMRI
        {
            get { return ColumnFMRIData.Data.FMRIs[SelectedFMRIIndex].Item1; }
        }

        public int SelectedVolumeIndex
        {
            get
            {
                int index = 0;
                for (int i = 0; i < SelectedFMRIIndex; i++)
                {
                    index += ColumnFMRIData.Data.FMRIs[i].Item1.Volumes.Count;
                }

                return index + Timeline.CurrentIndex;
            }
        }

        public FMRITimeline Timeline { get; private set; } = new FMRITimeline();

        #endregion

        #region Events

        [HideInInspector] public UnityEvent OnChangeSelectedFMRI = new();

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

        #endregion

        #region Public Methods

        public override (System.Action Compute, System.Action Publish) PrepareActivityComputation(bool roiActive, Core.Enums.SiteInfluenceByDistanceType influenceRule, bool supportsMarsAtlas)
        {
            var updateMasks = PrepareSitesMaskUpdate(roiActive);
            var generator = (Core.DLL.FMRIGenerator)ActivityGenerator;
            var volumesAndMasks = new List<(Core.DLL.Volume, Core.DLL.Volume)>();
            foreach (var item in ColumnFMRIData.Data.FMRIs)
            foreach (var volume in item.Item1.Volumes)
                volumesAndMasks.Add((volume, item.Item1.MaskVolume));
            float negativeMin = FMRIParameters.FMRINegativeCalMinFactor;
            float negativeMax = FMRIParameters.FMRINegativeCalMaxFactor;
            float positiveMin = FMRIParameters.FMRIPositiveCalMinFactor;
            float positiveMax = FMRIParameters.FMRIPositiveCalMaxFactor;
            bool hideLower = FMRIParameters.HideLowerValues;
            bool hideMiddle = FMRIParameters.HideMiddleValues;
            bool hideHigher = FMRIParameters.HideHigherValues;
            return (() =>
            {
                updateMasks();
                generator.ComputeActivity(volumesAndMasks);
                generator.AdjustValues(negativeMin, negativeMax, positiveMin, positiveMax);
                generator.HideExtremeValues(hideLower, hideMiddle, hideHigher);
            }, null);
        }

        public override void Initialize(int idColumn, Column baseColumn, Core.Object3D.Implantation3D implantation, List<GameObject> sceneSitePatientParent)
        {
            base.Initialize(idColumn, baseColumn, implantation, sceneSitePatientParent);

            ActivityGenerator = new Core.DLL.FMRIGenerator();
            SelectedFMRIIndex = 0;
        }

        /// <summary>
        /// Compute the UVs of the meshes for the brain activity
        /// </summary>
        /// <param name="brainSurface">Surface of the brain</param>
        public override void ComputeSurfaceBrainUVWithActivity()
        {
            SurfaceGenerator.ComputeActivityUV(SelectedVolumeIndex, ActivityAlpha);
        }

        /// <summary>
        /// Method called when initializing the activity on the column
        /// </summary>
        public override void ComputeActivityData()
        {
            ObserveTimeline(Timeline, OnUpdateCurrentTimelineID.Invoke);
        }

        /// <summary>
        /// Load the column configuration from the column data
        /// </summary>
        /// <param name="firstCall">Has this method not been called by another load method ?</param>
        public override void LoadConfiguration(bool firstCall = true)
        {
            if (firstCall) ResetConfiguration();
            FMRIParameters.SetSpanValues(ColumnFMRIData.FMRIConfiguration.NegativeMin, ColumnFMRIData.FMRIConfiguration.NegativeMax, ColumnFMRIData.FMRIConfiguration.PositiveMin, ColumnFMRIData.FMRIConfiguration.PositiveMax);
            FMRIParameters.SetHideValues(ColumnFMRIData.FMRIConfiguration.HideLowerValues, ColumnFMRIData.FMRIConfiguration.HideMiddleValues, ColumnFMRIData.FMRIConfiguration.HideHigherValues);
            base.LoadConfiguration(false);
        }

        /// <summary>
        /// Save the configuration of this column to the data column
        /// </summary>
        public override void SaveConfiguration()
        {
            ColumnFMRIData.FMRIConfiguration.NegativeMin = FMRIParameters.FMRINegativeCalMinFactor;
            ColumnFMRIData.FMRIConfiguration.NegativeMax = FMRIParameters.FMRINegativeCalMaxFactor;
            ColumnFMRIData.FMRIConfiguration.PositiveMin = FMRIParameters.FMRIPositiveCalMinFactor;
            ColumnFMRIData.FMRIConfiguration.PositiveMax = FMRIParameters.FMRIPositiveCalMaxFactor;
            ColumnFMRIData.FMRIConfiguration.HideLowerValues = FMRIParameters.HideLowerValues;
            ColumnFMRIData.FMRIConfiguration.HideMiddleValues = FMRIParameters.HideMiddleValues;
            ColumnFMRIData.FMRIConfiguration.HideHigherValues = FMRIParameters.HideHigherValues;
            base.SaveConfiguration();
        }

        /// <summary>
        /// Reset the configuration of this column
        /// </summary>
        public override void ResetConfiguration()
        {
            FMRIParameters.ResetSpanValues();
            FMRIParameters.ResetHideValues();
            base.ResetConfiguration();
        }

        #endregion
    }
}
