using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class SiteStateExport : Tool
    {
        #region Properties
        /// <summary>
        /// Import states from a file
        /// </summary>
        [SerializeField] private Button m_Import;
        /// <summary>
        /// Export states to a file
        /// </summary>
        [SerializeField] private Button m_Export;
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
                
                LoadSiteStatesToSelectedColumn();
            });
            m_Export.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SaveSiteStatesOfSelectedColumn();
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
            m_Import.interactable = true;
            m_Export.interactable = true;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Save the sites of the selected column
        /// </summary>
        private void SaveSiteStatesOfSelectedColumn()
        {
#if UNITY_STANDALONE_OSX
            FileBrowser.GetSavedFileNameAsync((savePath) =>
            {
                if (!string.IsNullOrEmpty(savePath))
                {
                    SelectedColumn.SaveSiteStates(savePath);
                    DialogBoxManager.Open(DialogBoxManager.AlertType.Informational, "Site states saved", "Site states of the selected column have been saved to <color=#3080ffff>" + savePath + "</color>");
                }
            }, new string[] { "csv" }, "Save site states to", Application.dataPath);
#else
            string savePath = FileBrowser.GetSavedFileName(new string[] { "csv" }, "Save site states to", Application.dataPath);
            if (!string.IsNullOrEmpty(savePath))
            {
                SelectedColumn.SaveSiteStates(savePath);
                DialogBoxManager.Open(DialogBoxManager.AlertType.Informational, "Site states saved", "Site states of the selected column have been saved to <color=#3080ffff>" + savePath + "</color>");
            }
#endif
        }
        /// <summary>
        /// Load the sites of the selected column
        /// </summary>
        private void LoadSiteStatesToSelectedColumn()
        {
#if UNITY_STANDALONE_OSX
            FileBrowser.GetExistingFileNameAsync((loadPath) =>
            {
                if (!string.IsNullOrEmpty(loadPath))
                {
                    SelectedColumn.LoadSiteStates(loadPath);
                }
            }, new string[] { "csv" }, "Load site states");
#else
            string loadPath = FileBrowser.GetExistingFileName(new string[] { "csv" }, "Load site states");
            if (!string.IsNullOrEmpty(loadPath))
            {
                SelectedColumn.LoadSiteStates(loadPath);
            }
#endif
        }
#endregion
    }
}