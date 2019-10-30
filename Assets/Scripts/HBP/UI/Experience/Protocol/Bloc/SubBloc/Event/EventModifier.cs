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
                m_TypeDropdown.interactable = value && ItemTemp.Type == Data.Enums.MainSecondaryEnum.Secondary;
            }
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onValueChanged.AddListener((name) => ItemTemp.Name = name);
            m_CodesInputField.onValueChanged.AddListener((codes) => ItemTemp.CodesString = codes);
            m_TypeDropdown.onValueChanged.AddListener((i) => ItemTemp.Type = (Data.Enums.MainSecondaryEnum)i);
        }
        protected override void SetFields(d.Event objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_CodesInputField.text = objectToDisplay.CodesString;
            m_TypeDropdown.Set(typeof(Data.Enums.MainSecondaryEnum), (int) objectToDisplay.Type);
        }
        #endregion
    }
}