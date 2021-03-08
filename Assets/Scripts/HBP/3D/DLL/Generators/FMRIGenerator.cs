using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Module3D.DLL
{
    public class FMRIGenerator : ActivityGenerator
    {
        #region Public Methods
        public void ComputeActivity(Column3DFMRI column)
        {
            HandleRef[] volumeHandleRefs = column.ColumnFMRIData.Data.FMRIs.SelectMany(fmri => fmri.Volumes).Select(v => v.getHandle()).ToArray();
            compute_activity_FMRIGenerator(_handle, volumeHandleRefs, volumeHandleRefs.Length);
        }
        public void AdjustValues(Column3DFMRI column)
        {
            adjust_values_FMRIGenerator(_handle, column.FMRIParameters.FMRINegativeCalMinFactor, column.FMRIParameters.FMRINegativeCalMaxFactor, column.FMRIParameters.FMRIPositiveCalMinFactor, column.FMRIParameters.FMRIPositiveCalMaxFactor);
        }
        #endregion

        #region Memory Management
        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            _handle = new HandleRef(this, create_FMRIGenerator());
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            delete_FMRIGenerator(_handle);
        }
        #endregion

        #region DLLImport
        [DllImport("hbp_export", EntryPoint = "create_FMRIGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_FMRIGenerator();
        [DllImport("hbp_export", EntryPoint = "delete_FMRIGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_FMRIGenerator(HandleRef generator);
        [DllImport("hbp_export", EntryPoint = "compute_activity_FMRIGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void compute_activity_FMRIGenerator(HandleRef generator, HandleRef[] volumes, int volumesNumber);
        [DllImport("hbp_export", EntryPoint = "adjust_values_FMRIGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void adjust_values_FMRIGenerator(HandleRef generator, float negativeMin, float negativeMax, float positiveMin, float positiveMax);
        #endregion
    }
}