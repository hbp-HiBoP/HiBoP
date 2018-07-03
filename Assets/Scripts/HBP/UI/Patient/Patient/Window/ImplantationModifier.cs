using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Anatomy;
using Tools.Unity;

namespace HBP.UI.Anatomy
{
    public class ImplantationModifier : ItemModifier<Implantation>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] FileSelector m_FileSelector;
        [SerializeField] FileSelector m_MarsAtlasSelector;
        [SerializeField] Button m_OKButton;
        #endregion

        #region Private Methods
        protected override void SetFields(Implantation objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_FileSelector.File = objectToDisplay.SavedFile;
            m_MarsAtlasSelector.File = objectToDisplay.SavedMarsAtlas;
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
            m_NameInputField.onValueChanged.RemoveAllListeners();
            m_NameInputField.onValueChanged.AddListener((name) => ItemTemp.Name = name);

            m_FileSelector.onValueChanged.RemoveAllListeners();
            m_FileSelector.onValueChanged.AddListener((file) => ItemTemp.File = file);

            m_MarsAtlasSelector.onValueChanged.RemoveAllListeners();
            m_MarsAtlasSelector.onValueChanged.AddListener((file) => ItemTemp.MarsAtlas = file);
        }
        #endregion
    }
}