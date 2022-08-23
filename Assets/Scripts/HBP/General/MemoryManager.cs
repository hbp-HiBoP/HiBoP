using UnityEngine;
using System;
using System.Runtime.InteropServices;
using UnityEngine.Events;

namespace HBP.Display.Tools
{
    public class MemoryManager : MonoBehaviour
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
        #region Properties
        private static MemoryManager m_Instance;

        const float DELAY = 5.0f; // in s.
        const long MEMORY_LIMIT = 1000; // in MB

        private long m_AvailableMemory;
        /// <summary>
        /// Available memory in MB.
        /// </summary>
        public long AvailableMemory
        {
            get
            {
                m_AvailableMemory = PerformanceInfo.GetPhysicalAvailableMemoryInMiB();
                return m_AvailableMemory;
            }
        }
        public bool EnoughAvailableMemory { get { return AvailableMemory > MEMORY_LIMIT; } }
        public static UnityEvent OnNotEnoughAvailableMemory = new UnityEvent();
        #endregion

        #region Private Methods
        private void Awake()
        {
            if (m_Instance == null)
            {
                m_Instance = this;
            }
            else
            {
                Destroy(this);
            }
        }
        private void Start()
        {
            //InvokeRepeating("CheckMemory", 0, DELAY);
        }
        #endregion

        #region Public Methods
        public static void CheckMemory()
        {
            if (!m_Instance.EnoughAvailableMemory) OnNotEnoughAvailableMemory.Invoke();
        }
        #endregion

        #region Internal Class
        static class PerformanceInfo
        {
            [DllImport("psapi.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetPerformanceInfo([Out] out PerformanceInformation PerformanceInformation, [In] int Size);

            [StructLayout(LayoutKind.Sequential)]
            public struct PerformanceInformation
            {
                public int Size;
                public IntPtr CommitTotal;
                public IntPtr CommitLimit;
                public IntPtr CommitPeak;
                public IntPtr PhysicalTotal;
                public IntPtr PhysicalAvailable;
                public IntPtr SystemCache;
                public IntPtr KernelTotal;
                public IntPtr KernelPaged;
                public IntPtr KernelNonPaged;
                public IntPtr PageSize;
                public int HandlesCount;
                public int ProcessCount;
                public int ThreadCount;
            }

            public static Int64 GetPhysicalAvailableMemoryInMiB()
            {
                PerformanceInformation pi = new PerformanceInformation();
                if (GetPerformanceInfo(out pi, Marshal.SizeOf(pi)))
                {
                    return Convert.ToInt64((pi.PhysicalAvailable.ToInt64() * pi.PageSize.ToInt64() / 1048576));
                }
                else
                {
                    return -1;
                }

            }

            public static Int64 GetTotalMemoryInMiB()
            {
                PerformanceInformation pi = new PerformanceInformation();
                if (GetPerformanceInfo(out pi, Marshal.SizeOf(pi)))
                {
                    return Convert.ToInt64((pi.PhysicalTotal.ToInt64() * pi.PageSize.ToInt64() / 1048576));
                }
                else
                {
                    return -1;
                }

            }
        }
        #endregion
#endif
    }
}
