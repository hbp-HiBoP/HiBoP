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

        [SerializeField]
        /// <summary>
        /// Sprite for the brain type icon
        /// </summary>
        private Sprite m_BrainTypeSprite;

        /// <summary>
        /// Dictionary that stores the MeshType info corresponding to the OptionData in the dropdown menu
        /// </summary>
        private Dictionary<Dropdown.OptionData, SceneStatesInfo.MeshType> m_BrainTypeByOptionData = new Dictionary<Dropdown.OptionData, SceneStatesInfo.MeshType>();
        /// <summary>
        /// Dictionary that stores the OptionData info corresponding to the MeshType in the dropdown menu
        /// </summary>
        private Dictionary<SceneStatesInfo.MeshType, Dropdown.OptionData> m_OptionDataByBrainType = new Dictionary<SceneStatesInfo.MeshType, Dropdown.OptionData>();
        
        public GenericEvent<SceneStatesInfo.MeshType> OnChangeValue = new GenericEvent<SceneStatesInfo.MeshType>();
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Dropdown.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                SceneStatesInfo.MeshType type = m_BrainTypeByOptionData[m_Dropdown.options[value]];
                ApplicationState.Module3D.SelectedScene.UpdateMeshTypeToDisplay(type);
                OnChangeValue.Invoke(type);
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
                m_BrainTypeByOptionData.Clear();
                m_OptionDataByBrainType.Clear();
                List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
                Dropdown.OptionData greyBrain = new Dropdown.OptionData("G", m_BrainTypeSprite);
                Dropdown.OptionData whiteBrain = new Dropdown.OptionData("W", m_BrainTypeSprite);
                Dropdown.OptionData inflatedBrain = new Dropdown.OptionData("I", m_BrainTypeSprite);
                if (ApplicationState.Module3D.SelectedScene.SceneInformation.GreyMeshesAvailables)
                {
                    options.Add(greyBrain);
                    m_BrainTypeByOptionData.Add(greyBrain, SceneStatesInfo.MeshType.Grey);
                    m_OptionDataByBrainType.Add(SceneStatesInfo.MeshType.Grey, greyBrain);
                }
                if (ApplicationState.Module3D.SelectedScene.SceneInformation.WhiteMeshesAvailables)
                {
                    options.Add(whiteBrain);
                    m_BrainTypeByOptionData.Add(whiteBrain, SceneStatesInfo.MeshType.White);
                    m_OptionDataByBrainType.Add(SceneStatesInfo.MeshType.White, whiteBrain);
                }
                if (ApplicationState.Module3D.SelectedScene.SceneInformation.WhiteInflatedMeshesAvailables)
                {
                    options.Add(inflatedBrain);
                    m_BrainTypeByOptionData.Add(inflatedBrain, SceneStatesInfo.MeshType.Inflated);
                    m_OptionDataByBrainType.Add(SceneStatesInfo.MeshType.Inflated, inflatedBrain);
                }
                m_Dropdown.options = options;

                int dropDownIndex = 0;
                switch (ApplicationState.Module3D.SelectedScene.SceneInformation.MeshTypeToDisplay)
                {
                    case SceneStatesInfo.MeshType.Grey:
                        dropDownIndex = m_Dropdown.options.FindIndex((o) => o == greyBrain);
                        break;
                    case SceneStatesInfo.MeshType.White:
                        dropDownIndex = m_Dropdown.options.FindIndex((o) => o == whiteBrain);
                        break;
                    case SceneStatesInfo.MeshType.Inflated:
                        dropDownIndex = m_Dropdown.options.FindIndex((o) => o == inflatedBrain);
                        break;
                    default:
                        dropDownIndex = 0;
                        break;
                }
                m_Dropdown.value = dropDownIndex;
            }
        }
        public void ChangeMarsAtlasCallback(bool isOn)
        {
            if (isOn)
            {
                Dropdown.OptionData whiteMeshOptionData = m_OptionDataByBrainType[SceneStatesInfo.MeshType.White];
                m_Dropdown.value = m_Dropdown.options.FindIndex((o) => o == whiteMeshOptionData);
            }
        }
        #endregion
    }
}