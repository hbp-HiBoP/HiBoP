﻿using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class SiteModifier : Tool
    {
        #region Properties
        [SerializeField]
        private Button m_Button;
        [SerializeField]
        private DropWindow m_DropWindow;
        [SerializeField]
        private Dropdown m_Selector;
        [SerializeField]
        private InputField m_Filter;
        [SerializeField]
        private Dropdown m_Action;
        [SerializeField]
        private Toggle m_AllColumns;
        [SerializeField]
        private Button m_Apply;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            ApplicationState.Module3D.OnSelectSite.AddListener((site) =>
            {
                SiteFilter filter = (SiteFilter)m_Selector.value;
                m_Apply.interactable = !((filter == SiteFilter.Site || filter == SiteFilter.Electrode || filter == SiteFilter.Patient) && site == null);
            });
            m_Selector.onValueChanged.AddListener((value) =>
            {
                SiteFilter filter = (SiteFilter)value;
                if (filter == SiteFilter.Name || filter == SiteFilter.MarsAtlas || filter == SiteFilter.Broadman)
                {
                    m_Filter.gameObject.SetActive(true);
                }
                else
                {
                    m_Filter.gameObject.SetActive(false);
                }
                m_Apply.interactable = !((filter == SiteFilter.Site || filter == SiteFilter.Electrode || filter == SiteFilter.Patient) && ApplicationState.Module3D.SelectedColumn.SelectedSite == null);
            });
            m_Apply.onClick.AddListener(() =>
            {
                ApplicationState.Module3D.SelectedScene.UpdateSitesMasks(m_AllColumns.isOn, (SiteAction)m_Action.value, (SiteFilter)m_Selector.value, m_Filter.text);
                m_DropWindow.ChangeWindowState();
            });
            m_Filter.gameObject.SetActive(false);
        }
        public override void DefaultState()
        {
            m_Button.interactable = false;
            m_Selector.value = 0;
            m_Selector.interactable = false;
            m_Filter.text = "";
            m_Filter.interactable = false;
            m_Action.value = 0;
            m_Action.interactable = false;
            m_AllColumns.isOn = false;
            m_AllColumns.interactable = true;
            m_Apply.interactable = false;
        }
        public override void UpdateInteractable()
        {
            SiteFilter filter = (SiteFilter)m_Selector.value;
            bool interactable = !((filter == SiteFilter.Site || filter == SiteFilter.Electrode || filter == SiteFilter.Patient) && ApplicationState.Module3D.SelectedColumn.SelectedSite == null);
            m_Button.interactable = true;
            m_Selector.interactable = true;
            m_Filter.interactable = true;
            m_Action.interactable = true;
            m_Apply.interactable = interactable;
            m_AllColumns.interactable = true;
        }
        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Scene || type == Toolbar.UpdateToolbarType.Column)
            {
                Site site = ApplicationState.Module3D.SelectedColumn.SelectedSite;
                m_Apply.interactable = (site != null);
            }
        }
        #endregion
    }
}