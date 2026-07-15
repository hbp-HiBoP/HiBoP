using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HBP.Core.DLL.HbpCore;

namespace HBP.Core.DLL
{
    public class MEGGenerator : ActivityGenerator
    {
        #region Public Methods
        public void ComputeActivity(IEnumerable<(Volume, Volume)> volumesAndMasks)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                List<Volume> volumes = new();
                List<Volume> masks = new();
                foreach (var volumeAndMask in volumesAndMasks)
                {
                    volumes.Add(volumeAndMask.Item1);
                    masks.Add(volumeAndMask.Item2);
                }
                ThrowIfFailed(hbp_meg_generator_compute_activity(_handle.Handle, ToNativeVolumeHandles(volumes, nameof(ComputeActivity)), ToNativeVolumeHandles(masks, nameof(ComputeActivity)), volumes.Count));
                return;
            }

            using MultiVolume multiVolume = new();
            using MultiVolume maskMultiVolume = new();
            foreach (var volumeAndMask in volumesAndMasks)
            {
                multiVolume.AddVolume(volumeAndMask.Item1);
                maskMultiVolume.AddVolume(volumeAndMask.Item2);
            }
            compute_activity_MEGGenerator(_handle, multiVolume.getHandle(), maskMultiVolume.getHandle());
        }

        public void AdjustValues(float fmriNegativeCalMinFactor, float fmriNegativeCalMaxFactor, float fmriPositiveCalMinFactor, float fmriPositiveCalMaxFactor)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_meg_generator_adjust_values(_handle.Handle, fmriNegativeCalMinFactor, fmriNegativeCalMaxFactor, fmriPositiveCalMinFactor, fmriPositiveCalMaxFactor));
                return;
            }
            adjust_values_MEGGenerator(_handle, fmriNegativeCalMinFactor, fmriNegativeCalMaxFactor, fmriPositiveCalMinFactor, fmriPositiveCalMaxFactor);
        }

        public void HideExtremeValues(bool hideLower, bool hideMiddle, bool hideHigher)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_meg_generator_set_hide_values(_handle.Handle, hideLower ? 1 : 0, hideMiddle ? 1 : 0, hideHigher ? 1 : 0));
                return;
            }
            set_hide_values_MEGGenerator(_handle, hideLower, hideMiddle, hideHigher);
        }
        #endregion

        #region Memory Management
        protected override void create_DLL_class()
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_meg_generator_create(out IntPtr generator));
                _handle = new HandleRef(this, generator);
                return;
            }
            _handle = new HandleRef(this, create_MEGGenerator());
        }

        protected override void delete_DLL_class()
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_meg_generator_destroy(_handle.Handle));
                return;
            }
            delete_MEGGenerator(_handle);
        }
        #endregion

        #region DLLImport
        [DllImport(NativeDll.HbpExport, EntryPoint = "create_MEGGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_MEGGenerator();
        [DllImport(NativeDll.HbpExport, EntryPoint = "delete_MEGGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_MEGGenerator(HandleRef generator);
        [DllImport(NativeDll.HbpExport, EntryPoint = "compute_activity_MEGGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void compute_activity_MEGGenerator(HandleRef generator, HandleRef multiVolume, HandleRef maskMultiVolume);
        [DllImport(NativeDll.HbpExport, EntryPoint = "adjust_values_MEGGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void adjust_values_MEGGenerator(HandleRef generator, float negativeMin, float negativeMax, float positiveMin, float positiveMax);
        [DllImport(NativeDll.HbpExport, EntryPoint = "set_hide_values_MEGGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void set_hide_values_MEGGenerator(HandleRef generator, bool lower, bool middle, bool higher);

        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_meg_generator_create", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_meg_generator_create(out IntPtr generator);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_meg_generator_destroy", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_meg_generator_destroy(IntPtr generator);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_meg_generator_compute_activity", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_meg_generator_compute_activity(IntPtr generator, [In] IntPtr[] volumes, [In] IntPtr[] masks, int volumeCount);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_meg_generator_adjust_values", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_meg_generator_adjust_values(IntPtr generator, float negativeMin, float negativeMax, float positiveMin, float positiveMax);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_meg_generator_set_hide_values", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_meg_generator_set_hide_values(IntPtr generator, int lower, int middle, int higher);
        #endregion
    }
}
