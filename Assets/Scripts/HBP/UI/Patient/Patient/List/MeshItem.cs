using UnityEngine;
using UnityEngine.UI;
using Tools.Unity.Lists;

namespace HBP.UI.Anatomy
{
    public class MeshItem : SavableItem<Data.Anatomy.Mesh>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        public override Data.Anatomy.Mesh Object
        {
            get
            {
                return base.Object;
            }

            set
            {
                base.Object = value;
                m_NameInputField.text = value.Name;
            }
        }
        public override bool interactable
        {
            set
            {
                base.interactable = value;
                m_NameInputField.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Save()
        {
            Object.Name = m_NameInputField.text;
        }
        #endregion

    }
}