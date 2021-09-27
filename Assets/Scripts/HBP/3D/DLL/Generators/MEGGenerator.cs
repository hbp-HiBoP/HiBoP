using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Module3D.DLL
{
    public class MEGGenerator : ActivityGenerator
    {
        #region Public Methods
        public void ComputeActivity(Column3DMEG column)
        {
            HandleRef[] volumeHandleRefs = column.ColumnMEGData.Data.FMRIs.SelectMany(fmri => fmri.Item1.Volumes).Select(v => v.getHandle()).ToArray();
            compute_activity_MEGGenerator(_handle, volumeHandleRefs, volumeHandleRefs.Length);
        }
        public void AdjustValues(Column3DMEG column)
        {
            adjust_values_MEGGenerator(_handle, column.MEGParameters.FMRINegativeCalMinFactor, column.MEGParameters.FMRINegativeCalMaxFactor, column.MEGParameters.FMRIPositiveCalMinFactor, column.MEGParameters.FMRIPositiveCalMaxFactor);
        }
        public void HideExtremeValues(Column3DMEG column)
        {
            set_hide_values_MEGGenerator(_handle, column.MEGParameters.HideLowerValues, column.MEGParameters.HideMiddleValues, column.MEGParameters.HideHigherValues);
        }
        #endregion

        #region Memory Management
        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            _handle = new HandleRef(this, create_MEGGenerator());
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            delete_MEGGenerator(_handle);
        }
        #endregion

        #region DLLImport
        [DllImport("hbp_export", EntryPoint = "create_MEGGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_MEGGenerator();
        [DllImport("hbp_export", EntryPoint = "delete_MEGGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_MEGGenerator(HandleRef generator);
        [DllImport("hbp_export", EntryPoint = "compute_activity_MEGGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void compute_activity_MEGGenerator(HandleRef generator, HandleRef[] volumes, int volumesNumber);
        [DllImport("hbp_export", EntryPoint = "adjust_values_MEGGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void adjust_values_MEGGenerator(HandleRef generator, float negativeMin, float negativeMax, float positiveMin, float positiveMax);
        [DllImport("hbp_export", EntryPoint = "set_hide_values_MEGGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void set_hide_values_MEGGenerator(HandleRef generator, bool lower, bool middle, bool higher);
        #endregion
    }
}