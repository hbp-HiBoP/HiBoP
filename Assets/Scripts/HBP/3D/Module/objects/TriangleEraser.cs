using System.Collections.Generic;
using System.Linq;
using Tools.CSharp;
using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// Class containing tools to erase parts of the brain mesh
    /// </summary>
    /// <remarks>
    /// Possible actions:
    /// <list type="bullet">
    /// <item>
    /// <term>Erase single triangle</term>
    /// <description>Erase one triangle of the mesh by clicking on it</description>
    /// </item>
    /// <item>
    /// <term>Erase cylinder</term>
    /// <description>Erase a cylinder of triangles from the camera to the clicked direction</description>
    /// </item>
    /// <item>
    /// <term>Erase zone</term>
    /// <description>Erase each nextdoor triangle from a clicked triangle until a specific amount of degrees has been reached between the normal of the clicked triangle and the normal of the considered triangle</description>
    /// </item>
    /// <item>
    /// <term>Expand</term>
    /// <description>Expand the erased zone to every nextdoor triangles of already erased triangles</description>
    /// </item>
    /// <item>
    /// <term>Invert</term>
    /// <description>Every erased triangles become non-erased and inversely</description>
    /// </item>
    /// <item>
    /// <term>Cancel</term>
    /// <description>Cancel the last erasing action (stack size of <see cref="MAX_STACK_SIZE"/> actions)</description>
    /// </item>
    /// </list>
    /// </remarks>
    public class TriangleEraser : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Parent scene of the Triangle Eraser
        /// </summary>
        [SerializeField] private Base3DScene m_Scene;
        /// <summary>
        /// Component containing references to GameObjects of the 3D scene
        /// </summary>
        [SerializeField] private DisplayedObjects m_DisplayedObjects;

        private bool m_IsEnabled = false;
        /// <summary>
        /// Can the user perform erasing actions ?
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                return m_IsEnabled;
            }
            set
            {
                if (m_IsEnabled != value)
                {
                    m_IsEnabled = value;
                    for (int ii = 0; ii < m_DisplayedObjects.InvisibleBrainSurfaceMeshes.Count; ++ii)
                        m_DisplayedObjects.InvisibleBrainSurfaceMeshes[ii].SetActive(value);
                    m_Scene.SceneInformation.CollidersNeedUpdate = true;
                }
            }
        }
        /// <summary>
        /// Check if the current mode of the triangle eraser needs a click on the scene to perform its action
        /// </summary>
        public bool IsClickAvailable
        {
            get
            {
                return (CurrentMode != Data.Enums.TriEraserMode.Expand) && (CurrentMode != Data.Enums.TriEraserMode.Invert);
            }
        }
        /// <summary>
        /// Does the mesh have erased triangles ?
        /// </summary>
        public bool MeshHasInvisibleTriangles { get; private set; } = false;
        /// <summary>
        /// Do we also erase triangles of the simplified mesh ? (this is useful for better colliders)
        /// </summary>
        private bool m_EraseTrianglesOfSimplifiedMesh = true;

        private Data.Enums.TriEraserMode m_CurrentMode = Data.Enums.TriEraserMode.OneTri;
        /// <summary>
        /// Currently selected erasing mode (see <see cref="Data.Enums.TriEraserMode"/> for possible values)
        /// </summary>
        public Data.Enums.TriEraserMode CurrentMode
        {
            get
            {
                return m_CurrentMode;
            }
            set
            {
                Data.Enums.TriEraserMode previousMode = m_CurrentMode;
                m_CurrentMode = value;

                if (value == Data.Enums.TriEraserMode.Expand || value == Data.Enums.TriEraserMode.Invert)
                {
                    EraseTriangles(new Vector3(), new Vector3());
                    m_Scene.UpdateMeshesFromDLL();
                    m_CurrentMode = previousMode;
                }
            }
        }
        /// <summary>
        /// Number of degrees to consider for the zone erasing mode (maximum angle between a triangle and the clicked triangle to define a zone)
        /// </summary>
        public int Degrees { get; set; } = 30;
        /// <summary>
        /// Is there something in the stack so the user can revert some erasing actions ? (maximum stack size is <see cref="MAX_STACK_SIZE"/>)
        /// </summary>
        public bool CanCancelLastAction
        {
            get
            {
                return m_FullMasksStack.Count > 0;
            }
        }
        /// <summary>
        /// Maximum stack size for erasing actions
        /// </summary>
        private const int MAX_STACK_SIZE = 20;

        /// <summary>
        /// Stack containing the mask data for every triangles of the whole mesh
        /// </summary>
        private LimitedSizeStack<int[]> m_FullMasksStack = new LimitedSizeStack<int[]>(MAX_STACK_SIZE);
        /// <summary>
        /// Stack containing the mask data for every triangles of the simplified mesh
        /// </summary>
        private LimitedSizeStack<int[]> m_SimplifiedFullMasksStack = new LimitedSizeStack<int[]>(MAX_STACK_SIZE);
        /// <summary>
        /// Stack containing the data for every triangles for each splitted mesh
        /// </summary>
        private LimitedSizeStack<List<int[]>> m_SplittedMasksStack = new LimitedSizeStack<List<int[]>>(MAX_STACK_SIZE);
        /// <summary>
        /// Currently used masks: the first array is the currently used mask for the whole mesh, the second array is the currently used mask for the simplified mesh (if using simplified mesh for better colliders), the rest concerns splits
        /// </summary>
        public List<int[]> CurrentMasks
        {
            get
            {
                List<int[]> masks = new List<int[]>();
                masks.Add(m_Scene.SceneInformation.MeshToDisplay.VisibilityMask);
                if (m_EraseTrianglesOfSimplifiedMesh) masks.Add(m_Scene.SceneInformation.SimplifiedMeshToUse.VisibilityMask);
                foreach (var split in m_Scene.SplittedMeshes)
                {
                    masks.Add(split.VisibilityMask);
                }
                return masks;
            }
            set
            {
                if (value.Count == m_Scene.SplittedMeshes.Count + 2)
                {
                    m_Scene.SceneInformation.MeshToDisplay.UpdateVisibilityMask(value[0]).Dispose();
                    if (m_EraseTrianglesOfSimplifiedMesh) m_Scene.SceneInformation.SimplifiedMeshToUse.UpdateVisibilityMask(value[1]).Dispose();
                    for (int i = 0; i < m_Scene.SplittedMeshes.Count; ++i)
                    {
                        DLL.Surface brainInvisibleMeshesDLL = m_Scene.SplittedMeshes[i].UpdateVisibilityMask(value[i + 2]);
                        brainInvisibleMeshesDLL.UpdateMeshFromDLL(m_DisplayedObjects.InvisibleBrainSurfaceMeshes[i].GetComponent<MeshFilter>().mesh);
                        brainInvisibleMeshesDLL.Dispose();
                    }
                    MeshHasInvisibleTriangles = m_Scene.SceneInformation.MeshToDisplay.VisibilityMask.Contains(0);

                    m_Scene.ResetIEEG();
                    m_Scene.UpdateMeshesFromDLL();
                    ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Reset the Triangle Eraser to default values (masks with only 1s, new gameObjects etc.)
        /// </summary>
        public void ResetEraser()
        {
            m_DisplayedObjects.ResetInvisibleMesh(m_IsEnabled);

            int[] fullMask = ArrayExtensions.Create(m_Scene.SceneInformation.MeshToDisplay.NumberOfTriangles, 1);
            m_Scene.SceneInformation.MeshToDisplay.UpdateVisibilityMask(fullMask).Dispose();
            
            for (int ii = 0; ii < m_Scene.SplittedMeshes.Count; ++ii)
            {
                int[] mask = ArrayExtensions.Create(m_Scene.SplittedMeshes[ii].NumberOfTriangles, 1);
                m_Scene.SplittedMeshes[ii].UpdateVisibilityMask(mask).Dispose();
            }
            
            int[] simplifiedFullMask = ArrayExtensions.Create(m_Scene.SceneInformation.SimplifiedMeshToUse.NumberOfTriangles, 1);
            m_Scene.SceneInformation.SimplifiedMeshToUse.UpdateVisibilityMask(simplifiedFullMask).Dispose();

            MeshHasInvisibleTriangles = false;

            m_FullMasksStack.Clear();
            m_SimplifiedFullMasksStack.Clear();
            m_SplittedMasksStack.Clear();

            m_Scene.ResetIEEG();
            m_Scene.UpdateMeshesFromDLL();
            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        /// <summary>
        /// Erase triangles and update the invisible mesh
        /// </summary>
        /// <param name="rayDirection">Direction of the raycast triggered from a mouse click</param>
        /// <param name="hitPoint">Hitpoint of the raycast triggered from a mouse click</param>
        public void EraseTriangles(Vector3 rayDirection, Vector3 hitPoint)
        {
            rayDirection.x = -rayDirection.x;
            hitPoint.x = -hitPoint.x;

            // Save current masks
            m_FullMasksStack.Push(m_Scene.SceneInformation.MeshToDisplay.VisibilityMask);
            List<int[]> splittedMasks = new List<int[]>(m_Scene.SplittedMeshes.Count);
            for (int ii = 0; ii < m_Scene.SplittedMeshes.Count; ++ii)
            {
                splittedMasks.Add(m_Scene.SplittedMeshes[ii].VisibilityMask);
            }
            m_SplittedMasksStack.Push(splittedMasks);
            if (m_EraseTrianglesOfSimplifiedMesh) m_SimplifiedFullMasksStack.Push(m_Scene.SceneInformation.SimplifiedMeshToUse.VisibilityMask);

            // Apply rays and retrieve mask
            m_Scene.SceneInformation.MeshToDisplay.UpdateVisibilityMask(rayDirection, hitPoint, CurrentMode, Degrees).Dispose();
            if (m_EraseTrianglesOfSimplifiedMesh) m_Scene.SceneInformation.SimplifiedMeshToUse.UpdateVisibilityMask(rayDirection, hitPoint, CurrentMode, Degrees).Dispose();
            int[] newFullMask = m_Scene.SceneInformation.MeshToDisplay.VisibilityMask;

            // Split it
            int currId = 0;
            for (int ii = 0; ii < m_Scene.SplittedMeshes.Count; ++ii)
            {
                int numberOfTriangles = m_Scene.SplittedMeshes[ii].NumberOfTriangles;
                int[] mask = new int[numberOfTriangles];
                for (int jj = 0; jj < numberOfTriangles; ++jj) mask[jj] = newFullMask[currId++];

                DLL.Surface brainInvisibleMeshesDLL = m_Scene.SplittedMeshes[ii].UpdateVisibilityMask(mask);
                brainInvisibleMeshesDLL.UpdateMeshFromDLL(m_DisplayedObjects.InvisibleBrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);
                brainInvisibleMeshesDLL.Dispose();
            }

            MeshHasInvisibleTriangles = m_Scene.SceneInformation.MeshToDisplay.VisibilityMask.ToList().FindIndex((m) => m != 1) != -1;

            m_Scene.ResetIEEG();
            m_Scene.UpdateMeshesFromDLL();
            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        /// <summary>
        /// Cancel the last action and update the invisible mesh
        /// </summary>
        public void CancelLastAction()
        {
            m_Scene.SceneInformation.MeshToDisplay.UpdateVisibilityMask(m_FullMasksStack.Pop()).Dispose();
            if (m_EraseTrianglesOfSimplifiedMesh) m_Scene.SceneInformation.SimplifiedMeshToUse.UpdateVisibilityMask(m_SimplifiedFullMasksStack.Pop()).Dispose();

            List<int[]> splittedMasks = m_SplittedMasksStack.Pop();
            for (int ii = 0; ii < m_Scene.SplittedMeshes.Count; ++ii)
            {
                DLL.Surface brainInvisibleMeshesDLL = m_Scene.SplittedMeshes[ii].UpdateVisibilityMask(splittedMasks[ii]);
                brainInvisibleMeshesDLL.UpdateMeshFromDLL(m_DisplayedObjects.InvisibleBrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);
                brainInvisibleMeshesDLL.Dispose();
            }

            MeshHasInvisibleTriangles = m_Scene.SceneInformation.MeshToDisplay.VisibilityMask.ToList().FindIndex((m) => m != 1) != -1;

            m_Scene.ResetIEEG();
            m_Scene.UpdateMeshesFromDLL();
            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        #endregion
    }
}