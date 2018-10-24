using UnityEngine;
using System.Linq;
using UnityEngine.UI;

namespace HBP.UI.Anatomy
{
    public class GroupGestion : SavableWindow
    {
        #region Properties
        [SerializeField] GroupListGestion m_GroupListGestion;
        [SerializeField] Button m_AddButton;
        [SerializeField] Button m_RemoveButton;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
            set
            {
                base.Interactable = value;

                m_GroupListGestion.Interactable = value;
                m_AddButton.interactable = value;
                m_RemoveButton.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Save()
        {
            ApplicationState.ProjectLoaded.SetGroups(m_GroupListGestion.Items);
            base.Save();
        }
        #endregion

        #region Private Methods
        protected override void SetFields()
        {
            m_GroupListGestion.Initialize(m_SubWindows);
            m_GroupListGestion.Items = ApplicationState.ProjectLoaded.Groups.ToList();
            base.SetFields();
        }
        #endregion
    }
}