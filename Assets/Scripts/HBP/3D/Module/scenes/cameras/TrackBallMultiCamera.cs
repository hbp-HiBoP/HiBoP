

/**
 * \file    TrackBallMultiCamera.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define TrackBallMultiCamera class
 */

// system
using System.Collections.Generic;

// unity
using UnityEngine;


namespace HBP.Module3D.Cam
{
    /// <summary>
    /// A derived camera specialized for multi patients 3D scene
    /// </summary>
    public class TrackBallMultiCamera : TrackBallCamera
    {
        #region Properties
        private Transform m_MPCameraParent; /**< MP camera parent */
        private MultiPatients3DScene m_associatedMPScene; /**< MP associated scene */
        #endregion

        #region Private Methods
        protected void Start()
        {
            m_Type = SceneType.MultiPatients;

            m_AssociatedScene = StaticComponents.MultiPatientsScene;
            m_associatedMPScene = (MultiPatients3DScene)m_AssociatedScene;
            m_MPCameraParent = transform.parent;

            int layer = 0;
            layer |= 1 << LayerMask.NameToLayer(ColumnLayer);
            layer |= 1 << LayerMask.NameToLayer("Meshes_MP");
            EEGCullingMask = FMRICullingMask = layer;
            MinimizedCullingMask = 0;

            if (!m_IsMinimized)
            {
                switch (Type)
                {
                    case CameraType.EEG:
                        GetComponent<Camera>().cullingMask = EEGCullingMask;
                        break;
                    case CameraType.fMRI:
                        GetComponent<Camera>().cullingMask = FMRICullingMask;
                        break;
                    default:
                        break;
                }
            }
            else
                GetComponent<Camera>().cullingMask = MinimizedCullingMask;

            // listeners
            m_AssociatedScene.ModifyPlanesCuts.AddListener(() =>
            {
                if (!m_AssociatedScene.SceneInformation.mriLoaded)
                    return;

                m_PlanesCutsCirclesVertices = new List<Vector3[]>();
                for (int ii = 0; ii < m_AssociatedScene.PlanesList.Count; ++ii)
                {
                    Vector3 point = m_AssociatedScene.PlanesList[ii].point;
                    point.x *= -1;
                    Vector3 normal = m_AssociatedScene.PlanesList[ii].normal;
                    normal.x *= -1;
                    Quaternion q = Quaternion.FromToRotation(new Vector3(0, 0, 1), normal);
                    m_PlanesCutsCirclesVertices.Add(Geometry.create_3D_circle_points(new Vector3(0, 0, 0), 100, 150));
                    for (int jj = 0; jj < 150; ++jj)
                    {
                        m_PlanesCutsCirclesVertices[ii][jj] = q * m_PlanesCutsCirclesVertices[ii][jj];
                        m_PlanesCutsCirclesVertices[ii][jj] += point;
                    }
                }

                m_DisplayPlanesTimeStart = (float)TimeExecution.get_world_time();
                m_DisplayPlanesTimer = 0;
                m_DisplayCutsCircles = true;
            });
        }
        new protected void OnGUI()
        {
            base.OnGUI();

            if (m_IsMinimized || !m_IsFocusedOnCamera || !m_IsFocusedOn3DModule)
                return;

            // zoom scroll mouse
            Vector2 scrollDelta = Input.mouseScrollDelta;
            if (scrollDelta.y != 0)
            {
                if (!m_associatedMPScene.IsRegionOfInterestModeEnabled())
                {
                    if (scrollDelta.y < 0)
                        move_backward(m_ZoomSpeed);
                    else
                        move_forward(m_ZoomSpeed);
                }
            }
        }
        public void LateUpdate()
        {
            // if mouse not in the screen, abort
            if (!is_focus())
                return;

            // force others camera alignment
            foreach (Transform child in m_MPCameraParent)
            {
                if (child.gameObject.CompareTag("MultiCamera"))
                {
                    if (child.gameObject.GetComponent<TrackBallMultiCamera>().m_Line == m_Line)
                    {
                        child.transform.position = transform.position;
                        child.transform.rotation = transform.rotation;
                        child.GetComponent<TrackBallMultiCamera>().m_Target = m_Target;
                    }
                }
            }
        }
        #endregion
    }
}