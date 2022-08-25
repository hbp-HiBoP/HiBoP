using System;
using System.Runtime.InteropServices;
using HBP.Core.Enums;

namespace HBP.Core.DLL
{
    public class DensityGenerator : ActivityGenerator
    {
        #region Properties
        public float MaxDensity { get { return max_density_DensityGenerator(_handle); } }
        #endregion

        #region Public Methods
        public void ComputeActivity(RawSiteList rawElectrodes, float influenceDistance, SiteInfluenceByDistanceType influenceByDistance)
        {
            compute_activity_DensityGenerator(_handle, rawElectrodes.getHandle(), influenceDistance, (int)influenceByDistance);
        }
        #endregion

        #region Memory Management
        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            _handle = new HandleRef(this, create_DensityGenerator());
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            delete_DensityGenerator(_handle);
        }
        #endregion

        #region DLLImport
        [DllImport("hbp_export", EntryPoint = "create_DensityGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_DensityGenerator();
        [DllImport("hbp_export", EntryPoint = "delete_DensityGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_DensityGenerator(HandleRef generator);
        [DllImport("hbp_export", EntryPoint = "compute_activity_DensityGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void compute_activity_DensityGenerator(HandleRef generator, HandleRef rawSiteList, float maxDistance, int ratioDistance);
        [DllImport("hbp_export", EntryPoint = "max_density_DensityGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern float max_density_DensityGenerator(HandleRef generator);
        #endregion
    }
}