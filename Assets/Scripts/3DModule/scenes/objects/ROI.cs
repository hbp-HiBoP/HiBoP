

/**
 * \file    ROI.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define Bubble and ROI classes
 */

// system
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// unity
using UnityEngine;

namespace HBP.VISU3D
{
    /// <summary>
    /// Define a ROI containing bubbles
    /// </summary>
    public class ROI : MonoBehaviour
    {
        #region members

        public string m_name = "default_ROI_name";
        public int m_layer; /**< ROI layer */

        public int m_idSelectedBubble; /**< id of the selected bubble of the ROI */
        public int idSelectedBubble { get { return m_idSelectedBubble; } }

        private DLL.ROI m_dllROI; /**< associated ROI DLL */
        private List<GameObject> m_bubbles = new List<GameObject>(); /**< bubbles of the ROI */

        #endregion members

        #region mono_behaviour

        /// <summary>
        /// This function is always called before any Start functions and also just after a prefab is instantiated. (If a GameObject is inactive during start up Awake is not called until it is made active.)
        /// </summary>
        void Awake()
        {
            // init dll
            m_dllROI = new DLL.ROI();
        }

        #endregion mono_behaviour

        #region others

        /// <summary>
        /// Clean the ROI
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
        public void setVisible(bool visibility)
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
        public void setRenderingState(bool state)
        {
            int inactiveLayer = LayerMask.NameToLayer("Inactive");
            for (int ii = 0; ii < m_bubbles.Count; ++ii)
            {
                m_bubbles[ii].layer = (state ? m_layer : inactiveLayer);
            }
        }

        /// <summary>
        /// Return the number of ROI spheres
        /// </summary>
        /// <returns></returns>
        public int getNbSpheres()
        {
            return m_bubbles.Count;
        }

