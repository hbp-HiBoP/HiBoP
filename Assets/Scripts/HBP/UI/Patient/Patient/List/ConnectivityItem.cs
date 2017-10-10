using UnityEngine;
using UnityEngine.UI;
using Tools.Unity.Lists;
using HBP.Data.Anatomy;

namespace HBP.UI.Anatomy
{
    public class ConnectivityItem : ActionnableItem<Connectivity>
    {
        #region Properties
        [SerializeField] Text m_NameInputField;
        [SerializeField] Image m_ConnectivityIcon;

        public override Connectivity Object
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
                m_ConnectivityIcon.color = value.HasConnectivity ? normalColor : notInteractableColor;
            }
        }
        #endregion
    }
}