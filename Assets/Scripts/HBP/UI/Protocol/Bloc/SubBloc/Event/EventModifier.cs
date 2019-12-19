using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;

namespace HBP.UI.Experience.Protocol
{
    public class EventModifier : ObjectModifier<d.Event>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] InputField m_CodesInputField;
        [SerializeField] Dropdown m_TypeDropdown;

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
                m_TypeDropdown.interactable = value && ItemTemp != null && ItemTemp.Type == Data.Enums.MainSecondaryEnum.Secondary;
            }
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onEndEdit.AddListener(OnChangeName);
            m_CodesInputField.onValueChanged.AddListener(OnChangeCodes);
            m_TypeDropdown.onValueChanged.AddListener(OnChangeType);
        }
        protected override void SetFields(d.Event objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_CodesInputField.text = objectToDisplay.CodesString;
            m_TypeDropdown.Set(typeof(Data.Enums.MainSecondaryEnum), (int) objectToDisplay.Type);
            m_TypeDropdown.interactable = m_Interactable && ItemTemp != null && ItemTemp.Type == Data.Enums.MainSecondaryEnum.Secondary;
        }

        protected void OnChangeName(string value)
        {
            if(value != "")
            {
                ItemTemp.Name = value;
            }
            else
            {
                m_NameInputField.text = ItemTemp.Name;
            }
        }
        protected void OnChangeCodes(string value)
        {
            ItemTemp.CodesString = value;
        }
        protected void OnChangeType(int value)
        {
            ItemTemp.Type = (Data.Enums.MainSecondaryEnum)value;
        }
        #endregion
    }
}