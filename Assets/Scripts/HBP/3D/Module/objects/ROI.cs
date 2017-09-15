

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
using System.Runtime.Serialization;

// unity
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Module3D
{
    /// <summary>
    /// Define a ROI containing bubbles
    /// </summary>
    public class ROI : MonoBehaviour
    {
        #region Properties
        public const string DEFAULT_ROI_NAME = "ROI";
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

        public int SelectedSphereID { get; set; }

        private DLL.ROI m_DLLROI;
        private List<Sphere> m_Spheres = new List<Sphere>();
        public ReadOnlyCollection<Sphere> Spheres
        {
            get
            {
                return new ReadOnlyCollection<Sphere>(m_Spheres);
            }
        }

        /// <summary>
        /// Number of bubbles in ROI
        /// </summary>
        public int NumberOfBubbles
        {
            get
            {
                return m_Spheres.Count;
            }
        }

        public UnityEvent OnChangeNumberOfVolumeInROI = new UnityEvent();
        public UnityEvent OnChangeROIVolumeRadius = new UnityEvent();

        [SerializeField]
        private GameObject m_SpherePrefab;
        #endregion

        #region Private Methods
        void Awake()
        {
            m_DLLROI = new DLL.ROI();
        }
        /// <summary>
        /// 
        /// </summary>
        private void UnselectSphere()
        {
            if (SelectedSphereID == -1 || SelectedSphereID > m_Spheres.Count) // no sphere selected
                return;

            m_Spheres[SelectedSphereID].GetComponent<Sphere>().Selected = false;
            SelectedSphereID = -1;
            ApplicationState.Module3D.OnSelectROIVolume.Invoke();
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
            for (int ii = 0; ii < m_Spheres.Count; ++ii)
            {
                Destroy(m_Spheres[ii]);
            }
        }
        /// <summary>
        /// Set the visibility of all the bubbles
        /// </summary>
        /// <param name="visibility"></param>
        public void SetVisibility(bool visibility)
        {
            for (int ii = 0; ii < m_Spheres.Count; ++ii)
            {
                m_Spheres[ii].gameObject.SetActive(visibility);
            }
        }
        /// <summary>
        /// Enable of disable the rendering of the ROI
        /// </summary>
        /// <param name="state"></param>
        public void SetRenderingState(bool state)
        {
            int inactiveLayer = LayerMask.NameToLayer("Inactive");
            for (int ii = 0; ii < m_Spheres.Count; ++ii)
            {
                m_Spheres[ii].gameObject.layer = (state ? m_Layer : inactiveLayer);
            }
        }
        /// <summary>
        /// Check if a collision occurs with the ROI bubbles
        /// </summary>
        /// <param name="ray"></param>
        /// <returns></returns>
        public bool CheckCollision(Ray ray)
        {
            for (int ii = 0; ii < m_Spheres.Count; ++ii)
            {
                RaycastHit hitInfo;
                if(m_Spheres[ii].GetComponent<Sphere>().CheckCollision(ray, out hitInfo))
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

            for (int ii = 0; ii < m_Spheres.Count; ++ii)
            {
                RaycastHit hitInfo;
                if (m_Spheres[ii].GetComponent<Sphere>().CheckCollision(ray, out hitInfo))
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
        /// <param name="sphereID"></param>
        public void SelectSphere(int sphereID)
        {
            if (sphereID >= m_Spheres.Count)
                return;

            UnselectSphere();

            if (sphereID >= 0)
            {
                m_Spheres[sphereID].GetComponent<Sphere>().Selected = true;
                SelectedSphereID = sphereID;
            }
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
            GameObject newBubble = Instantiate(m_SpherePrefab);
            newBubble.GetComponent<MeshFilter>().sharedMesh = SharedMeshes.ROISphere;
            newBubble.name = GObubbleName;
            newBubble.transform.SetParent(transform);
            Sphere sphere = newBubble.GetComponent<Sphere>();
            sphere.Initialize(m_Layer, ray, position);
            sphere.OnChangeROIVolumeRadius.AddListener(() =>
            {
                OnChangeROIVolumeRadius.Invoke();
            });
            m_Spheres.Add(sphere);

            // DLL
            Vector3 positionBubble = position;
            positionBubble.x = -positionBubble.x;
            m_DLLROI.AddBubble(ray, positionBubble);

            OnChangeNumberOfVolumeInROI.Invoke();
            SelectSphere(m_Spheres.Count - 1);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idBubble"></param>
        public void RemoveBubble(int idBubble)
        {
            if (SelectedSphereID > idBubble)
                SelectedSphereID--;
            else if(SelectedSphereID == idBubble)
                SelectedSphereID = -1;

            // remove the bubble
            Destroy(m_Spheres[idBubble].gameObject);
            m_Spheres.RemoveAt(idBubble);

            // remove dll sphere
            m_DLLROI.RemoveBubble(idBubble);

            OnChangeNumberOfVolumeInROI.Invoke();

            // if not we removed the selected bubble, select instead the last one
            if (SelectedSphereID == -1)
            {
                if(m_Spheres.Count > 0)
                    SelectSphere(m_Spheres.Count - 1);
            }
        }
        public void RemoveSelectedSphere()
        {
            RemoveBubble(SelectedSphereID);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idBubble"></param>
        /// <param name="coeff"></param>
        public void ChangeBubbleSize(int idBubble, float coeff)
        {
            if (idBubble < 0 || idBubble >= m_Spheres.Count)
                return;

            m_Spheres[idBubble].GetComponent<Sphere>().Radius *= coeff;

            // DLL
            m_DLLROI.UpdateBubble(idBubble, m_Spheres[idBubble].GetComponent<Sphere>().Radius);

            OnChangeROIVolumeRadius.Invoke();
        }
        public void ChangeSelectedBubbleSize(float direction)
        {
            ChangeBubbleSize(SelectedSphereID, direction < 0 ? 0.9f : 1.1f);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idBubble"></param>
        /// <returns></returns>
        public Sphere GetBubbleByIndex(int idBubble)
        {
            return m_Spheres[idBubble].GetComponent<Sphere>();
        }
        /// <summary>
        /// Return a string containing all bubbles infos of the ROI
        /// </summary>
        /// <returns></returns>
        public string BubblesInformationIntoString()
        {
            string text = m_Name + "\n";
            for (int ii = 0; ii< m_Spheres.Count; ++ii)
            {
                Vector3 pos = m_Spheres[ii].transform.position;
                text += ii + " " + m_Spheres[ii].GetComponent<Sphere>().Radius + " " + pos.x + " " + pos.y + " " + pos.z + "\n";
            }

            return text;
        }
        /// <summary>
        /// Start the growing animation
        /// </summary>
        public void StartAnimation()
        {
            foreach (Sphere sphere in Spheres)
            {
                sphere.StartAnimation();
            }
        }
        #endregion
    }
}