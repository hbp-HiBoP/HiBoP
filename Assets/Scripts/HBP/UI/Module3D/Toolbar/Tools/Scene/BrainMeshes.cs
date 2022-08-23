﻿using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Enums;

namespace HBP.UI.Toolbar
{
    public class BrainMeshes : Tool
    {
        #region Properties
        /// <summary>
        /// Display left part of the mesh
        /// </summary>
        [SerializeField] private Toggle m_Left;
        /// <summary>
        /// Display right part of the mesh
        /// </summary>
        [SerializeField] private Toggle m_Right;
        /// <summary>
        /// Is the currently selected mesh left-right or single ?
        /// </summary>
        private bool m_IsMeshLeftRight;
        #endregion

        #region Private Methods
        /// <summary>
        /// Return the mesh part according to which mesh is displayed
        /// </summary>
        /// <param name="left">Is the left mesh displayed ?</param>
        /// <param name="right">Is the right mesh displayed ?</param>
        /// <returns>Mesh part enum identifier</returns>
        private MeshPart GetMeshPart(bool left, bool right)
        {
            if (left && right) return MeshPart.Both;
            if (left && !right) return MeshPart.Left;
            if (!left && right) return MeshPart.Right;
            return MeshPart.None;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Left.onValueChanged.AddListener((display) =>
            {
                if (ListenerLock) return;

                if (m_IsMeshLeftRight)
                {
                    if (m_Left.isOn || m_Right.isOn)
                    {
                        MeshPart mesh = GetMeshPart(m_Left.isOn, m_Right.isOn);
                        SelectedScene.MeshManager.SelectMeshPart(mesh);
                    }
                    else
                    {
                        m_Right.isOn = true;
                    }
                }
                else
                {
                    MeshPart mesh = GetMeshPart(true, true);
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
                        MeshPart mesh = GetMeshPart(m_Left.isOn, m_Right.isOn);
                        SelectedScene.MeshManager.SelectMeshPart(mesh);
                    }
                    else
                    {
                        m_Left.isOn = true;
                    }
                }
                else
                {
                    MeshPart mesh = GetMeshPart(true, true);
                    SelectedScene.MeshManager.SelectMeshPart(mesh);
                }
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Left.isOn = false;
            m_Left.interactable = false;
            m_Right.isOn = false;
            m_Right.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isMeshLeftRight = SelectedScene.MeshManager.SelectedMesh is Core.Object3D.LeftRightMesh3D;

            m_Left.interactable = isMeshLeftRight;
            m_Right.interactable = isMeshLeftRight;
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            ChangeBrainTypeCallback();
            switch (SelectedScene.MeshManager.MeshPartToDisplay)
            {
                case MeshPart.Left:
                    m_Left.isOn = true;
                    m_Right.isOn = false;
                    break;
                case MeshPart.Right:
                    m_Left.isOn = false;
                    m_Right.isOn = true;
                    break;
                case MeshPart.Both:
                    m_Left.isOn = true;
                    m_Right.isOn = true;
                    break;
                case MeshPart.None:
                    m_Left.isOn = false;
                    m_Right.isOn = false;
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// Callback method when the brain has been changed
        /// </summary>
        public void ChangeBrainTypeCallback()
        {
            m_IsMeshLeftRight = SelectedScene.MeshManager.SelectedMesh is Core.Object3D.LeftRightMesh3D;
            if (!m_IsMeshLeftRight)
            {
                m_Left.isOn = false;
                m_Right.isOn = false;
            }
            else
            {
                if (SelectedScene.MeshManager.MeshPartToDisplay == MeshPart.Both)
                {
                    m_Left.isOn = true;
                    m_Right.isOn = true;
                }
                else
                {
                    m_Left.isOn = (SelectedScene.MeshManager.MeshPartToDisplay == MeshPart.Left);
                    m_Right.isOn = (SelectedScene.MeshManager.MeshPartToDisplay == MeshPart.Right);
                }
            }
        }
        #endregion
    }
}