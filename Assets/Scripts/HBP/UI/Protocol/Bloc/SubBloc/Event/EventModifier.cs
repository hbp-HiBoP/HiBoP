using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;

namespace HBP.UI.Experience.Protocol
{
    /// <summary>
    /// Window to modify a event.
    /// </summary>
    public class EventModifier : ObjectModifier<d.Event>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] InputField m_CodesInputField;
        [SerializeField] Dropdown m_TypeDropdown;

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
                m_CodesInputField.interactable = value;
                m_TypeDropdown.interactable = value && ObjectTemp != null && ObjectTemp.Type == Data.Enums.MainSecondaryEnum.Secondary;
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
            m_CodesInputField.onValueChanged.AddListener(ChangeCodes);
            m_TypeDropdown.onValueChanged.AddListener(ChangeType);
        }
        /// <summary>
        /// Set the fields.
        /// </summary>
        /// <param name="eventToModify">Event to modify</param>
        protected override void SetFields(d.Event eventToModify)
        {
            m_NameInputField.text = eventToModify.Name;
            m_CodesInputField.text = eventToModify.CodesString;
            m_TypeDropdown.Set(typeof(Data.Enums.MainSecondaryEnum), (int) eventToModify.Type);
            m_TypeDropdown.interactable = m_Interactable && ObjectTemp != null && ObjectTemp.Type == Data.Enums.MainSecondaryEnum.Secondary;
        }

        /// <summary>
        /// Change the name of the event.
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
        /// Change the codes of the event.
        /// </summary>
        /// <param name="value">Codes</param>
        protected void ChangeCodes(string value)
        {
            ObjectTemp.CodesString = value;
        }
        /// <summary>
        /// Change the type of the event.
        /// </summary>
        /// <param name="value">Event</param>
        protected void ChangeType(int value)
        {
            ObjectTemp.Type = (Data.Enums.MainSecondaryEnum)value;
        }
        #endregion
    }
}