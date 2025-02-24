using HBP.Data.Database;
using HBP.UI.Tools.Lists;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Database
{
    public class WorkspaceItem : ActionnableItem<Workspace>
    {
        #region Properties
        [SerializeField] Text m_NameText;
        [SerializeField] Text m_SelectedText;

        public override Workspace Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_NameText.text = value.Name;
            }
        }
        #endregion

        #region Public Methods
        public void UpdateSelectedWorkspace(Workspace selectedWorkspace)
        {
            m_SelectedText.gameObject.SetActive(selectedWorkspace.ID == Object.ID);
        }
        #endregion
    }
}