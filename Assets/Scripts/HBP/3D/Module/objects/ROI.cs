

/**
 * \file    ROI.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define Bubble and ROI classes
 */

// system
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public static string DEFAULT_ROI_NAME = "ROI";
        private string m_Name = DEFAULT_ROI_NAME;
        public string Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                m_Name = value;
            }
        }
        private int m_Layer;

        public int SelectedBubbleID { get; set; }

        private DLL.ROI m_DLLROI;
        private List<Bubble> m_Bubbles = new List<Bubble>();
        public ReadOnlyCollection<Bubble> Bubbles
        {
            get
            {
                return new ReadOnlyCollection<Bubble>(m_Bubbles);
            }
        }

        /// <summary>
        /// Number of bubbles in ROI
        /// </summary>
        public int NumberOfBubbles
        {
            get
            {
                return m_Bubbles.Count;
            }
        }
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
                m_Bubbles[ii].gameObject.SetActive(visibility);
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
                m_Bubbles[ii].gameObject.layer = (state ? m_Layer : inactiveLayer);
            }
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
            ApplicationState.Module3D.OnSelectROIVolume.Invoke();
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
            ApplicationState.Module3D.OnSelectROIVolume.Invoke();
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
            Bubble bubble = newBubble.GetComponent<Bubble>();
            bubble.Initialize(m_Layer, ray, position);

            m_Bubbles.Add(bubble);

            // DLL
            Vector3 positionBubble = position;
            positionBubble.x = -positionBubble.x;
            m_DLLROI.AddBubble(ray, positionBubble);

            ApplicationState.Module3D.OnChangeNumberOfVolumeInROI.Invoke();
            SelectBubble(m_Bubbles.Count - 1);
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
            Destroy(m_Bubbles[idBubble].gameObject);
            m_Bubbles.RemoveAt(idBubble);

            // remove dll sphere
            m_DLLROI.RemoveBubble(idBubble);

            ApplicationState.Module3D.OnChangeNumberOfVolumeInROI.Invoke();

            // if not we removed the selected bubble, select instead the last one
            if (SelectedBubbleID == -1)
            {
                if(m_Bubbles.Count > 0)
                    SelectBubble(m_Bubbles.Count - 1);
            }
        }
        public void RemoveSelectedBubble()
        {
            RemoveBubble(SelectedBubbleID);
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

            ApplicationState.Module3D.OnChangeROIVolumeRadius.Invoke();
        }
        public void ChangeSelectedBubbleSize(float direction)
        {
            ChangeBubbleSize(SelectedBubbleID, direction < 0 ? 0.9f : 1.1f);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idBubble"></param>
        /// <returns></returns>
        public Bubble GetBubbleByIndex(int idBubble)
        {
            return m_Bubbles[idBubble].GetComponent<Bubble>();
        }
        /// <summary>
        /// Return a string containing all bubbles infos of the ROI
        /// </summary>
        /// <returns></returns>
        public string BubblesInformationIntoString()
        {
            string text = m_Name + "\n";
            for (int ii = 0; ii< m_Bubbles.Count; ++ii)
            {
                Vector3 pos = m_Bubbles[ii].transform.position;
                text += ii + " " + m_Bubbles[ii].GetComponent<Bubble>().Radius + " " + pos.x + " " + pos.y + " " + pos.z + "\n";
            }

            return text;
        }
        #endregion
    }
}