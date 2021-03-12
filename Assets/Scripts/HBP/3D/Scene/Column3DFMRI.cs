using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using HBP.Module3D.DLL;
using HBP.Data.Enums;
using HBP.Data.Visualization;

namespace HBP.Module3D
{
    /// <summary>
    /// Class containing FMRI data for a column
    /// </summary>
    public class Column3DFMRI : Column3D
    {
        #region Properties
        /// <summary>
        /// FMRI data of this column (contains information about what to display)
        /// </summary>
        public FMRIColumn ColumnFMRIData
        {
            get
            {
                return ColumnData as FMRIColumn;
            }
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
            get
            {
                return m_SelectedFMRIIndex;
            }
            set
            {
                m_SelectedFMRIIndex = value % ColumnFMRIData.Data.FMRIs.Count;
                Timeline.Update(SelectedFMRI);
                OnChangeSelectedFMRI.Invoke();
            }
        }
        public FMRI SelectedFMRI { get { return ColumnFMRIData.Data.FMRIs[SelectedFMRIIndex]; } }
        public int SelectedVolumeIndex
        {
            get
            {
                int index = 0;
                for (int i = 0; i < SelectedFMRIIndex; i++)
                {
                    index += ColumnFMRIData.Data.FMRIs[i].NIFTI.NumberOfVolumes;
                }
                return index + Timeline.CurrentIndex;
            }
        }

        public FMRITimeline Timeline { get; private set; } = new FMRITimeline();
        #endregion

        #region Events
        [HideInInspector] public UnityEvent OnChangeSelectedFMRI = new UnityEvent();
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
        public override void Initialize(int idColumn, Column baseColumn, Implantation3D implantation, List<GameObject> sceneSitePatientParent)
        {
            base.Initialize(idColumn, baseColumn, implantation, sceneSitePatientParent);

            ActivityGenerator = new FMRIGenerator();
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
            Timeline.OnUpdateCurrentIndex.AddListener(() =>
            {
                OnUpdateCurrentTimelineID.Invoke();
                if (IsSelected)
                {
                    ApplicationState.Module3D.OnUpdateSelectedColumnTimeLineIndex.Invoke();
                }
            });
        }
        /// <summary>
        /// Load the column configuration from the column data
        /// </summary>
        /// <param name="firstCall">Has this method not been called by another load method ?</param>
        public override void LoadConfiguration(bool firstCall = true)
        {
            if (firstCall) ResetConfiguration();
            FMRIParameters.SetSpanValues(ColumnFMRIData.FMRIConfiguration.NegativeMin, ColumnFMRIData.FMRIConfiguration.NegativeMax, ColumnFMRIData.FMRIConfiguration.PositiveMin, ColumnFMRIData.FMRIConfiguration.PositiveMax);
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
            base.SaveConfiguration();
        }
        /// <summary>
        /// Reset the configuration of this column
        /// </summary>
        public override void ResetConfiguration()
        {
            FMRIParameters.ResetSpanValues();
            base.ResetConfiguration();
        }
        #endregion
    }
}
