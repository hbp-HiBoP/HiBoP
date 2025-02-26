using HBP.Core.Data;
using UnityEngine;
using HBP.UI.Tools;
using HBP.Core.Tools;

namespace HBP.UI.Main
{
    public class GroupGestion : GestionWindow<Group>
    {
        #region Properties
        [SerializeField] GroupListGestion m_ListGestion;
        public override ListGestion<Group> ListGestion => m_ListGestion;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;

                m_ListGestion.Interactable = value;
                m_ListGestion.Modifiable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void OK()
        {
            base.OK();
            ApplicationState.LoadedProject.SetGroups(ListGestion.List.Objects);
            InteractableStateManager.SetInteractables();
            UITools.CheckProjectIDAndAskForRegeneration().Forget();
        }
        public override void Close()
        {
            LoadingManager.Load(update => RestoreOldValuesAsync(ApplicationState.LoadedProject.Groups, update), false);
            base.Close();
        }
        #endregion

        #region Private Methods
        protected override void SetFields()
        {
            base.SetFields();
            SetList(ApplicationState.LoadedProject.Groups);
        }
        #endregion
    }
}