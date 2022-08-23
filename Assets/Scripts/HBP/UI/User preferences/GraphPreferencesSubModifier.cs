using UnityEngine;
using UnityEngine.UI;
using HBP.Display.Preferences;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class GraphPreferencesSubModifier : SubModifier<GraphPreferences>
    {
        #region Properties
        [SerializeField] Toggle m_ShowCurvesOfMinimizedColumns;
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
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();

            m_ShowCurvesOfMinimizedColumns.onValueChanged.AddListener(value => Object.ShowCurvesOfMinimizedColumns = value);
            for (int i = 0; i < m_ColorButtons.Length; i++)
            {
                int index = i;
                m_ColorButtons[index].onClick.AddListener(() =>
                {
                    ColorPicker.Open(Object.GetColor(index), (c) =>
                    {
                        Object.SetColor(index, c);
                        m_ColorImages[index].color = c;
                    });
                });
            }
            m_Default.onClick.AddListener(() =>
            {
                DialogBoxManager.Open(DialogBoxManager.AlertType.WarningMultiOptions, "Restore colors to default", "Do you want to restore the colors to their original states?", () =>
                {
                    Object.SetDefaultColors();
                    SetFields(Object);
                }, "Yes", () => { }, "No");
            });
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(GraphPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_ShowCurvesOfMinimizedColumns.isOn = objectToDisplay.ShowCurvesOfMinimizedColumns;
            Color[] colors = objectToDisplay.Colors;
            for (int i = 0; i < colors.Length; i++)
            {
                m_ColorImages[i].color = colors[i];
            }
        }
        #endregion
    }
}