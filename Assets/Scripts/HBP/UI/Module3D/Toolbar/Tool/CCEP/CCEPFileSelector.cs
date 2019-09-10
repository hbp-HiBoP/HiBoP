using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class CCEPFileSelector : Tool
    {
        #region Properties
        [SerializeField]
        private Dropdown m_Dropdown;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Dropdown.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                SelectedColumn.CurrentLatencyFile = value;
            });
        }

        public override void DefaultState()
        {
            m_Dropdown.interactable = false;
        }

        public override void UpdateInteractable()
        {
            bool isCCEP = SelectedScene.IsLatencyModeEnabled && SelectedScene.Type == Data.Enums.SceneType.SinglePatient;

            m_Dropdown.interactable = isCCEP;
        }

        public override void UpdateStatus()
        {
            m_Dropdown.options.Clear();
            if (SelectedScene.Type == Data.Enums.SceneType.SinglePatient)
            {
                foreach (HBP.Module3D.Latencies latencies in SelectedScene.ImplantationsManager.SelectedImplantation.Latencies)
                {
                    m_Dropdown.options.Add(new Dropdown.OptionData(latencies.Name));
                }
                m_Dropdown.value = SelectedColumn.CurrentLatencyFile != -1 ? SelectedColumn.CurrentLatencyFile : 0;
            }
            m_Dropdown.RefreshShownValue();
        }
        #endregion
    }
}