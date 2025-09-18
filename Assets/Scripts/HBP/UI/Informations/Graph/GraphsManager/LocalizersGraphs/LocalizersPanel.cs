using HBP.Core.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Informations
{
    #region Enums
    public enum LocalizersGraphsMode { Voxel, Region, Atlas }
    public enum LocalizersGraphsAtlas { MarsAtlas, Jubrain }
    #endregion

    public class LocalizersPanel : MonoBehaviour
    {
        #region Properties
        [SerializeField] private Dropdown m_LocalizersGraphsModeDropdown;
        [SerializeField] private Dropdown m_LocalizersGraphsAtlasDropdown;
        [SerializeField] private Slider m_LocalizersGraphsPrecisionSlider;

        [SerializeField] private GameObject m_LocalizersGraphsVoxelSettingsContainer;
        [SerializeField] private GameObject m_LocalizersGraphsRegionSettingsContainer;
        [SerializeField] private GameObject m_LocalizersGraphsAtlasSettingsContainer;
        #endregion

        #region Public Methods
        public void Initialize()
        {
            m_LocalizersGraphsModeDropdown.Set(typeof(LocalizersGraphsMode), (int)LocalizersGraphsMode.Voxel);
            m_LocalizersGraphsModeDropdown.onValueChanged.AddListener(OnChangeLocalizersGraphsMode);
            m_LocalizersGraphsAtlasDropdown.Set(typeof(LocalizersGraphsAtlas), (int)LocalizersGraphsAtlas.MarsAtlas);
            m_LocalizersGraphsPrecisionSlider.minValue = 1;
            m_LocalizersGraphsPrecisionSlider.maxValue = 10;
            m_LocalizersGraphsPrecisionSlider.value = 1;
        }
        public async void GenerateLocalizersGraphs()
        {

        }
        #endregion

        #region Private Methods
        private void OnChangeLocalizersGraphsMode(int value)
        {
            LocalizersGraphsMode mode = (LocalizersGraphsMode)value;
            m_LocalizersGraphsVoxelSettingsContainer.SetActive(mode == LocalizersGraphsMode.Voxel);
            m_LocalizersGraphsRegionSettingsContainer.SetActive(mode == LocalizersGraphsMode.Region);
            m_LocalizersGraphsAtlasSettingsContainer.SetActive(mode == LocalizersGraphsMode.Atlas);
        }
        #endregion
    }
}