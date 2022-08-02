using HBP.Module3D;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class ROIManager : Tool
    {
        #region Properties
        /// <summary>
        /// Button to add a ROI to the scene
        /// </summary>
        [SerializeField] private Button m_AddROI;
        /// <summary>
        /// Dropdown to select a ROI
        /// </summary>
        [SerializeField] private Dropdown m_ROISelector;
        /// <summary>
        /// RectTransform of the label to display the name of the selected ROI
        /// </summary>
        [SerializeField] private RectTransform m_ROINameParent;
        /// <summary>
        /// Inputfield to change the name of the ROI
        /// </summary>
        [SerializeField] private InputField m_ROIName;
        /// <summary>
        /// Remove the selected ROI
        /// </summary>
        [SerializeField] private Button m_RemoveROI;
        /// <summary>
        /// Select a sphere of the selected ROI
        /// </summary>
        [SerializeField] private Dropdown m_SphereSelector;
        /// <summary>
        /// Remove the selected sphere of the selected ROI
        /// </summary>
        [SerializeField] private Button m_RemoveSphere;
        #endregion

        #region Private Methods
        /// <summary>
        /// Update the available options on the ROI dropdown
        /// </summary>
        public void UpdateROIDropdownOptions()
        {
            m_ROISelector.options.Clear();
            m_ROISelector.options.Add(new Dropdown.OptionData("None"));
            for (int i = 0; i < SelectedScene.ROIManager.ROIs.Count; i++)
            {
                Core.Object3D.ROI roi = SelectedScene.ROIManager.ROIs[i];
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
        /// <summary>
        /// Update the available options on the sphere dropdown
        /// </summary>
        public void UpdateVolumeDropdownOptions()
        {
            m_SphereSelector.options.Clear();
            m_SphereSelector.options.Add(new Dropdown.OptionData("None"));
            Core.Object3D.ROI selectedROI = SelectedScene.ROIManager.SelectedROI;
            if (selectedROI)
            {
                for (int i = 0; i < selectedROI.Spheres.Count; i++)
                {
                    Core.Object3D.Sphere sphere = selectedROI.Spheres[i];
                    m_SphereSelector.options.Add(new Dropdown.OptionData("Sphere " + i + " (R=" + sphere.Radius.ToString("N1") + ")"));
                }
            }
            m_SphereSelector.RefreshShownValue();
        }
        /// <summary>
        /// Update the tool for the selected ROI
        /// </summary>
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
                m_SphereSelector.value = SelectedScene.ROIManager.SelectedROI.SelectedSphereID + 1;
            }
            ListenerLock = false;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
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
            m_SphereSelector.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                SelectedScene.ROIManager.SelectedROI.SelectSphere(value - 1);
            });
            m_RemoveSphere.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SelectedScene.ROIManager.SelectedROI.RemoveSelectedSphere();
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_AddROI.interactable = false;
            m_RemoveROI.interactable = false;
            m_ROIName.interactable = false;
            m_ROIName.text = "";
            m_ROISelector.interactable = false;
            m_SphereSelector.interactable = false;
            m_RemoveSphere.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
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
            m_SphereSelector.interactable = hasVolume;
            m_RemoveSphere.interactable = hasVolume;
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            UpdateROIDropdownOptions();
            UpdateSelectedROIUI();
        }
        #endregion
    }
}