using System.Collections.Generic;
using System.Linq;
using Tools.CSharp;
using UnityEngine;
using HBP.Core.Data.Enums;

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
                    m_DisplayedObjects.InvisibleBrain.SetActive(value);
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
                return (CurrentMode != TriEraserMode.Expand) && (CurrentMode != TriEraserMode.Invert);
            }
        }
        /// <summary>
        /// Does the mesh have erased triangles ?
        /// </summary>
        public bool MeshHasInvisibleTriangles { get; private set; } = false;

        private TriEraserMode m_CurrentMode = TriEraserMode.OneTri;
        /// <summary>
        /// Currently selected erasing mode (see <see cref="TriEraserMode"/> for possible values)
        /// </summary>
        public TriEraserMode CurrentMode
        {
            get
            {
                return m_CurrentMode;
            }
            set
            {
                TriEraserMode previousMode = m_CurrentMode;
                m_CurrentMode = value;

                if (value == TriEraserMode.Expand || value == TriEraserMode.Invert)
                {
                    EraseTriangles(new Vector3(), new Vector3());
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
                return m_MasksStack.Count > 0;
            }
        }
        /// <summary>
        /// Maximum stack size for erasing actions
        /// </summary>
        private const int MAX_STACK_SIZE = 20;

        /// <summary>
        /// Stack containing the mask data for every triangles of the whole mesh
        /// </summary>
        private LimitedSizeStack<int[]> m_MasksStack = new LimitedSizeStack<int[]>(MAX_STACK_SIZE);
        /// <summary>
        /// Stack containing the mask data for every triangles of the simplified mesh
        /// </summary>
        private LimitedSizeStack<int[]> m_SimplifiedMasksStack = new LimitedSizeStack<int[]>(MAX_STACK_SIZE);
        /// <summary>
        /// Currently used masks: the first array is the currently used mask for the whole mesh, the second array is the currently used mask for the simplified mesh (if using simplified mesh for better colliders)
        /// </summary>
        public List<int[]> CurrentMasks
        {
            get
            {
                return new List<int[]>
                {
                    m_Scene.MeshManager.BrainSurface.VisibilityMask,
                    m_Scene.MeshManager.SimplifiedMeshToUse.VisibilityMask
                };
            }
            set
            {
                if (value.Count >= 2)
                {
                    m_Scene.MeshManager.BrainSurface.UpdateVisibilityMask(value[0]).Dispose();
                    m_Scene.MeshManager.SimplifiedMeshToUse.UpdateVisibilityMask(value[1]).Dispose();
                    MeshHasInvisibleTriangles = m_Scene.MeshManager.BrainSurface.VisibilityMask.Contains(0);
                    m_Scene.ResetGenerators();
                    m_Scene.MeshManager.UpdateMeshesFromDLL();
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
            m_DisplayedObjects.InstantiateInvisibleMesh(m_IsEnabled);

            int[] mask = ArrayExtensions.Create(m_Scene.MeshManager.BrainSurface.NumberOfTriangles, 1);
            m_Scene.MeshManager.BrainSurface.UpdateVisibilityMask(mask).Dispose();

            int[] simplifiedMask = ArrayExtensions.Create(m_Scene.MeshManager.SimplifiedMeshToUse.NumberOfTriangles, 1);
            m_Scene.MeshManager.SimplifiedMeshToUse.UpdateVisibilityMask(simplifiedMask).Dispose();

            MeshHasInvisibleTriangles = false;

            m_MasksStack.Clear();
            m_SimplifiedMasksStack.Clear();

            m_Scene.ResetGenerators();
            m_Scene.MeshManager.UpdateMeshesFromDLL();
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
            m_MasksStack.Push(m_Scene.MeshManager.BrainSurface.VisibilityMask);
            m_SimplifiedMasksStack.Push(m_Scene.MeshManager.SimplifiedMeshToUse.VisibilityMask);

            // Apply erasing
            Core.DLL.Surface invisibleSurface = m_Scene.MeshManager.BrainSurface.UpdateVisibilityMask(rayDirection, hitPoint, CurrentMode, Degrees);
            invisibleSurface.UpdateMeshFromDLL(m_DisplayedObjects.InvisibleBrain.GetComponent<MeshFilter>().mesh);
            invisibleSurface.Dispose();
            m_Scene.MeshManager.SimplifiedMeshToUse.UpdateVisibilityMask(rayDirection, hitPoint, CurrentMode, Degrees).Dispose();
            MeshHasInvisibleTriangles = m_Scene.MeshManager.BrainSurface.VisibilityMask.ToList().FindIndex((m) => m != 1) != -1;

            m_Scene.ResetGenerators();
            m_Scene.MeshManager.UpdateMeshesFromDLL();
            m_Scene.FMRIManager.UpdateSurfaceFMRIValues();
            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        /// <summary>
        /// Cancel the last action and update the invisible mesh
        /// </summary>
        public void CancelLastAction()
        {
            Core.DLL.Surface invisibleSurface = m_Scene.MeshManager.BrainSurface.UpdateVisibilityMask(m_MasksStack.Pop());
            invisibleSurface.UpdateMeshFromDLL(m_DisplayedObjects.InvisibleBrain.GetComponent<MeshFilter>().mesh);
            invisibleSurface.Dispose();
            m_Scene.MeshManager.SimplifiedMeshToUse.UpdateVisibilityMask(m_SimplifiedMasksStack.Pop()).Dispose();
            MeshHasInvisibleTriangles = m_Scene.MeshManager.BrainSurface.VisibilityMask.ToList().FindIndex((m) => m != 1) != -1;

            m_Scene.ResetGenerators();
            m_Scene.MeshManager.UpdateMeshesFromDLL();
            m_Scene.FMRIManager.UpdateSurfaceFMRIValues();
            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        #endregion
    }
}