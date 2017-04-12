

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


namespace HBP.VISU3D.Cam
{
    /// <summary>
    /// A derived camera specialized for multi patients 3D scene
    /// </summary>
    public class TrackBallMultiCamera : TrackBallCamera
    {
        #region members

        private Transform m_MPCameraParent; /**< MP camera parent */
        private MP3DScene m_associatedMPScene; /**< MP associated scene */

        #endregion members

        #region mono_behaviour

        protected void Start()
        {
            m_spCamera = false;

            m_associatedScene = StaticComponents.MPScene;
            m_associatedMPScene = (MP3DScene)m_associatedScene;
            m_MPCameraParent = transform.parent;

            int layer = 0;
            layer |= 1 << LayerMask.NameToLayer(m_columnLayer);
            layer |= 1 << LayerMask.NameToLayer("Meshes_MP");
            m_cullingMask = m_IRMFCullingMask = layer;
            m_minimizedCullingMask = 0;

            if (!m_isMinimized)
            {
                if (!m_fMRI)
                    GetComponent<Camera>().cullingMask = m_cullingMask;
                else
                    GetComponent<Camera>().cullingMask = m_IRMFCullingMask;
            }
            else
                GetComponent<Camera>().cullingMask = m_minimizedCullingMask;

            // listeners
            m_associatedScene.ModifyPlanesCuts.AddListener(() =>
            {
                if (!m_associatedScene.data_.mriLoaded)
                    return;

                m_planesCutsCirclesVertices = new List<Vector3[]>();
                for (int ii = 0; ii < m_associatedScene.planesList.Count; ++ii)
                {
                    Vector3 point = m_associatedScene.planesList[ii].point;
                    point.x *= -1;
                    Vector3 normal = m_associatedScene.planesList[ii].normal;
                    normal.x *= -1;
                    Quaternion q = Quaternion.FromToRotation(new Vector3(0, 0, 1), normal);
                    m_planesCutsCirclesVertices.Add(Geometry.create_3D_circle_points(new Vector3(0, 0, 0), 100, 150));
                    for (int jj = 0; jj < 150; ++jj)
                    {
                        m_planesCutsCirclesVertices[ii][jj] = q * m_planesCutsCirclesVertices[ii][jj];
                        m_planesCutsCirclesVertices[ii][jj] += point;
                    }
                }

                m_displayPlanesTimeStart = (float)TimeExecution.get_world_time();
                m_displayPlanesTimer = 0;
                m_displayCutsCircles = true;
            });
        }

        new protected void OnGUI()
        {
            base.OnGUI();

            if (m_isMinimized || !m_cameraFocus || !m_moduleFocus)
                return;

            // zoom scroll mouse
            Vector2 scrollDelta = Input.mouseScrollDelta;
            if (scrollDelta.y != 0)
            {
                if (!m_associatedMPScene.is_ROI_mode_enabled())
                {
                    if (scrollDelta.y < 0)
                        move_backward(m_zoomSpeed);
                    else
                        move_forward(m_zoomSpeed);
                    m_camerasNeedUpdate = true;
                }
            }
        }

        public void LateUpdate()
        {
            if (!m_camerasNeedUpdate)
            {
                return;
            }

            // force others camera alignment
            foreach (Transform child in m_MPCameraParent)
            {
                if (child.gameObject.CompareTag("MultiCamera"))
                {
                    if (child.gameObject.GetComponent<TrackBallMultiCamera>().m_idLineCamera == m_idLineCamera)
                    {
                        child.transform.position = transform.position;
                        child.transform.rotation = transform.rotation;
                        child.GetComponent<TrackBallMultiCamera>().m_target = m_target;
                    }
                }
            }

            m_camerasNeedUpdate = false;
        }

        #endregion mono_behaviour
    }
}