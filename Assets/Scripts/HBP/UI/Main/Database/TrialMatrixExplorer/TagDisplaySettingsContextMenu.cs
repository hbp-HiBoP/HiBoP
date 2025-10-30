using HBP.Core.Data;
using HBP.Data.Preferences;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI.Database
{
    public class TagDisplaySettingsContextMenu : MonoBehaviour
    {
        #region Enums
        public enum TagsType { Patient, Site }
        #endregion

        #region Properties
        [SerializeField] private GameObject m_TagSelectionItemPrefab;
        [SerializeField] private Transform m_TagSelectionItemParent;
        [SerializeField] private TagsType m_TagsType = TagsType.Patient;

        private Dictionary<BaseTag, TagSelectionItem> m_TagSelectionItems = new Dictionary<BaseTag, TagSelectionItem>();
        private bool m_SelectionChanged = false;
        private bool m_Initialized = false;
        #endregion

        #region Events
        public UnityEvent OnTagSelectionChanged = new();
        #endregion

        #region Private Methods
        private void Awake()
        {
            if (!m_Initialized) Initialize();
            gameObject.SetActive(false);
        }
        private void Update()
        {
            if (m_SelectionChanged)
            {
                m_SelectionChanged = false;
                OnTagSelectionChanged.Invoke();
            }
        }
        private void Initialize()
        {
            List<BaseTag> tags = m_TagsType == TagsType.Patient ? PersistentDataManager.Tags.GeneralTags.Concat(PersistentDataManager.Tags.PatientsTags).ToList() : PersistentDataManager.Tags.GeneralTags.Concat(PersistentDataManager.Tags.SitesTags).ToList();
            foreach (BaseTag tag in tags)
            {
                GameObject itemObject = Instantiate(m_TagSelectionItemPrefab, m_TagSelectionItemParent);
                TagSelectionItem item = itemObject.GetComponent<TagSelectionItem>();
                item.Set(tag);
                item.OnValueChanged.AddListener((value) => m_SelectionChanged = true);
                m_TagSelectionItems.Add(tag, item);
            }
            m_Initialized = true;
        }
        #endregion

        #region Public Methods
        public void SelectAll()
        {
            foreach (var item in m_TagSelectionItems.Values) item.Selected = true;
        }
        public void DeselectAll()
        {
            foreach (var item in m_TagSelectionItems.Values) item.Selected = false;
        }
        public bool IsDisplayed(BaseTag tag)
        {
            if (!m_Initialized) Initialize();
            return m_TagSelectionItems.TryGetValue(tag, out TagSelectionItem item) && item.Selected;
        }
        #endregion
    }
}