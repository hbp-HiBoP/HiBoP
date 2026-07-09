using System;
using System.Runtime.InteropServices;
using HBP.Core.DLL.HbpCore;
using HBP.Core.Enums;

namespace HBP.Core.DLL
{
    public class DensityGenerator : ActivityGenerator
    {
        #region Properties
        public float MaxDensity
        {
            get
            {
                if (m_Backend == NativeBackend.HbpCore)
                {
                    ThrowIfFailed(hbp_density_generator_get_max_density(_handle.Handle, out float maxDensity));
                    return maxDensity;
                }
                return max_density_DensityGenerator(_handle);
            }
        }
        #endregion

        #region Public Methods
        public void ComputeActivity(RawSiteList rawElectrodes, float influenceDistance, SiteInfluenceByDistanceType influenceByDistance)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_density_generator_compute_activity_from_sites(
                    _handle.Handle,
                    rawElectrodes.getHandle().Handle,
                    influenceDistance,
                    (int)influenceByDistance));
                return;
            }
            compute_activity_DensityGenerator(_handle, rawElectrodes.getHandle(), influenceDistance, (int)influenceByDistance);
        }
        #endregion

        #region Memory Management
        protected override void create_DLL_class()
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_density_generator_create(out IntPtr generator));
                _handle = new HandleRef(this, generator);
                return;
            }
            _handle = new HandleRef(this, create_DensityGenerator());
        }

        protected override void delete_DLL_class()
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_density_generator_destroy(_handle.Handle));
                return;
            }
            delete_DensityGenerator(_handle);
        }
        #endregion

        #region DLLImport
        [DllImport(NativeDll.HbpExport, EntryPoint = "create_DensityGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_DensityGenerator();
        [DllImport(NativeDll.HbpExport, EntryPoint = "delete_DensityGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_DensityGenerator(HandleRef generator);
        [DllImport(NativeDll.HbpExport, EntryPoint = "compute_activity_DensityGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void compute_activity_DensityGenerator(HandleRef generator, HandleRef rawSiteList, float maxDistance, int ratioDistance);
        [DllImport(NativeDll.HbpExport, EntryPoint = "max_density_DensityGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern float max_density_DensityGenerator(HandleRef generator);

        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_density_generator_create", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_density_generator_create(out IntPtr generator);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_density_generator_destroy", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_density_generator_destroy(IntPtr generator);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_density_generator_compute_activity_from_sites", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_density_generator_compute_activity_from_sites(IntPtr generator, IntPtr sites, float maxDistance, int ratioDistance);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_density_generator_get_max_density", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_density_generator_get_max_density(IntPtr generator, out float maxDensity);
        #endregion
    }
}
