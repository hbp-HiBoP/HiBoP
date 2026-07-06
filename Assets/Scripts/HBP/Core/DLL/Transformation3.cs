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

        public Transformation3(float[] linear9, Vector3 translation) : base(CreateTransformation(linear9, translation))
        {
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

        private static IntPtr CreateTransformation(float[] linear9, Vector3 translation)
        {
            if (linear9 == null || linear9.Length != 9)
            {
                throw new ArgumentException("Expected a 3x3 linear transform matrix.", nameof(linear9));
            }

            Vec3 nativeTranslation = Vec3.FromVector3(translation);
            ThrowIfFailed(hbp_transform_create(linear9, ref nativeTranslation, out IntPtr transformation));
            return transformation;
        }

        private static void ThrowIfFailed(HbpCoreStatus status)
        {
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core Transformation3 call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }

        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_transform_create_identity", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_transform_create_identity(out IntPtr transformation);

        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_transform_create", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_transform_create(float[] linear9, ref Vec3 translation, out IntPtr transformation);

        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_transform_destroy", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_transform_destroy(IntPtr transformation);

        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_transform_apply_point", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_transform_apply_point(IntPtr transformation, ref Vec3 point, out Vec3 outPoint);
    }
}
