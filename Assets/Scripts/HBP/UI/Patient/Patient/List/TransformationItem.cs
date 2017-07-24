using UnityEngine;
using UnityEngine.UI;
using Tools.Unity.Lists;
using HBP.Data.Anatomy;

namespace HBP.UI.Anatomy
{
    public class TransformationItem : SavableItem<Data.Anatomy.Transformation>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] Tools.Unity.FileSelector m_FileSelector;
        public override Transformation Object
        {
            get
            {
                return base.Object;
            }

            set
            {
                base.Object = value;
                m_NameInputField.text = value.Name;
                m_FileSelector.File = value.Path;
            }
        }
        public override bool interactable
        {
            get
            {
                return base.interactable;
            }

            set
            {
                base.interactable = value;
                m_NameInputField.interactable = value;
                m_FileSelector.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Save()
        {
            Object.Name = m_NameInputField.text;
            Object.Path = m_FileSelector.File;
        }
        #endregion
    }
}