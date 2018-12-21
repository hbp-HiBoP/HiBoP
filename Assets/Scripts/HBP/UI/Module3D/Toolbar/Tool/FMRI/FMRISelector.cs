using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class FMRISelector : Tool
    {
        #region Properties
        [SerializeField]
        private Button m_AddFMRI;

        [SerializeField]
        private Button m_RemoveFMRI;

        public UnityEvent OnChangeFMRI = new UnityEvent();
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_AddFMRI.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SelectedScene.LoadFMRI();
                OnChangeFMRI.Invoke();
            });
            m_RemoveFMRI.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SelectedScene.UnloadFMRI();
                OnChangeFMRI.Invoke();
            });
        }

        public override void DefaultState()
        {
            m_AddFMRI.interactable = false;
            m_RemoveFMRI.interactable = false;
        }

        public override void UpdateInteractable()
        {
            bool hasFMRI = SelectedScene.ColumnManager.FMRI != null;

            m_AddFMRI.interactable = !hasFMRI;
            m_RemoveFMRI.interactable = hasFMRI;
        }
        #endregion
    }
}