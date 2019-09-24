using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Module3D.DLL
{
    /// <summary>
    ///  A ROI_dll interface class (see C++ documentation for more details)
    /// </summary>
    public class ROI : Tools.DLL.CppDLLImportBase, IDisposable
    {
        #region Memory Management
        protected override void create_DLL_class()
        {
            _handle = new HandleRef(this, create_ROI());
        }
        protected override void delete_DLL_class()
        {
            delete_ROI(_handle);
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
        public void UpdateBubbleRadius(int index, float newRadius)
        {
            updateSphere_ROI(_handle, index, newRadius);
        }

        public void UpdateBubblePosition(int index, Vector3 position)
        {
            float[] positionArray = new float[3];
            positionArray[0] = position.x;
            positionArray[1] = position.y;
            positionArray[2] = position.z;
            updateSpherePosition_ROI(_handle, index, positionArray);
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

        [DllImport("hbp_export", EntryPoint = "updateSpherePosition_ROI", CallingConvention = CallingConvention.Cdecl)]
        static private extern void updateSpherePosition_ROI(HandleRef handleROI, int index, float[] center);

        [DllImport("hbp_export", EntryPoint = "removeSphere_ROI", CallingConvention = CallingConvention.Cdecl)]
        static private extern void removeSphere_ROI(HandleRef handleROI, int index);

        [DllImport("hbp_export", EntryPoint = "isInside_ROI", CallingConvention = CallingConvention.Cdecl)]
        static private extern int isInside_ROI(HandleRef handleROI, HandleRef handleRawList, int id);
        #endregion
    }
}