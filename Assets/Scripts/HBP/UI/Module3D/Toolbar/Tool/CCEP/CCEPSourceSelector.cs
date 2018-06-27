﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class CCEPSourceSelector : Tool
    {
        #region Properties
        [SerializeField]
        private Text m_Text;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
        }

        public override void DefaultState()
        {
            m_Text.text = "CCEP mode disabled";
        }

        public override void UpdateInteractable()
        {
        }

        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Column || type == Toolbar.UpdateToolbarType.Scene)
            {
                if (ApplicationState.Module3D.SelectedScene.IsLatencyModeEnabled)
                {
                    HBP.Module3D.Column3D column = ApplicationState.Module3D.SelectedColumn;
                    if (column.SourceDefined)
                    {
                        m_Text.text = column.Sites[column.SelectedSiteID].Information.DisplayedName;
                    }
                    else
                    {
                        m_Text.text = "No source selected";
                    }
                }
                else
                {
                    m_Text.text = "CCEP mode disabled";
                }
            }
        }
        #endregion
    }
}