using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class MRISelector : Tool
    {
        #region Properties
        [SerializeField]
        private Dropdown m_Dropdown;

        public GenericEvent<int> OnChangeValue = new GenericEvent<int>();
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Dropdown.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                ApplicationState.Module3D.SelectedScene.UpdateMRIToDisplay(value);
                OnChangeValue.Invoke(value);
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
                foreach (MRI3D mri in selectedScene.ColumnManager.MRIs)
                {
                    m_Dropdown.options.Add(new Dropdown.OptionData(mri.Name));
                }
                m_Dropdown.value = selectedScene.ColumnManager.SelectedMRIID;
                m_Dropdown.RefreshShownValue();
            }
        }
        #endregion
    }
}