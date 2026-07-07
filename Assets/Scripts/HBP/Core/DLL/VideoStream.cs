using System;
using System.Runtime.InteropServices;

using UnityEngine;

namespace HBP.Core.DLL
{
    public class VideoStream : CppDLLImportBase
    {
        private Texture m_FrameTexture;

        #region Public Methods
        public void Open(string path, int width, int height, float fps)
        {
            open_VideoStream(_handle, path, width, height, fps);
        }
        public void WriteFrame(Texture texture)
        {
            write_frame_VideoStream(_handle, texture.getHandle());
        }
        public void WriteFrame(Texture2D texture)
        {
            if (texture == null) throw new ArgumentNullException(nameof(texture));

            m_FrameTexture ??= new Texture();
            m_FrameTexture.FromTexture2D(texture);
            WriteFrame(m_FrameTexture);
        }
        #endregion

        #region Memory Management
        protected override void create_DLL_class()
        {
            _handle = new HandleRef(this, create_VideoStream());
        }
        protected override void delete_DLL_class()
        {
            m_FrameTexture?.Dispose();
            m_FrameTexture = null;
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
