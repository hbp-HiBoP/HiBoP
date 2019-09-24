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
        [SerializeField] private Dropdown m_Dropdown;
        
        public GenericEvent<int> OnChangeValue = new GenericEvent<int>();
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Dropdown.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                SelectedScene.MeshManager.Select(m_Dropdown.options[value].text);
                OnChangeValue.Invoke(value);
            });
        }
        public override void DefaultState()
        {
            m_Dropdown.value = 0;
            m_Dropdown.interactable = false;
        }
        public override void UpdateInteractable()
        {
            m_Dropdown.interactable = true;
        }
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