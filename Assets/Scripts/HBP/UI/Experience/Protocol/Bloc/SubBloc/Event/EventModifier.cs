using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;

namespace HBP.UI.Experience.Protocol
{
    public class EventModifier : ItemModifier<d.Event>
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
                m_TypeDropdown.interactable = (ItemTemp.Type != d.Event.TypeEnum.Main) && value;
            }
        }
        #endregion

        protected override void SetFields(d.Event objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_CodesInputField.text = objectToDisplay.CodesString;
            m_TypeDropdown.Set(typeof(d.Event.TypeEnum), (int) objectToDisplay.Type);
        }

        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onValueChanged.RemoveAllListeners();
            m_NameInputField.onValueChanged.AddListener((name) => ItemTemp.Name = name);

            m_CodesInputField.onValueChanged.RemoveAllListeners();
            m_CodesInputField.onValueChanged.AddListener((codes) => ItemTemp.CodesString = codes);

            m_TypeDropdown.onValueChanged.RemoveAllListeners();
            m_TypeDropdown.onValueChanged.AddListener((i) => ItemTemp.Type = (d.Event.TypeEnum) i);
        }
    }
}