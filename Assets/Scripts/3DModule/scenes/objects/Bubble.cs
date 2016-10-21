

/**
 * \file    Bubble.cs
 * \author  Lance Florian
 * \date    22/04/2016
 * \brief   Define Bubble
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
    /// Define a ROI bubble
    /// </summary>
    public class Bubble : MonoBehaviour
    {
        private float m_minRaySphere = 0.5f; /**< min ray of the ROI spheres */
        private float m_maxRaySphere = 100f; /**< max ray of the ROI spheres */
        public float m_radius = 1f;         /**< bubble radius */
        public bool m_selected = false;     /**< is the bubble selected ? */

        /// <summary>
        /// Init the bubble
        /// </summary
        /// <param name="layer"></param>
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
        /// Unselect the bubble
        /// </summary>
        public void unselect()
        {
            GetComponent<Renderer>().sharedMaterial = SharedMaterials.ROI.Normal;
            m_selected = false;
        }

        /// <summary>
        /// Select the bubble
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
        public bool checkCollision(Ray ray, out RaycastHit hitInfo)
        {
            return GetComponent<SphereCollider>().Raycast(ray, out hitInfo, Mathf.Infinity);
        }

        /// <summary>
        /// Change the bubble size
        /// </summary>
        public void changeSize(float coeff)
        {
            m_radius *= coeff;
            if (m_radius > m_maxRaySphere)
                m_radius = m_maxRaySphere;

            if (m_radius < m_minRaySphere)
                m_radius = m_minRaySphere;

            transform.localScale = new Vector3(m_radius, m_radius, m_radius);
        }
    }
}