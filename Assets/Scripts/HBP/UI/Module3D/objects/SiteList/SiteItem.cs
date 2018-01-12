using HBP.Module3D;
using Tools.Unity.Lists;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class SiteItem : Item<Site>
    {
        #region Properties
        [SerializeField]
        private Button m_Site;
        [SerializeField]
        private Toggle m_Excluded;
        [SerializeField]
        private Toggle m_Blacklisted;
        [SerializeField]
        private Toggle m_Marked;
        [SerializeField]
        private Toggle m_Highlighted;

        public override Site Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_Site.GetComponentInChildren<Text>().text = value.Information.Name + " (" + value.Information.Patient.Name + ")";
                m_Site.interactable = value.IsActive;
                m_Excluded.isOn = value.State.IsExcluded;
                m_Blacklisted.isOn = value.State.IsBlackListed;
                m_Marked.isOn = value.State.IsMarked;
                m_Highlighted.isOn = value.State.IsHighlighted;
            }
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_Site.onClick.AddListener(() =>
            {
                ApplicationState.Module3D.SelectedColumn.SelectedSiteID = Object.Information.GlobalID;
            });

            m_Excluded.onValueChanged.AddListener((isOn) =>
            {
                ApplicationState.Module3D.SelectedScene.ChangeSiteState(isOn ? SiteAction.Exclude : SiteAction.Include, Object);
                m_Site.interactable = Object.IsActive;
            });

            m_Blacklisted.onValueChanged.AddListener((isOn) =>
            {
                ApplicationState.Module3D.SelectedScene.ChangeSiteState(isOn ? SiteAction.Blacklist : SiteAction.Unblacklist, Object);
                m_Site.interactable = Object.IsActive;
            });

            m_Marked.onValueChanged.AddListener((isOn) =>
            {
                ApplicationState.Module3D.SelectedScene.ChangeSiteState(isOn ? SiteAction.Mark : SiteAction.Unmark, Object);
                m_Site.interactable = Object.IsActive;
            });

            m_Highlighted.onValueChanged.AddListener((isOn) =>
            {
                ApplicationState.Module3D.SelectedScene.ChangeSiteState(isOn ? SiteAction.Highlight : SiteAction.Unhighlight, Object);
                m_Site.interactable = Object.IsActive;
            });
        }
        #endregion
    }
}