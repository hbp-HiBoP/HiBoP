using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Core.DLL
{
    /// <summary>
    /// Class used to give information to the dll about the ROIs in the scene
    /// </summary>
    public class ROI : CppDLLImportBase
    {
        #region Public Methods
        /// <summary>
        /// Add a sphere to this ROI
        /// </summary>
        /// <param name="radius">Radius of the sphere</param>
        /// <param name="center">Center of the sphere</param>
        public void AddSphere(float radius, Vector3 center)
        {
            float[] centerArray = new float[3];
            centerArray[0] = center.x;
            centerArray[1] = center.y;
            centerArray[2] = center.z;
            addSphere_ROI(_handle, radius, centerArray);
        }
        /// <summary>
        /// Notify the dll that the radius of a sphere changed
        /// </summary>
        /// <param name="index">Index of the sphere</param>
        /// <param name="newRadius">New value for the radius</param>
        public void UpdateSphereRadius(int index, float newRadius)
        {
            updateSphere_ROI(_handle, index, newRadius);
        }
        /// <summary>
        /// Notify the dll that the position of the sphere changed
        /// </summary>
        /// <param name="index">Index of the sphere</param>
        /// <param name="position">New value for the center of the sphere</param>
        public void UpdateSpherePosition(int index, Vector3 position)
        {
            float[] positionArray = new float[3];
            positionArray[0] = position.x;
            positionArray[1] = position.y;
            positionArray[2] = position.z;
            updateSpherePosition_ROI(_handle, index, positionArray);
        }
        /// <summary>
        /// Remove the sphere from this ROI
        /// </summary>
        /// <param name="index">Index of the sphere to remove</param>
        public void RemoveSphere(int index)
        {
            removeSphere_ROI(_handle, index);
        }
        /// <summary>
        /// Update the mask of the sites using this ROI
        /// </summary>
        /// <param name="sites">List of the sites of the scene</param>
        /// <param name="mask">Array containing the mask value for each site (true if the site is masked)</param>
        public void UpdateMask(RawSiteList sites, bool[] mask)
        {
            for (int ii = 0; ii < sites.NumberOfSites; ++ii)
                mask[ii] = isInside_ROI(_handle, sites.getHandle(), ii) != 1;
        }
        #endregion

        #region Memory Management
        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            _handle = new HandleRef(this, create_ROI());
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            delete_ROI(_handle);
        }
        #endregion

        #region DLLImport
        [DllImport("hbp_export", EntryPoint = "create_ROI", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_ROI();
        [DllImport("hbp_export", EntryPoint = "delete_ROI", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_ROI(HandleRef handleROI);
        [DllImport("hbp_export", EntryPoint = "addSphere_ROI", CallingConvention = CallingConvention.Cdecl)]
        static private extern void addSphere_ROI(HandleRef handleROI, float radius, float[] center);
        [DllImport("hbp_export", EntryPoint = "updateSphere_ROI", CallingConvention = CallingConvention.Cdecl)]
        static private extern void updateSphere_ROI(HandleRef handleROI, int index, float newRadius);
        [DllImport("hbp_export", EntryPoint = "updateSpherePosition_ROI", CallingConvention = CallingConvention.Cdecl)]
        static private extern void updateSpherePosition_ROI(HandleRef handleROI, int index, float[] center);
        [DllImport("hbp_export", EntryPoint = "removeSphere_ROI", CallingConvention = CallingConvention.Cdecl)]
        static private extern void removeSphere_ROI(HandleRef handleROI, int index);
        [DllImport("hbp_export", EntryPoint = "isInside_ROI", CallingConvention = CallingConvention.Cdecl)]
        static private extern int isInside_ROI(HandleRef handleROI, HandleRef handleRawList, int id);
        #endregion
    }
}