using HBP.Core.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Display.Module3D
{
    /// <summary>
    /// Class containing FMRI data for a column
    /// </summary>
    public class Column3DMEG : Column3D
    {
        #region Properties
        /// <summary>
        /// FMRI data of this column (contains information about what to display)
        /// </summary>
        public MEGColumn ColumnMEGData
        {
            get
            {
                return ColumnData as MEGColumn;
            }
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
            get
            {
                return m_SelectedMEGIndex;
            }
            set
            {
                m_SelectedMEGIndex = value % ColumnMEGData.Data.MEGItems.Count;
                Timeline.Update(SelectedFMRI);
                OnChangeSelectedMEG.Invoke();
            }
        }
        public Core.Data.Processed.MEGItem SelectedMEGItem { get { return ColumnMEGData.Data.MEGItems[SelectedMEGIndex]; } }
        public Core.Object3D.FMRI SelectedFMRI { get { return SelectedMEGItem.FMRI; } }
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
        [HideInInspector] public UnityEvent OnChangeSelectedMEG = new UnityEvent();
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
        #endregion

        #region Public Methods
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
            Timeline.OnUpdateCurrentIndex.AddListener(() =>
            {
                OnUpdateCurrentTimelineID.Invoke();
                if (IsSelected)
                {
                    Module3DMain.OnUpdateSelectedColumnTimeLineIndex.Invoke();
                }
            });
            Timeline.OnStopTimelinePlay.AddListener(() =>
            {
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
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
        public override void SaveConfiguration()
        {
            ColumnMEGData.MEGConfiguration.NegativeMin = MEGParameters.FMRINegativeCalMinFactor;
            ColumnMEGData.MEGConfiguration.NegativeMax = MEGParameters.FMRINegativeCalMaxFactor;
            ColumnMEGData.MEGConfiguration.PositiveMin = MEGParameters.FMRIPositiveCalMinFactor;
            ColumnMEGData.MEGConfiguration.PositiveMax = MEGParameters.FMRIPositiveCalMaxFactor;
            ColumnMEGData.MEGConfiguration.HideLowerValues = MEGParameters.HideLowerValues;
            ColumnMEGData.MEGConfiguration.HideMiddleValues = MEGParameters.HideMiddleValues;
            ColumnMEGData.MEGConfiguration.HideHigherValues = MEGParameters.HideHigherValues;
            base.SaveConfiguration();
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
