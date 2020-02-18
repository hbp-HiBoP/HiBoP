using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class BrainSelector : Tool
    {
        #region Properties
        /// <summary>
        /// Dropdown to select the brain to display
        /// </summary>
        [SerializeField] private Dropdown m_Dropdown;
        #endregion

        #region Events
        /// <summary>
        /// Event called when the value of the dropdown has been changed
        /// </summary>
        public GenericEvent<int> OnChangeValue = new GenericEvent<int>();
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Dropdown.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                SelectedScene.MeshManager.Select(m_Dropdown.options[value].text);
                OnChangeValue.Invoke(value);
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Dropdown.value = 0;
            m_Dropdown.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            m_Dropdown.interactable = true;
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            m_Dropdown.options.Clear();
            foreach (Mesh3D mesh in SelectedScene.MeshManager.Meshes)
            {
                m_Dropdown.options.Add(new Dropdown.OptionData(mesh.Name.ToString()));
            }
            m_Dropdown.value = SelectedScene.MeshManager.SelectedMeshID;
            m_Dropdown.RefreshShownValue();
        }
        #endregion
    }
}