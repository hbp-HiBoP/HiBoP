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
                SelectedScene.ImplantationManager.ComparingSites = isOn;
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
            bool isSiteSelected = SelectedColumn.SelectedSite != null;
            bool isComparingSites = SelectedScene.ImplantationManager.ComparingSites;

            m_Toggle.interactable = isSiteSelected || isComparingSites;
        }
        public override void UpdateStatus()
        {
            m_Toggle.isOn = SelectedScene.ImplantationManager.ComparingSites;
        }
        #endregion
    }
}