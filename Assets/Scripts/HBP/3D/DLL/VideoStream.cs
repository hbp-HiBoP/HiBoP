using System;
using System.Runtime.InteropServices;

namespace HBP.Module3D.DLL
{
    public class VideoStream : Tools.DLL.CppDLLImportBase
    {
        #region Public Methods
        public void Open(string path, int width, int height, float fps)
        {
            open_VideoStream(_handle, path, width, height, fps);
        }
        public void WriteFrame(Texture texture)
        {
            write_frame_VideoStream(_handle, texture.getHandle());
        }
        #endregion

        #region Memory Management
        protected override void create_DLL_class()
        {
            _handle = new HandleRef(this, create_VideoStream());
        }
        protected override void delete_DLL_class()
        {
            delete_VideoStream(_handle);
        }
        #endregion

        #region DLLImport
        [DllImport("hbp_export", EntryPoint = "create_VideoStream", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_VideoStream();
        [DllImport("hbp_export", EntryPoint = "delete_VideoStream", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr delete_VideoStream(HandleRef videoStreamHandle);
        [DllImport("hbp_export", EntryPoint = "open_VideoStream", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr open_VideoStream(HandleRef videoStreamHandle, string path, int width, int height, float fps);
        [DllImport("hbp_export", EntryPoint = "write_frame_VideoStream", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr write_frame_VideoStream(HandleRef videoStreamHandle, HandleRef textureHandle);
        #endregion
    }
}