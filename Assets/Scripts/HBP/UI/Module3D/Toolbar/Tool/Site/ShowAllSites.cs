using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class ShowAllSites : Tool
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
                if (ListenerLock) return;
                
                ApplicationState.Module3D.SelectedScene.ShowAllSites = isOn;
            });
        }
        public override void DefaultState()
        {
            m_Toggle.interactable = false;
            m_Toggle.isOn = false;
        }
        public override void UpdateInteractable()
        {
            m_Toggle.interactable = true;
        }
        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Scene)
            {
                m_Toggle.isOn = ApplicationState.Module3D.SelectedScene.ShowAllSites;
            }
        }
        #endregion
    }
}