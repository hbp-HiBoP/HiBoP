using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Module3D.DLL
{
    /// <summary>
    ///  A ROI_dll interface class (see C++ documentation for more details)
    /// </summary>
    public class ROI : IDisposable
    {
        #region Memory Management
        /// <summary>
        /// pointer to C+ dll class
        /// </summary>
        private HandleRef _handle;
        /// <summary>
        /// ROI_dll constructor
        /// </summary>
        public ROI()
        {
            _handle = new HandleRef(this, create_ROI());
        }
        /// <summary>
        /// delete_ROI Destructor
        /// </summary>
        ~ROI()
        {
            Cleanup();
        }
        /// <summary>
        /// Force delete C++ DLL data (remove GC for this object)
        /// </summary>
        public void Dispose()
        {
            Cleanup();
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Delete C+ DLL data, and set handle to IntPtr.Zero
        /// </summary>
        private void Cleanup()
        {
            delete_ROI(_handle);
            _handle = new HandleRef(this, IntPtr.Zero);
        }
        /// <summary>
        /// Return pointer to C++ DLL
        /// </summary>
        /// <returns></returns>
        public HandleRef getHandle()
        {
            return _handle;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="center"></param>
        public void AddBubble(float radius, Vector3 center)
        {
            float[] centerArray = new float[3];
            centerArray[0] = center.x;
            centerArray[1] = center.y;
            centerArray[2] = center.z;
            addSphere_ROI(_handle, radius, centerArray);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="newRadius"></param>
        public void UpdateBubble(int index, float newRadius)
        {
            updateSphere_ROI(_handle, index, newRadius);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        public void RemoveBubble(int index)
        {
            removeSphere_ROI(_handle, index);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sites"></param>
        /// <param name="mask"></param>
        public void UpdateMask(RawSiteList sites, bool[] mask)
        {
            for (int ii = 0; ii < sites.NumberOfSites; ++ii)
                mask[ii] = isInside_ROI(_handle, sites.getHandle(), ii) != 1;
        }
        #endregion

        #region DLLImport

        //  memory management
        [DllImport("hbp_export", EntryPoint = "create_ROI", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_ROI();

        [DllImport("hbp_export", EntryPoint = "delete_ROI", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_ROI(HandleRef handleROI);

        // actions
        [DllImport("hbp_export", EntryPoint = "addSphere_ROI", CallingConvention = CallingConvention.Cdecl)]
        static private extern void addSphere_ROI(HandleRef handleROI, float radius, float[] center);

        [DllImport("hbp_export", EntryPoint = "updateSphere_ROI", CallingConvention = CallingConvention.Cdecl)]
        static private extern void updateSphere_ROI(HandleRef handleROI, int index, float newRadius);

        [DllImport("hbp_export", EntryPoint = "removeSphere_ROI", CallingConvention = CallingConvention.Cdecl)]
        static private extern void removeSphere_ROI(HandleRef handleROI, int index);

        [DllImport("hbp_export", EntryPoint = "isInside_ROI", CallingConvention = CallingConvention.Cdecl)]
        static private extern int isInside_ROI(HandleRef handleROI, HandleRef handleRawList, int id);

        #endregion
    }
}