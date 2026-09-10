using HBP.Core.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Data.Module3D
{
    /// <summary>
    /// Class containing FMRI data for a column
    /// </summary>
    public class Column3DMEG : Column3D
    {
        #region Properties

        public override Core.Data.BasicTimeline NavigationTimeline => Timeline;

        /// <summary>
        /// FMRI data of this column (contains information about what to display)
        /// </summary>
        public MEGColumn ColumnMEGData
        {
            get { return ColumnData as MEGColumn; }
        }

        /// <summary>
        /// Parameters on how to display the activity on the column
        /// </summary>
        public MEGDataParameters MEGParameters { get; } = new MEGDataParameters();

        private int m_SelectedMEGIndex = 0;

        /// <summary>
        /// Currently selected FMRI
        /// </summary>
        public int SelectedMEGIndex
        {
            get { return m_SelectedMEGIndex; }
            set
            {
                m_SelectedMEGIndex = value % ColumnMEGData.Data.MEGItems.Count;
                Timeline.Update(SelectedFMRI);
                OnChangeSelectedMEG.Invoke();
            }
        }

        public Core.Data.Processed.MEGItem SelectedMEGItem
        {
            get { return ColumnMEGData.Data.MEGItems[SelectedMEGIndex]; }
        }

        public Core.Object3D.FMRI SelectedFMRI
        {
            get { return SelectedMEGItem.FMRI; }
        }

        public int SelectedVolumeIndex
        {
            get
            {
                int index = 0;
                for (int i = 0; i < SelectedMEGIndex; i++)
                {
                    index += ColumnMEGData.Data.MEGItems[i].FMRI.Volumes.Count;
                }

                return index + Timeline.CurrentIndex;
            }
        }

        public FMRITimeline Timeline { get; private set; } = new FMRITimeline();

        #endregion

        #region Events

        [HideInInspector] public UnityEvent OnChangeSelectedMEG = new();

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
            var generator = (Core.DLL.MEGGenerator)ActivityGenerator;
            var volumesAndMasks = new List<(Core.DLL.Volume, Core.DLL.Volume)>();
            foreach (var item in ColumnMEGData.Data.MEGItems)
            foreach (var volume in item.FMRI.Volumes)
                volumesAndMasks.Add((volume, item.FMRI.MaskVolume));
            float negativeMin = MEGParameters.FMRINegativeCalMinFactor;
            float negativeMax = MEGParameters.FMRINegativeCalMaxFactor;
            float positiveMin = MEGParameters.FMRIPositiveCalMinFactor;
            float positiveMax = MEGParameters.FMRIPositiveCalMaxFactor;
            bool hideLower = MEGParameters.HideLowerValues;
            bool hideMiddle = MEGParameters.HideMiddleValues;
            bool hideHigher = MEGParameters.HideHigherValues;
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

            ActivityGenerator = new Core.DLL.MEGGenerator();
            SelectedMEGIndex = 0;
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
            MEGParameters.SetSpanValues(ColumnMEGData.MEGConfiguration.NegativeMin, ColumnMEGData.MEGConfiguration.NegativeMax, ColumnMEGData.MEGConfiguration.PositiveMin, ColumnMEGData.MEGConfiguration.PositiveMax);
            MEGParameters.SetHideValues(ColumnMEGData.MEGConfiguration.HideLowerValues, ColumnMEGData.MEGConfiguration.HideMiddleValues, ColumnMEGData.MEGConfiguration.HideHigherValues);
            base.LoadConfiguration(false);
        }

        /// <summary>
        /// Save the configuration of this column to the data column
        /// </summary>
        public override void SaveConfiguration() => CaptureConfiguration(ColumnData);

        public override void CaptureConfiguration(Core.Data.Column target)
        {
            ((MEGColumn)target).MEGConfiguration.NegativeMin = MEGParameters.FMRINegativeCalMinFactor;
            ((MEGColumn)target).MEGConfiguration.NegativeMax = MEGParameters.FMRINegativeCalMaxFactor;
            ((MEGColumn)target).MEGConfiguration.PositiveMin = MEGParameters.FMRIPositiveCalMinFactor;
            ((MEGColumn)target).MEGConfiguration.PositiveMax = MEGParameters.FMRIPositiveCalMaxFactor;
            ((MEGColumn)target).MEGConfiguration.HideLowerValues = MEGParameters.HideLowerValues;
            ((MEGColumn)target).MEGConfiguration.HideMiddleValues = MEGParameters.HideMiddleValues;
            ((MEGColumn)target).MEGConfiguration.HideHigherValues = MEGParameters.HideHigherValues;
            base.CaptureConfiguration(target);
        }

        /// <summary>
        /// Reset the configuration of this column
        /// </summary>
        public override void ResetConfiguration()
        {
            MEGParameters.ResetSpanValues();
            MEGParameters.ResetHideValues();
            base.ResetConfiguration();
        }

        #endregion
    }
}
