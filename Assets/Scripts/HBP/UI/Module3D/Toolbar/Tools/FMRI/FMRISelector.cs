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
        [SerializeField] private Button m_AddFMRI;
        [SerializeField] private Button m_RemoveFMRI;
        private string m_LastFMRIPath;

        public UnityEvent OnChangeFMRI = new UnityEvent();
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_AddFMRI.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                string path = FileBrowser.GetExistingFileName(new string[] { "nii", "img" }, "Select an fMRI file", m_LastFMRIPath);
                if (!string.IsNullOrEmpty(path))
                {
                    m_LastFMRIPath = path;
                    SelectedScene.FMRIManager.FMRI = new MRI3D(new Data.Anatomy.MRI("FMRI", path));
                }
                OnChangeFMRI.Invoke();
            });
            m_RemoveFMRI.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SelectedScene.FMRIManager.FMRI = null;
                OnChangeFMRI.Invoke();
            });
        }

        public override void DefaultState()
        {
            m_AddFMRI.interactable = false;
            m_RemoveFMRI.interactable = false;
            m_LastFMRIPath = "";
        }

        public override void UpdateInteractable()
        {
            bool hasFMRI = SelectedScene.FMRIManager.FMRI != null;

            m_AddFMRI.interactable = !hasFMRI;
            m_RemoveFMRI.interactable = hasFMRI;
        }
        #endregion
    }
}