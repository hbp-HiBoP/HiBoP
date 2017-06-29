﻿using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class ROIManager : Tool
    {
        #region Properties
        [SerializeField]
        private Button m_AddROI;
        [SerializeField]
        private Dropdown m_ROISelector;
        [SerializeField]
        private InputField m_ROIName;
        [SerializeField]
        private Button m_RemoveROI;
        [SerializeField]
        private Dropdown m_VolumeSelector;
        [SerializeField]
        private Button m_RemoveVolume;
        #endregion

        #region Private Methods
        public void UpdateROIDropdownOptions()
        {
            m_ROISelector.options.Clear();
            for (int i = 0; i < ApplicationState.Module3D.SelectedColumn.ROIs.Count; i++)
            {
                ROI roi = ApplicationState.Module3D.SelectedColumn.ROIs[i];
                if (roi.Name == ROI.DEFAULT_ROI_NAME)
                {
                    m_ROISelector.options.Add(new Dropdown.OptionData("ROI " + i));
                }
                else
                {
                    m_ROISelector.options.Add(new Dropdown.OptionData(roi.Name));
                }
            }
            ListenerLock = true;
            m_ROISelector.value = m_ROISelector.value;
            ListenerLock = false;
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            ApplicationState.Module3D.OnChangeNumberOfROI.AddListener(() =>
            {
                UpdateROIDropdownOptions();
                UpdateInteractable();
            });
            ApplicationState.Module3D.OnSelectROI.AddListener(() =>
            {
                ListenerLock = true;
                int roiID = ApplicationState.Module3D.SelectedColumn.SelectedROIID;
                m_ROISelector.value = roiID;
                if (roiID != -1)
                {
                    m_ROIName.text = ApplicationState.Module3D.SelectedColumn.SelectedROI.Name;
                }
                else
                {
                    m_ROIName.text = "";
                }
                UpdateInteractable();
                ListenerLock = false;
            });
            m_AddROI.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                ApplicationState.Module3D.SelectedColumn.AddROI();
            });
            m_RemoveROI.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                ApplicationState.Module3D.SelectedColumn.RemoveSelectedROI();
            });
            m_ROISelector.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                ApplicationState.Module3D.SelectedColumn.SelectedROIID = value;
            });
            m_ROIName.onEndEdit.AddListener((value) =>
            {
                if (ListenerLock) return;

                ApplicationState.Module3D.SelectedColumn.SelectedROI.Name = value;
                UpdateROIDropdownOptions();
            });
        }
        public override void DefaultState()
        {
            m_AddROI.interactable = false;
            m_RemoveROI.interactable = false;
            m_ROIName.interactable = false;
            m_ROIName.text = "";
            m_ROISelector.interactable = false;
            m_VolumeSelector.interactable = false;
            m_RemoveVolume.interactable = false;
        }
        public override void UpdateInteractable()
        {
            bool hasROI = ApplicationState.Module3D.SelectedColumn.ROIs.Count > 0;
            bool hasVolume = false;
            if (hasROI && ApplicationState.Module3D.SelectedColumn.SelectedROI)
            {
                hasVolume = ApplicationState.Module3D.SelectedColumn.SelectedROI.NumberOfBubbles > 0;
            }
            switch (ApplicationState.Module3D.SelectedScene.ModesManager.CurrentMode.ID)
            {
                case Mode.ModesId.NoPathDefined:
                    m_AddROI.interactable = false;
                    m_RemoveROI.interactable = false;
                    m_ROIName.interactable = false;
                    m_ROISelector.interactable = false;
                    m_VolumeSelector.interactable = false;
                    m_RemoveVolume.interactable = false;
                    m_RemoveROI.gameObject.SetActive(false);
                    m_ROIName.gameObject.SetActive(false);
                    m_ROISelector.gameObject.SetActive(false);
                    m_VolumeSelector.gameObject.SetActive(false);
                    m_RemoveVolume.gameObject.SetActive(false);
                    break;
                case Mode.ModesId.MinPathDefined:
                    m_AddROI.interactable = true;
                    m_RemoveROI.interactable = hasROI;
                    m_ROIName.interactable = hasROI;
                    m_ROISelector.interactable = hasROI;
                    m_VolumeSelector.interactable = hasVolume;
                    m_RemoveVolume.interactable = hasVolume;
                    m_RemoveROI.gameObject.SetActive(hasROI);
                    m_ROIName.gameObject.SetActive(hasROI);
                    m_ROISelector.gameObject.SetActive(hasROI);
                    m_VolumeSelector.gameObject.SetActive(hasVolume);
                    m_RemoveVolume.gameObject.SetActive(hasVolume);
                    break;
                case Mode.ModesId.AllPathDefined:
                    m_AddROI.interactable = true;
                    m_RemoveROI.interactable = hasROI;
                    m_ROIName.interactable = hasROI;
                    m_ROISelector.interactable = hasROI;
                    m_VolumeSelector.interactable = hasVolume;
                    m_RemoveVolume.interactable = hasVolume;
                    m_RemoveROI.gameObject.SetActive(hasROI);
                    m_ROIName.gameObject.SetActive(hasROI);
                    m_ROISelector.gameObject.SetActive(hasROI);
                    m_VolumeSelector.gameObject.SetActive(hasVolume);
                    m_RemoveVolume.gameObject.SetActive(hasVolume);
                    break;
                case Mode.ModesId.ComputingAmplitudes:
                    m_AddROI.interactable = false;
                    m_RemoveROI.interactable = false;
                    m_ROIName.interactable = false;
                    m_ROISelector.interactable = false;
                    m_VolumeSelector.interactable = false;
                    m_RemoveVolume.interactable = false;
                    break;
                case Mode.ModesId.AmplitudesComputed:
                    m_AddROI.interactable = true;
                    m_RemoveROI.interactable = hasROI;
                    m_ROIName.interactable = hasROI;
                    m_ROISelector.interactable = hasROI;
                    m_VolumeSelector.interactable = hasVolume;
                    m_RemoveVolume.interactable = hasVolume;
                    m_RemoveROI.gameObject.SetActive(hasROI);
                    m_ROIName.gameObject.SetActive(hasROI);
                    m_ROISelector.gameObject.SetActive(hasROI);
                    m_VolumeSelector.gameObject.SetActive(hasVolume);
                    m_RemoveVolume.gameObject.SetActive(hasVolume);
                    break;
                case Mode.ModesId.TriErasing:
                    m_AddROI.interactable = false;
                    m_RemoveROI.interactable = false;
                    m_ROIName.interactable = false;
                    m_ROISelector.interactable = false;
                    m_VolumeSelector.interactable = false;
                    m_RemoveVolume.interactable = false;
                    m_RemoveROI.gameObject.SetActive(false);
                    m_ROIName.gameObject.SetActive(false);
                    m_ROISelector.gameObject.SetActive(false);
                    m_VolumeSelector.gameObject.SetActive(false);
                    m_RemoveVolume.gameObject.SetActive(false);
                    break;
                case Mode.ModesId.ROICreation:
                    m_AddROI.interactable = true;
                    m_RemoveROI.interactable = hasROI;
                    m_ROIName.interactable = hasROI;
                    m_ROISelector.interactable = hasROI;
                    m_VolumeSelector.interactable = hasVolume;
                    m_RemoveVolume.interactable = hasVolume;
                    m_RemoveROI.gameObject.SetActive(hasROI);
                    m_ROIName.gameObject.SetActive(hasROI);
                    m_ROISelector.gameObject.SetActive(hasROI);
                    m_VolumeSelector.gameObject.SetActive(hasVolume);
                    m_RemoveVolume.gameObject.SetActive(hasVolume);
                    break;
                case Mode.ModesId.AmpNeedUpdate:
                    m_AddROI.interactable = true;
                    m_RemoveROI.interactable = hasROI;
                    m_ROIName.interactable = hasROI;
                    m_ROISelector.interactable = hasROI;
                    m_VolumeSelector.interactable = hasVolume;
                    m_RemoveVolume.interactable = hasVolume;
                    m_RemoveROI.gameObject.SetActive(hasROI);
                    m_ROIName.gameObject.SetActive(hasROI);
                    m_ROISelector.gameObject.SetActive(hasROI);
                    m_VolumeSelector.gameObject.SetActive(hasVolume);
                    m_RemoveVolume.gameObject.SetActive(hasVolume);
                    break;
                case Mode.ModesId.Error:
                    m_AddROI.interactable = false;
                    m_RemoveROI.interactable = false;
                    m_ROIName.interactable = false;
                    m_ROISelector.interactable = false;
                    m_VolumeSelector.interactable = false;
                    m_RemoveVolume.interactable = false;
                    m_RemoveROI.gameObject.SetActive(false);
                    m_ROIName.gameObject.SetActive(false);
                    m_ROISelector.gameObject.SetActive(false);
                    m_VolumeSelector.gameObject.SetActive(false);
                    m_RemoveVolume.gameObject.SetActive(false);
                    break;
                default:
                    break;
            }
        }
        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Scene || type == Toolbar.UpdateToolbarType.Column)
            {
                UpdateROIDropdownOptions();
                ApplicationState.Module3D.OnSelectROI.Invoke();
            }
        }
        #endregion
    }
}