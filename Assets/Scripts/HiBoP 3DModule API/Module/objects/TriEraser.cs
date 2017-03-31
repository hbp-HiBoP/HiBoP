



/**
 * \file    TriEraser.cs
 * \author  Lance Florian
 * \date    2016
 * \brief   Define TriEraser
 */

// system
using System.Collections.Generic;

// unity
using UnityEngine;

namespace HBP.VISU3D
{
    /// <summary>
    /// A class for erasing parts of the brain meshes
    /// </summary>
    public class TriEraser
    {
        public enum Mode : int
        {
            OneTri, Cylinder, Zone, Invert, Expand
        }; /**< modes id */

        private bool m_isEnabled = false;
        private Mode m_mode = Mode.OneTri;
        private float m_degrees = 30f;

        int[] m_fullMask;
        private List<int[]> m_splittedMasks = null;

        private DLL.Surface m_brainMeshDLL = null;
        private List<DLL.Surface> m_brainMeshesSplittedDLL = null;

        private List<GameObject> m_brainInvisibleMeshesGO = null;
                
        /// <summary>
        /// Are the invisible part meshes GO enabled
        /// </summary>
        /// <returns></returns>
        public bool is_enabled() { return m_isEnabled; }

        /// <summary>
        /// Return the current mode
        /// </summary>
        /// <returns></returns>
        public Mode mode() { return m_mode; }

        /// <summary>
        /// Set the activation of the invisible mesh part gameobjects
        /// </summary>
        /// <param name="isEnabled"></param>
        public void set_enabled(bool isEnabled)
        {
            m_isEnabled = isEnabled;
            for (int ii = 0; ii < m_brainInvisibleMeshesGO.Count; ++ii)
                m_brainInvisibleMeshesGO[ii].SetActive(isEnabled);
        }

        /// <summary>
        /// Define the current tri erasing mode
        /// </summary>
        /// <param name="mode"></param>
        public void set_tri_erasing_mode(Mode mode)
        {
            m_mode = mode;
        }

        /// <summary>
        /// Define the number of degrees for the zone pencil
        /// </summary>
        /// <param name="degrees"></param>
        public void set_zone_degrees(float degrees)
        {
            m_degrees = degrees;
        }

        /// <summary>
        /// Check if the current mode of the tri eraser needs clicks on the scene
        /// </summary>
        /// <returns></returns>
        public bool is_click_available()
        {
            return (m_mode != Mode.Expand) && (m_mode != Mode.Invert);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="brainInvisibleMeshesGO"></param>
        /// <param name="branMeshDLL"></param>
        /// <param name="brainMeshesSplittedDLL"></param>
        /// <param name="namesGO"></param>
        public void reset(List<GameObject> brainInvisibleMeshesGO, DLL.Surface brainMeshDLL, List<DLL.Surface> brainMeshesSplittedDLL)
        {
            m_brainInvisibleMeshesGO = brainInvisibleMeshesGO;
            m_brainMeshesSplittedDLL = brainMeshesSplittedDLL;
            m_brainMeshDLL = brainMeshDLL;

            m_fullMask = new int[m_brainMeshDLL.triangles_nb()];
            for (int ii = 0; ii < m_fullMask.Length; ++ii)
                m_fullMask[ii] = 1;
            m_brainMeshDLL.update_visibility_mask(m_fullMask);

            m_splittedMasks = new List<int[]>(m_brainMeshesSplittedDLL.Count);
            for (int ii = 0; ii < m_brainMeshesSplittedDLL.Count; ++ii)
            {
                int[] mask = new int[m_brainMeshesSplittedDLL[ii].triangles_nb()];
                for (int jj = 0; jj < mask.Length; ++jj)
                    mask[jj] = 1;

                m_brainMeshesSplittedDLL[ii].update_visibility_mask(mask); // return an empty mesh
                m_splittedMasks.Add(mask);                
            }
        }

        /// <summary>
        /// Erase triangles and update the invisible part mesh GO
        /// </summary>
        /// <param name="hitPoint"></param>
        public void erase_triangles(Vector3 rayDirection, Vector3 hitPoint)
        {
            rayDirection.x = -rayDirection.x;
            hitPoint.x = -hitPoint.x;

            // save current masks
            m_fullMask = m_brainMeshDLL.visibility_mask();
            for (int ii = 0; ii < m_brainMeshesSplittedDLL.Count; ++ii)
                m_splittedMasks[ii] = m_brainMeshesSplittedDLL[ii].visibility_mask();

            // apply rays and retrieve mask
            /*DLL.Surface brainInvisibleFullMeshDLL = */m_brainMeshDLL.update_visibility_mask(rayDirection, hitPoint, m_mode, m_degrees);
            int[] newFullMask = m_brainMeshDLL.visibility_mask();

            // split it
            int nbSplits = m_brainMeshesSplittedDLL.Count;            
            int size = m_fullMask.Length / nbSplits;
            int lastSize = size + m_fullMask.Length % nbSplits;

            int currId = 0;
            List<int[]> newSplittedMasks = new List<int[]>(nbSplits);
            for(int ii = 0; ii < nbSplits; ++ii)
            {
                int currentSize = (ii < nbSplits - 1) ? size : lastSize;
                int[] mask = new int[currentSize];

                for (int jj = 0; jj < currentSize; ++jj)
                    mask[jj] = newFullMask[currId++];

                newSplittedMasks.Add(mask);
            }

            for (int ii = 0; ii < m_brainMeshesSplittedDLL.Count; ++ii)
            {
                DLL.Surface brainInvisibleMeshesDLL = m_brainMeshesSplittedDLL[ii].update_visibility_mask(newSplittedMasks[ii]);
                brainInvisibleMeshesDLL.update_mesh_from_dll(m_brainInvisibleMeshesGO[ii].GetComponent<MeshFilter>().mesh);
            }
        }

        /// <summary>
        /// Cancel the last action and update the invisible part mesh GO
        /// </summary>
        public void cancel_last_action()
        {
            m_brainMeshDLL.update_visibility_mask(m_fullMask);
            for (int ii = 0; ii < m_brainMeshesSplittedDLL.Count; ++ii)
            {
                DLL.Surface brainInvisibleMeshesDLL = m_brainMeshesSplittedDLL[ii].update_visibility_mask(m_splittedMasks[ii]);
                brainInvisibleMeshesDLL.update_mesh_from_dll(m_brainInvisibleMeshesGO[ii].GetComponent<MeshFilter>().mesh);
            }
        }


    }
}