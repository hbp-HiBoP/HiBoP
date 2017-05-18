



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

namespace HBP.Module3D
{
    /// <summary>
    /// A class for erasing parts of the brain meshes
    /// </summary>
    public class TriEraser
    {
        #region Properties
        public enum Mode
        {
            OneTri, Cylinder, Zone, Invert, Expand
        }; /**< modes id */

        private bool m_IsEnabled = false;
        public bool IsEnabled
        {
            get
            {
                return m_IsEnabled;
            }
            set
            {
                m_IsEnabled = value;
                for (int ii = 0; ii < m_BrainInvisibleMeshesGO.Count; ++ii)
                    m_BrainInvisibleMeshesGO[ii].SetActive(m_IsEnabled);
            }
        }
        private Mode m_CurrentMode = Mode.OneTri;
        public Mode CurrentMode
        {
            get
            {
                return m_CurrentMode;
            }
            set
            {
                m_CurrentMode = value;
            }
        }
        private float m_Degrees = 30f;
        public float Degrees
        {
            get
            {
                return m_Degrees;
            }
            set
            {
                m_Degrees = value;
            }
        }

        int[] m_FullMask;
        private List<int[]> m_SplittedMasks = null;

        private DLL.Surface m_BrainMeshDLL = null;
        private List<DLL.Surface> m_BrainMeshesSplittedDLL = null;

        private List<GameObject> m_BrainInvisibleMeshesGO = null;

        /// <summary>
        /// Check if the current mode of the tri eraser needs clicks on the scene
        /// </summary>
        public bool IsClickAvailable
        {
            get
            {
                return (m_CurrentMode != Mode.Expand) && (m_CurrentMode != Mode.Invert);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="brainInvisibleMeshesGO"></param>
        /// <param name="branMeshDLL"></param>
        /// <param name="brainMeshesSplittedDLL"></param>
        /// <param name="namesGO"></param>
        public void Reset(List<GameObject> brainInvisibleMeshesGO, DLL.Surface brainMeshDLL, List<DLL.Surface> brainMeshesSplittedDLL)
        {
            m_BrainInvisibleMeshesGO = brainInvisibleMeshesGO;
            m_BrainMeshesSplittedDLL = brainMeshesSplittedDLL;
            m_BrainMeshDLL = brainMeshDLL;

            m_FullMask = new int[m_BrainMeshDLL.NumberOfTriangles];
            for (int ii = 0; ii < m_FullMask.Length; ++ii)
                m_FullMask[ii] = 1;
            m_BrainMeshDLL.UpdateVisibilityMask(m_FullMask);

            m_SplittedMasks = new List<int[]>(m_BrainMeshesSplittedDLL.Count);
            for (int ii = 0; ii < m_BrainMeshesSplittedDLL.Count; ++ii)
            {
                int[] mask = new int[m_BrainMeshesSplittedDLL[ii].NumberOfTriangles];
                for (int jj = 0; jj < mask.Length; ++jj)
                    mask[jj] = 1;

                m_BrainMeshesSplittedDLL[ii].UpdateVisibilityMask(mask); // return an empty mesh
                m_SplittedMasks.Add(mask);                
            }
        }
        /// <summary>
        /// Erase triangles and update the invisible part mesh GO
        /// </summary>
        /// <param name="hitPoint"></param>
        public void EraseTriangles(Vector3 rayDirection, Vector3 hitPoint)
        {
            rayDirection.x = -rayDirection.x;
            hitPoint.x = -hitPoint.x;

            // save current masks
            m_FullMask = m_BrainMeshDLL.VisibilityMask;
            for (int ii = 0; ii < m_BrainMeshesSplittedDLL.Count; ++ii)
                m_SplittedMasks[ii] = m_BrainMeshesSplittedDLL[ii].VisibilityMask;

            // apply rays and retrieve mask
            /*DLL.Surface brainInvisibleFullMeshDLL = */m_BrainMeshDLL.UpdateVisibilityMask(rayDirection, hitPoint, m_CurrentMode, m_Degrees);
            int[] newFullMask = m_BrainMeshDLL.VisibilityMask;

            // split it
            int nbSplits = m_BrainMeshesSplittedDLL.Count;            
            int size = m_FullMask.Length / nbSplits;
            int lastSize = size + m_FullMask.Length % nbSplits;

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

            for (int ii = 0; ii < m_BrainMeshesSplittedDLL.Count; ++ii)
            {
                DLL.Surface brainInvisibleMeshesDLL = m_BrainMeshesSplittedDLL[ii].UpdateVisibilityMask(newSplittedMasks[ii]);
                brainInvisibleMeshesDLL.UpdateMeshFromDLL(m_BrainInvisibleMeshesGO[ii].GetComponent<MeshFilter>().mesh);
            }
        }
        /// <summary>
        /// Cancel the last action and update the invisible part mesh GO
        /// </summary>
        public void CancelLastAction()
        {
            m_BrainMeshDLL.UpdateVisibilityMask(m_FullMask);
            for (int ii = 0; ii < m_BrainMeshesSplittedDLL.Count; ++ii)
            {
                DLL.Surface brainInvisibleMeshesDLL = m_BrainMeshesSplittedDLL[ii].UpdateVisibilityMask(m_SplittedMasks[ii]);
                brainInvisibleMeshesDLL.UpdateMeshFromDLL(m_BrainInvisibleMeshesGO[ii].GetComponent<MeshFilter>().mesh);
            }
        }
        #endregion
    }
}