using UnityEngine.UI;
using HBP.Data.Anatomy;
using System;
using Tools.Unity;

namespace HBP.UI.Anatomy
{
    public class MRIModifier : ItemModifier<MRI>
    {
        #region Properties
        InputField m_NameInputField;
        FileSelector m_FileSelector;
        Button m_OKButton;
        #endregion

        #region Private Methods
        protected override void SetFields(MRI objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_FileSelector.File = objectToDisplay.File;
        }
        protected override void SetInteractableFields(bool interactable)
        {
            m_NameInputField.interactable = interactable;
            m_FileSelector.interactable = interactable;
            m_OKButton.interactable = interactable;
        }
        protected override void SetWindow()
        {
            m_NameInputField = transform.Find("Content").Find("General").Find("Name").Find("InputField").GetComponent<InputField>();
            m_NameInputField.onValueChanged.RemoveAllListeners();
            m_NameInputField.onValueChanged.AddListener((name) => ItemTemp.Name = name);

            m_FileSelector = transform.Find("Content").Find("General").Find("File").Find("FileSelector").GetComponent<FileSelector>();
            m_FileSelector.onValueChanged.RemoveAllListeners();
            m_FileSelector.onValueChanged.AddListener((file) => ItemTemp.File = file);

            m_OKButton = transform.Find("Content").Find("Buttons").Find("OK").GetComponent<Button>();
        }
        #endregion
    }
}