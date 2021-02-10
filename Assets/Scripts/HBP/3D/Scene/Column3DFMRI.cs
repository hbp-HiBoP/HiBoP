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
                OnChangeSelectedFMRI.Invoke();
            }
        }
        public MRI3D SelectedFMRI { get { return ColumnFMRIData.Data.FMRIs[SelectedFMRIIndex]; } }
        #endregion

        #region Events
        [HideInInspector] public UnityEvent OnChangeSelectedFMRI = new UnityEvent();
        #endregion

        #region Private Methods
        #endregion

        #region Public Methods
        public override void Initialize(int idColumn, Column baseColumn, Implantation3D implantation, List<GameObject> sceneSitePatientParent)
        {
            base.Initialize(idColumn, baseColumn, implantation, sceneSitePatientParent);

            ActivityGenerator = new FMRIGenerator();
        }
        /// <summary>
        /// Compute the UVs of the meshes for the brain activity
        /// </summary>
        /// <param name="brainSurface">Surface of the brain</param>
        public override void ComputeSurfaceBrainUVWithActivity()
        {
            SurfaceGenerator.ComputeActivityUV(SelectedFMRIIndex, ActivityAlpha);
        }
        #endregion
    }
}
