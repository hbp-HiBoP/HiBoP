﻿using HBP.Module3D;
using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class ROIExport : Tool
    {
        #region Properties
        /// <summary>
        /// Import a ROI from a file
        /// </summary>
        [SerializeField] private Button m_Import;
        /// <summary>
        /// Export the selected ROI to a file
        /// </summary>
        [SerializeField] private Button m_Export;
        #endregion

        #region Private Methods
        /// <summary>
        /// Save the selected ROI to a file
        /// </summary>
        private void SaveSelectedROI()
        {
#if UNITY_STANDALONE_OSX
            FileBrowser.GetSavedFileNameAsync((savePath) =>
            {
                if (!string.IsNullOrEmpty(savePath))
                {
                    Data.Visualization.RegionOfInterest ROI = new Data.Visualization.RegionOfInterest(SelectedScene.ROIManager.SelectedROI);
                    ClassLoaderSaver.SaveToJSon(ROI, savePath, true);
                    ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Informational, "Region of Interest saved", "The selected ROI has been saved to <color=#3080ffff>" + savePath + "</color>");
                }
            }, new string[] { "roi" }, "Save ROI to", Application.dataPath);
#else
            string savePath = FileBrowser.GetSavedFileName(new string[] { "roi" }, "Save ROI to", Application.dataPath);
            if (!string.IsNullOrEmpty(savePath))
            {
                Data.Visualization.RegionOfInterest ROI = new Data.Visualization.RegionOfInterest(SelectedScene.ROIManager.SelectedROI);
                ClassLoaderSaver.SaveToJSon(ROI, savePath, true);
                ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Informational, "Region of Interest saved", "The selected ROI has been saved to <color=#3080ffff>" + savePath + "</color>");
            }
#endif
        }
        /// <summary>
        /// Load a ROI from a file to the scene
        /// </summary>
        private void LoadROI()
        {
#if UNITY_STANDALONE_OSX
            FileBrowser.GetExistingFileNameAsync((loadPath) =>
            {
                if (!string.IsNullOrEmpty(loadPath))
                {
                    Data.Visualization.RegionOfInterest serializedROI = ClassLoaderSaver.LoadFromJson<Data.Visualization.RegionOfInterest>(loadPath);
                    ROI roi = SelectedScene.ROIManager.AddROI(serializedROI.Name);
                    foreach (Data.Visualization.Sphere sphere in serializedROI.Spheres)
                    {
                        roi.AddSphere(SelectedColumn.Layer, "Sphere", sphere.Position.ToVector3(), sphere.Radius);
                    }
                }
            }, new string[] { "roi" }, "Load ROI file");
#else
            string loadPath = FileBrowser.GetExistingFileName(new string[] { "roi" }, "Load ROI file");
            if (!string.IsNullOrEmpty(loadPath))
            {
                Data.Visualization.RegionOfInterest serializedROI = ClassLoaderSaver.LoadFromJson<Data.Visualization.RegionOfInterest>(loadPath);
                ROI roi = SelectedScene.ROIManager.AddROI(serializedROI.Name);
                foreach (Data.Visualization.Sphere sphere in serializedROI.Spheres)
                {
                    roi.AddSphere(SelectedColumn.Layer, "Sphere", sphere.Position.ToVector3(), sphere.Radius);
                }
            }
#endif
        }
#endregion

#region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Import.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                LoadROI();
            });
            m_Export.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SaveSelectedROI();
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Import.interactable = false;
            m_Export.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool hasROI = SelectedScene.ROIManager.ROIs.Count > 0;

            m_Import.interactable = true;
            m_Export.interactable = hasROI;
        }
#endregion
    }
}