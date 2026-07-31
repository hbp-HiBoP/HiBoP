using System;
using System.Runtime.InteropServices;
using HBP.Core.DLL.HbpCore;
using UnityEngine;

namespace HBP.Core.DLL
{
    /// <summary>
    /// This class defines a native segment between two points.
    /// </summary>
    public class Segment3 : CppDLLImportBase
    {
        private Vector3 m_End1;
        private Vector3 m_End2;

        #region Properties

        /// <summary>
        /// First end of the segment.
        /// </summary>
        public Vector3 End1
        {
            get => m_End1;
            set
            {
                m_End1 = value;
                UpdateNativeSegment();
            }
        }

        /// <summary>
        /// Second end of the segment.
        /// </summary>
        public Vector3 End2
        {
            get => m_End2;
            set
            {
                m_End2 = value;
                UpdateNativeSegment();
            }
        }

        /// <summary>
        /// Length of the segment.
        /// </summary>
        public float Length
        {
            get
            {
                ThrowIfFailed(hbp_segment_get_length(_handle.Handle, out float length));
                return length;
            }
        }

        #endregion

        #region Constructors

        public Segment3(Vector3 end1, Vector3 end2) : base(CreateSegment(end1, end2))
        {
            m_End1 = end1;
            m_End2 = end2;
        }

        #endregion

        #region Memory Management

        protected override void create_DLL_class()
        {
            _handle = new HandleRef(this, CreateSegment(Vector3.zero, Vector3.zero));
        }

        protected override void delete_DLL_class()
        {
            ThrowIfFailed(hbp_segment_destroy(_handle.Handle));
        }

        #endregion

        private void UpdateNativeSegment()
        {
            if (_handle.Handle == IntPtr.Zero)
            {
                return;
            }

            Vec3 nativeEnd1 = Vec3.FromVector3(m_End1);
            Vec3 nativeEnd2 = Vec3.FromVector3(m_End2);
            ThrowIfFailed(hbp_segment_set(_handle.Handle, ref nativeEnd1, ref nativeEnd2));
        }

        private static IntPtr CreateSegment(Vector3 end1, Vector3 end2)
        {
            Vec3 nativeEnd1 = Vec3.FromVector3(end1);
            Vec3 nativeEnd2 = Vec3.FromVector3(end2);
            ThrowIfFailed(hbp_segment_create(ref nativeEnd1, ref nativeEnd2, out IntPtr segment));
            return segment;
        }

        private static void ThrowIfFailed(HbpCoreStatus status)
        {
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core Segment3 call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_segment_create", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_segment_create(ref Vec3 end1, ref Vec3 end2, out IntPtr segment);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_segment_destroy", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_segment_destroy(IntPtr segment);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_segment_set", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_segment_set(IntPtr segment, ref Vec3 end1, ref Vec3 end2);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_segment_get_length", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_segment_get_length(IntPtr segment, out float length);
    }
}
