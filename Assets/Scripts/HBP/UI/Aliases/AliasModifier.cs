using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI
{
    /// <summary>
    /// Window to modify a Alias.
    /// </summary>
    public class AliasModifier : ObjectModifier<Data.Alias>
    {
        #region Properties
        [SerializeField] InputField m_KeyInputField;
        [SerializeField] InputField m_ValueInputField;

        /// <summary>
        /// True if interactable, False otherwise.
        /// </summary>
        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_KeyInputField.interactable = value;
                m_ValueInputField.interactable = value;
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

            m_KeyInputField.onValueChanged.AddListener(key => ObjectTemp.Key = key);
            m_ValueInputField.onValueChanged.AddListener(value => ObjectTemp.Value = value);
        }
        /// <summary>
        /// Set the fields.
        /// </summary>
        /// <param name="objectToDisplay">Alias to display</param>
        protected override void SetFields(Data.Alias objectToDisplay)
        {
            m_KeyInputField.text = objectToDisplay.Key;
            m_ValueInputField.text = objectToDisplay.Value;
        }
        #endregion
    }
}

