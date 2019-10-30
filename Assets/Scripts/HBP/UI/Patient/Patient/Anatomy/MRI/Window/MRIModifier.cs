using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Anatomy;

namespace HBP.UI.Anatomy
{
    public class MRIModifier : ItemModifier<MRI>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] FileSelector m_FileSelector;

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
                m_FileSelector.interactable = value;
            }
        }
        #endregion

        #region Private Methods
        protected override void SetFields(MRI objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_FileSelector.File = objectToDisplay.SavedFile;
        }
        protected override void Initialize()
        {
            base.Initialize();
            m_NameInputField.onValueChanged.RemoveAllListeners();
            m_NameInputField.onValueChanged.AddListener((name) => ItemTemp.Name = name);

            m_FileSelector.onValueChanged.RemoveAllListeners();
            m_FileSelector.onValueChanged.AddListener((file) => ItemTemp.File = file);
        }
        #endregion
    }
}