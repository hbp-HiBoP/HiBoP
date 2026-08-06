using HBP.Core.Data;
using HBP.Core.DLL.HbpCore;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Core.DLL
{
    public abstract class ActivityGenerator : CppDLLImportBase
    {
        public float Progress
        {
            get
            {
                ThrowIfFailed(hbp_activity_generator_get_progress(_handle.Handle, out float progress));
                return progress;
            }
        }

        public GeneratorSurface GeneratorSurface { get; private set; }

        public void Initialize(GeneratorSurface generatorSurface)
        {
            if (generatorSurface == null) throw new ArgumentNullException(nameof(generatorSurface));
            GeneratorSurface = generatorSurface;
            ThrowIfFailed(hbp_activity_generator_initialize(_handle.Handle, generatorSurface.getHandle().Handle));
        }

        public void SetSmoothActivityBoundaries(bool enabled)
        {
            ThrowIfFailed(hbp_activity_generator_set_smooth_activity_boundaries(_handle.Handle, enabled ? 1 : 0));
        }

        public bool SaveActivityAsNifti(string path, SubTimeline timeline, string description)
        {
            if (timeline == null) throw new ArgumentNullException(nameof(timeline));
            return SaveActivityAsNifti(path, timeline.Length, timeline.Frequency.RawValue, timeline.MinTime, description);
        }

        internal bool SaveActivityAsNifti(string path, int timelineLength, float samplingFrequency, float startTime, string description)
        {
            return hbp_activity_generator_save_activity_nifti(_handle.Handle, path, timelineLength, samplingFrequency, startTime, description) == HbpCoreStatus.Ok;
        }

        public bool SaveMaskAsNifti(string path, string description)
        {
            return hbp_activity_generator_save_mask_nifti(_handle.Handle, path, description) == HbpCoreStatus.Ok;
        }

        internal static Vec3[] ToNativePositions(Vector3[] positions)
        {
            Vec3[] result = new Vec3[positions.Length];
            for (int i = 0; i < positions.Length; ++i)
            {
                result[i] = Vec3.FromVector3(positions[i]);
            }

            return result;
        }

        internal static IntPtr[] ToNativeVolumeHandles(IReadOnlyList<Volume> volumes, string methodName)
        {
            IntPtr[] result = new IntPtr[volumes.Count];
            for (int i = 0; i < volumes.Count; ++i)
            {
                Volume volume = volumes[i];
                result[i] = volume == null ? IntPtr.Zero : volume.getHandle().Handle;
            }

            return result;
        }

        internal static void ThrowIfFailed(HbpCoreStatus status)
        {
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core ActivityGenerator call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_activity_generator_initialize", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_activity_generator_initialize(IntPtr generator, IntPtr generatorSurface);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_activity_generator_set_smooth_activity_boundaries", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_activity_generator_set_smooth_activity_boundaries(IntPtr generator, int enabled);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_activity_generator_get_progress", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_activity_generator_get_progress(IntPtr generator, out float progress);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_activity_generator_save_activity_nifti", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_activity_generator_save_activity_nifti(IntPtr generator, string path, int timelineLength, float samplingFrequency, float startTime, string description);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_activity_generator_save_mask_nifti", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_activity_generator_save_mask_nifti(IntPtr generator, string path, string description);
    }
}
