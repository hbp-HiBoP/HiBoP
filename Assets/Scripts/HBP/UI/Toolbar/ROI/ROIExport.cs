using HBP.Core.Tools;
using UnityEngine;
using UnityEngine.UI;
using HBP.UI.Tools;
using System;
using System.Linq;
using HBP.Data.Module3D;

namespace HBP.UI.Toolbar
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
        private async void SaveSelectedROI()
        {
            string savePath = await FileBrowser.GetSavedFileNameAsync(new string[] { "roi" }, "Save ROI to");
            if (!string.IsNullOrEmpty(savePath))
            {
                Core.Data.RegionOfInterest ROI = new(SelectedScene.ROIManager.SelectedROI.Name, SelectedScene.ROIManager.SelectedROI.Spheres.Select(s => new Core.Data.Sphere(s.Position, s.Radius)).ToList());
                ClassLoaderSaver.SaveToJSon(ROI, savePath, true);
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Informational, "Region of Interest saved", "The selected ROI has been saved to <color=#3080ffff>" + savePath + "</color>").Forget();
            }
        }
        /// <summary>
        /// Load a ROI from a file to the scene
        /// </summary>
        private async void LoadROI()
        {
            string loadPath = await FileBrowser.GetExistingFileNameAsync(new string[] { "roi" }, "Load ROI file");
            if (!string.IsNullOrEmpty(loadPath))
            {
                try
                {
                    Core.Data.RegionOfInterest serializedROI = ClassLoaderSaver.LoadFromJson<Core.Data.RegionOfInterest>(loadPath);
                    ROI roi = SelectedScene.ROIManager.AddROI(serializedROI.Name);
                    foreach (Core.Data.Sphere sphere in serializedROI.Spheres)
                    {
                        roi.AddSphere(Module3DMain.DEFAULT_MESHES_LAYER, "Sphere", sphere.Position.ToVector3(), sphere.Radius);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Can not load ROI", "The ROI file you are trying to load is not valid.").Forget();
                }
            }
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