using HBP.Core.DLL.HbpCore;
using System;
using System.Runtime.InteropServices;

namespace HBP.Core.DLL
{
    public class GeneratorSurface : CppDLLImportBase
    {
        private NativeBackend m_Backend = NativeBackendOptions.ExperimentalBackend;

        #region Properties
        internal NativeBackend Backend => m_Backend;
        public bool UsesHbpCore => m_Backend == NativeBackend.HbpCore;
        public Surface Surface { get; private set; }
        public Volume Volume { get; private set; }
        #endregion

        #region Public Methods
        public void Initialize(Surface surface, Volume volume, int dimension)
        {
            Surface = surface;
            Volume = volume;
            if (m_Backend == NativeBackend.HbpCore)
            {
                if (surface.Backend != NativeBackend.HbpCore || volume.Backend != NativeBackend.HbpCore)
                {
                    throw new InvalidOperationException($"GeneratorSurface.Initialize cannot mix {surface.Backend} surface and {volume.Backend} volume with hbp_core.");
                }
                ThrowIfFailed(hbp_generator_surface_initialize(_handle.Handle, surface.getHandle().Handle, volume.getHandle().Handle, dimension));
                return;
            }
            initialize_GeneratorSurface(_handle, surface.getHandle(), volume.getHandle(), dimension);
        }
        #endregion

        #region Private Methods
        protected override void create_DLL_class()
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_generator_surface_create(out IntPtr generatorSurface));
                _handle = new HandleRef(this, generatorSurface);
                return;
            }
            _handle = new HandleRef(this, create_GeneratorSurface());
        }

        protected override void delete_DLL_class()
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_generator_surface_destroy(_handle.Handle));
                return;
            }
            delete_GeneratorSurface(_handle);
        }
        #endregion

        private static void ThrowIfFailed(HbpCoreStatus status)
        {
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core GeneratorSurface call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }

        #region DLLImport
        [DllImport(NativeDll.HbpExport, EntryPoint = "create_GeneratorSurface", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_GeneratorSurface();
        [DllImport(NativeDll.HbpExport, EntryPoint = "delete_GeneratorSurface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_GeneratorSurface(HandleRef generator);
        [DllImport(NativeDll.HbpExport, EntryPoint = "initialize_GeneratorSurface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void initialize_GeneratorSurface(HandleRef generatorSurface, HandleRef surface, HandleRef volume, int dimension);

        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_generator_surface_create", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_generator_surface_create(out IntPtr generatorSurface);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_generator_surface_destroy", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_generator_surface_destroy(IntPtr generatorSurface);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_generator_surface_initialize", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_generator_surface_initialize(IntPtr generatorSurface, IntPtr surface, IntPtr volume, int dimension);
        #endregion
    }
}
