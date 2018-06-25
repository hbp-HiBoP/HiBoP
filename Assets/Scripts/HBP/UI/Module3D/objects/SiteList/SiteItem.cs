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
        private Image m_SelectedImage;
        [SerializeField]
        private Text m_Patient;
        [SerializeField]
        private Toggle m_Excluded;
        [SerializeField]
        private Toggle m_Blacklisted;
        [SerializeField]
        private Toggle m_Marked;
        [SerializeField]
        private Toggle m_Highlighted;
        [SerializeField]
        private Toggle m_Suspicious;

        public override Site Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                UpdateFields();
                value.State.OnChangeState.AddListener(() => m_UpdateRequired = true);
            }
        }
        private bool m_UpdateRequired;
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_Site.onClick.AddListener(() =>
            {
                Object.IsSelected = true;
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

            m_Suspicious.onValueChanged.AddListener((isOn) =>
            {
                ApplicationState.Module3D.SelectedScene.ChangeSiteState(isOn ? SiteAction.Suspect : SiteAction.Unsuspect, Object);
                m_Site.interactable = Object.IsActive;
            });
        }
        private void Update()
        {
            if (m_UpdateRequired)
            {
                UpdateFields();
            }
        }
        private void UpdateFields()
        {
            m_Site.GetComponentInChildren<Text>().text = Object.Information.ChannelName;
            m_Site.interactable = Object.IsActive;
            m_SelectedImage.gameObject.SetActive(Object.IsSelected);
            m_Patient.text = Object.Information.Patient.Name;
            m_Excluded.isOn = Object.State.IsExcluded;
            m_Blacklisted.isOn = Object.State.IsBlackListed;
            m_Marked.isOn = Object.State.IsMarked;
            m_Highlighted.isOn = Object.State.IsHighlighted;
            m_Suspicious.isOn = Object.State.IsSuspicious;
            m_UpdateRequired = false;
        }
        #endregion
    }
}