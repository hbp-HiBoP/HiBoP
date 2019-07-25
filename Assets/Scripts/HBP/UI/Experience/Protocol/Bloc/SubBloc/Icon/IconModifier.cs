using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;
using Tools.Unity;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
    public class IconModifier : ItemModifier<d.Icon>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] RangeSlider m_WindowSlider;
        [SerializeField] ImageSelector m_ImageSelector;

        Tools.CSharp.Window m_Window;
        public Tools.CSharp.Window Window
        {
            get
            {
                return m_Window;
            }
            set
            {
                m_Window = value;
                m_WindowSlider.minLimit = m_Window.Start;
                m_WindowSlider.maxLimit = m_Window.End;
            }
        }

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;
                m_NameInputField.interactable = value;
                m_WindowSlider.interactable = value;
                m_ImageSelector.interactable = value;
            }
        }
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();
            m_NameInputField.onValueChanged.AddListener((name) => ItemTemp.Name = name);
            m_WindowSlider.onValueChanged.AddListener((min,max) => ItemTemp.Window = new Tools.CSharp.Window((int) min, (int) max));
            m_ImageSelector.onValueChanged.AddListener(() => ItemTemp.IllustrationPath = m_ImageSelector.Path);
        }
        protected override void SetFields(d.Icon objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;

            Data.Preferences.ProtocolPreferences preferences = ApplicationState.UserPreferences.Data.Protocol;

            m_WindowSlider.minLimit = preferences.MinLimit;
            m_WindowSlider.maxLimit = preferences.MaxLimit;
            m_WindowSlider.step = preferences.Step;

            m_WindowSlider.Values = objectToDisplay.Window.ToVector2();

            m_ImageSelector.Path = objectToDisplay.IllustrationPath;
        }
        #endregion
    }
}