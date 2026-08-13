using HBP.Core.DLL;
using HBP.Tests.Serialization;
using HBP.Core.Data;
using HBP.Core.DLL.HbpCore;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Tests.Serialization.LegacyNative
{
    public abstract class ActivityGenerator : CppDLLImportBase
    {
        private protected BenchmarkBackend m_Backend = OracleBackendContext.Current;

        #region Properties

        internal BenchmarkBackend Backend => m_Backend;
        public bool UsesHbpCore => m_Backend == BenchmarkBackend.HbpCore;

        public float Progress
        {
            get
            {
                if (m_Backend == BenchmarkBackend.HbpCore)
                {
                    ThrowIfFailed(hbp_activity_generator_get_progress(_handle.Handle, out float progress));
                    return progress;
                }

                return get_progress_ActivityGenerator(_handle);
            }
        }

        public GeneratorSurface GeneratorSurface { get; private set; }

        #endregion

        #region Public Methods

        public void Initialize(GeneratorSurface generatorSurface)
        {
            GeneratorSurface = generatorSurface;
            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                if (generatorSurface.Backend != BenchmarkBackend.HbpCore)
                {
                    throw new InvalidOperationException($"ActivityGenerator.Initialize cannot use a {generatorSurface.Backend} generator surface with hbp_core.");
                }

                ThrowIfFailed(hbp_activity_generator_initialize_projection_grid(_handle.Handle, generatorSurface.getHandle().Handle));
                return;
            }

            initialize_ActivityGenerator(_handle, generatorSurface.getHandle());
        }

        public bool SaveActivityAsNifti(string path, SubTimeline timeline, string description)
        {
            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                return SaveActivityAsNifti(path, timeline.Length, timeline.Frequency.RawValue, timeline.MinTime, description);
            }

            return save_activity_as_nifti_ActivityGenerator(_handle, path, timeline.Length, timeline.Frequency.RawValue, timeline.MinTime, description);
        }

        internal bool SaveActivityAsNifti(string path, int timelineLength, float samplingFrequency, float startTime, string description)
        {
            if (m_Backend != BenchmarkBackend.HbpCore)
            {
                throw new NotSupportedException("Raw activity NIfTI export parameters are only available with hbp_core.");
            }

            return hbp_activity_generator_save_activity_nifti(_handle.Handle, path, timelineLength, samplingFrequency, startTime, description) == HbpCoreStatus.Ok;
        }

        public bool SaveMaskAsNifti(string path, string description)
        {
            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                return hbp_activity_generator_save_mask_nifti(_handle.Handle, path, description) == HbpCoreStatus.Ok;
            }

            return save_mask_as_nifti(_handle, path, description);
        }

        #endregion

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
                if (volume == null)
                {
                    result[i] = IntPtr.Zero;
                    continue;
                }

                if (volume.Backend != BenchmarkBackend.HbpCore)
                {
                    throw new InvalidOperationException($"{methodName} cannot use a {volume.Backend} volume with hbp_core.");
                }

                result[i] = volume.getHandle().Handle;
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

        #region DLLImport

        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "initialize_ActivityGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void initialize_ActivityGenerator(HandleRef generator, HandleRef generatorSurface);

        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "get_progress_ActivityGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern float get_progress_ActivityGenerator(HandleRef generator);

        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "save_activity_as_nifti_ActivityGenerator", CallingConvention = CallingConvention.Cdecl)]
        static public extern bool save_activity_as_nifti_ActivityGenerator(HandleRef generator, string path, int timelineLength, float samplingFrequency, float startTime, string description);

        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "save_mask_as_nifti_ActivityGenerator", CallingConvention = CallingConvention.Cdecl)]
        static public extern bool save_mask_as_nifti(HandleRef generator, string path, string description);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_activity_generator_initialize_projection_grid", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_activity_generator_initialize_projection_grid(IntPtr generator, IntPtr projectionGrid);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_activity_generator_get_progress", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_activity_generator_get_progress(IntPtr generator, out float progress);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_activity_generator_save_activity_nifti", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_activity_generator_save_activity_nifti(IntPtr generator, string path, int timelineLength, float samplingFrequency, float startTime, string description);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_activity_generator_save_mask_nifti", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_activity_generator_save_mask_nifti(IntPtr generator, string path, string description);

        #endregion
    }
}
