﻿using HBP.Module3D;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class CCEPModeSelector : Tool
    {
        #region Properties
        /// <summary>
        /// Set the source mode to "Site source"
        /// </summary>
        [SerializeField] private Toggle m_Site;
        /// <summary>
        /// Set the source mode to "MarsAtlas area source"
        /// </summary>
        [SerializeField] private Toggle m_MarsAtlas;
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Site.onValueChanged.AddListener(isOn =>
            {
                if (ListenerLock) return;

                ((Column3DCCEP)SelectedColumn).Mode = Column3DCCEP.CCEPMode.Site;
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            });
            m_MarsAtlas.onValueChanged.AddListener(isOn =>
            {
                if (ListenerLock) return;

                ((Column3DCCEP)SelectedColumn).Mode = Column3DCCEP.CCEPMode.MarsAtlas;
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            gameObject.SetActive(false);
            m_Site.isOn = true;
            m_Site.interactable = false;
            m_MarsAtlas.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isColumnCCEP = SelectedColumn is Column3DCCEP;
            bool isMarsAtlasAvailable = ApplicationState.ProjectLoaded.Preferences.Tags.FirstOrDefault(t => t.Name == "MarsAtlas") != null && ApplicationState.Module3D.MarsAtlas.Loaded;

            gameObject.SetActive(isColumnCCEP);
            m_Site.interactable = isColumnCCEP;
            m_MarsAtlas.interactable = isColumnCCEP && isMarsAtlasAvailable;
        }
        #endregion
    }
}