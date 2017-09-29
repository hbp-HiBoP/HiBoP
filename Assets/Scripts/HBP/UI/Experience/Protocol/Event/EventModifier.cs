using System;
using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;

namespace HBP.UI.Experience.Protocol
{
    public class EventModifier : ItemModifier<d.Event>
    {
        #region Properties
        InputField m_NameInputField;
        InputField m_CodesInputField;
        Dropdown m_TypeDropdown;
        #endregion

        protected override void SetFields(d.Event objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_NameInputField.onValueChanged.RemoveAllListeners();
            m_NameInputField.onValueChanged.AddListener((name) => ItemTemp.Name = name);

            m_CodesInputField.text = objectToDisplay.CodesString;
            m_CodesInputField.onValueChanged.RemoveAllListeners();
            m_CodesInputField.onValueChanged.AddListener((codes) => ItemTemp.CodesString = codes);

            string[] types = Enum.GetNames(typeof(d.Event.TypeEnum));
            m_TypeDropdown.ClearOptions();
            foreach (string i_type in types)
            {
                m_TypeDropdown.options.Add(new Dropdown.OptionData(i_type));
            }
            m_TypeDropdown.value = (int) ItemTemp.Type;
            m_TypeDropdown.RefreshShownValue();
            m_TypeDropdown.onValueChanged.RemoveAllListeners();
            m_TypeDropdown.onValueChanged.AddListener((i) => ItemTemp.Type = (d.Event.TypeEnum)i);
            if (objectToDisplay.Type == d.Event.TypeEnum.Main) m_TypeDropdown.interactable = false;
        }

        protected override void SetInteractableFields(bool interactable)
        {
            m_NameInputField.interactable = interactable;
            m_CodesInputField.interactable = interactable;
        }

        protected override void SetWindow()
        {
            m_NameInputField = transform.Find("Content").Find("General").Find("Name").GetComponentInChildren<InputField>();
            m_CodesInputField = transform.Find("Content").Find("General").Find("Codes").GetComponentInChildren<InputField>();
            m_TypeDropdown = transform.Find("Content").Find("General").Find("Type").GetComponentInChildren<Dropdown>();
        }
    }
}