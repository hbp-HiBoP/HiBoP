using UnityEngine;
using UnityEngine.UI;
using Tools.Unity.Lists;
using HBP.Data.Anatomy;
using NewTheme.Components;

namespace HBP.UI.Anatomy
{
    public class ConnectivityItem : ActionnableItem<Connectivity>
    {
        #region Properties
        [SerializeField] Text m_NameInputField;
        [SerializeField] ThemeElement m_ConnectivityThemeElement;
        [SerializeField] State m_ErrorState;

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
                if (value.HasConnectivity) m_ConnectivityThemeElement.Set();
                else m_ConnectivityThemeElement.Set(m_ErrorState);
            }
        }
        #endregion
    }
}