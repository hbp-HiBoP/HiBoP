using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class BlacklistedSitesDisplay : Tool
    {
        #region Properties
        [SerializeField]
        private Toggle m_Toggle;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Toggle.onValueChanged.AddListener((isOn) =>
            {
                ApplicationState.Module3D.SelectedScene.HideBlacklistedSites = isOn;
            });
        }
        public override void DefaultState()
        {
            m_Toggle.interactable = false;
            m_Toggle.isOn = false;
        }
        public override void UpdateInteractable()
        {
            switch (ApplicationState.Module3D.SelectedScene.ModesManager.CurrentMode.ID)
            {
                case Mode.ModesId.NoPathDefined:
                    m_Toggle.interactable = false;
                    break;
                case Mode.ModesId.MinPathDefined:
                    m_Toggle.interactable = true;
                    break;
                case Mode.ModesId.AllPathDefined:
                    m_Toggle.interactable = true;
                    break;
                case Mode.ModesId.ComputingAmplitudes:
                    m_Toggle.interactable = false;
                    break;
                case Mode.ModesId.AmplitudesComputed:
                    m_Toggle.interactable = true;
                    break;
                case Mode.ModesId.TriErasing:
                    m_Toggle.interactable = false;
                    break;
                case Mode.ModesId.ROICreation:
                    m_Toggle.interactable = false;
                    break;
                case Mode.ModesId.AmpNeedUpdate:
                    m_Toggle.interactable = true;
                    break;
                case Mode.ModesId.Error:
                    m_Toggle.interactable = false;
                    break;
                default:
                    break;
            }
        }
        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Scene)
            {
                m_Toggle.isOn = ApplicationState.Module3D.SelectedScene.SceneInformation.HideBlacklistedSites;
            }
        }
        #endregion
    }
}