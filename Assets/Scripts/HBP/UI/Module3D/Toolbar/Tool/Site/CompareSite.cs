using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class CompareSite : Tool
    {
        #region Properties
        [SerializeField]
        private Toggle m_Toggle;

        private Site m_LastSelectedSite;
        #endregion

        #region Public Methods
        public override void AddListeners()
        {
            ApplicationState.Module3D.OnSelectSite.AddListener((site) =>
            {
                if (!site)
                {
                    m_Toggle.interactable = false;
                    m_Toggle.isOn = false;
                    return;
                }
                else
                {
                    m_Toggle.interactable = true;
                }

                if (m_Toggle.isOn)
                {
                    ApplicationState.Module3D.SelectedScene.SendAdditionalSiteInfoRequest(m_LastSelectedSite);
                    m_Toggle.isOn = false;
                }
            });

            m_Toggle.onValueChanged.AddListener((isOn) =>
            {
                m_LastSelectedSite = isOn ? ApplicationState.Module3D.SelectedColumn.SelectedSite : null;
            });
        }
        public override void DefaultState()
        {
            m_Toggle.interactable = false;
            m_Toggle.isOn = false;
        }
        public override void UpdateInteractable()
        {
            bool isSiteSelected = ApplicationState.Module3D.SelectedColumn.SelectedSite != null;
            switch (ApplicationState.Module3D.SelectedScene.ModesManager.CurrentMode.ID)
            {
                case Mode.ModesId.NoPathDefined:
                    m_Toggle.interactable = false;
                    break;
                case Mode.ModesId.MinPathDefined:
                    m_Toggle.interactable = isSiteSelected;
                    break;
                case Mode.ModesId.AllPathDefined:
                    m_Toggle.interactable = isSiteSelected;
                    break;
                case Mode.ModesId.ComputingAmplitudes:
                    m_Toggle.interactable = false;
                    break;
                case Mode.ModesId.AmplitudesComputed:
                    m_Toggle.interactable = isSiteSelected;
                    break;
                case Mode.ModesId.TriErasing:
                    m_Toggle.interactable = false;
                    break;
                case Mode.ModesId.ROICreation:
                    m_Toggle.interactable = false;
                    break;
                case Mode.ModesId.AmpNeedUpdate:
                    m_Toggle.interactable = isSiteSelected;
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
            if (type == Toolbar.UpdateToolbarType.Scene || type == Toolbar.UpdateToolbarType.Column)
            {
                Site site = ApplicationState.Module3D.SelectedColumn.SelectedSite;
                if (!site)
                {
                    m_Toggle.interactable = false;
                    m_Toggle.isOn = false;
                }
            }
        }
        #endregion
    }
}