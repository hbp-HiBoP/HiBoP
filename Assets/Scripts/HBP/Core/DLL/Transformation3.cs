using System;
using System.Runtime.InteropServices;
using HBP.Core.DLL.HbpCore;
using UnityEngine;

namespace HBP.Core.DLL
{
    public class Transformation3 : CppDLLImportBase
    {
        public Transformation3()
        {
        }

        private Transformation3(IntPtr transformation) : base(transformation)
        {
        }

        public static Transformation3 FromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Expected a transformation file path.", nameof(path));
            }

            ThrowIfFailed(hbp_transform_create_from_file(path, out IntPtr transformation));
            return new Transformation3(transformation);
        }

        public Vector3 ApplyPoint(Vector3 point)
        {
            Vec3 nativePoint = Vec3.FromVector3(point);
            ThrowIfFailed(hbp_transform_apply_point(_handle.Handle, ref nativePoint, out Vec3 result));
            return result.ToVector3();
        }

        protected override void create_DLL_class()
        {
            ThrowIfFailed(hbp_transform_create_identity(out IntPtr transformation));
            _handle = new HandleRef(this, transformation);
        }

        protected override void delete_DLL_class()
        {
            ThrowIfFailed(hbp_transform_destroy(_handle.Handle));
        }

        private static void ThrowIfFailed(HbpCoreStatus status)
        {
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core Transformation3 call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_transform_create_identity", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_transform_create_identity(out IntPtr transformation);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_transform_create_from_file", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_transform_create_from_file(string path, out IntPtr transformation);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_transform_destroy", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_transform_destroy(IntPtr transformation);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_transform_apply_point", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_transform_apply_point(IntPtr transformation, ref Vec3 point, out Vec3 outPoint);
    }
}
