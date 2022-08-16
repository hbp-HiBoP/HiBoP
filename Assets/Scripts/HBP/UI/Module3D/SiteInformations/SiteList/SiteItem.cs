using UnityEngine;
using UnityEngine.UI;
using HBP.UI.Lists;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// Component to display a module3D site in a list.
    /// </summary>
    public class SiteItem : Item<Core.Object3D.Site>
    {
        #region Properties
        [SerializeField] private Button m_Site;
        [SerializeField] private SiteLabel m_SiteLabel;
        [SerializeField] private Image m_SelectedImage;
        [SerializeField] private SiteLabels m_Labels;
        [SerializeField] private Text m_LabelsText;
        [SerializeField] private Text m_Patient;
        [SerializeField] private Toggle m_Blacklisted;
        [SerializeField] private Toggle m_Highlighted;
        [SerializeField] private Button m_Color;
        [SerializeField] private Image m_ColorImage;
        [SerializeField] private GameObject m_ColorPickerPrefab;

        /// <summary>
        /// Site to display.
        /// </summary>
        public override Core.Object3D.Site Object
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
        /// <summary>
        /// True if a update if required, False otherwise.
        /// </summary>
        private bool m_UpdateRequired;
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_Site.onClick.AddListener(() =>
            {
                Object.IsSelected = true;
            });

            m_Blacklisted.onValueChanged.AddListener((isOn) =>
            {
                Object.State.IsBlackListed = isOn;
            });

            m_Highlighted.onValueChanged.AddListener((isOn) =>
            {
                Object.State.IsHighlighted = isOn;
            });

            m_Color.onClick.AddListener(() =>
            {
                ColorPicker.Open(Object.State.Color, (c) => Object.State.Color = c);
            });
        }
        private void Update()
        {
            if (m_UpdateRequired)
            {
                UpdateFields();
            }
        }
        /// <summary>
        /// Update all fields.
        /// </summary>
        private void UpdateFields()
        {
            m_Site.GetComponentInChildren<Text>().text = Object.Information.Name;
            m_SiteLabel.Initialize(Object);
            m_SelectedImage.gameObject.SetActive(Object.IsSelected);
            m_LabelsText.text = Object.State.Labels.Count.ToString();
            m_Patient.text = Object.Information.Patient.Name;
            m_Blacklisted.isOn = Object.State.IsBlackListed;
            m_Highlighted.isOn = Object.State.IsHighlighted;
            m_ColorImage.color = Object.State.Color;
            m_Labels.Initialize(Object);
            m_UpdateRequired = false;
        }
        #endregion
    }
}