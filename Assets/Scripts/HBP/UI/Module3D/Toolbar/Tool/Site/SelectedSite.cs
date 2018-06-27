﻿using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class SelectedSite : Tool
    {
        #region Properties
        [SerializeField]
        private Text m_Text;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            ApplicationState.Module3D.OnSelectSite.AddListener((site) =>
            {
                if (site)
                {
                    m_Text.text = site.Information.DisplayedName;
                }
                else
                {
                    DefaultState();
                }
            });
        }
        public override void DefaultState()
        {
            m_Text.text = "No site selected";
        }
        public override void UpdateInteractable()
        {

        }
        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Column || type == Toolbar.UpdateToolbarType.Scene)
            {
                Site site = ApplicationState.Module3D.SelectedColumn.SelectedSite;
                m_Text.text = site ? site.Information.DisplayedName : "No site selected";
            }
        }
        #endregion
    }
}