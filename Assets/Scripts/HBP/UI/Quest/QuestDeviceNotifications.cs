using System;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;

namespace HBP.UI.Quest
{
    /// <summary>Callbacks only signal work; discovery and Unity access happen on their owning threads.</summary>
    internal sealed class QuestDeviceNotifications : IDisposable
    {
        private int networkChanged = 1, usbChanged = 1;
        private IntPtr notification;
        private IntPtr interfaceNotification, addressNotification;
        private bool networkSubscribed;
        private GCHandle self;
        private static readonly NotificationCallback callback = DeviceChanged;
        private static readonly NetworkCallback networkCallback = InterfaceChanged;

        public QuestDeviceNotifications()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            self = GCHandle.Alloc(this);
            IntPtr context = GCHandle.ToIntPtr(self);
            NotifyIpInterfaceChange(0, networkCallback, context, false, out interfaceNotification);
            NotifyUnicastIpAddressChange(0, networkCallback, context, false, out addressNotification);
            var filter = new NotificationFilter { Size = 416, Flags = 1 };
            uint result = CM_Register_Notification(ref filter, context, callback, out notification);
            if (result != 0) notification = IntPtr.Zero;
#else
            try { NetworkChange.NetworkAddressChanged += NetworkChanged; networkSubscribed = true; }
            catch (PlatformNotSupportedException) { /* Passive Quest announcements still detect network availability. */ }
#endif
        }

        private void NetworkChanged(object sender, EventArgs args) => Interlocked.Exchange(ref networkChanged, 1);
        public bool TakeNetworkChange() => Interlocked.Exchange(ref networkChanged, 0) != 0;
        public bool TakeUsbChange() => Interlocked.Exchange(ref usbChanged, 0) != 0;

        [AOT.MonoPInvokeCallback(typeof(NetworkCallback))]
        private static void InterfaceChanged(IntPtr context, IntPtr row, uint change)
        {
            if (context != IntPtr.Zero && GCHandle.FromIntPtr(context).Target is QuestDeviceNotifications owner)
                Interlocked.Exchange(ref owner.networkChanged, 1);
        }

        [AOT.MonoPInvokeCallback(typeof(NotificationCallback))]
        private static uint DeviceChanged(IntPtr handle, IntPtr context, uint action, IntPtr data, uint size)
        {
            if (context != IntPtr.Zero && GCHandle.FromIntPtr(context).Target is QuestDeviceNotifications owner)
                Interlocked.Exchange(ref owner.usbChanged, 1);
            return 0;
        }

        public void Dispose()
        {
            if (networkSubscribed)
            {
                NetworkChange.NetworkAddressChanged -= NetworkChanged;
                networkSubscribed = false;
            }

            if (interfaceNotification != IntPtr.Zero)
            {
                CancelMibChangeNotify2(interfaceNotification);
                interfaceNotification = IntPtr.Zero;
            }

            if (addressNotification != IntPtr.Zero)
            {
                CancelMibChangeNotify2(addressNotification);
                addressNotification = IntPtr.Zero;
            }

            if (notification != IntPtr.Zero)
            {
                CM_Unregister_Notification(notification);
                notification = IntPtr.Zero;
            }

            if (self.IsAllocated) self.Free();
        }

        [StructLayout(LayoutKind.Explicit, Size = 416)]
        private struct NotificationFilter
        {
            [FieldOffset(0)] public uint Size;
            [FieldOffset(4)] public uint Flags;
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate uint NotificationCallback(IntPtr handle, IntPtr context, uint action, IntPtr data, uint size);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void NetworkCallback(IntPtr context, IntPtr row, uint change);

        [DllImport("iphlpapi.dll")]
        private static extern uint NotifyIpInterfaceChange(ushort family, NetworkCallback callback, IntPtr context, [MarshalAs(UnmanagedType.U1)] bool initial, out IntPtr handle);

        [DllImport("iphlpapi.dll")]
        private static extern uint NotifyUnicastIpAddressChange(ushort family, NetworkCallback callback, IntPtr context, [MarshalAs(UnmanagedType.U1)] bool initial, out IntPtr handle);

        [DllImport("iphlpapi.dll")]
        private static extern uint CancelMibChangeNotify2(IntPtr handle);

        [DllImport("cfgmgr32.dll")]
        private static extern uint CM_Register_Notification(ref NotificationFilter filter, IntPtr context, NotificationCallback callback, out IntPtr notification);

        [DllImport("cfgmgr32.dll")]
        private static extern uint CM_Unregister_Notification(IntPtr notification);
    }
}
