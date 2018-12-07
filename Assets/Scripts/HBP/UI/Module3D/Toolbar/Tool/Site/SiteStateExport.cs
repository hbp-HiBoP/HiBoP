using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class SiteStateExport : Tool
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
                
                LoadSiteStatesToSelectedColumn();
            });
            m_Export.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SaveSiteStatesOfSelectedColumn();
            });
        }
        public override void DefaultState()
        {
            m_Import.interactable = false;
            m_Export.interactable = false;
        }
        public override void UpdateInteractable()
        {
            m_Import.interactable = true;
            m_Export.interactable = true;
        }
        #endregion

        #region Private Methods
        private void SaveSiteStatesOfSelectedColumn()
        {
            string savePath = FileBrowser.GetSavedFileName(new string[] { "csv" }, "Save site states to", Application.dataPath);
            if (!string.IsNullOrEmpty(savePath))
            {
                SelectedColumn.SaveSiteStates(savePath);
                ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Informational, "Site states saves", "Site states of the selected column have been saved to <color=#3080ffff>" + savePath + "</color>");
            }
        }
        private void LoadSiteStatesToSelectedColumn()
        {
            string loadPath = FileBrowser.GetExistingFileName(new string[] { "csv" }, "Load site states", Application.dataPath);
            if (!string.IsNullOrEmpty(loadPath))
            {
                SelectedColumn.LoadSiteStates(loadPath);
            }
        }
        #endregion
    }
}