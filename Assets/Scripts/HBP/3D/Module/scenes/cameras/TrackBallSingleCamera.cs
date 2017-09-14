﻿
/**
 * \file    TrackBallSingleCamera.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define TrackBallSingleCamera class
 */

// system
using System.Collections.Generic;

// unity
using UnityEngine;

namespace HBP.Module3D.Cam
{
    /// <summary>
    /// A derived camera specialized for single patient 3D scene
    /// </summary>
    public class TrackBallSingleCamera : TrackBallCamera
    {
        #region Properties
        private Transform m_SinglePatientCameraParent; /**< SP camera parent */
        #endregion

        #region Private Methods
        protected void Start()
        {
            m_SceneType = SceneType.SinglePatient;

            //m_AssociatedScene = StaticComponents.SinglePatientScene;
            m_SinglePatientCameraParent = transform.parent;

            int layer = 0;
            layer |= 1 << LayerMask.NameToLayer(ColumnLayer);
            layer |= 1 << LayerMask.NameToLayer("Default");
            m_EEGCullingMask = FMRICullingMask = layer;
            MinimizedCullingMask = 0;

            if (!m_IsMinimized)
            {
                switch (Type)
                {
                    case CameraType.EEG:
                        GetComponent<Camera>().cullingMask = m_EEGCullingMask;
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
            m_AssociatedScene.OnModifyPlanesCuts.AddListener(() =>
            {
                if (!m_AssociatedScene.SceneInformation.MRILoaded)
                    return;

                m_PlanesCutsCirclesVertices = new List<Vector3[]>();
                for (int ii = 0; ii < m_AssociatedScene.Cuts.Count; ++ii)
                {
                    Vector3 point = m_AssociatedScene.Cuts[ii].Point;
                    point.x *= -1;
                    Vector3 normal = m_AssociatedScene.Cuts[ii].Normal;
                    normal.x *= -1;
                    Quaternion q = Quaternion.FromToRotation(new Vector3(0, 0, 1), normal);
                    m_PlanesCutsCirclesVertices.Add(Geometry.Create3DCirclePoints(new Vector3(0, 0, 0), 100, 150));
                    for (int jj = 0; jj < 150; ++jj)
                    {
                        m_PlanesCutsCirclesVertices[ii][jj] = q * m_PlanesCutsCirclesVertices[ii][jj];
                        m_PlanesCutsCirclesVertices[ii][jj] += point;
                    }
                }

                m_DisplayPlanesTimeStart = (float)TimeExecution.GetWorldTime();
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
                if (scrollDelta.y < 0)
                    MoveBackward(m_ZoomSpeed);
                else
                    MoveForward(m_ZoomSpeed);
            }
        }

        public void LateUpdate()
        {
            // if mouse not in the screen, abort
            if (!m_IsFocusedOnCamera)
                return;

            UnityEngine.Profiling.Profiler.BeginSample("TEST-LATE-Update-SP");

            // force others camera alignment
            foreach (Transform child in m_SinglePatientCameraParent)
            {
                if (child.gameObject.CompareTag("SingleCamera"))
                {
                    if (child.gameObject.GetComponent<TrackBallSingleCamera>().m_Line == m_Line)
                    {
                        child.transform.position = transform.position;
                        child.transform.rotation = transform.rotation;
                        child.GetComponent<TrackBallSingleCamera>().m_Target = m_Target;
                    }
                }
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }
        #endregion
    }
}