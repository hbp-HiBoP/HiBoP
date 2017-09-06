using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class BrainTypes : Tool
    {
        #region Properties
        [SerializeField]
        private Dropdown m_Dropdown;
        
        public GenericEvent<int> OnChangeValue = new GenericEvent<int>();
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Dropdown.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;
                
                ApplicationState.Module3D.SelectedScene.UpdateMeshToDisplay(value);
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
            switch (ApplicationState.Module3D.SelectedScene.ModesManager.CurrentMode.ID)
            {
                case Mode.ModesId.NoPathDefined:
                    m_Dropdown.interactable = false;
                    break;
                case Mode.ModesId.MinPathDefined:
                    m_Dropdown.interactable = true;
                    break;
                case Mode.ModesId.AllPathDefined:
                    m_Dropdown.interactable = true;
                    break;
                case Mode.ModesId.ComputingAmplitudes:
                    m_Dropdown.interactable = false;
                    break;
                case Mode.ModesId.AmplitudesComputed:
                    m_Dropdown.interactable = true;
                    break;
                case Mode.ModesId.TriErasing:
                    m_Dropdown.interactable = false;
                    break;
                case Mode.ModesId.ROICreation:
                    m_Dropdown.interactable = false;
                    break;
                case Mode.ModesId.AmpNeedUpdate:
                    m_Dropdown.interactable = true;
                    break;
                case Mode.ModesId.Error:
                    m_Dropdown.interactable = false;
                    break;
                default:
                    break;
            }
        }
        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Scene)
            {
                Base3DScene selectedScene = ApplicationState.Module3D.SelectedScene;
                m_Dropdown.options.Clear();
                foreach (Mesh3D mesh in selectedScene.ColumnManager.Meshes)
                {
                    m_Dropdown.options.Add(new Dropdown.OptionData(mesh.Name.ToString()));
                }
                m_Dropdown.value = selectedScene.ColumnManager.SelectedMeshID;
                m_Dropdown.RefreshShownValue();
            }
        }
        #endregion
    }
}