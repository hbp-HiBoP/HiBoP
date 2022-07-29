using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class SiteLabels : MonoBehaviour
    {
        #region Properties
        [SerializeField] private InputField m_AddSiteLabelInputField;
        [SerializeField] private Text m_AutocompleteText;
        [SerializeField] private Button m_AddSiteLabelButton;
        [SerializeField] private RectTransform m_SiteLabelsContainer;
        [SerializeField] private GameObject m_SiteLabelPrefab;
        private Core.Object3D.Site m_Site;
        #endregion

        #region Public Methods
        public void Initialize(Core.Object3D.Site site)
        {
            m_Site = site;

            // Change Site Labels
            foreach (Transform label in m_SiteLabelsContainer)
            {
                if (!site.State.Labels.Any(l => l == label.gameObject.GetComponent<SiteLabelItem>().Label))
                {
                    Destroy(label.gameObject);
                }
            }
            foreach (var label in site.State.Labels)
            {
                if (!m_SiteLabelsContainer.GetComponentsInChildren<SiteLabelItem>().Any(l => l.Label == label))
                {
                    Instantiate(m_SiteLabelPrefab, m_SiteLabelsContainer).GetComponent<SiteLabelItem>().Initialize(label);
                }
            }
            foreach (var item in m_SiteLabelsContainer.GetComponentsInChildren<SiteLabelItem>())
            {
                item.OnRemoveLabel.RemoveAllListeners();
                item.OnRemoveLabel.AddListener(label => m_Site.State.RemoveLabel(label));
            }

            // Listeners
            m_AddSiteLabelButton.onClick.RemoveAllListeners();
            m_AddSiteLabelButton.onClick.AddListener(() =>
            {
                AddLabel(m_AddSiteLabelInputField.text);
            });
            m_AddSiteLabelInputField.onEndEdit.RemoveAllListeners();
            m_AddSiteLabelInputField.onEndEdit.AddListener((text) =>
            {
                if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
                {
                    AddLabel(text);
                    EventSystem system = EventSystem.current;
                    m_AddSiteLabelInputField.OnPointerClick(new PointerEventData(system));
                    system.SetSelectedGameObject(m_AddSiteLabelInputField.gameObject, new BaseEventData(system));
                }
            });
            m_AddSiteLabelInputField.onValueChanged.RemoveAllListeners();
            m_AddSiteLabelInputField.onValueChanged.AddListener((text) =>
            {
                if (string.IsNullOrEmpty(text))
                {
                    m_AutocompleteText.text = "";
                    return;
                }

                List<string> labels = ApplicationState.Module3D.SelectedColumn.Sites.SelectMany(s => s.State.Labels).Distinct().ToList();
                string existingLabel = labels.Find(s => s.StartsWith(text));
                if (!string.IsNullOrEmpty(existingLabel))
                {
                    if (string.Compare(text, existingLabel) == 0)
                    {
                        m_AutocompleteText.text = "";
                    }
                    else
                    {
                        m_AutocompleteText.text = existingLabel;
                    }
                }
                else
                {
                    m_AutocompleteText.text = "";
                }
            });
        }
        #endregion

        #region Private Methods
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                EventSystem system = EventSystem.current;
                if (system)
                {
                    GameObject currentObject = system.currentSelectedGameObject;
                    if (currentObject == m_AddSiteLabelInputField.gameObject)
                    {
                        if (!string.IsNullOrEmpty(m_AutocompleteText.text))
                        {
                            m_AddSiteLabelInputField.text = m_AutocompleteText.text;
                            m_AddSiteLabelInputField.OnPointerClick(new PointerEventData(system));
                            system.SetSelectedGameObject(m_AddSiteLabelInputField.gameObject, new BaseEventData(system));
                            m_AddSiteLabelInputField.caretPosition = m_AddSiteLabelInputField.text.Length;
                        }
                    }
                }
            }
        }
        private void OnEnable()
        {
            m_AddSiteLabelInputField.text = "";
        }
        private void AddLabel(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                m_Site.State.AddLabel(text);
                m_AddSiteLabelInputField.text = "";
            }
        }
        #endregion
    }
}