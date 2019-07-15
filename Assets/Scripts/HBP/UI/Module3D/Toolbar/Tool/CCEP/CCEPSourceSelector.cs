using HBP.Module3D;
using System;
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
        [SerializeField] private Dropdown m_Dropdown;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Dropdown.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                if (SelectedColumn is Column3DCCEP ccepColumn)
                {
                    ccepColumn.SelectedSourceID = value - 1;
                }
            });
        }

        public override void DefaultState()
        {
            m_Dropdown.enabled = false;
        }

        public override void UpdateInteractable()
        {
            bool isColumnCCEP = SelectedColumn is Column3DCCEP;

            m_Dropdown.enabled = isColumnCCEP;
        }

        public override void UpdateStatus()
        {
            m_Dropdown.options.Clear();
            if (SelectedColumn is Column3DCCEP ccepColumn)
            {
                m_Dropdown.options.Add(new Dropdown.OptionData("No source selected"));
                foreach (Site site in ccepColumn.Sources)
                {
                    m_Dropdown.options.Add(new Dropdown.OptionData(string.Format("{0} ({1})", site.Information.ChannelName, site.Information.Patient.Name)));
                }
                m_Dropdown.value = ccepColumn.SelectedSourceID + 1;
                m_Dropdown.RefreshShownValue();
            }
        }
        #endregion
    }
}