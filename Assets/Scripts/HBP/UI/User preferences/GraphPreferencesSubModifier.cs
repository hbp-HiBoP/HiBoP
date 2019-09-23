using HBP.Data.Preferences;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.UserPreferences
{
    public class GraphPreferencesSubModifier : SubModifier<GraphPreferences>
    {
        #region Properties
        [SerializeField] Toggle m_ShowCurvesOfMinimizedColumns;

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
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(GraphPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_ShowCurvesOfMinimizedColumns.isOn = objectToDisplay.ShowCurvesOfMinimizedColumns;
        }
        #endregion
    }
}