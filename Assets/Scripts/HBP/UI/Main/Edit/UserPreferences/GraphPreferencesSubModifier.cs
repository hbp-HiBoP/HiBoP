using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Preferences;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class GraphPreferencesSubModifier : SubModifier<GraphPreferences>
    {
        #region Properties
        [SerializeField] Toggle m_ShowCurvesOfMinimizedColumns;
        [SerializeField] Toggle m_ShowSEM;
        [SerializeField] Button m_Default;
        [SerializeField] Slider m_MaxSitesSlider;
        [SerializeField] Slider m_MaxColumnsSlider;
        [SerializeField] Slider m_MaxGroupsSlider;
        [SerializeField] Button m_RegenerateGridButton;

        [SerializeField] GameObject m_ColumnTitlePrefab;
        [SerializeField] GameObject m_RowPrefab;
        [SerializeField] GameObject m_ColorButtonPrefab;
        [SerializeField] Transform m_ColorsContainer;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
            set
            {
                base.Interactable = value;

                m_ShowCurvesOfMinimizedColumns.interactable = value;
                m_ShowSEM.interactable = value;
                m_Default.interactable = value;
                m_MaxSitesSlider.interactable = value;
                m_MaxColumnsSlider.interactable = value;
                m_MaxGroupsSlider.interactable = value;
                m_RegenerateGridButton.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();

            m_ShowCurvesOfMinimizedColumns.onValueChanged.AddListener(value => Object.ShowCurvesOfMinimizedColumns = value);
            m_ShowSEM.onValueChanged.AddListener(value => Object.ShowSEM = value);
            m_Default.onClick.AddListener(async () =>
            {
                int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Informational, "Restore colors to default", "Do you want to restore the colors to their original states?", "Yes", "No");
                if (result == 0)
                {
                    Object.SetDefaultColors();
                    SetFields(Object);
                }
            });
            m_RegenerateGridButton.onClick.AddListener(RegenerateGrid);
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(GraphPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_ShowCurvesOfMinimizedColumns.isOn = objectToDisplay.ShowCurvesOfMinimizedColumns;
            m_ShowSEM.isOn = objectToDisplay.ShowSEM;

            m_MaxColumnsSlider.minValue = GraphPreferences.MINIMUM_NUMBER_OF_COLUMNS;
            m_MaxColumnsSlider.maxValue = GraphPreferences.MAXIMUM_NUMBER_OF_COLUMNS;
            m_MaxColumnsSlider.wholeNumbers = true;
            m_MaxColumnsSlider.value = objectToDisplay.MaxColumns;

            m_MaxSitesSlider.minValue = GraphPreferences.MINIMUM_NUMBER_OF_SITES;
            m_MaxSitesSlider.maxValue = GraphPreferences.MAXIMUM_NUMBER_OF_SITES;
            m_MaxSitesSlider.wholeNumbers = true;
            m_MaxSitesSlider.value = objectToDisplay.MaxSites;

            m_MaxGroupsSlider.minValue = GraphPreferences.MINIMUM_NUMBER_OF_GROUPS;
            m_MaxGroupsSlider.maxValue = GraphPreferences.MAXIMUM_NUMBER_OF_GROUPS;
            m_MaxGroupsSlider.wholeNumbers = true;
            m_MaxGroupsSlider.value = objectToDisplay.MaxGroups;
            GenerateGrid();
        }
        protected async void RegenerateGrid()
        {
            var result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Informational, "Regenerate color grid", "If you regenerate the color grid, some colors may be reset — especially if you reduced one of the sliders. Do you want to proceed?", "Yes", "No");
            if (result == 0)
            {
                Object.MaxColumns = (int)m_MaxColumnsSlider.value;
                Object.MaxSites = (int)m_MaxSitesSlider.value;
                Object.MaxGroups = (int)m_MaxGroupsSlider.value;
                GenerateGrid();
            }
        }
        protected void GenerateGrid()
        {
            foreach (Transform child in m_ColorsContainer)
            {
                Destroy(child.gameObject);
            }
            // Header
            var header = Instantiate(m_RowPrefab, m_ColorsContainer);
            header.GetComponentInChildren<Text>().text = "Column n°";
            for (int i = 0; i < Object.MaxColumns; i++)
            {
                var columnTitle = Instantiate(m_ColumnTitlePrefab, header.transform);
                columnTitle.GetComponentInChildren<Text>().text = (i + 1).ToString();
            }
            // Channels
            for (int i = 0; i < Object.MaxSites; i++)
            {
                var row = Instantiate(m_RowPrefab, m_ColorsContainer);
                row.GetComponentInChildren<Text>().text = $"Channel {(i + 1)}";
                for (int j = 0; j < Object.MaxColumns; j++)
                {
                    var colorButton = Instantiate(m_ColorButtonPrefab, row.transform).GetComponent<ColorPickerButton>();
                    colorButton.Initialize(Object.SiteColors.GetColor(i, j));
                    colorButton.OnColorPicked.AddListener(color => Object.SiteColors.SetColor(i, j, color));
                }
            }
            // Groups
            for (int i = 0; i < Object.MaxGroups; i++)
            {
                var row = Instantiate(m_RowPrefab, m_ColorsContainer);
                row.GetComponentInChildren<Text>().text = $"Channel group {(i + 1)}";
                for (int j = 0; j < Object.MaxColumns; j++)
                {
                    var colorButton = Instantiate(m_ColorButtonPrefab, row.transform).GetComponent<ColorPickerButton>();
                    colorButton.Initialize(Object.GroupColors.GetColor(i, j));
                    colorButton.OnColorPicked.AddListener(color => Object.GroupColors.SetColor(i, j, color));
                }
            }
            // ROI
            var roiRow = Instantiate(m_RowPrefab, m_ColorsContainer);
            roiRow.GetComponentInChildren<Text>().text = "ROI";
            for (int j = 0; j < Object.MaxColumns; j++)
            {
                var colorButton = Instantiate(m_ColorButtonPrefab, roiRow.transform).GetComponent<ColorPickerButton>();
                colorButton.Initialize(Object.ROIColors.GetColor(0, j));
                colorButton.OnColorPicked.AddListener(color => Object.ROIColors.SetColor(0, j, color));
            }
        }
        #endregion
    }
}