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
        #endregion

        #region Private Methods
        protected override void SetFields(Implantation objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_FileSelector.File = objectToDisplay.File;
            m_MarsAtlasSelector.File = objectToDisplay.MarsAtlas;
        }
        protected override void SetInteractable(bool interactable)
        {
            base.SetInteractable(interactable);
            m_NameInputField.interactable = interactable;
            m_FileSelector.interactable = interactable;
            m_MarsAtlasSelector.interactable = interactable;
        }
        protected override void Initialize()
        {
            base.Initialize();
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