// HiBoP: Unity's socket layer does not expose IP_PKTINFO on all supported runtimes.
// Use the OS datagram APIs on the existing managed socket; no native plugin is shipped.
using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Makaretu.Dns
{
    internal sealed class MdnsNativeReceiver : IDisposable
    {
        private static readonly bool Windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private static readonly bool Mac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        private readonly IntPtr peer = Marshal.AllocHGlobal(128);
        private readonly IntPtr data = Marshal.AllocHGlobal(9000);
        private readonly IntPtr control = Marshal.AllocHGlobal(512);
        private readonly IntPtr vector = Marshal.AllocHGlobal(16);
        private readonly ReceiveMessage windowsReceive;
        private readonly Socket socket;

        [StructLayout(LayoutKind.Sequential)] private struct WinBuffer { public uint Length; public IntPtr Pointer; }
        [StructLayout(LayoutKind.Sequential)] private struct WinMessage { public IntPtr Name; public int NameLength; public IntPtr Buffers; public uint BufferCount; public WinBuffer Control; public uint Flags; }
        [StructLayout(LayoutKind.Sequential)] private struct LinuxMessage { public IntPtr Name; public uint NameLength; public IntPtr Vectors; public UIntPtr VectorCount; public IntPtr Control; public UIntPtr ControlLength; public int Flags; }
        [StructLayout(LayoutKind.Sequential)] private struct MacMessage { public IntPtr Name; public uint NameLength; public IntPtr Vectors; public int VectorCount; public IntPtr Control; public uint ControlLength; public int Flags; }
        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)] private delegate int ReceiveMessage(IntPtr socket, ref WinMessage message, out uint bytes, IntPtr overlapped, IntPtr completion);

        public MdnsNativeReceiver(Socket socket)
        {
            this.socket = socket;
            try
            {
            if (IntPtr.Size != 8) throw new PlatformNotSupportedException("HiBoP mDNS requires a 64-bit Desktop.");
            int enabled = 1;
            bool v6 = socket.AddressFamily == AddressFamily.InterNetworkV6;
            int option = Windows ? 19 : Mac ? (v6 ? 61 : 20) : (v6 ? 49 : 8);
            int result = Windows ? WinSetOption(socket.Handle, v6 ? 41 : 0, option, ref enabled, 4) : UnixSetOption(socket.Handle.ToInt32(), v6 ? 41 : 0, option, ref enabled, 4);
            if (result != 0) throw new SocketException(Windows ? WSAGetLastError() : Marshal.GetLastWin32Error());
            socket.ReceiveTimeout = 500;
            if (Windows)
            {
                var id = new Guid("f689d7c8-6f1f-436b-8a53-e54fe351c322");
                if (WSAIoctl(socket.Handle, 0xc8000006, ref id, 16, out var function, IntPtr.Size, out _, IntPtr.Zero, IntPtr.Zero) != 0) throw new SocketException(WSAGetLastError());
                windowsReceive = Marshal.GetDelegateForFunctionPointer<ReceiveMessage>(function);
                Marshal.StructureToPtr(new WinBuffer { Length = 9000, Pointer = data }, vector, false);
            }
            else
            {
                Marshal.WriteIntPtr(vector, data);
                Marshal.WriteInt64(vector, 8, 9000);
            }
            }
            catch { Dispose(); throw; }
        }

        public MdnsPacketEventArgs Receive()
        {
            // Unity may keep the underlying OS socket nonblocking even when Blocking is true.
            // Poll only on this worker so timeout/readiness does not rely on the managed option mapping.
            if (!socket.Poll(500000, SelectMode.SelectRead)) return null;
            long length;
            int controlLength, flags;
            int lastError;
            if (Windows)
            {
                var message = new WinMessage { Name = peer, NameLength = 128, Buffers = vector, BufferCount = 1, Control = new WinBuffer { Length = 512, Pointer = control } };
                int result = windowsReceive(socket.Handle, ref message, out uint bytes, IntPtr.Zero, IntPtr.Zero);
                lastError = Marshal.GetLastWin32Error();
                length = result == 0 ? bytes : -1;
                controlLength = (int)message.Control.Length;
                flags = (int)message.Flags;
            }
            else if (Mac)
            {
                var message = new MacMessage { Name = peer, NameLength = 128, Vectors = vector, VectorCount = 1, Control = control, ControlLength = 512 };
                length = MacReceive(socket.Handle.ToInt32(), ref message, 0).ToInt64();
                lastError = Marshal.GetLastWin32Error();
                controlLength = (int)message.ControlLength;
                flags = message.Flags;
            }
            else
            {
                var message = new LinuxMessage { Name = peer, NameLength = 128, Vectors = vector, VectorCount = (UIntPtr)1, Control = control, ControlLength = (UIntPtr)512 };
                length = LinuxReceive(socket.Handle.ToInt32(), ref message, 0).ToInt64();
                lastError = Marshal.GetLastWin32Error();
                controlLength = (int)message.ControlLength.ToUInt64();
                flags = message.Flags;
            }
            if (length < 0)
            {
                int code = lastError;
                if (code == 10060 || code == 10035 || code == 10040 || !Windows && (code == 11 || code == 35)) return null;
                throw new SocketException(code);
            }
            // MSG_TRUNC / MSG_CTRUNC; never parse an incomplete control block or packet.
            if (length <= 0 || length > 9000 || (flags & (Windows ? 0x300 : Mac ? 0x30 : 0x28)) != 0) return null;
            bool ipv6 = socket.AddressFamily == AddressFamily.InterNetworkV6;
            int index = 0;
            int header = Mac ? 12 : 16;
            for (int offset = 0; offset + header <= controlLength;)
            {
                var item = IntPtr.Add(control, offset);
                long size = Mac ? (uint)Marshal.ReadInt32(item) : Marshal.ReadInt64(item);
                if (size < header || size > controlLength - offset) break;
                int level = Marshal.ReadInt32(item, Mac ? 4 : 8), type = Marshal.ReadInt32(item, Mac ? 8 : 12);
                if (ipv6 && level == 41 && type == (Windows ? 19 : Mac ? 46 : 50) && size >= header + 20) index = Marshal.ReadInt32(item, header + 16);
                if (!ipv6 && level == 0)
                {
                    if (Mac && type == 20 && size >= header + 4) index = (ushort)Marshal.ReadInt16(item, header + 2);
                    else if (!Mac && type == (Windows ? 19 : 8) && size >= header + 8) index = Marshal.ReadInt32(item, header + (Windows ? 4 : 0));
                }
                int align = Mac ? 4 : 8;
                offset += checked(((int)size + align - 1) & ~(align - 1));
            }
            if (index <= 0) return null;
            var addressBytes = new byte[ipv6 ? 16 : 4];
            Marshal.Copy(IntPtr.Add(peer, ipv6 ? 8 : 4), addressBytes, 0, addressBytes.Length);
            var address = new IPAddress(addressBytes);
            if (ipv6 && address.IsIPv6LinkLocal) address.ScopeId = index;
            int port = Marshal.ReadByte(peer, 2) * 256 + Marshal.ReadByte(peer, 3);
            var bytesBuffer = new byte[(int)length];
            Marshal.Copy(data, bytesBuffer, 0, bytesBuffer.Length);
            return new MdnsPacketEventArgs { InterfaceIndex = index, Result = new UdpReceiveResult(bytesBuffer, new IPEndPoint(address, port)) };
        }

        public void Dispose() { Marshal.FreeHGlobal(peer); Marshal.FreeHGlobal(data); Marshal.FreeHGlobal(control); Marshal.FreeHGlobal(vector); }
        [DllImport("ws2_32.dll", EntryPoint = "setsockopt", SetLastError = true)] private static extern int WinSetOption(IntPtr socket, int level, int option, ref int value, int length);
        [DllImport("libc", EntryPoint = "setsockopt", SetLastError = true)] private static extern int UnixSetOption(int socket, int level, int option, ref int value, uint length);
        [DllImport("ws2_32.dll")] private static extern int WSAGetLastError();
        [DllImport("ws2_32.dll")] private static extern int WSAIoctl(IntPtr socket, uint code, ref Guid input, int inputLength, out IntPtr output, int outputLength, out uint bytes, IntPtr overlapped, IntPtr completion);
        [DllImport("libc", EntryPoint = "recvmsg", SetLastError = true)] private static extern IntPtr LinuxReceive(int socket, ref LinuxMessage message, int flags);
        [DllImport("libc", EntryPoint = "recvmsg", SetLastError = true)] private static extern IntPtr MacReceive(int socket, ref MacMessage message, int flags);
    }
}
