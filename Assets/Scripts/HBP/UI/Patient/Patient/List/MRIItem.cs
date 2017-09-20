using UnityEngine;
using UnityEngine.UI;
using Tools.Unity.Lists;
using HBP.Data.Anatomy;

namespace HBP.UI.Anatomy
{
    public class MRIItem : ActionnableItem<MRI>
    {
        #region Properties
        [SerializeField] Text m_NameInputField;
        [SerializeField] Image m_MRIIcon;

        public override MRI Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_NameInputField.text = value.Name;
                Color normalColor = Color.white;
                Color notInteractableColor = ApplicationState.Theme.General.NotInteractable;
                m_MRIIcon.color = value.HasMRI ? normalColor : notInteractableColor;
            }
        }
        #endregion
    }
}