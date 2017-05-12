

/**
 * \file    Bubble.cs
 * \author  Lance Florian
 * \date    22/04/2016
 * \brief   Define Bubble
 */

// unity
using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// Define a bubble used in ROI
    /// </summary>
    public class Bubble : MonoBehaviour
    {
        #region Properties
        private float m_MinRaySphere = 0.5f;    
        private float m_MaxRaySphere = 100f;    
        private float m_Radius = 1f;
        public float Radius
        {
            get
            {
                return m_Radius;
            }
            set
            {
                m_Radius = value;
                if (m_Radius > m_MaxRaySphere)
                    m_Radius = m_MaxRaySphere;

                if (m_Radius < m_MinRaySphere)
                    m_Radius = m_MinRaySphere;

                transform.localScale = new Vector3(m_Radius, m_Radius, m_Radius);
            }
        }
        private bool m_Selected = false;
        public bool Selected
        {
            get
            {
                return m_Selected;
            }
            set
            {
                m_Selected = value;
                if (m_Selected)
                {
                    GetComponent<Renderer>().sharedMaterial = SharedMaterials.ROI.Selected;
                }
                else
                {
                    GetComponent<Renderer>().sharedMaterial = SharedMaterials.ROI.Normal;
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Init the bubble
        /// </summary
        /// <param name="layer"> layer of the bubble gameobject </param>
        /// <param name="radius"></param>
        /// <param name="position"></param>
        public void Initialize(int layer, float radius, Vector3 position)
        {
            gameObject.layer = layer;
            transform.position = position;
            m_Radius = radius;
            transform.localScale = new Vector3(radius, radius, radius);
            gameObject.SetActive(true);

            // add mesh
            gameObject.GetComponent<MeshFilter>().sharedMesh = SharedMeshes.ROIBubble;
        }
        /// <summary>
        /// Check if a collision occurs with the input ray
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="hitInfo"></param>
        /// <returns></returns>
        public bool CheckCollision(Ray ray, out RaycastHit hitInfo)
        {
            return GetComponent<SphereCollider>().Raycast(ray, out hitInfo, Mathf.Infinity);
        }
        #endregion
    }
}