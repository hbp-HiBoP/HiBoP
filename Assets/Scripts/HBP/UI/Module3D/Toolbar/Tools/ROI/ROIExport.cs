using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using Tools.Unity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using T = Tools.Unity;

namespace HBP.UI.Module3D.Tools
{
    public class ROIExport : Tool
    {
        #region Properties
        [SerializeField]
        private Button m_Import;
        [SerializeField]
        private Button m_Export;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Import.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                LoadROIToSelectedColumn();
            });
            m_Export.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SaveSelectedROI();
            });
        }
        public override void DefaultState()
        {
            m_Import.interactable = false;
            m_Export.interactable = false;
        }
        public override void UpdateInteractable()
        {
            bool hasROI = SelectedColumn.ROIs.Count > 0;

            m_Import.interactable = true;
            m_Export.interactable = hasROI;
        }
        #endregion

        #region Private Methods
        private void SaveSelectedROI()
        {
            string savePath = FileBrowser.GetSavedFileName(new string[] { "roi" }, "Save ROI to", Application.dataPath);
            if (!string.IsNullOrEmpty(savePath))
            {
                Data.Visualization.RegionOfInterest ROI = new Data.Visualization.RegionOfInterest(SelectedColumn.SelectedROI);
                ClassLoaderSaver.SaveToJSon(ROI, savePath, true);
                ApplicationState.DialogBoxManager.Open(T.DialogBoxManager.AlertType.Informational, "Region of Interest saved", "The selected ROI has been saved to <color=#3080ffff>" + savePath + "</color>");
            }
        }
        private void LoadROIToSelectedColumn()
        {
            string loadPath = FileBrowser.GetExistingFileName(new string[] { "roi" }, "Load ROI file", Application.dataPath);
            if (!string.IsNullOrEmpty(loadPath))
            {
                Data.Visualization.RegionOfInterest serializedROI = ClassLoaderSaver.LoadFromJson<Data.Visualization.RegionOfInterest>(loadPath);
                ROI roi = SelectedColumn.AddROI(serializedROI.Name);
                foreach (Data.Visualization.Sphere sphere in serializedROI.Spheres)
                {
                    roi.AddSphere(SelectedColumn.Layer, "Bubble", sphere.Position.ToVector3(), sphere.Radius);
                }
            }
        }
        #endregion
    }
}