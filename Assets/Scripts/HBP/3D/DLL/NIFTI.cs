using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Module3D.DLL
{
    public class NIFTI : Tools.DLL.CppDLLImportBase
    {
        #region Properties
        public bool IsLoaded { get; private set; }
        #endregion

        #region Public Methods
        public bool Load(string path)
        {
            IsLoaded = (loadNiiFile_NIFTI(_handle, path) == 1);
            return IsLoaded;
        }
        public void FillVolumeWithNifti(Volume volume, int t)
        {
            fill_volume_NIFTI(_handle, volume.getHandle(), t);
        }
        #endregion

        #region Memory Management
        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            _handle = new HandleRef(this, create_NIFTI());
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            delete_NIFTI(_handle);
        }
        #endregion

        #region DLLimport
        [DllImport("hbp_export", EntryPoint = "create_NIFTI", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_NIFTI();
        [DllImport("hbp_export", EntryPoint = "delete_NIFTI", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_NIFTI(HandleRef handleVolume);
        [DllImport("hbp_export", EntryPoint = "loadNiiFile_NIFTI", CallingConvention = CallingConvention.Cdecl)]
        static private extern int loadNiiFile_NIFTI(HandleRef handleNii, string pathFile);
        [DllImport("hbp_export", EntryPoint = "fill_volume_NIFTI", CallingConvention = CallingConvention.Cdecl)]
        static private extern void fill_volume_NIFTI(HandleRef handleNii, HandleRef handleVolume, int t);
        #endregion
    }
}