        /// <summary>
        /// Check if a collision occurs with the ROI spheres
        /// </summary>
        /// <param name="ray"></param>
        /// <returns></returns>
        public bool checkCollision(Ray ray)
        {
            for (int ii = 0; ii < m_bubbles.Count; ++ii)
            {
                RaycastHit hitInfo;
                if(m_bubbles[ii].GetComponent<Bubble>().checkCollision(ray, out hitInfo))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Update the DLL ROI mask
        /// </summary>
        /// <param name="plots"></param>
        /// <param name="mask"></param>
        public void updateMask(DLL.RawPlotList plots, bool[] mask)
        {
            m_dllROI.updateMask(plots, mask);
        }

        /// <summary>
        /// If collision with a bubble return the id of the closest, else return -1
        /// </summary>
        /// <param name="ray"></param>
        /// <returns></returns>
        public int collidedClosestSphereId(Ray ray)
        {
            bool collision = false;
            int minDistId = -1;
            float minDist = float.MaxValue;

            for (int ii = 0; ii < m_bubbles.Count; ++ii)
            {
                RaycastHit hitInfo;
                if (m_bubbles[ii].GetComponent<Bubble>().checkCollision(ray, out hitInfo))
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
        /// Unselect the current selected bubble
        /// </summary>
        public void unselectBubble()
        {
            if (m_idSelectedBubble == -1 || m_idSelectedBubble > m_bubbles.Count) // no sphere selected
                return;

            m_bubbles[m_idSelectedBubble].GetComponent<Bubble>().unselect();
            m_idSelectedBubble = -1;
        }

        /// <summary>
        /// Select the bubble corresponding to the input id
        /// </summary>
        /// <param name="idBubble"></param>
        public void selectBubble(int idBubble)
        {
            if (idBubble < 0 || idBubble >= m_bubbles.Count)
                return;

            unselectBubble();

            m_bubbles[idBubble].GetComponent<Bubble>().select();            
            m_idSelectedBubble = idBubble;
        }

        /// <summary>
        /// Add a bubble to the ROI
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="name"></param>
        /// <param name="position"></param>
        /// <param name="ray"></param>
        public void addBubble(string layer, string name, Vector3 position, float ray)
        {
            m_layer = LayerMask.NameToLayer(layer);
            GameObject newBubble = Instantiate(BaseGameObjects.ROIBubble);
            newBubble.GetComponent<MeshFilter>().sharedMesh = SharedMeshes.ROIBubble;
            newBubble.name = name;
            newBubble.transform.SetParent(transform);            
            newBubble.GetComponent<Bubble>().init(m_layer, ray, position);

            m_bubbles.Add(newBubble);


            // DLL
            Vector3 positionBubble = position;
            positionBubble.x = -positionBubble.x;
            m_dllROI.addSphere(ray, positionBubble);

        }

        /// <summary>
        /// Remove the bubble corresponding to the input id from the ROI
        /// </summary>
        /// <param name="idBubble"></param>
        public void removeBubble(int idBubble)
        {
            if (m_idSelectedBubble > idBubble)
                m_idSelectedBubble--;
            else if(m_idSelectedBubble == idBubble)
                m_idSelectedBubble = -1;

            // remove the bubble
            Destroy(m_bubbles[idBubble]);
            m_bubbles.RemoveAt(idBubble);

            // remove dll sphere
            m_dllROI.removeSphere(idBubble);

            // if not we removed the selected bubble, select instead the last one
            if (m_idSelectedBubble == -1)
            {
                if(m_bubbles.Count > 0)
                    selectBubble(m_bubbles.Count - 1);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="idBubble"></param>
        /// <param name="coeff"></param>
        public void changeBubbleSize(int idBubble, float coeff)
        {
            if (idBubble < 0 || idBubble >= m_bubbles.Count)
                return;

            m_bubbles[idBubble].GetComponent<Bubble>().changeSize(coeff);

            // DLL
            m_dllROI.updateSphere(idBubble, m_bubbles[idBubble].GetComponent<Bubble>().m_radius);
        }

        /// <summary>
        /// Get the bubble corresponding to the input id
        /// </summary>
        /// <param name="idBubble"></param>
        /// <returns></returns>
        public Bubble getBubble(int idBubble)
        {
            return m_bubbles[idBubble].GetComponent<Bubble>();
        }

        /// <summary>
        /// Return a string contaiing all bubbles infos of the ROI
        /// </summary>
        /// <returns></returns>
        public string getROIStr()
        {
            string text = m_name + "\n";
            for (int ii = 0; ii< m_bubbles.Count; ++ii)
            {
                Vector3 pos = m_bubbles[ii].transform.position;
                text += ii + " " + m_bubbles[ii].GetComponent<Bubble>().m_radius + " " + pos.x + " " + pos.y + " " + pos.z + "\n";
            }

            return text;
        }

        #endregion others
    }

    namespace DLL
    {
        /// <summary>
        ///  A ROI_dll interface class (see C++ documentation for more details)
        /// </summary>
        public class ROI : IDisposable
        {
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
            static private extern bool isInside_ROI(HandleRef handleROI, HandleRef handleRawList, int id);

            #endregion DLLImport

            #region memory_management

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

            #endregion memory_management

            #region functions

            /// <summary>
            /// Add a sphere in the ROI
            /// </summary>
            /// <param name="radius"></param>
            /// <param name="center"></param>
            public void addSphere(float radius, Vector3 center)
            {
                float[] centerArray = new float[3];
                centerArray[0] = center.x;
                centerArray[1] = center.y;
                centerArray[2] = center.z;
                addSphere_ROI(_handle, radius, centerArray);
            }

            /// <summary>
            /// Update the sphere corresponding to the index
            /// </summary>
            /// <param name="index"></param>
            /// <param name="newRadius"></param>
            public void updateSphere(int index, float newRadius)
            {
                updateSphere_ROI(_handle, index, newRadius);
            }

            /// <summary>
            /// Remove the sphere corresponding to the index
            /// </summary>
            /// <param name="index"></param>
            public void removeSphere(int index)
            {
                removeSphere_ROI(_handle, index);
            }

            /// <summary>
            /// Check if the input point is inside the ROI
            /// </summary>
            /// <param name="pt"></param>
            /// <returns>true if inside else false</returns>
            //public bool isInside(Vector3 pt)
            //{
            //    float[] ptArray = new float[3];
            //    ptArray[0] = pt.x;
            //    ptArray[1] = pt.y;
            //    ptArray[2] = pt.z;
            //    return isInside_ROI(_handle, ptArray);
            //}

            public void updateMask(RawPlotList plots, bool[] mask)
            {
                //string t = "";
                for (int ii = 0; ii < plots.getPlotsNumber(); ++ii)
                {
                    mask[ii] = !isInside_ROI(_handle, plots.getHandle(), ii);
                    //t += mask[ii] + " ";
                }
                //Debug.Log("mask  " + t);
            }

            #endregion functions
        }
    }
}