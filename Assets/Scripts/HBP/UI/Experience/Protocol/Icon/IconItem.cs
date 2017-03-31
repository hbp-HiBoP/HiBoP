using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Experience.Protocol;
using Tools.CSharp;

namespace HBP.UI.Experience.Protocol
{
    /// <summary>
    /// The script which manage the icon panel.
    /// </summary>
    public class IconItem : Tools.Unity.Lists.ListItemWithSave<Icon>
    {
        #region Attributs
        /// <summary>
        /// The label inputField.
        /// </summary>
        [SerializeField]
        InputField m_labelInputField;

        /// <summary>
        /// The path inputField.
        /// </summary>
        [SerializeField]
        InputField m_pathInputField;

        /// <summary>
        /// The window min inputField.
        /// </summary>
        [SerializeField]
        InputField m_minInputField;

        /// <summary>
        /// The window max inputField.
        /// </summary>
        [SerializeField]
        InputField m_maxInputField;
        #endregion

        #region Public Methods
        protected override void SetObject(Icon icon)
        {
            m_object = icon;
            m_labelInputField.text = icon.Name;
            m_pathInputField.text = icon.IllustrationPath;
            m_minInputField.text = icon.Window.Start.ToString();
            m_maxInputField.text = icon.Window.End.ToString();
        }

        public override void Save()
        {
            Object.Name = m_labelInputField.text;
            Object.IllustrationPath = m_pathInputField.text;
            Object.Window = new Tools.CSharp.Window(float.Parse(m_minInputField.text), float.Parse(m_maxInputField.text));
        }

        public void OpenIllustrationPath()
        {
            string l_resultStandalone = VISU3D.DLL.QtGUI.get_existing_file_name(new string[] { "png", "jpg" }, "Please select the illustration of the Icon", m_pathInputField.text);
            StringExtension.StandardizeToPath(ref l_resultStandalone);
            if (l_resultStandalone != string.Empty)
            {
                m_pathInputField.text = l_resultStandalone;
            }
        }
        #endregion
    }
}