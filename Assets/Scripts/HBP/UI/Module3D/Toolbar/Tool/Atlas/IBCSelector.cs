using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class IBCSelector : Tool, IScrollHandler
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

                SelectedScene.ColumnManager.FMRIManager.SelectedIBCContrastID = value;
            });
        }

        public override void DefaultState()
        {
            m_Dropdown.gameObject.SetActive(false);
        }

        public override void UpdateInteractable()
        {
            bool isIBC = SelectedScene.ColumnManager.FMRIManager.DisplayIBCContrasts;

            m_Dropdown.gameObject.SetActive(isIBC);
        }

        public override void UpdateStatus()
        {
            m_Dropdown.options.Clear();
            if (ApplicationState.Module3D.IBCObjects.Loaded)
            {
                foreach (var contrast in ApplicationState.Module3D.IBCObjects.Contrasts)
                {
                    m_Dropdown.options.Add(new Dropdown.OptionData(contrast.Name));
                }
                m_Dropdown.value = SelectedScene.ColumnManager.FMRIManager.SelectedIBCContrastID;
            }
            m_Dropdown.RefreshShownValue();
        }

        public void OnScroll(PointerEventData eventData)
        {
            int newValue = m_Dropdown.value + (eventData.scrollDelta.y < 0 ? 1 : -1);
            int total = m_Dropdown.options.Count;
            m_Dropdown.value = ((newValue % total) + total) % total;
            m_Dropdown.RefreshShownValue();
        }
        #endregion
    }
}