using HBP.Core.Enums;
using HBP.Core.Preferences;
using HBP.UI.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class QuestPreferencesSubModifier : SubModifier<QuestPreferences>
    {
        [SerializeField] Toggle m_AutomaticallySynchronizeScenesToggle;
        [SerializeField] Toggle m_AutomaticallyConnectToggle;
        [SerializeField] Dropdown m_EnvironmentDropdown;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_AutomaticallySynchronizeScenesToggle.interactable = value;
                m_AutomaticallyConnectToggle.interactable = value;
                if (m_EnvironmentDropdown != null) m_EnvironmentDropdown.interactable = value;
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            m_AutomaticallySynchronizeScenesToggle.onValueChanged.AddListener(value => Object.AutomaticallySynchronizeScenes = value);
            m_AutomaticallyConnectToggle.onValueChanged.AddListener(value => Object.AutomaticallyConnect = value);
            m_EnvironmentDropdown.onValueChanged.AddListener(value => Object.Environment = (QuestEnvironment)value);
        }

        protected override void SetFields(QuestPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_AutomaticallySynchronizeScenesToggle.SetIsOnWithoutNotify(objectToDisplay.AutomaticallySynchronizeScenes);
            m_AutomaticallyConnectToggle.SetIsOnWithoutNotify(objectToDisplay.AutomaticallyConnect);
            m_EnvironmentDropdown.SetValueWithoutNotify((int)objectToDisplay.Environment);
        }
    }
}
