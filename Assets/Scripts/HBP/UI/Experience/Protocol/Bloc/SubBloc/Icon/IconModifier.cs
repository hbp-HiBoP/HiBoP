using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;
using Tools.Unity;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
    public class IconModifier : ObjectModifier<d.Icon>
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
            m_NameInputField.onValueChanged.AddListener(OnChangeName);
            m_WindowSlider.onValueChanged.AddListener(OnChangeWindow);
            m_ImageSelector.onValueChanged.AddListener(OnChangeImage);
        }
        protected override void SetFields(d.Icon objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_WindowSlider.minLimit = ApplicationState.UserPreferences.Data.Protocol.MinLimit;
            m_WindowSlider.maxLimit = ApplicationState.UserPreferences.Data.Protocol.MaxLimit;
            m_WindowSlider.step = ApplicationState.UserPreferences.Data.Protocol.Step;
            m_WindowSlider.Values = objectToDisplay.Window.ToVector2();
            m_ImageSelector.Path = objectToDisplay.IllustrationPath;
        }
        void OnChangeName(string name)
        {
            ItemTemp.Name = name;
        }
        void OnChangeWindow(float min, float max)
        {
            ItemTemp.Window = new Tools.CSharp.Window((int)min, (int)max);
        }
        void OnChangeImage(string path)
        {
            ItemTemp.IllustrationPath = path;
        }
        #endregion
    }
}