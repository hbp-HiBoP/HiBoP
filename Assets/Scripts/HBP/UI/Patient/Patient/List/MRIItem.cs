using UnityEngine;
using UnityEngine.UI;
using Tools.Unity.Lists;

namespace HBP.UI.Anatomy
{
    public class MRIItem : SavableItem<Data.Anatomy.MRI>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] Tools.Unity.FileSelector m_FileSelector;
        #endregion

        #region Public Methods
        public override void Save()
        {
            Object.Name = m_NameInputField.text;
            Object.Path = m_FileSelector.File;
        }
        #endregion

        #region Protected Methods
        protected override void SetObject(Data.Anatomy.MRI objectToSet)
        {
            m_NameInputField.text = objectToSet.Name;
            m_FileSelector.File = objectToSet.Path;
        }
        #endregion
    }
}