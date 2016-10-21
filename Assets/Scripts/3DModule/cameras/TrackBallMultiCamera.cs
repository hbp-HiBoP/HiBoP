

/**
 * \file    TrackBallMultiCamera.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define TrackBallMultiCamera class
 */

// system
using System.Collections;
using System.Collections.Generic;
using System;

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

        /// <summary>
        /// Start is called before the first frame update only if the script instance is enabled.
        /// </summary>
        protected void Start()
        {
            m_spCamera = false;

            m_associatedScene = StaticVisuComponents.MPScene;
            m_associatedMPScene = (MP3DScene)m_associatedScene;
            m_MPCameraParent = transform.parent;

            int layer = 0;
            layer |= 1 << LayerMask.NameToLayer(m_columnLayer);
            layer |= 1 << LayerMask.NameToLayer("Meshes_MP");
            m_cullingMask = m_IRMFCullingMask = layer;
            m_minimizedCullingMask = 0;

            if (!m_isMinimized)
            {
                if (!m_IRMF)
                    GetComponent<Camera>().cullingMask = m_cullingMask;
                else
                    GetComponent<Camera>().cullingMask = m_IRMFCullingMask;
            }
            else
                GetComponent<Camera>().cullingMask = m_minimizedCullingMask;

            // listeners
            m_associatedScene.ModifyPlanesCuts.AddListener(() =>
            {
                if (!m_associatedScene.data_.volumeLoaded)
                    return;

                m_planesCutsCirclesVertices = new List<Vector3[]>();
                for (int ii = 0; ii < m_associatedScene.CM.planesList.Count; ++ii)
                {
                    Vector3 point = m_associatedScene.CM.planesList[ii].point;
                    point.x *= -1;
                    Vector3 normal = m_associatedScene.CM.planesList[ii].normal;
                    normal.x *= -1;
                    Quaternion q = Quaternion.FromToRotation(new Vector3(0, 0, 1), normal);
                    m_planesCutsCirclesVertices.Add(Geometry.create3DCirclePoints(new Vector3(0, 0, 0), 100, 150));
                    for (int jj = 0; jj < 150; ++jj)
                    {
                        m_planesCutsCirclesVertices[ii][jj] = q * m_planesCutsCirclesVertices[ii][jj];
                        m_planesCutsCirclesVertices[ii][jj] += point;
                    }
                }

                m_displayPlanesTimeStart = (float)TimeExecution.getWorldTime();
                m_displayPlanesTimer = 0;
                m_displayCutsCircles = true;
            });
        }


        /// <summary>
        /// Called multiple times per frame in response to GUI events. The Layout and Repaint events are processed first, followed by a Layout and keyboard/mouse event for each input event.
        /// </summary>
        new protected void OnGUI()
        {
            base.OnGUI();

            if (m_isMinimized || !m_cameraFocus || !m_moduleFocus)
                return;

            // zoom scroll mouse
            Vector2 scrollDelta = Input.mouseScrollDelta;
            if (scrollDelta.y != 0)
            {
                if (!m_associatedMPScene.ROIModeEnabled())
                {
                    if (scrollDelta.y < 0)
                        moveBackward(m_zoomSpeed);
                    else
                        moveForward(m_zoomSpeed);
                }
            }
        }

        /// <summary>
        ///  LateUpdate is called once per frame, after Update has finished. Any calculations that are performed in Update will have completed when LateUpdate begins.
        /// </summary>
        public void LateUpdate()
        {
            // if mouse not in the screen, abort
            if (!isFocus())
                return;

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
        }

        #endregion mono_behaviour
    }
}