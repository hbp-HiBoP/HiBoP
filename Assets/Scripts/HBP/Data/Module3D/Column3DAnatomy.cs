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

        public int[] DensityHistogramBins { get; private set; }
        private Core.DLL.ActivityProjectionGrid m_DensityReferenceGrid;
        private Core.Enums.SiteInfluenceByDistanceType m_DensityReferenceRule;

        public bool HasDensityReference(Core.Enums.SiteInfluenceByDistanceType rule) => DensityHistogramBins != null && ReferenceEquals(m_DensityReferenceGrid, ActivityGenerator?.ProjectionGrid) && m_DensityReferenceRule == rule;

        public (System.Action Compute, System.Action Publish) PrepareDensityReferenceComputation(Core.Enums.SiteInfluenceByDistanceType rule)
        {
            var density = (Core.DLL.DensityGenerator)ActivityGenerator;
            var sites = RawElectrodes;
            var grid = density.ProjectionGrid;
            (float Maximum, int[] Bins) reference = default;
            return (() => reference = density.PrepareReference(sites, AnatomyDataParameters.ReferenceInfluenceDistance, rule, Core.Tools.UnityTextureFactory.HistogramBinCount), () => PublishDensityReference(sites, grid, rule, reference));
        }

        private void PublishDensityReference(Core.DLL.RawSiteList sites, Core.DLL.ActivityProjectionGrid grid, Core.Enums.SiteInfluenceByDistanceType rule, (float Maximum, int[] Bins) reference)
        {
            if (!ReferenceEquals(sites, RawElectrodes) || !ReferenceEquals(grid, ActivityGenerator?.ProjectionGrid)) return;
            if (!ReferenceEquals(m_DensityReferenceGrid, grid)) AnatomyParameters.PublishProjectionMaximum(null);
            DensityHistogramBins = reference.Bins;
            m_DensityReferenceGrid = grid;
            m_DensityReferenceRule = rule;
            AnatomyParameters.PublishReference(reference.Maximum);
        }

        #endregion

        #region Events

        #endregion

        #region Private Methods

        #endregion

        #region Public Methods

        public override void Initialize(int idColumn, Core.Data.Column baseColumn, Core.Object3D.Implantation3D implantation, List<GameObject> sceneSitePatientParent)
        {
            ActivityGenerator = new Core.DLL.DensityGenerator();
            base.Initialize(idColumn, baseColumn, implantation, sceneSitePatientParent);
        }

        public override void LoadConfiguration(bool firstCall = true)
        {
            if (firstCall) ResetConfiguration();
            AnatomyParameters.InfluenceDistance = ((Core.Data.AnatomicColumn)ColumnData).AnatomicConfiguration.MaximumInfluence;
            AnatomyParameters.LoadConfiguration(((Core.Data.AnatomicColumn)ColumnData).AnatomicConfiguration);
            base.LoadConfiguration(false);
        }

        public override void UpdateSites(Core.Object3D.Implantation3D implantation, List<GameObject> sceneSitePatientParent)
        {
            DensityHistogramBins = null;
            AnatomyParameters.PublishProjectionMaximum(null);
            AnatomyParameters.PublishReference(0f);
            base.UpdateSites(implantation, sceneSitePatientParent);
        }

        public override void CaptureConfiguration(Core.Data.Column target)
        {
            AnatomyParameters.CaptureConfiguration(((Core.Data.AnatomicColumn)target).AnatomicConfiguration);
            base.CaptureConfiguration(target);
        }

        public override void ResetConfiguration()
        {
            AnatomyParameters.InfluenceDistance = 15f;
            AnatomyParameters.ResetSpanValues();
            base.ResetConfiguration();
        }

        public override (System.Action Compute, System.Action Publish) PrepareActivityComputation(bool roiActive, Core.Enums.SiteInfluenceByDistanceType influenceRule, bool supportsMarsAtlas)
        {
            var updateMasks = PrepareSitesMaskUpdate(roiActive);
            var density = (Core.DLL.DensityGenerator)ActivityGenerator;
            var sites = RawElectrodes;
            var grid = density.ProjectionGrid;
            float distance = AnatomyParameters.InfluenceDistance;
            float minRatio = AnatomyParameters.SpanMinRatio;
            float middleRatio = AnatomyParameters.MiddleRatio;
            float maxRatio = AnatomyParameters.SpanMaxRatio;
            float projectionMaximum = 0f;
            (float Maximum, int[] Bins) reference = default;
            return (() =>
            {
                updateMasks();
                reference = density.PrepareReference(sites, AnatomyDataParameters.ReferenceInfluenceDistance, influenceRule, Core.Tools.UnityTextureFactory.HistogramBinCount);
                density.ComputeActivity(sites, distance, influenceRule);
                projectionMaximum = density.MaxDensity;
                density.AdjustValues(middleRatio * projectionMaximum, minRatio * projectionMaximum, maxRatio * projectionMaximum);
            }, () =>
            {
                if (!ReferenceEquals(sites, RawElectrodes) || !ReferenceEquals(grid, ActivityGenerator?.ProjectionGrid)) return;
                PublishDensityReference(sites, grid, influenceRule, reference);
                AnatomyParameters.PublishProjectionMaximum(projectionMaximum);
            });
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
