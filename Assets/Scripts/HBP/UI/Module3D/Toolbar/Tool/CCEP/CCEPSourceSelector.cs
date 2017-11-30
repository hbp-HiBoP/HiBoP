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

                HBP.Module3D.Column3D column = ApplicationState.Module3D.SelectedColumn;
                column.SetCurrentSiteAsSource();
                ApplicationState.Module3D.SelectedScene.UpdateSitesRendering();
                m_Text.text = column.Sites[column.SourceSelectedID].Information.Name;
                UpdateInteractable();
            });
            m_UnsetSource.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                ApplicationState.Module3D.SelectedColumn.UndefineSource();
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
            bool isCCEP = false, isSourceDefined = false, isSiteSelected = false;
            HBP.Module3D.Base3DScene scene = ApplicationState.Module3D.SelectedScene;
            HBP.Module3D.Column3D column = ApplicationState.Module3D.SelectedColumn;
            isCCEP = scene.IsLatencyModeEnabled && (column.CurrentLatencyFile != -1) && ApplicationState.Module3D.SelectedScene.Type == SceneType.SinglePatient;
            isSourceDefined = column.SourceDefined;
            isSiteSelected = column.SelectedSite != null;
            if (isSiteSelected && column.CurrentLatencyFile != -1)
            {
                isSiteSelected &= scene.ColumnManager.SelectedImplantation.Latencies[column.CurrentLatencyFile].IsSiteASource(column.SelectedSiteID);
            }

            m_SetSource.interactable = isCCEP && isSiteSelected;
            m_UnsetSource.interactable = isCCEP && isSourceDefined;
        }

        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Column)
            {
                HBP.Module3D.Column3D column = ApplicationState.Module3D.SelectedColumn;
                if (column.SourceDefined)
                {
                    m_Text.text = column.Sites[column.SourceSelectedID].Information.Name;
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