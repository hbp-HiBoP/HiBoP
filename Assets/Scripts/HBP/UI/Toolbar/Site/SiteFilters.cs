using HBP.Core.Object3D;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Core.Preferences;
using HBP.UI.Module3D;
using HBP.UI.Tools;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    public class SiteFilters : Tool
    {
        #region Properties
        [SerializeField] private Button m_OpenFiltersButton;
        [SerializeField] private Button m_ResetFiltersButton;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_OpenFiltersButton.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                ToolbarExternalActions.OpenSiteFilters(SelectedScene);
            });
            m_ResetFiltersButton.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                foreach (var column in SelectedScene.Columns)
                    foreach (var site in column.Sites)
                        site.State.IsFiltered = true;

                Module3DMain.OnRequestUpdateInSiteList.Invoke();
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_OpenFiltersButton.interactable = false;
            m_ResetFiltersButton.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            m_OpenFiltersButton.interactable = true;
            m_ResetFiltersButton.interactable = true;
        }
        #endregion
    }
}
