using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class Screenshot : Tool
    {
        #region Properties
        [SerializeField]
        private Button m_SingleScreenshot;
        [SerializeField]
        private Button m_MultiScreenshots;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_SingleScreenshot.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                ApplicationState.Module3D.SelectedScene.OnRequestScreenshot.Invoke(false);
            });
            m_MultiScreenshots.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                ApplicationState.Module3D.SelectedScene.OnRequestScreenshot.Invoke(true);
            });
        }
        public override void DefaultState()
        {
            m_SingleScreenshot.interactable = false;
            m_MultiScreenshots.interactable = false;
        }
        public override void UpdateInteractable()
        {
            switch (ApplicationState.Module3D.SelectedScene.ModesManager.CurrentMode.ID)
            {
                case Mode.ModesId.NoPathDefined:
                    m_SingleScreenshot.interactable = false;
                    m_MultiScreenshots.interactable = false;
                    break;
                case Mode.ModesId.MinPathDefined:
                    m_SingleScreenshot.interactable = true;
                    m_MultiScreenshots.interactable = true;
                    break;
                case Mode.ModesId.AllPathDefined:
                    m_SingleScreenshot.interactable = true;
                    m_MultiScreenshots.interactable = true;
                    break;
                case Mode.ModesId.ComputingAmplitudes:
                    m_SingleScreenshot.interactable = true;
                    m_MultiScreenshots.interactable = true;
                    break;
                case Mode.ModesId.AmplitudesComputed:
                    m_SingleScreenshot.interactable = true;
                    m_MultiScreenshots.interactable = true;
                    break;
                case Mode.ModesId.TriErasing:
                    m_SingleScreenshot.interactable = false;
                    m_MultiScreenshots.interactable = false;
                    break;
                case Mode.ModesId.ROICreation:
                    m_SingleScreenshot.interactable = false;
                    m_MultiScreenshots.interactable = false;
                    break;
                case Mode.ModesId.AmpNeedUpdate:
                    m_SingleScreenshot.interactable = true;
                    m_MultiScreenshots.interactable = true;
                    break;
                case Mode.ModesId.Error:
                    m_SingleScreenshot.interactable = false;
                    m_MultiScreenshots.interactable = false;
                    break;
                default:
                    break;
            }
        }
        #endregion
    }
}