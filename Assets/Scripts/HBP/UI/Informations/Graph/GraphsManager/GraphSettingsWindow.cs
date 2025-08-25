using Cysharp.Threading.Tasks;
using HBP.Data.Informations;
using HBP.Data.Preferences;
using HBP.UI.Tools;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI.Informations
{
    public class GraphSettingsWindow : DialogWindow
    {
        #region Properties
        [SerializeField] private ChannelStructsGroupListGestion m_ChannelStructsGroupListGestion;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_ChannelStructsGroupListGestion.Interactable = value;
            }
        }

        private List<ChannelStructsGroup> m_ChannelStructsGroups = new();
        public List<ChannelStructsGroup> ChannelStructsGroups
        {
            get => m_ChannelStructsGroupListGestion.List.Objects.ToList();
            set
            {
                m_ChannelStructsGroups = value;
                SetFields();
            }
        }
        #endregion

        #region Public Methods
        public async void OpenUserPreferences()
        {
            var window = WindowsManager.OpenModifier(PersistentDataManager.UserPreferences, null);
            var navigator = window.GetComponent<ToggleNavigator>();
            navigator.Navigate("Visualization");
            await UniTask.WaitForEndOfFrame();
            navigator.Navigate("Visualization_Graph");
            Close();
        }
        #endregion

        #region Private Methods
        protected override void SetFields()
        {
            base.SetFields();

            m_ChannelStructsGroupListGestion.List.Set(m_ChannelStructsGroups);
            m_ChannelStructsGroupListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
        }
        #endregion
    }
}