using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace HBP.Core.DLL
{
    public class FMRIGenerator : ActivityGenerator
    {
        #region Public Methods
        public void ComputeActivity(IEnumerable<(Volume, Volume)> volumesAndMasks)
        {
            MultiVolume multiVolume = new MultiVolume();
            MultiVolume maskMultiVolume = new MultiVolume();
            foreach (var volumeAndMask in volumesAndMasks)
            {
                multiVolume.AddVolume(volumeAndMask.Item1);
                maskMultiVolume.AddVolume(volumeAndMask.Item2);
            }
            compute_activity_FMRIGenerator(_handle, multiVolume.getHandle(), maskMultiVolume.getHandle());
        }
        public void AdjustValues(float fmriNegativeCalMinFactor, float fmriNegativeCalMaxFactor, float fmriPositiveCalMinFactor, float fmriPositiveCalMaxFactor)
        {
            adjust_values_FMRIGenerator(_handle, fmriNegativeCalMinFactor, fmriNegativeCalMaxFactor, fmriPositiveCalMinFactor, fmriPositiveCalMaxFactor);
        }
        public void HideExtremeValues(bool hideLower, bool hideMiddle, bool hideHigher)
        {
            set_hide_values_FMRIGenerator(_handle, hideLower, hideMiddle, hideHigher);
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
        static private extern void compute_activity_FMRIGenerator(HandleRef generator, HandleRef multiVolume, HandleRef maskMultiVolume);
        [DllImport("hbp_export", EntryPoint = "adjust_values_FMRIGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void adjust_values_FMRIGenerator(HandleRef generator, float negativeMin, float negativeMax, float positiveMin, float positiveMax);
        [DllImport("hbp_export", EntryPoint = "set_hide_values_FMRIGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void set_hide_values_FMRIGenerator(HandleRef generator, bool lower, bool middle, bool higher);
        #endregion
    }
}