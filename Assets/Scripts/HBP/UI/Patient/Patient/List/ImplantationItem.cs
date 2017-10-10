using UnityEngine;
using UnityEngine.UI;
using Tools.Unity.Lists;
using HBP.Data.Anatomy;

namespace HBP.UI.Anatomy
{
    public class ImplantationItem : ActionnableItem<Implantation>
    {
        #region Properties
        [SerializeField] Text m_NameInputField;
        [SerializeField] Image m_ImplantationIcon;

        public override Implantation Object
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
                Color notInteractableColor = ApplicationState.GeneralSettings.Theme.General.NotInteractable;
                m_ImplantationIcon.color = value.HasImplantation ? normalColor : notInteractableColor;
            }
        }
        #endregion
    }
}