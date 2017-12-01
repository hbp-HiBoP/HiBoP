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
            bool hasROI = ApplicationState.Module3D.SelectedColumn.ROIs.Count > 0;

            m_Import.interactable = true;
            m_Export.interactable = hasROI;
        }
        #endregion

        #region Private Methods
        private void SaveSelectedROI()
        {
            string savePath = HBP.Module3D.DLL.QtGUI.GetSavedFileName(new string[] { "roi" }, "Save ROI to", Application.dataPath);
            if (!string.IsNullOrEmpty(savePath))
            {
                Data.Visualization.RegionOfInterest ROI = new Data.Visualization.RegionOfInterest(ApplicationState.Module3D.SelectedColumn.SelectedROI);
                ClassLoaderSaver.SaveToJSon(ROI, savePath, true);
                ApplicationState.DialogBoxManager.Open(T.DialogBoxManager.AlertType.Informational, "Region of Interest saved", "The selected ROI has been saved to <color=#3080ffff>" + savePath + "</color>");
            }
        }
        private void LoadROIToSelectedColumn()
        {
            string loadPath = HBP.Module3D.DLL.QtGUI.GetExistingFileName(new string[] { "roi" }, "Load ROI file", Application.dataPath);
            if (!string.IsNullOrEmpty(loadPath))
            {
                Data.Visualization.RegionOfInterest serializedROI = ClassLoaderSaver.LoadFromJson<Data.Visualization.RegionOfInterest>(loadPath);
                Column3D column = ApplicationState.Module3D.SelectedColumn;
                ROI roi = column.AddROI(serializedROI.Name);
                foreach (Data.Visualization.Sphere sphere in serializedROI.Spheres)
                {
                    roi.AddBubble(column.Layer, "Bubble", sphere.Position.ToVector3(), sphere.Radius);
                }
            }
        }
        #endregion
    }
}