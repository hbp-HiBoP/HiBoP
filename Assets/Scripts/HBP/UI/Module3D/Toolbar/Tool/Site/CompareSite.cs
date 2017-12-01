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
        public override void Initialize()
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

            m_Toggle.interactable = isSiteSelected;
        }
        #endregion
    }
}