using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HBP.Core.DLL.HbpCore;

namespace HBP.Core.DLL
{
    /// <summary>
    /// Native owner for a temporary list of hbp_core surfaces.
    /// </summary>
    public class SurfaceList : CppDLLImportBase
    {
        public int Count
        {
            get
            {
                ThrowIfFailed(hbp_surface_list_get_count(_handle.Handle, out int count));
                return count;
            }
        }

        internal SurfaceList(IntPtr surfaceListHandle) : base(surfaceListHandle)
        {
        }

        public Surface TakeSurface(int index)
        {
            ThrowIfFailed(hbp_surface_list_take_surface(_handle.Handle, index, out IntPtr surface));
            return new Surface(surface, NativeBackend.HbpCore);
        }

        public List<Surface> TakeAllSurfaces()
        {
            List<Surface> surfaces = new(Count);
            while (Count > 0)
            {
                surfaces.Add(TakeSurface(0));
            }
            return surfaces;
        }

        protected override void create_DLL_class()
        {
            throw new NotSupportedException("SurfaceList instances are returned by hbp_core surface operations.");
        }

        protected override void delete_DLL_class()
        {
            ThrowIfFailed(hbp_surface_list_destroy(_handle.Handle));
        }

        private static void ThrowIfFailed(HbpCoreStatus status)
        {
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core SurfaceList call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }

        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_list_destroy", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_list_destroy(IntPtr surfaces);

        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_list_get_count", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_list_get_count(IntPtr surfaces, out int count);

        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_list_take_surface", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_list_take_surface(IntPtr surfaces, int index, out IntPtr surface);
    }
}
