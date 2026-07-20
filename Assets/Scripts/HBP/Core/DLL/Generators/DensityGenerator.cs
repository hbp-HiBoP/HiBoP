using System;
using System.Runtime.InteropServices;
using HBP.Core.DLL.HbpCore;
using HBP.Core.Enums;

namespace HBP.Core.DLL
{
    public class DensityGenerator : ActivityGenerator
    {
        public float MaxDensity
        {
            get
            {
                ThrowIfFailed(hbp_density_generator_get_max_density(_handle.Handle, out float maxDensity));
                return maxDensity;
            }
        }

        public void ComputeActivity(RawSiteList rawElectrodes, float influenceDistance, SiteInfluenceByDistanceType influenceByDistance)
        {
            if (rawElectrodes == null) throw new ArgumentNullException(nameof(rawElectrodes));
            ThrowIfFailed(hbp_density_generator_compute_activity_from_sites(
                _handle.Handle,
                rawElectrodes.getHandle().Handle,
                influenceDistance,
                (int)influenceByDistance));
        }

        protected override void create_DLL_class()
        {
            ThrowIfFailed(hbp_density_generator_create(out IntPtr generator));
            _handle = new HandleRef(this, generator);
        }

        protected override void delete_DLL_class()
        {
            ThrowIfFailed(hbp_density_generator_destroy(_handle.Handle));
        }

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_density_generator_create", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_density_generator_create(out IntPtr generator);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_density_generator_destroy", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_density_generator_destroy(IntPtr generator);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_density_generator_compute_activity_from_sites", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_density_generator_compute_activity_from_sites(IntPtr generator, IntPtr sites, float maxDistance, int ratioDistance);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_density_generator_get_max_density", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_density_generator_get_max_density(IntPtr generator, out float maxDensity);
    }
}
