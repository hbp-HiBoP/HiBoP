using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI
{
    /// <summary>
    /// Window to modify a MRI. 
    /// </summary>
    public class MRIModifier : ObjectModifier<Core.Data.MRI>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] FileSelector m_FileSelector;

        /// <summary>
        /// True if is interactable, False otherwise.
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
                m_FileSelector.interactable = value;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Initialize the window.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onEndEdit.AddListener(ChangeName);
            m_FileSelector.onValueChanged.AddListener(ChangeFile);
        }
        /// <summary>
        /// Set the fields.
        /// </summary>
        /// <param name="objectToDisplay">MRI to modify</param>
        protected override void SetFields(Core.Data.MRI objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_FileSelector.File = objectToDisplay.SavedFile;
        }
        /// <summary>
        /// Change the name.
        /// </summary>
        /// <param name="value">Name of the MRI</param>
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
        /// Change the path to the MRI file.
        /// </summary>
        /// <param name="value">Path to the MRI file.</param>
        protected void ChangeFile(string value)
        {
            ObjectTemp.File = value;
        }
        #endregion
    }
}