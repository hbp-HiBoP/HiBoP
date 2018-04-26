using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class ImplantationSelector : Tool
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

                ApplicationState.Module3D.SelectedScene.UpdateSites(m_Dropdown.options[value].text);
            });
        }
        public override void DefaultState()
        {
            m_Dropdown.value = 0;
            m_Dropdown.interactable = false;
        }
        public override void UpdateInteractable()
        {
            m_Dropdown.interactable = true;
        }
        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Scene)
            {
                Base3DScene selectedScene = ApplicationState.Module3D.SelectedScene;
                m_Dropdown.options.Clear();
                if (selectedScene != null)
                {
                    foreach (Implantation3D implantation in selectedScene.ColumnManager.Implantations)
                    {
                        m_Dropdown.options.Add(new Dropdown.OptionData(implantation.Name));
                    }
                    m_Dropdown.value = selectedScene.ColumnManager.SelectedImplantationID;
                }
                m_Dropdown.RefreshShownValue();
            }
        }
        #endregion
    }
}