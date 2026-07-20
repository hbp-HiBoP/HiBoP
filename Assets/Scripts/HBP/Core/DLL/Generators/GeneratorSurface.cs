using HBP.Core.DLL.HbpCore;
using HBP.Core.Enums;
using System;
using System.Runtime.InteropServices;

namespace HBP.Core.DLL
{
    public class GeneratorSurface : CppDLLImportBase
    {
        public Surface Surface { get; private set; }
        public Volume Volume { get; private set; }

        public void Initialize(Surface surface, Volume volume, int dimension)
        {
            Initialize(surface, volume, dimension, VolumeInterpolation.Nearest);
        }

        public void Initialize(Surface surface, Volume volume, int dimension, VolumeInterpolation interpolation)
        {
            if (surface == null) throw new ArgumentNullException(nameof(surface));
            if (volume == null) throw new ArgumentNullException(nameof(volume));
            if (!Enum.IsDefined(typeof(VolumeInterpolation), interpolation))
            {
                throw new ArgumentOutOfRangeException(nameof(interpolation));
            }

            Surface = surface;
            Volume = volume;
            ThrowIfFailed(hbp_generator_surface_initialize(_handle.Handle, surface.getHandle().Handle, volume.getHandle().Handle, dimension));
            ThrowIfFailed(hbp_generator_surface_set_volume_interpolation(_handle.Handle, interpolation));
        }

        protected override void create_DLL_class()
        {
            ThrowIfFailed(hbp_generator_surface_create(out IntPtr generatorSurface));
            _handle = new HandleRef(this, generatorSurface);
        }

        protected override void delete_DLL_class()
        {
            ThrowIfFailed(hbp_generator_surface_destroy(_handle.Handle));
        }

        private static void ThrowIfFailed(HbpCoreStatus status)
        {
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core GeneratorSurface call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_generator_surface_create", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_generator_surface_create(out IntPtr generatorSurface);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_generator_surface_destroy", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_generator_surface_destroy(IntPtr generatorSurface);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_generator_surface_initialize", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_generator_surface_initialize(IntPtr generatorSurface, IntPtr surface, IntPtr volume, int dimension);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_generator_surface_set_volume_interpolation", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_generator_surface_set_volume_interpolation(IntPtr generatorSurface, VolumeInterpolation interpolation);
    }
}
