using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class CutAroundSite : Tool
    {
        #region Properties
        [SerializeField]
        private Button m_Button;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Button.onClick.AddListener(() =>
            {
                ApplicationState.Module3D.SelectedScene.CutAroundSelectedSite();
            });
        }
        public override void DefaultState()
        {
            m_Button.interactable = false;
        }
        public override void UpdateInteractable()
        {
            bool isInteractable = ApplicationState.Module3D.SelectedColumn.SelectedSite != null;

            m_Button.interactable = isInteractable;
        }
        #endregion
    }
}