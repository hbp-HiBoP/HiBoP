using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class BrainMeshes : Tool
    {
        #region Properties
        [SerializeField]
        private Toggle m_Left;

        [SerializeField]
        private Toggle m_Right;

        private bool m_IsMeshLeftRight;
        #endregion

        #region Private Methods
        /// <summary>
        /// Return the mesh part according to which mesh is displayed
        /// </summary>
        /// <param name="left">Is the left mesh displayed ?</param>
        /// <param name="right">Is the right mesh displayed ?</param>
        /// <returns>Mesh part enum identifier</returns>
        private Data.Enums.MeshPart GetMeshPart(bool left, bool right)
        {
            if (left && right) return Data.Enums.MeshPart.Both;
            if (left && !right) return Data.Enums.MeshPart.Left;
            if (!left && right) return Data.Enums.MeshPart.Right;
            return Data.Enums.MeshPart.None;
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Left.onValueChanged.AddListener((display) =>
            {
                if (ListenerLock) return;

                if (m_IsMeshLeftRight)
                {
                    if (m_Left.isOn || m_Right.isOn)
                    {
                        Data.Enums.MeshPart mesh = GetMeshPart(m_Left.isOn, m_Right.isOn);
                        SelectedScene.MeshManager.SelectMeshPart(mesh);
                    }
                    else
                    {
                        m_Right.isOn = true;
                    }
                }
                else
                {
                    Data.Enums.MeshPart mesh = GetMeshPart(true, true);
                    SelectedScene.MeshManager.SelectMeshPart(mesh);
                }
            });

            m_Right.onValueChanged.AddListener((display) =>
            {
                if (ListenerLock) return;

                if (m_IsMeshLeftRight)
                {
                    if (m_Left.isOn || m_Right.isOn)
                    {
                        Data.Enums.MeshPart mesh = GetMeshPart(m_Left.isOn, m_Right.isOn);
                        SelectedScene.MeshManager.SelectMeshPart(mesh);
                    }
                    else
                    {
                        m_Left.isOn = true;
                    }
                }
                else
                {
                    Data.Enums.MeshPart mesh = GetMeshPart(true, true);
                    SelectedScene.MeshManager.SelectMeshPart(mesh);
                }
            });
        }
        public override void DefaultState()
        {
            m_Left.isOn = false;
            m_Left.interactable = false;
            m_Right.isOn = false;
            m_Right.interactable = false;
        }
        public override void UpdateInteractable()
        {
            bool isMeshLeftRight = SelectedScene.MeshManager.SelectedMesh is LeftRightMesh3D;

            m_Left.interactable = isMeshLeftRight;
            m_Right.interactable = isMeshLeftRight;
        }
        public override void UpdateStatus()
        {
            ChangeBrainTypeCallback();
            switch (SelectedScene.SceneInformation.MeshPartToDisplay)
            {
                case Data.Enums.MeshPart.Left:
                    m_Left.isOn = true;
                    m_Right.isOn = false;
                    break;
                case Data.Enums.MeshPart.Right:
                    m_Left.isOn = false;
                    m_Right.isOn = true;
                    break;
                case Data.Enums.MeshPart.Both:
                    m_Left.isOn = true;
                    m_Right.isOn = true;
                    break;
                case Data.Enums.MeshPart.None:
                    m_Left.isOn = false;
                    m_Right.isOn = false;
                    break;
                default:
                    break;
            }
        }
        public void ChangeBrainTypeCallback()
        {
            m_IsMeshLeftRight = SelectedScene.MeshManager.SelectedMesh is LeftRightMesh3D;
            if (!m_IsMeshLeftRight)
            {
                m_Left.isOn = false;
                m_Right.isOn = false;
            }
            else
            {
                if (SelectedScene.SceneInformation.MeshPartToDisplay == Data.Enums.MeshPart.Both)
                {
                    m_Left.isOn = true;
                    m_Right.isOn = true;
                }
                else
                {
                    m_Left.isOn = (SelectedScene.SceneInformation.MeshPartToDisplay == Data.Enums.MeshPart.Left);
                    m_Right.isOn = (SelectedScene.SceneInformation.MeshPartToDisplay == Data.Enums.MeshPart.Right);
                }
            }
        }
        #endregion
    }
}