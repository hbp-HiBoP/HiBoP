using System;
using System.Runtime.InteropServices;

namespace HBP.Tests.Serialization
{
    internal sealed class LegacyVideoStreamBridge : IDisposable
    {
        private IntPtr m_Handle = create_VideoStream();

        public void Open(string path, int width, int height, float fps)
        {
            open_VideoStream(m_Handle, path, width, height, fps);
        }

        public void WriteFrame(LegacyTextureBridge texture)
        {
            write_frame_VideoStream(m_Handle, texture.Handle);
        }

        public void Dispose()
        {
            if (m_Handle == IntPtr.Zero)
            {
                return;
            }
            delete_VideoStream(m_Handle);
            m_Handle = IntPtr.Zero;
        }

        [DllImport("hbp_export", EntryPoint = "create_VideoStream", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create_VideoStream();

        [DllImport("hbp_export", EntryPoint = "delete_VideoStream", CallingConvention = CallingConvention.Cdecl)]
        private static extern void delete_VideoStream(IntPtr stream);

        [DllImport("hbp_export", EntryPoint = "open_VideoStream", CallingConvention = CallingConvention.Cdecl)]
        private static extern void open_VideoStream(IntPtr stream, string path, int width, int height, float fps);

        [DllImport("hbp_export", EntryPoint = "write_frame_VideoStream", CallingConvention = CallingConvention.Cdecl)]
        private static extern void write_frame_VideoStream(IntPtr stream, IntPtr texture);
    }
}
