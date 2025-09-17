using Cysharp.Threading.Tasks;
using HBP.Core.Tools;
using HBP.Data.Informations;
using HBP.Data.Preferences;
using HBP.UI.Tools;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Informations
{
    #region Enums
    public enum LocalizersGraphsMode { Voxel, Region, Atlas }
    public enum LocalizersGraphsAtlas { MarsAtlas, Jubrain }
    #endregion

    public class GraphSettingsWindow : DialogWindow
    {

        #region Properties
        // Custom channel groups graphs
        [SerializeField] private ChannelStructsGroupListGestion m_ChannelStructsGroupListGestion;

        // Localizers graphs
        [SerializeField] private Dropdown m_LocalizersGraphsModeDropdown;
        [SerializeField] private Dropdown m_LocalizersGraphsAtlasDropdown;
        [SerializeField] private InputField m_LocalizersGraphsPrecisionInputField;

        [SerializeField] private GameObject m_LocalizersGraphsVoxelSettingsContainer;
        [SerializeField] private GameObject m_LocalizersGraphsRegionSettingsContainer;
        [SerializeField] private GameObject m_LocalizersGraphsAtlasSettingsContainer;

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
                m_ChannelStructsGroupListGestion.List.Set(m_ChannelStructsGroups);
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
        public async void GenerateLocalizersGraphs()
        {

        }
        #endregion

        #region Private Methods
        protected override void SetFields()
        {
            base.SetFields();

            m_ChannelStructsGroupListGestion.List.Set(m_ChannelStructsGroups);
            m_ChannelStructsGroupListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);

            m_LocalizersGraphsModeDropdown.Set(typeof(LocalizersGraphsMode), (int)LocalizersGraphsMode.Voxel);
            m_LocalizersGraphsModeDropdown.onValueChanged.AddListener(OnChangeLocalizersGraphsMode);
            m_LocalizersGraphsAtlasDropdown.Set(typeof(LocalizersGraphsAtlas), (int)LocalizersGraphsAtlas.MarsAtlas);
            m_LocalizersGraphsPrecisionInputField.text = "1";
        }
        protected void OnChangeLocalizersGraphsMode(int value)
        {
            LocalizersGraphsMode mode = (LocalizersGraphsMode)value;
            m_LocalizersGraphsVoxelSettingsContainer.SetActive(mode == LocalizersGraphsMode.Voxel);
            m_LocalizersGraphsRegionSettingsContainer.SetActive(mode == LocalizersGraphsMode.Region);
            m_LocalizersGraphsAtlasSettingsContainer.SetActive(mode == LocalizersGraphsMode.Atlas);
        }
        #endregion
    }
}