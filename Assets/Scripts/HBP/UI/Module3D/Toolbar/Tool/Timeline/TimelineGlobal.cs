using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class TimelineGlobal : Tool
    {
        #region Properties
        [SerializeField]
        private Toggle m_Toggle;

        public GenericEvent<bool> OnChangeValue = new GenericEvent<bool>();
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Toggle.onValueChanged.AddListener((isOn) =>
            {
                if (ListenerLock) return;
                
                OnChangeValue.Invoke(isOn);
            });
        }

        public override void DefaultState()
        {
            m_Toggle.interactable = false;
        }

        public override void UpdateInteractable()
        {
            bool isColumnIEEG = SelectedColumn.Type == Data.Enums.ColumnType.iEEG;
            bool areAmplitudesComputed = SelectedScene.SceneInformation.IsGeneratorUpToDate;

            m_Toggle.interactable = isColumnIEEG && areAmplitudesComputed;
        }
        #endregion
    }
}