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

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_AutomaticallySynchronizeScenesToggle.interactable = value;
                m_AutomaticallyConnectToggle.interactable = value;
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            m_AutomaticallySynchronizeScenesToggle.onValueChanged.AddListener(value => Object.AutomaticallySynchronizeScenes = value);
            m_AutomaticallyConnectToggle.onValueChanged.AddListener(value => Object.AutomaticallyConnect = value);
        }

        protected override void SetFields(QuestPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_AutomaticallySynchronizeScenesToggle.SetIsOnWithoutNotify(objectToDisplay.AutomaticallySynchronizeScenes);
            m_AutomaticallyConnectToggle.SetIsOnWithoutNotify(objectToDisplay.AutomaticallyConnect);
        }
    }
}
