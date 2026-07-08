using System;
using System.Runtime.InteropServices;
using HBP.Core.DLL.HbpCore;
using HBP.Core.Enums;

namespace HBP.Core.DLL
{
    public class IEEGGenerator : ActivityGenerator
    {
        #region Public Methods
        public void ComputeActivity(RawSiteList rawElectrodes, float influenceDistance, float[] activityValues, int timelineLength, int numberOfSites, SiteInfluenceByDistanceType siteInfluenceByDistance)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_ieeg_generator_compute_activity(
                    _handle.Handle,
                    ToNativePositions(rawElectrodes.SitePositions),
                    rawElectrodes.SiteMask,
                    rawElectrodes.NumberOfSites,
                    influenceDistance,
                    activityValues,
                    timelineLength,
                    (int)siteInfluenceByDistance));
                return;
            }
            compute_activity_IEEGGenerator(_handle, rawElectrodes.getHandle(), influenceDistance, activityValues, timelineLength, numberOfSites, (int)siteInfluenceByDistance);
        }

        public void ComputeActivityAtlas(float[] activityValues, int timelineLength, int[] areaMask, MarsAtlas marsAtlas)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                if (marsAtlas.Backend != NativeBackend.HbpCore)
                {
                    throw new InvalidOperationException($"IEEGGenerator.ComputeActivityAtlas cannot use a {marsAtlas.Backend} atlas with hbp_core.");
                }
                ThrowIfFailed(hbp_ieeg_generator_compute_activity_atlas(_handle.Handle, activityValues, timelineLength, areaMask.Length, areaMask, marsAtlas.getHandle().Handle));
                return;
            }
            compute_activity_atlas_IEEGGenerator(_handle, activityValues, timelineLength, areaMask, marsAtlas.getHandle());
        }

        public void AdjustValues(float middle, float spanMin, float spanMax)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_ieeg_generator_adjust_values(_handle.Handle, middle, spanMin, spanMax));
                return;
            }
            adjust_values_IEEGGenerator(_handle, middle, spanMin, spanMax);
        }
        #endregion

        #region Memory Management
        protected override void create_DLL_class()
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_ieeg_generator_create(out IntPtr generator));
                _handle = new HandleRef(this, generator);
                return;
            }
            _handle = new HandleRef(this, create_IEEGGenerator());
        }

        protected override void delete_DLL_class()
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_ieeg_generator_destroy(_handle.Handle));
                return;
            }
            delete_IEEGGenerator(_handle);
        }
        #endregion

        #region DLLImport
        [DllImport(NativeDll.HbpExport, EntryPoint = "create_IEEGGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_IEEGGenerator();
        [DllImport(NativeDll.HbpExport, EntryPoint = "delete_IEEGGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_IEEGGenerator(HandleRef generator);
        [DllImport(NativeDll.HbpExport, EntryPoint = "compute_activity_IEEGGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void compute_activity_IEEGGenerator(HandleRef generator, HandleRef rawSiteList, float maxDistance, float[] activity, int timelineLength, int sitesNumber, int ratioDistance);
        [DllImport(NativeDll.HbpExport, EntryPoint = "compute_activity_atlas_IEEGGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void compute_activity_atlas_IEEGGenerator(HandleRef generator, float[] activity, int timelineLength, int[] mask, HandleRef marsAtlas);
        [DllImport(NativeDll.HbpExport, EntryPoint = "adjust_values_IEEGGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void adjust_values_IEEGGenerator(HandleRef generator, float middle, float min, float max);

        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_ieeg_generator_create", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_ieeg_generator_create(out IntPtr generator);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_ieeg_generator_destroy", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_ieeg_generator_destroy(IntPtr generator);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_ieeg_generator_compute_activity", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_ieeg_generator_compute_activity(IntPtr generator, [In] Vec3[] sitePositions, [In] int[] siteMask, int siteCount, float maxDistance, [In] float[] activity, int timelineLength, int ratioDistance);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_ieeg_generator_compute_activity_atlas", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_ieeg_generator_compute_activity_atlas(IntPtr generator, [In] float[] activity, int timelineLength, int areaCount, [In] int[] mask, IntPtr atlas);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_ieeg_generator_adjust_values", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_ieeg_generator_adjust_values(IntPtr generator, float middle, float min, float max);
        #endregion
    }
}
