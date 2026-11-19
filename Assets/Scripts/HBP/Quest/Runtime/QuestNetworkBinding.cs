using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace HBP.Quest
{
    internal static class QuestNetworkBinding
    {
        public static void Bind(Socket socket, long network)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Unity IL2CPP Socket.Handle is the OS descriptor. Bind before bind/listen/connect.
            if (android_setsocknetwork(unchecked((ulong)network), socket.Handle.ToInt32()) != 0)
                throw new System.IO.IOException("Could not bind the Quest socket to its Android network (errno " + Marshal.GetLastWin32Error() + ").");
#endif
        }

        [DllImport("android", SetLastError = true)]
        private static extern int android_setsocknetwork(ulong network, int descriptor);
    }
}
