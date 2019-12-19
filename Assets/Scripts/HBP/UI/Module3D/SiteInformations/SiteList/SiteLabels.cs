using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class SiteLabels : MonoBehaviour
    {
        #region Properties
        [SerializeField] private InputField m_AddSiteLabelInputField;
        [SerializeField] private Button m_AddSiteLabelButton;
        [SerializeField] private RectTransform m_SiteLabelsContainer;
        [SerializeField] private GameObject m_SiteLabelPrefab;
        private HBP.Module3D.Site m_Site;
        #endregion

        #region Public Methods
        public void Initialize(HBP.Module3D.Site site)
        {
            m_Site = site;

            // Change Site Labels
            foreach (Transform label in m_SiteLabelsContainer)
            {
                Destroy(label.gameObject);
            }
            foreach (var label in site.State.Labels)
            {
                Instantiate(m_SiteLabelPrefab, m_SiteLabelsContainer).GetComponent<SiteLabelItem>().Initialize(site, label);
            }

            // Listeners
            m_AddSiteLabelButton.onClick.RemoveAllListeners();
            m_AddSiteLabelButton.onClick.AddListener(() =>
            {
                AddLabel();
            });
            m_AddSiteLabelInputField.onEndEdit.RemoveAllListeners();
            m_AddSiteLabelInputField.onEndEdit.AddListener((text) =>
            {
                if(Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
                {
                    AddLabel(); 
                    UnityEngine.EventSystems.EventSystem system = UnityEngine.EventSystems.EventSystem.current;
                    m_AddSiteLabelInputField.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(system));
                    system.SetSelectedGameObject(m_AddSiteLabelInputField.gameObject, new UnityEngine.EventSystems.BaseEventData(system));
                }
            });
        }
        #endregion

        #region Private Methods
        private void OnEnable()
        {
            m_AddSiteLabelInputField.text = "";
        }
        private void AddLabel()
        {
            if (!string.IsNullOrEmpty(m_AddSiteLabelInputField.text))
            {
                m_Site.State.AddLabel(m_AddSiteLabelInputField.text);
                m_AddSiteLabelInputField.text = "";
            }
        }
        #endregion
    }
}