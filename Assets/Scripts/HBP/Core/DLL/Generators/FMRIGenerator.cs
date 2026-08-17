using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HBP.Core.DLL.HbpCore;

namespace HBP.Core.DLL
{
    public class FMRIGenerator : ActivityGenerator
    {
        public void ComputeActivity(IEnumerable<(Volume, Volume)> volumesAndMasks)
        {
            if (volumesAndMasks == null) throw new ArgumentNullException(nameof(volumesAndMasks));
            List<Volume> volumes = new();
            List<Volume> masks = new();
            foreach (var volumeAndMask in volumesAndMasks)
            {
                volumes.Add(volumeAndMask.Item1);
                masks.Add(volumeAndMask.Item2);
            }

            ThrowIfFailed(hbp_fmri_generator_compute_activity(_handle.Handle, ToNativeVolumeHandles(volumes, nameof(ComputeActivity)), ToNativeVolumeHandles(masks, nameof(ComputeActivity)), volumes.Count));
        }

        public void AdjustValues(float fmriNegativeCalMinFactor, float fmriNegativeCalMaxFactor, float fmriPositiveCalMinFactor, float fmriPositiveCalMaxFactor)
        {
            ThrowIfFailed(hbp_fmri_generator_adjust_values(_handle.Handle, fmriNegativeCalMinFactor, fmriNegativeCalMaxFactor, fmriPositiveCalMinFactor, fmriPositiveCalMaxFactor));
        }

        public void HideExtremeValues(bool hideLower, bool hideMiddle, bool hideHigher)
        {
            ThrowIfFailed(hbp_fmri_generator_set_hide_values(_handle.Handle, hideLower ? 1 : 0, hideMiddle ? 1 : 0, hideHigher ? 1 : 0));
        }

        protected override void create_DLL_class()
        {
            ThrowIfFailed(hbp_fmri_generator_create(out IntPtr generator));
            _handle = new HandleRef(this, generator);
        }

        protected override void delete_DLL_class()
        {
            ThrowIfFailed(hbp_fmri_generator_destroy(_handle.Handle));
        }

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_fmri_generator_create", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_fmri_generator_create(out IntPtr generator);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_fmri_generator_destroy", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_fmri_generator_destroy(IntPtr generator);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_fmri_generator_compute_activity", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_fmri_generator_compute_activity(IntPtr generator, [In] IntPtr[] volumes, [In] IntPtr[] masks, int volumeCount);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_fmri_generator_adjust_values", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_fmri_generator_adjust_values(IntPtr generator, float negativeMin, float negativeMax, float positiveMin, float positiveMax);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_fmri_generator_set_hide_values", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_fmri_generator_set_hide_values(IntPtr generator, int lower, int middle, int higher);
    }
}
