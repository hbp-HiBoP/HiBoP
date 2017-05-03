

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
        private float m_minRaySphere = 0.5f;    
        private float m_maxRaySphere = 100f;    
        public float m_radius = 1f;             
        public bool m_selected = false;
        #endregion

        #region Public Methods
        /// <summary>
        /// Init the bubble
        /// </summary
        /// <param name="layer"> layer of the bubble gameobject </param>
        /// <param name="radius"></param>
        /// <param name="position"></param>
        public void init(int layer, float radius, Vector3 position)
        {
            gameObject.layer = layer;
            transform.position = position;
            m_radius = radius;
            transform.localScale = new Vector3(radius, radius, radius);
            gameObject.SetActive(true);

            // add mesh
            gameObject.GetComponent<MeshFilter>().sharedMesh = SharedMeshes.ROIBubble;
        }
        /// <summary>
        /// Unselect the bubble and update its material
        /// </summary>
        public void unselect()
        {
            GetComponent<Renderer>().sharedMaterial = SharedMaterials.ROI.Normal;
            m_selected = false;
        }
        /// <summary>
        /// Select the bubble and update its material
        /// </summary>
        public void select()
        {
            GetComponent<Renderer>().sharedMaterial = SharedMaterials.ROI.Selected;
            m_selected = true;
        }
        /// <summary>
        /// Check if a collision occurs with the input ray
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="hitInfo"></param>
        /// <returns></returns>
        public bool check_collistion(Ray ray, out RaycastHit hitInfo)
        {
            return GetComponent<SphereCollider>().Raycast(ray, out hitInfo, Mathf.Infinity);
        }
        /// <summary>
        /// Multiply the current bubble size with the input coeff
        /// </summary>
        /// <param name="coeff"></param>
        public void change_size(float coeff)
        {
            m_radius *= coeff;
            if (m_radius > m_maxRaySphere)
                m_radius = m_maxRaySphere;

            if (m_radius < m_minRaySphere)
                m_radius = m_minRaySphere;

            transform.localScale = new Vector3(m_radius, m_radius, m_radius);
        }
        #endregion
    }
}