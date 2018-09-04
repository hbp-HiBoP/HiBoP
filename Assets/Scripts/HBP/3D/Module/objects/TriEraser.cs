



/**
 * \file    TriEraser.cs
 * \author  Lance Florian
 * \date    2016
 * \brief   Define TriEraser
 */

// system
using System.Collections.Generic;
using System.Linq;

// unity
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Module3D
{
    /// <summary>
    /// A class for erasing parts of the brain meshes
    /// </summary>
    public class TriEraser
    {
        #region Properties
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
        private Data.Enums.TriEraserMode m_CurrentMode = Data.Enums.TriEraserMode.OneTri;
        public Data.Enums.TriEraserMode CurrentMode
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
        private int m_Degrees = 30;
        public int Degrees
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

        public bool CanCancelLastAction
        {
            get
            {
                return m_FullMasksStack.Count > 0;
            }
        }

        private const int MAX_STACK_SIZE = 20;

        // Regular Mesh
        private Tools.CSharp.LimitedSizeStack<int[]> m_FullMasksStack = new Tools.CSharp.LimitedSizeStack<int[]>(MAX_STACK_SIZE);
        private Tools.CSharp.LimitedSizeStack<List<int[]>> m_SplittedMasksStack = new Tools.CSharp.LimitedSizeStack<List<int[]>>(MAX_STACK_SIZE);
        private DLL.Surface m_BrainMeshDLL;
        private List<DLL.Surface> m_BrainMeshesSplittedDLL = new List<DLL.Surface>();
        private List<GameObject> m_BrainInvisibleMeshesGO = new List<GameObject>();

        // Simplified Mesh
        private bool m_EraseTrianglesOfSimplifiedMesh = false;
        private Tools.CSharp.LimitedSizeStack<int[]> m_SimplifiedFullMasksStack = new Tools.CSharp.LimitedSizeStack<int[]>(MAX_STACK_SIZE);
        private DLL.Surface m_SimplifiedBrainMeshDLL;

        /// <summary>
        /// Check if the current mode of the tri eraser needs clicks on the scene
        /// </summary>
        public bool IsClickAvailable
        {
            get
            {
                return (m_CurrentMode != Data.Enums.TriEraserMode.Expand) && (m_CurrentMode != Data.Enums.TriEraserMode.Invert);
            }
        }
        private bool m_MeshHasInvisibleTriangles = false;
        public bool MeshHasInvisibleTriangles
        {
            get
            {
                return m_MeshHasInvisibleTriangles;
            }
        }
        #endregion

        #region Events
        public UnityEvent OnModifyInvisiblePart = new UnityEvent();
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

            int[] fullMask = new int[m_BrainMeshDLL.NumberOfTriangles];
            for (int ii = 0; ii < fullMask.Length; ++ii)
                fullMask[ii] = 1;
            m_BrainMeshDLL.UpdateVisibilityMask(fullMask);

            m_FullMasksStack.Clear();

            for (int ii = 0; ii < m_BrainMeshesSplittedDLL.Count; ++ii)
            {
                int[] mask = new int[m_BrainMeshesSplittedDLL[ii].NumberOfTriangles];
                for (int jj = 0; jj < mask.Length; ++jj)
                    mask[jj] = 1;

                m_BrainMeshesSplittedDLL[ii].UpdateVisibilityMask(mask); // return an empty mesh
            }
            m_SplittedMasksStack.Clear();

            m_MeshHasInvisibleTriangles = m_BrainMeshDLL.VisibilityMask.ToList().FindIndex((m) => m != 1) != -1;
            OnModifyInvisiblePart.Invoke();
        }
        public void ResetSimplified(DLL.Surface simplifiedMeshDLL)
        {
            m_SimplifiedBrainMeshDLL = simplifiedMeshDLL;

            int[] simplifiedFullMask = new int[m_SimplifiedBrainMeshDLL.NumberOfTriangles];
            for (int ii = 0; ii < simplifiedFullMask.Length; ++ii)
                simplifiedFullMask[ii] = 1;
            m_SimplifiedBrainMeshDLL.UpdateVisibilityMask(simplifiedFullMask);

            m_SimplifiedFullMasksStack.Clear();

            m_EraseTrianglesOfSimplifiedMesh = true;
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
            m_FullMasksStack.Push(m_BrainMeshDLL.VisibilityMask);
            List<int[]> splittedMasks = new List<int[]>(m_BrainMeshesSplittedDLL.Count);
            for (int ii = 0; ii < m_BrainMeshesSplittedDLL.Count; ++ii)
                splittedMasks.Add(m_BrainMeshesSplittedDLL[ii].VisibilityMask);
            m_SplittedMasksStack.Push(splittedMasks);
            if (m_EraseTrianglesOfSimplifiedMesh) m_SimplifiedFullMasksStack.Push(m_SimplifiedBrainMeshDLL.VisibilityMask);

            // apply rays and retrieve mask
            m_BrainMeshDLL.UpdateVisibilityMask(rayDirection, hitPoint, m_CurrentMode, m_Degrees);
            if (m_EraseTrianglesOfSimplifiedMesh) m_SimplifiedBrainMeshDLL.UpdateVisibilityMask(rayDirection, hitPoint, m_CurrentMode, m_Degrees);
            int[] newFullMask = m_BrainMeshDLL.VisibilityMask;

            // split it
            int nbSplits = m_BrainMeshesSplittedDLL.Count;            
            int size = m_FullMasksStack.Peek().Length / nbSplits;
            int lastSize = size + m_FullMasksStack.Peek().Length % nbSplits;

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

            m_MeshHasInvisibleTriangles = m_BrainMeshDLL.VisibilityMask.ToList().FindIndex((m) => m != 1) != -1;
            OnModifyInvisiblePart.Invoke();
        }
        /// <summary>
        /// Cancel the last action and update the invisible part mesh GO
        /// </summary>
        public void CancelLastAction()
        {
            m_BrainMeshDLL.UpdateVisibilityMask(m_FullMasksStack.Pop());
            if (m_EraseTrianglesOfSimplifiedMesh) m_SimplifiedBrainMeshDLL.UpdateVisibilityMask(m_SimplifiedFullMasksStack.Pop());

            List<int[]> splittedMasks = m_SplittedMasksStack.Pop();
            for (int ii = 0; ii < m_BrainMeshesSplittedDLL.Count; ++ii)
            {
                DLL.Surface brainInvisibleMeshesDLL = m_BrainMeshesSplittedDLL[ii].UpdateVisibilityMask(splittedMasks[ii]);
                brainInvisibleMeshesDLL.UpdateMeshFromDLL(m_BrainInvisibleMeshesGO[ii].GetComponent<MeshFilter>().mesh);
            }

            m_MeshHasInvisibleTriangles = m_BrainMeshDLL.VisibilityMask.ToList().FindIndex((m) => m != 1) != -1;
            OnModifyInvisiblePart.Invoke();
        }
        #endregion
    }
}