using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Module3D.DLL
{
    public class DensityGenerator : ActivityGenerator
    {
        #region Public Methods
        public void ComputeActivity(Column3DAnatomy column)
        {
            compute_activity_DensityGenerator(_handle, column.RawElectrodes.getHandle(), column.AnatomyParameters.InfluenceDistance, (int)ApplicationState.UserPreferences.Visualization._3D.SiteInfluenceByDistance);
        }
        #endregion

        #region Memory Management
        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            _handle = new HandleRef(this, create_DensityGenerator());
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            delete_DensityGenerator(_handle);
        }
        #endregion

        #region DLLImport
        [DllImport("hbp_export", EntryPoint = "create_DensityGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_DensityGenerator();
        [DllImport("hbp_export", EntryPoint = "delete_DensityGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_DensityGenerator(HandleRef generator);
        [DllImport("hbp_export", EntryPoint = "compute_activity_DensityGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void compute_activity_DensityGenerator(HandleRef generator, HandleRef rawSiteList, float maxDistance, int ratioDistance);
        #endregion
    }
}