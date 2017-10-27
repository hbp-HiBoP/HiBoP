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
        [SerializeField]
        private Button m_SetSource;
        [SerializeField]
        private Button m_UnsetSource;
        [SerializeField]
        private Text m_Text;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_SetSource.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                ((HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedColumn).SetCurrentSiteAsSource();
                ApplicationState.Module3D.SelectedScene.UpdateSitesRendering();
                m_Text.text = ApplicationState.Module3D.SelectedColumn.Sites[((HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedColumn).SourceSelectedID].Information.Name;
                UpdateInteractable();
            });
            m_UnsetSource.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                ((HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedColumn).UndefineSource();
                ApplicationState.Module3D.SelectedScene.UpdateSitesRendering();
                m_Text.text = "None";
                UpdateInteractable();
            });
        }

        public override void DefaultState()
        {
            m_SetSource.interactable = false;
            m_UnsetSource.interactable = false;
            m_Text.text = "None";
        }

        public override void UpdateInteractable()
        {
            bool isCCEP = ApplicationState.Module3D.SelectedScene.IsLatencyModeEnabled && (((HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedColumn).CurrentLatencyFile != -1);
            bool isSourceDefined = ((HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedColumn).SourceDefined;
            bool isSiteSelected = ApplicationState.Module3D.SelectedColumn.SelectedSite != null;
            switch (ApplicationState.Module3D.SelectedScene.ModesManager.CurrentModeID)
            {
                case HBP.Module3D.Mode.ModesId.NoPathDefined:
                    m_SetSource.interactable = false;
                    m_UnsetSource.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.MinPathDefined:
                    m_SetSource.interactable = isCCEP && isSiteSelected;
                    m_UnsetSource.interactable = isCCEP && isSourceDefined;
                    break;
                case HBP.Module3D.Mode.ModesId.AllPathDefined:
                    m_SetSource.interactable = isCCEP && isSiteSelected;
                    m_UnsetSource.interactable = isCCEP && isSourceDefined;
                    break;
                case HBP.Module3D.Mode.ModesId.ComputingAmplitudes:
                    m_SetSource.interactable = isCCEP && isSiteSelected;
                    m_UnsetSource.interactable = isCCEP && isSourceDefined;
                    break;
                case HBP.Module3D.Mode.ModesId.AmplitudesComputed:
                    m_SetSource.interactable = isCCEP && isSiteSelected;
                    m_UnsetSource.interactable = isCCEP && isSourceDefined;
                    break;
                case HBP.Module3D.Mode.ModesId.TriErasing:
                    m_SetSource.interactable = false;
                    m_UnsetSource.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.ROICreation:
                    m_SetSource.interactable = false;
                    m_UnsetSource.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.AmpNeedUpdate:
                    m_SetSource.interactable = isCCEP && isSiteSelected;
                    m_UnsetSource.interactable = isCCEP && isSourceDefined;
                    break;
                case HBP.Module3D.Mode.ModesId.Error:
                    m_SetSource.interactable = false;
                    m_UnsetSource.interactable = false;
                    break;
                default:
                    break;
            }
        }

        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Column)
            {
                if (((HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedColumn).SourceDefined)
                {
                    m_Text.text = ApplicationState.Module3D.SelectedColumn.Sites[((HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedColumn).SourceSelectedID].Information.Name;
                }
                else
                {
                    m_Text.text = "None";
                }
            }
        }
        #endregion
    }
}