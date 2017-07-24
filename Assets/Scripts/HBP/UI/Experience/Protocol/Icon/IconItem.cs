using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Experience.Protocol;
using Tools.CSharp;

namespace HBP.UI.Experience.Protocol
{
    /// <summary>
    /// The script which manage the icon panel.
    /// </summary>
    public class IconItem : Tools.Unity.Lists.SavableItem<Icon>
    {
        #region Properties
        /// <summary>
        /// The label inputField.
        /// </summary>
        [SerializeField] InputField m_LabelInputField;
        /// <summary>
        /// The path inputField.
        /// </summary>
        [SerializeField] Tools.Unity.FileSelector m_IllustrationFileSelector;
        /// <summary>
        /// The window min inputField.
        /// </summary>
        [SerializeField] InputField m_MinInputField;
        /// <summary>
        /// The window max inputField.
        /// </summary>
        [SerializeField] InputField m_MaxInputField;
        public override Icon Object
        {
            get
            {
                return base.Object;
            }

            set
            {
                base.Object = value;
                m_LabelInputField.text = value.Name;
                m_IllustrationFileSelector.File = value.IllustrationPath;
                m_MinInputField.text = value.Window.Start.ToString();
                m_MaxInputField.text = value.Window.End.ToString();
            }
        }
        #endregion

        #region Public Methods
        public override void Save()
        {
            Object.Name = m_LabelInputField.text;
            Object.IllustrationPath = m_IllustrationFileSelector.File;
            Object.Window = new Tools.CSharp.Window(float.Parse(m_MinInputField.text), float.Parse(m_MaxInputField.text));
        }
        #endregion
    }
}