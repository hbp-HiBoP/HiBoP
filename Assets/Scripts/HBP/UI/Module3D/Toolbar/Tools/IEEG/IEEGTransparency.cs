using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class IEEGTransparency : Tool
    {
        #region Properties
        [SerializeField]
        private Slider m_Slider;

        public bool IsGlobal { get; set; }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Slider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;
                
                foreach (var column in GetColumnsDependingOnTypeAndGlobal(IsGlobal))
                {
                    column.DynamicParameters.AlphaMin = value;
                }
            });
        }

        public override void DefaultState()
        {
            m_Slider.value = 0.2f;
            m_Slider.interactable = false;
        }

        public override void UpdateInteractable()
        {
            bool isColumnDynamic = SelectedColumn is HBP.Module3D.Column3DDynamic;

            m_Slider.interactable = isColumnDynamic;
        }

        public override void UpdateStatus()
        {
            if (SelectedColumn is HBP.Module3D.Column3DDynamic dynamicColumn)
            {
                m_Slider.value = dynamicColumn.DynamicParameters.AlphaMin;
            }
            else
            {
                m_Slider.value = 0.2f;
            }
        }
        #endregion
    }
}