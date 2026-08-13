using HBP.Core.DLL;
using HBP.Tests.Serialization;
using HBP.Core.DLL.HbpCore;
using HBP.Core.Enums;
using System;
using System.Runtime.InteropServices;

namespace HBP.Tests.Serialization.LegacyNative
{
    public class GeneratorSurface : CppDLLImportBase
    {
        private BenchmarkBackend m_Backend = OracleBackendContext.Current;

        #region Properties

        internal BenchmarkBackend Backend => m_Backend;
        public bool UsesHbpCore => m_Backend == BenchmarkBackend.HbpCore;
        public Surface Surface { get; private set; }
        public Volume Volume { get; private set; }

        #endregion

        #region Public Methods

        public void Initialize(Surface surface, Volume volume, int dimension)
        {
            Initialize(surface, volume, dimension, VolumeInterpolation.Nearest);
        }

        public void Initialize(Surface surface, Volume volume, int dimension, VolumeInterpolation interpolation)
        {
            if (!Enum.IsDefined(typeof(VolumeInterpolation), interpolation))
            {
                throw new ArgumentOutOfRangeException(nameof(interpolation));
            }

            Surface = surface;
            Volume = volume;
            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                if (surface.Backend != BenchmarkBackend.HbpCore || volume.Backend != BenchmarkBackend.HbpCore)
                {
                    throw new InvalidOperationException($"GeneratorSurface.Initialize cannot mix {surface.Backend} surface and {volume.Backend} volume with hbp_core.");
                }

                ThrowIfFailed(hbp_activity_projection_grid_initialize(_handle.Handle, volume.getHandle().Handle, dimension));
                ThrowIfFailed(hbp_activity_projection_grid_set_volume_interpolation(_handle.Handle, interpolation));
                return;
            }

            if (interpolation != VolumeInterpolation.Nearest)
            {
                throw new NotSupportedException("Trilinear volume interpolation is only available with hbp_core.");
            }

            initialize_GeneratorSurface(_handle, surface.getHandle(), volume.getHandle(), dimension);
        }

        #endregion

        #region Private Methods

        protected override void create_DLL_class()
        {
            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                ThrowIfFailed(hbp_activity_projection_grid_create(out IntPtr projectionGrid));
                _handle = new HandleRef(this, projectionGrid);
                return;
            }

            _handle = new HandleRef(this, create_GeneratorSurface());
        }

        protected override void delete_DLL_class()
        {
            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                ThrowIfFailed(hbp_activity_projection_grid_destroy(_handle.Handle));
                return;
            }

            delete_GeneratorSurface(_handle);
        }

        #endregion

        private static void ThrowIfFailed(HbpCoreStatus status)
        {
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core ActivityProjectionGrid call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }

        #region DLLImport

        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "create_GeneratorSurface", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_GeneratorSurface();

        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "delete_GeneratorSurface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_GeneratorSurface(HandleRef generator);

        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "initialize_GeneratorSurface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void initialize_GeneratorSurface(HandleRef generatorSurface, HandleRef surface, HandleRef volume, int dimension);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_activity_projection_grid_create", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_activity_projection_grid_create(out IntPtr projectionGrid);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_activity_projection_grid_destroy", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_activity_projection_grid_destroy(IntPtr projectionGrid);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_activity_projection_grid_initialize", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_activity_projection_grid_initialize(IntPtr projectionGrid, IntPtr volume, int dimension);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_activity_projection_grid_set_volume_interpolation", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_activity_projection_grid_set_volume_interpolation(IntPtr projectionGrid, VolumeInterpolation interpolation);

        #endregion
    }
}
