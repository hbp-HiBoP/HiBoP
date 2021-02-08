using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using HBP.Module3D.DLL;
using HBP.Data.Enums;
using HBP.Data.Visualization;

namespace HBP.Module3D
{
    /// <summary>
    /// Class containing anatomical data for a column
    /// </summary>
    public class Column3DAnatomy : Column3D
    {
        #region Properties
        /// <summary>
        /// Parameters on how to display the activity on the column
        /// </summary>
        public AnatomyDataParameters AnatomyParameters { get; } = new AnatomyDataParameters();
        #endregion

        #region Events
        #endregion

        #region Private Methods
        #endregion

        #region Public Methods
        public override void Initialize(int idColumn, Column baseColumn, Implantation3D implantation, List<GameObject> sceneSitePatientParent)
        {
            base.Initialize(idColumn, baseColumn, implantation, sceneSitePatientParent);

            ActivityGenerator = new DensityGenerator();
        }
        /// <summary>
        /// Compute the UVs of the meshes for the brain activity
        /// </summary>
        /// <param name="brainSurface">Surface of the brain</param>
        public override void ComputeSurfaceBrainUVWithActivity()
        {
            SurfaceGenerator.ComputeActivityUV();
        }
        #endregion
    }
}
