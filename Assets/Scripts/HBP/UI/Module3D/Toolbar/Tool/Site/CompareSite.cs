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
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Toggle.onValueChanged.AddListener((isOn) =>
            {
                ApplicationState.Module3D.SelectedScene.ComparingSites = isOn;
                UpdateInteractable();
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
            bool isComparingSites = ApplicationState.Module3D.SelectedScene.ComparingSites;

            m_Toggle.interactable = isSiteSelected || isComparingSites;
        }
        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Scene)
            {
                m_Toggle.isOn = ApplicationState.Module3D.SelectedScene.ComparingSites;
            }
        }
        #endregion
    }
}