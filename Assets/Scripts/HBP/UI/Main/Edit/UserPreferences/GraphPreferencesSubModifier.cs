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
        [SerializeField] Image[] m_ColorImages;
        [SerializeField] Button[] m_ColorButtons;
        [SerializeField] Button m_Default;

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
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();

            m_ShowCurvesOfMinimizedColumns.onValueChanged.AddListener(value => Object.ShowCurvesOfMinimizedColumns = value);
            m_ShowSEM.onValueChanged.AddListener(value => Object.ShowSEM = value);
            for (int i = 0; i < m_ColorButtons.Length; i++)
            {
                int index = i;
                m_ColorButtons[index].onClick.AddListener(async () =>
                {
                    Color color = await ColorPickerManager.OpenColorPickerAsync(Object.GetColor(index));
                    Object.SetColor(index, color);
                    m_ColorImages[index].color = color;
                });
            }
            m_Default.onClick.AddListener(async () =>
            {
                int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Informational, "Restore colors to default", "Do you want to restore the colors to their original states?", "Yes", "No");
                if (result == 0)
                {
                    Object.SetDefaultColors();
                    SetFields(Object);
                }
            });
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(GraphPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_ShowCurvesOfMinimizedColumns.isOn = objectToDisplay.ShowCurvesOfMinimizedColumns;
            m_ShowSEM.isOn = objectToDisplay.ShowSEM;
            Color[] colors = objectToDisplay.Colors;
            for (int i = 0; i < colors.Length; i++)
            {
                m_ColorImages[i].color = colors[i];
            }
        }
        #endregion
    }
}