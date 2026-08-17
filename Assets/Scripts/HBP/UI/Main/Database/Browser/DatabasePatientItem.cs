using HBP.Core.Data;
using HBP.UI.Tools.Lists;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Database
{
    public class DatabasePatientItem : SelectableItem<Patient>
    {
        #region Properties

        [SerializeField] Text m_NameText;

        public override Patient Object
        {
            get { return base.Object; }
            set
            {
                SetInteractable();

                base.Object = value;
                m_NameText.text = value.Name;

                SetNotInteractable();
            }
        }

        #endregion
    }
}
