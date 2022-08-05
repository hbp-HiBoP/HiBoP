using UnityEngine.UI;
using Tools.Unity;
using UnityEngine;
using HBP.Core.Data;

namespace HBP.UI.Experience.Protocol
{
    /// <summary>
    /// Window to modify a Icon.
    /// </summary>
    public class IconModifier : ObjectModifier<Core.Data.Icon>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] RangeSlider m_WindowSlider;
        [SerializeField] ImageSelector m_ImageSelector;

        Tools.CSharp.Window m_Window;
        /// <summary>
        /// Window of the subBloc.
        /// </summary>
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

        /// <summary>
        /// True if interactable, False otherwise.
        /// </summary>
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
        /// <summary>
        /// Initialize the window.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            m_NameInputField.onEndEdit.AddListener(ChangeName);
            m_WindowSlider.onValueChanged.AddListener(ChangeWindow);
            m_ImageSelector.onValueChanged.AddListener(ChangeImage);
        }
        /// <summary>
        /// Set the fields.
        /// </summary>
        /// <param name="objectToDisplay">Icon to modify</param>
        protected override void SetFields(Core.Data.Icon objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_WindowSlider.minLimit = ApplicationState.UserPreferences.Data.Protocol.MinLimit;
            m_WindowSlider.maxLimit = ApplicationState.UserPreferences.Data.Protocol.MaxLimit;
            m_WindowSlider.step = ApplicationState.UserPreferences.Data.Protocol.Step;
            m_WindowSlider.Values = objectToDisplay.Window.ToVector2();
            m_ImageSelector.Path = objectToDisplay.ImagePath;
        }
        /// <summary>
        /// Change the name of the icon.
        /// </summary>
        /// <param name="value">Name</param>
        protected void ChangeName(string value)
        {
            if(value != "")
            {
                ObjectTemp.Name = value;
            }
            else
            {
                m_NameInputField.text = ObjectTemp.Name;
            }
        }
        /// <summary>
        /// Change the window.
        /// </summary>
        /// <param name="min">Min window</param>
        /// <param name="max">Max window</param>
        protected void ChangeWindow(float min, float max)
        {
            ObjectTemp.Window = new Tools.CSharp.Window((int)min, (int)max);
        }
        /// <summary>
        /// Change the image.
        /// </summary>
        /// <param name="path">Path to illustration path</param>
        protected void ChangeImage(string path)
        {
            ObjectTemp.ImagePath = path;
        }
        #endregion
    }
}