using System;
using System.Runtime.InteropServices;
using HBP.Core.DLL.HbpCore;
using UnityEngine;

namespace HBP.Core.DLL
{
    /// <summary>
    /// Class representing a 3D plane in the DLL.
    /// </summary>
    [System.Serializable]
    public class Plane : CppDLLImportBase
    {
        private Vector3 m_Point;
        private Vector3 m_Normal;

        #region Properties
        /// <summary>
        /// Point on the plane.
        /// </summary>
        public Vector3 Point
        {
            get => m_Point;
            set
            {
                m_Point = value;
                UpdateNativePlane();
            }
        }
        /// <summary>
        /// Normal to the plane.
        /// </summary>
        public Vector3 Normal
        {
            get => m_Normal;
            set
            {
                m_Normal = value;
                UpdateNativePlane();
            }
        }
        #endregion

        #region Constructors
        public Plane()
        {
            m_Point = Vector3.zero;
            m_Normal = Vector3.right;
        }

        public Plane(Vector3 point, Vector3 normal) : base(CreatePlane(point, normal))
        {
            m_Point = point;
            m_Normal = normal;
        }
        #endregion

        #region Public Methods
        public void Normalize()
        {
            ThrowIfFailed(hbp_plane_normalize(_handle.Handle));
            m_Normal = m_Normal.normalized;
        }

        public int PointSide(Vector3 point)
        {
            Vec3 nativePoint = Vec3.FromVector3(point);
            ThrowIfFailed(hbp_plane_point_side(_handle.Handle, ref nativePoint, out int side));
            return side;
        }

        public Vector3 ProjectPoint(Vector3 point)
        {
            Vec3 nativePoint = Vec3.FromVector3(point);
            ThrowIfFailed(hbp_plane_project_point(_handle.Handle, ref nativePoint, out Vec3 projected));
            return projected.ToVector3();
        }

        public bool IntersectSegment(Vector3 start, Vector3 end, out Vector3 point)
        {
            Vec3 nativeStart = Vec3.FromVector3(start);
            Vec3 nativeEnd = Vec3.FromVector3(end);
            ThrowIfFailed(hbp_plane_intersect_segment(_handle.Handle, ref nativeStart, ref nativeEnd, out Vec3 nativePoint, out int intersects));
            point = nativePoint.ToVector3();
            return intersects != 0;
        }
        #endregion

        #region Memory Management
        protected override void create_DLL_class()
        {
            _handle = new HandleRef(this, CreatePlane(Vector3.zero, Vector3.right));
        }

        protected override void delete_DLL_class()
        {
            ThrowIfFailed(hbp_plane_destroy(_handle.Handle));
        }
        #endregion

        private void UpdateNativePlane()
        {
            if (_handle.Handle == IntPtr.Zero)
            {
                return;
            }

            Vec3 nativePoint = Vec3.FromVector3(m_Point);
            Vec3 nativeNormal = Vec3.FromVector3(m_Normal);
            ThrowIfFailed(hbp_plane_set(_handle.Handle, ref nativePoint, ref nativeNormal));
        }

        private static IntPtr CreatePlane(Vector3 point, Vector3 normal)
        {
            Vec3 nativePoint = Vec3.FromVector3(point);
            Vec3 nativeNormal = Vec3.FromVector3(normal);
            ThrowIfFailed(hbp_plane_create(ref nativePoint, ref nativeNormal, out IntPtr plane));
            return plane;
        }

        private static void ThrowIfFailed(HbpCoreStatus status)
        {
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core Plane call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_plane_create", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_plane_create(ref Vec3 point, ref Vec3 normal, out IntPtr plane);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_plane_destroy", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_plane_destroy(IntPtr plane);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_plane_set", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_plane_set(IntPtr plane, ref Vec3 point, ref Vec3 normal);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_plane_normalize", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_plane_normalize(IntPtr plane);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_plane_point_side", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_plane_point_side(IntPtr plane, ref Vec3 point, out int side);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_plane_project_point", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_plane_project_point(IntPtr plane, ref Vec3 point, out Vec3 outPoint);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_plane_intersect_segment", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_plane_intersect_segment(IntPtr plane, ref Vec3 start, ref Vec3 end, out Vec3 outPoint, out int intersects);
    }
}
