using HBP.Data.Database;
using HBP.UI.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Database
{
    public class WorkspaceModifier : ObjectModifier<Workspace>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_NameInputField.interactable = value;
            }
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onValueChanged.AddListener(name => ObjectTemp.Name = name);
        }
        protected override void SetFields(Workspace objectToModify)
        {
            m_NameInputField.text = objectToModify.Name;
        }
        #endregion
    }
}