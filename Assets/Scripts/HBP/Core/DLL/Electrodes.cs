using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using HBP.Core.DLL.HbpCore;
using UnityEngine;

namespace HBP.Core.DLL
{
    public class RawSiteList : CppDLLImportBase
    {
        public int NumberOfSites
        {
            get
            {
                ThrowIfFailed(hbp_raw_site_list_get_count(_handle.Handle, out int count));
                return count;
            }
        }

        public RawSiteList()
        {
        }

        public RawSiteList(RawSiteList other) : base(CloneNative(other))
        {
        }

        public RawSiteList(IntPtr ptr) : base(ptr)
        {
        }

        public void SetPatients(IEnumerable<Data.Patient> patients)
        {
            if (patients == null) throw new ArgumentNullException(nameof(patients));
            ThrowIfFailed(hbp_raw_site_list_set_patients(_handle.Handle, string.Join("?", patients.Select(p => p.ID))));
        }

        public void AddSite(string name, Vector3 nativePosition, int patientIndex, int index)
        {
            Vec3 position = Vec3.FromVector3(nativePosition, convertReferenceSystem: false);
            ThrowIfFailed(hbp_raw_site_list_add_site(_handle.Handle, name, ref position, patientIndex, index));
        }

        public void UpdateMask(int idSite, bool mask)
        {
            ThrowIfFailed(hbp_raw_site_list_update_mask(_handle.Handle, idSite, mask ? 1 : 0));
        }

        public void GetSitesOnPlane(Plane plane, float precision, out int[] result)
        {
            if (plane == null) throw new ArgumentNullException(nameof(plane));
            result = new int[NumberOfSites];
            ThrowIfFailed(hbp_raw_site_list_copy_sites_on_plane(_handle.Handle, plane.getHandle().Handle, precision, result, result.Length));
        }

        public bool IsSiteOnAnyPlane(Object3D.Site site, IEnumerable<Plane> planes, float precision)
        {
            if (site == null) throw new ArgumentNullException(nameof(site));
            if (planes == null) throw new ArgumentNullException(nameof(planes));
            bool result = false;
            foreach (Plane plane in planes)
            {
                ThrowIfFailed(hbp_raw_site_list_is_site_on_plane(_handle.Handle, site.Information.Index, plane.getHandle().Handle, precision, out int onPlane));
                result |= onPlane == 1;
            }
            return result;
        }

        public int GetMarsAtlasLabelOfSite(int siteID)
        {
            ThrowIfFailed(hbp_raw_site_list_get_mars_atlas_label(_handle.Handle, siteID, out int label));
            return label;
        }

        protected override void create_DLL_class()
        {
            ThrowIfFailed(hbp_raw_site_list_create(out IntPtr list));
            _handle = new HandleRef(this, list);
        }

        protected override void delete_DLL_class()
        {
            ThrowIfFailed(hbp_raw_site_list_destroy(_handle.Handle));
        }

        private static IntPtr CloneNative(RawSiteList other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            ThrowIfFailed(hbp_raw_site_list_clone(other.getHandle().Handle, out IntPtr clone));
            return clone;
        }

        private static void ThrowIfFailed(HbpCoreStatus status)
        {
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core RawSiteList call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_raw_site_list_create", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_raw_site_list_create(out IntPtr list);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_raw_site_list_destroy", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_raw_site_list_destroy(IntPtr list);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_raw_site_list_clone", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_raw_site_list_clone(IntPtr list, out IntPtr clone);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_raw_site_list_set_patients", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_raw_site_list_set_patients(IntPtr list, [MarshalAs(UnmanagedType.LPUTF8Str)] string patients);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_raw_site_list_add_site", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_raw_site_list_add_site(IntPtr list, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, ref Vec3 position, int patientIndex, int index);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_raw_site_list_update_mask", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_raw_site_list_update_mask(IntPtr list, int siteId, int mask);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_raw_site_list_get_count", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_raw_site_list_get_count(IntPtr list, out int count);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_raw_site_list_copy_sites_on_plane", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_raw_site_list_copy_sites_on_plane(IntPtr list, IntPtr plane, float precision, [Out] int[] result, int resultCapacity);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_raw_site_list_is_site_on_plane", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_raw_site_list_is_site_on_plane(IntPtr list, int siteId, IntPtr plane, float precision, out int result);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_raw_site_list_get_mars_atlas_label", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_raw_site_list_get_mars_atlas_label(IntPtr list, int siteId, out int label);
    }
}
