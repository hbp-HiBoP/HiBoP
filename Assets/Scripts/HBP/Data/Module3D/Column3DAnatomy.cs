using System.Collections.Generic;
using UnityEngine;

namespace HBP.Data.Module3D
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

        public override void Initialize(int idColumn, Core.Data.Column baseColumn, Core.Object3D.Implantation3D implantation, List<GameObject> sceneSitePatientParent)
        {
            base.Initialize(idColumn, baseColumn, implantation, sceneSitePatientParent);

            ActivityGenerator = new Core.DLL.DensityGenerator();
        }

        public override (System.Action Compute, System.Action Publish) PrepareActivityComputation(bool roiActive, Core.Enums.SiteInfluenceByDistanceType influenceRule, bool supportsMarsAtlas)
        {
            var updateMasks = PrepareSitesMaskUpdate(roiActive);
            var density = (Core.DLL.DensityGenerator)ActivityGenerator;
            var sites = RawElectrodes;
            float distance = AnatomyParameters.InfluenceDistance;
            return (() =>
            {
                updateMasks();
                density.ComputeActivity(sites, distance, influenceRule);
            }, null);
        }

        /// <summary>
        /// Compute the UVs of the meshes for the brain activity
        /// </summary>
        /// <param name="brainSurface">Surface of the brain</param>
        public override void ComputeSurfaceBrainUVWithActivity()
        {
            SurfaceGenerator.ComputeActivityUV(0, ActivityAlpha);
        }

        #endregion
    }
}
