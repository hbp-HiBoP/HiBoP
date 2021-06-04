using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Module3D.DLL
{
    public class IEEGGenerator : ActivityGenerator
    {
        #region Public Methods
        public void ComputeActivity(Column3DDynamic column)
        {
            compute_activity_IEEGGenerator(_handle, column.RawElectrodes.getHandle(), column.DynamicParameters.InfluenceDistance, column.ActivityValues, column.Timeline.Length, column.RawElectrodes.NumberOfSites, (int)ApplicationState.UserPreferences.Visualization._3D.SiteInfluenceByDistance);
        }
        public void ComputeActivityAtlas(Column3DCCEP column)
        {
            compute_activity_atlas_IEEGGenerator(_handle, column.ActivityValues, column.Timeline.Length, column.AreaMask, ApplicationState.Module3D.MarsAtlas.getHandle());
        }
        public void AdjustValues(Column3DDynamic column)
        {
            adjust_values_IEEGGenerator(_handle, column.DynamicParameters.Middle, column.DynamicParameters.SpanMin, column.DynamicParameters.SpanMax);
        }
        #endregion

        #region Memory Management
        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            _handle = new HandleRef(this, create_IEEGGenerator());
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            delete_IEEGGenerator(_handle);
        }
        #endregion

        #region DLLImport
        [DllImport("hbp_export", EntryPoint = "create_IEEGGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_IEEGGenerator();
        [DllImport("hbp_export", EntryPoint = "delete_IEEGGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_IEEGGenerator(HandleRef generator);
        [DllImport("hbp_export", EntryPoint = "compute_activity_IEEGGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void compute_activity_IEEGGenerator(HandleRef generator, HandleRef rawSiteList, float maxDistance, float[] activity, int timelineLength, int sitesNumber, int ratioDistance);
        [DllImport("hbp_export", EntryPoint = "compute_activity_atlas_IEEGGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void compute_activity_atlas_IEEGGenerator(HandleRef generator, float[] activity, int timelineLength, int[] mask, HandleRef marsAtlas);
        [DllImport("hbp_export", EntryPoint = "adjust_values_IEEGGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void adjust_values_IEEGGenerator(HandleRef generator, float middle, float min, float max);
        #endregion
    }
}