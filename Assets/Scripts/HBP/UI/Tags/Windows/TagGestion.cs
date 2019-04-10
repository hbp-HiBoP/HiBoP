using UnityEngine;
using System.Linq;
using UnityEngine.UI;

namespace HBP.UI.Tags
{
    public class TagGestion : SavableWindow
    {
        #region Properties
        [SerializeField] TagListGestion m_TagListGestion;
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

                m_TagListGestion.Interactable = value;
                m_AddButton.interactable = value;
                m_RemoveButton.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Save()
        {
            ApplicationState.ProjectLoaded.SetTags(m_TagListGestion.Objects);
            base.Save();
            FindObjectOfType<MenuButtonState>().SetInteractables();
        }
        #endregion

        #region Private Methods
        protected override void SetFields()
        {
            m_TagListGestion.Initialize(m_SubWindows);
            m_TagListGestion.Objects = ApplicationState.ProjectLoaded.Tags.ToList();
            base.SetFields();
        }
        #endregion
    }
}