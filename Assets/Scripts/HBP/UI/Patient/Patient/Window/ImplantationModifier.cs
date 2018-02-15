using UnityEngine.UI;
using HBP.Data.Anatomy;
using System;
using Tools.Unity;

namespace HBP.UI.Anatomy
{
    public class ImplantationModifier : ItemModifier<Implantation>
    {
        #region Properties
        InputField m_NameInputField;
        FileSelector m_FileSelector;
        FileSelector m_MarsAtlasSelector;
        Button m_OKButton;
        #endregion

        #region Private Methods
        protected override void SetFields(Implantation objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_FileSelector.File = objectToDisplay.File;
            m_MarsAtlasSelector.File = objectToDisplay.MarsAtlas;
        }
        protected override void SetInteractableFields(bool interactable)
        {
            m_NameInputField.interactable = interactable;
            m_FileSelector.interactable = interactable;
            m_MarsAtlasSelector.interactable = interactable;
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

            m_MarsAtlasSelector = transform.Find("Content").Find("General").Find("MarsAtlas").Find("FileSelector").GetComponent<FileSelector>();
            m_MarsAtlasSelector.onValueChanged.RemoveAllListeners();
            m_MarsAtlasSelector.onValueChanged.AddListener((file) => ItemTemp.MarsAtlas = file);

            m_OKButton = transform.Find("Content").Find("Buttons").Find("OK").GetComponent<Button>();
        }
        #endregion
    }
}