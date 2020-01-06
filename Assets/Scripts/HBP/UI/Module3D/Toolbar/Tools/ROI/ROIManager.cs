using HBP.Module3D;
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
        private RectTransform m_ROINameParent;
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
            m_ROISelector.options.Add(new Dropdown.OptionData("None"));
            for (int i = 0; i < SelectedScene.ROIManager.ROIs.Count; i++)
            {
                ROI roi = SelectedScene.ROIManager.ROIs[i];
                if (roi.Name == "ROI")
                {
                    m_ROISelector.options.Add(new Dropdown.OptionData("ROI " + i));
                }
                else
                {
                    m_ROISelector.options.Add(new Dropdown.OptionData(roi.Name));
                }
            }
            m_ROISelector.RefreshShownValue();
        }
        public void UpdateVolumeDropdownOptions()
        {
            m_VolumeSelector.options.Clear();
            m_VolumeSelector.options.Add(new Dropdown.OptionData("None"));
            ROI selectedROI = SelectedScene.ROIManager.SelectedROI;
            if (selectedROI)
            {
                for (int i = 0; i < selectedROI.Spheres.Count; i++)
                {
                    Sphere sphere = selectedROI.Spheres[i];
                    m_VolumeSelector.options.Add(new Dropdown.OptionData("Sphere " + i + " (R=" + sphere.Radius.ToString("N1") + ")"));
                }
            }
            m_VolumeSelector.RefreshShownValue();
        }
        public void UpdateSelectedROIUI()
        {
            ListenerLock = true;
            int roiID = SelectedScene.ROIManager.SelectedROIID;
            m_ROISelector.value = roiID + 1;
            if (roiID != -1)
            {
                m_ROIName.text = SelectedScene.ROIManager.SelectedROI.Name;
            }
            else
            {
                m_ROIName.text = "";
            }
            UpdateInteractable();
            UpdateVolumeDropdownOptions();
            if (roiID != -1)
            {
                m_VolumeSelector.value = SelectedScene.ROIManager.SelectedROI.SelectedSphereID + 1;
            }
            ListenerLock = false;
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_AddROI.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SelectedScene.ROIManager.AddROI();
            });
            m_RemoveROI.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SelectedScene.ROIManager.RemoveSelectedROI();
            });
            m_ROISelector.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                SelectedScene.ROIManager.SelectedROIID = value - 1;
            });
            m_ROIName.onEndEdit.AddListener((value) =>
            {
                if (ListenerLock) return;

                SelectedScene.ROIManager.SelectedROI.Name = value;
                UpdateROIDropdownOptions();
            });
            m_VolumeSelector.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                SelectedScene.ROIManager.SelectedROI.SelectSphere(value - 1);
            });
            m_RemoveVolume.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SelectedScene.ROIManager.SelectedROI.RemoveSelectedSphere();
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
            bool hasROI = SelectedScene.ROIManager.ROIs.Count > 0;
            bool hasVolume = false;
            if (hasROI && SelectedScene.ROIManager.SelectedROI)
            {
                hasVolume = SelectedScene.ROIManager.SelectedROI.Spheres.Count > 0;
            }

            m_AddROI.interactable = true;
            m_RemoveROI.interactable = hasROI;
            m_ROIName.interactable = hasROI;
            m_ROISelector.interactable = hasROI;
            m_VolumeSelector.interactable = hasVolume;
            m_RemoveVolume.interactable = hasVolume;
        }
        public override void UpdateStatus()
        {
            UpdateROIDropdownOptions();
            UpdateSelectedROIUI();
        }
        #endregion
    }
}