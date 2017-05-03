

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
        public int m_layer; /**< ROI layer */

        public int idSelectedBubble;

        private DLL.ROI m_dllROI; /**< associated ROI DLL */
        private List<GameObject> m_bubbles = new List<GameObject>(); /**< bubbles of the ROI */
        #endregion

        #region Private Methods
        void Awake()
        {
            m_dllROI = new DLL.ROI();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        public void clean()
        {
            // Destroy the DLL
            m_dllROI.Dispose();

            // Destroy each bubble gameobject
            for (int ii = 0; ii < m_bubbles.Count; ++ii)
            {
                Destroy(m_bubbles[ii]);
            }
        }
        /// <summary>
        /// Set the visibility of all the bubbles
        /// </summary>
        /// <param name="visibility"></param>
        public void set_visible(bool visibility)
        {
            for (int ii = 0; ii < m_bubbles.Count; ++ii)
            {
                m_bubbles[ii].SetActive(visibility);
            }
        }
        /// <summary>
        /// Enable of disable the rendering of the ROI
        /// </summary>
        /// <param name="state"></param>
        public void set_rendering_state(bool state)
        {
            int inactiveLayer = LayerMask.NameToLayer("Inactive");
            for (int ii = 0; ii < m_bubbles.Count; ++ii)
            {
                m_bubbles[ii].layer = (state ? m_layer : inactiveLayer);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int bubbles_nb()
        {
            return m_bubbles.Count;
        }
        /// <summary>
        /// Check if a collision occurs with the ROI bubbles
        /// </summary>
        /// <param name="ray"></param>
        /// <returns></returns>
        public bool check_collision(Ray ray)
        {
            for (int ii = 0; ii < m_bubbles.Count; ++ii)
            {
                RaycastHit hitInfo;
                if(m_bubbles[ii].GetComponent<Bubble>().check_collistion(ray, out hitInfo))
                    return true;
            }

            return false;
        }
        /// <summary>
        /// Update the DLL ROI mask
        /// </summary>
        /// <param name="plots"></param>
        /// <param name="mask"></param>
        public void update_mask(DLL.RawSiteList plots, bool[] mask)
        {
            m_dllROI.update_mask(plots, mask);
        }
        /// <summary>
        /// If collision with a bubble return the id of the closest, else return -1
        /// </summary>
        /// <param name="ray"></param>
        /// <returns></returns>
        public int collided_closest_bubble_id(Ray ray)
        {
            bool collision = false;
            int minDistId = -1;
            float minDist = float.MaxValue;

            for (int ii = 0; ii < m_bubbles.Count; ++ii)
            {
                RaycastHit hitInfo;
                if (m_bubbles[ii].GetComponent<Bubble>().check_collistion(ray, out hitInfo))
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
        public void unselect_bubble()
        {
            if (idSelectedBubble == -1 || idSelectedBubble > m_bubbles.Count) // no sphere selected
                return;

            m_bubbles[idSelectedBubble].GetComponent<Bubble>().unselect();
            idSelectedBubble = -1;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idBubble"></param>
        public void select_bubble(int idBubble)
        {
            if (idBubble < 0 || idBubble >= m_bubbles.Count)
                return;

            unselect_bubble();

            m_bubbles[idBubble].GetComponent<Bubble>().select();            
            idSelectedBubble = idBubble;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="GObubbleName"></param>
        /// <param name="position"></param>
        /// <param name="ray"></param>
        public void add_bubble(string layer, string GObubbleName, Vector3 position, float ray)
        {
            m_layer = LayerMask.NameToLayer(layer);
            GameObject newBubble = Instantiate(GlobalGOPreloaded.ROIBubble);
            newBubble.GetComponent<MeshFilter>().sharedMesh = SharedMeshes.ROIBubble;
            newBubble.name = GObubbleName;
            newBubble.transform.SetParent(transform);            
            newBubble.GetComponent<Bubble>().init(m_layer, ray, position);

            m_bubbles.Add(newBubble);

            // DLL
            Vector3 positionBubble = position;
            positionBubble.x = -positionBubble.x;
            m_dllROI.add_bubble(ray, positionBubble);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idBubble"></param>
        public void remove_bubble(int idBubble)
        {
            if (idSelectedBubble > idBubble)
                idSelectedBubble--;
            else if(idSelectedBubble == idBubble)
                idSelectedBubble = -1;

            // remove the bubble
            Destroy(m_bubbles[idBubble]);
            m_bubbles.RemoveAt(idBubble);

            // remove dll sphere
            m_dllROI.remove_bubble(idBubble);

            // if not we removed the selected bubble, select instead the last one
            if (idSelectedBubble == -1)
            {
                if(m_bubbles.Count > 0)
                    select_bubble(m_bubbles.Count - 1);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idBubble"></param>
        /// <param name="coeff"></param>
        public void change_bubble_size(int idBubble, float coeff)
        {
            if (idBubble < 0 || idBubble >= m_bubbles.Count)
                return;

            m_bubbles[idBubble].GetComponent<Bubble>().change_size(coeff);

            // DLL
            m_dllROI.update_bubble(idBubble, m_bubbles[idBubble].GetComponent<Bubble>().m_radius);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idBubble"></param>
        /// <returns></returns>
        public Bubble bubble(int idBubble)
        {
            return m_bubbles[idBubble].GetComponent<Bubble>();
        }
        /// <summary>
        /// Return a string containing all bubbles infos of the ROI
        /// </summary>
        /// <returns></returns>
        public string ROIbubbulesInfos()
        {
            string text = m_ROIname + "\n";
            for (int ii = 0; ii< m_bubbles.Count; ++ii)
            {
                Vector3 pos = m_bubbles[ii].transform.position;
                text += ii + " " + m_bubbles[ii].GetComponent<Bubble>().m_radius + " " + pos.x + " " + pos.y + " " + pos.z + "\n";
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
            public void add_bubble(float radius, Vector3 center)
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
            public void update_bubble(int index, float newRadius)
            {
                updateSphere_ROI(_handle, index, newRadius);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="index"></param>
            public void remove_bubble(int index)
            {
                removeSphere_ROI(_handle, index);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="sites"></param>
            /// <param name="mask"></param>
            public void update_mask(RawSiteList sites, bool[] mask)
            {
                for (int ii = 0; ii < sites.sites_nb(); ++ii)
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