

/**
 * \file    ROI.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define Bubble and ROI classes
 */

// system
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// unity
using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// Define a ROI containing bubbles
    /// </summary>
    public class ROI : MonoBehaviour
    {
        #region Properties
        public string m_ROIname = "default_ROI_name";
        public int m_Layer; /**< ROI layer */

        public int SelectedBubbleID;

        private DLL.ROI m_DLLROI; /**< associated ROI DLL */
        private List<GameObject> m_Bubbles = new List<GameObject>(); /**< bubbles of the ROI */
        #endregion

        #region Private Methods
        void Awake()
        {
            m_DLLROI = new DLL.ROI();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        public void Clean()
        {
            // Destroy the DLL
            m_DLLROI.Dispose();

            // Destroy each bubble gameobject
            for (int ii = 0; ii < m_Bubbles.Count; ++ii)
            {
                Destroy(m_Bubbles[ii]);
            }
        }
        /// <summary>
        /// Set the visibility of all the bubbles
        /// </summary>
        /// <param name="visibility"></param>
        public void SetVisibility(bool visibility)
        {
            for (int ii = 0; ii < m_Bubbles.Count; ++ii)
            {
                m_Bubbles[ii].SetActive(visibility);
            }
        }
        /// <summary>
        /// Enable of disable the rendering of the ROI
        /// </summary>
        /// <param name="state"></param>
        public void SetRenderingState(bool state)
        {
            int inactiveLayer = LayerMask.NameToLayer("Inactive");
            for (int ii = 0; ii < m_Bubbles.Count; ++ii)
            {
                m_Bubbles[ii].layer = (state ? m_Layer : inactiveLayer);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int NumberOfBubbles()
        {
            return m_Bubbles.Count;
        }
        /// <summary>
        /// Check if a collision occurs with the ROI bubbles
        /// </summary>
        /// <param name="ray"></param>
        /// <returns></returns>
        public bool CheckCollision(Ray ray)
        {
            for (int ii = 0; ii < m_Bubbles.Count; ++ii)
            {
                RaycastHit hitInfo;
                if(m_Bubbles[ii].GetComponent<Bubble>().CheckCollision(ray, out hitInfo))
                    return true;
            }

            return false;
        }
        /// <summary>
        /// Update the DLL ROI mask
        /// </summary>
        /// <param name="plots"></param>
        /// <param name="mask"></param>
        public void UpdateMask(DLL.RawSiteList plots, bool[] mask)
        {
            m_DLLROI.UpdateMask(plots, mask);
        }
        /// <summary>
        /// If collision with a bubble return the id of the closest, else return -1
        /// </summary>
        /// <param name="ray"></param>
        /// <returns></returns>
        public int CollidedClosestBubbleID(Ray ray)
        {
            bool collision = false;
            int minDistId = -1;
            float minDist = float.MaxValue;

            for (int ii = 0; ii < m_Bubbles.Count; ++ii)
            {
                RaycastHit hitInfo;
                if (m_Bubbles[ii].GetComponent<Bubble>().CheckCollision(ray, out hitInfo))
                {
                    collision = true;

                    Vector3 p1 = hitInfo.point;
                    Vector3 p2 = ray.origin;
                    Vector3 vec = new Vector3(p1.x - p2.x, p1.y - p2.y, p1.z - p2.z);
                    float squareDist = vec[0] * vec[0] + vec[1] * vec[1] + vec[2] * vec[2];

                    if (squareDist < minDist)
                    {
                        minDist = squareDist;
                        minDistId = ii;
                    }
                }
            }

            if (collision)
            {
                return minDistId;
            }

            return -1;
        }
        /// <summary>
        /// 
        /// </summary>
        public void UnselectBubble()
        {
            if (SelectedBubbleID == -1 || SelectedBubbleID > m_Bubbles.Count) // no sphere selected
                return;

            m_Bubbles[SelectedBubbleID].GetComponent<Bubble>().Selected = false;
            SelectedBubbleID = -1;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idBubble"></param>
        public void SelectBubble(int idBubble)
        {
            if (idBubble < 0 || idBubble >= m_Bubbles.Count)
                return;

            UnselectBubble();

            m_Bubbles[idBubble].GetComponent<Bubble>().Selected = true;           
            SelectedBubbleID = idBubble;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="GObubbleName"></param>
        /// <param name="position"></param>
        /// <param name="ray"></param>
        public void AddBubble(string layer, string GObubbleName, Vector3 position, float ray)
        {
            m_Layer = LayerMask.NameToLayer(layer);
            GameObject newBubble = Instantiate(GlobalGOPreloaded.ROIBubble);
            newBubble.GetComponent<MeshFilter>().sharedMesh = SharedMeshes.ROIBubble;
            newBubble.name = GObubbleName;
            newBubble.transform.SetParent(transform);            
            newBubble.GetComponent<Bubble>().Initialize(m_Layer, ray, position);

            m_Bubbles.Add(newBubble);

            // DLL
            Vector3 positionBubble = position;
            positionBubble.x = -positionBubble.x;
            m_DLLROI.AddBubble(ray, positionBubble);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idBubble"></param>
        public void RemoveBubble(int idBubble)
        {
            if (SelectedBubbleID > idBubble)
                SelectedBubbleID--;
            else if(SelectedBubbleID == idBubble)
                SelectedBubbleID = -1;

            // remove the bubble
            Destroy(m_Bubbles[idBubble]);
            m_Bubbles.RemoveAt(idBubble);

            // remove dll sphere
            m_DLLROI.RemoveBubble(idBubble);

            // if not we removed the selected bubble, select instead the last one
            if (SelectedBubbleID == -1)
            {
                if(m_Bubbles.Count > 0)
                    SelectBubble(m_Bubbles.Count - 1);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idBubble"></param>
        /// <param name="coeff"></param>
        public void ChangeBubbleSize(int idBubble, float coeff)
        {
            if (idBubble < 0 || idBubble >= m_Bubbles.Count)
                return;

            m_Bubbles[idBubble].GetComponent<Bubble>().Radius *= coeff;

            // DLL
            m_DLLROI.UpdateBubble(idBubble, m_Bubbles[idBubble].GetComponent<Bubble>().Radius);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idBubble"></param>
        /// <returns></returns>
        public Bubble Bubble(int idBubble)
        {
            return m_Bubbles[idBubble].GetComponent<Bubble>();
        }
        /// <summary>
        /// Return a string containing all bubbles infos of the ROI
        /// </summary>
        /// <returns></returns>
        public string BubblesInformationIntoString()
        {
            string text = m_ROIname + "\n";
            for (int ii = 0; ii< m_Bubbles.Count; ++ii)
            {
                Vector3 pos = m_Bubbles[ii].transform.position;
                text += ii + " " + m_Bubbles[ii].GetComponent<Bubble>().Radius + " " + pos.x + " " + pos.y + " " + pos.z + "\n";
            }

            return text;
        }
        #endregion
    }

    namespace DLL
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
                for (int ii = 0; ii < sites.NumberOfSites(); ++ii)
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
}