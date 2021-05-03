using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;

namespace HBP.UI.Module3D.Tools
{
    public class DiFuMoSelector : Tool
    {
        #region Properties
        /// <summary>
        /// Dropdown to select the altas to display
        /// </summary>
        [SerializeField] private Dropdown m_AtlasDropdown;
        /// <summary>
        /// Dropdown to select the contrast to display
        /// </summary>
        [SerializeField] private Dropdown m_AreaDropdown;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_AtlasDropdown.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                SelectedScene.FMRIManager.SelectedDiFuMoAtlas = m_AtlasDropdown.options[value].text;
            });
            m_AreaDropdown.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                SelectedScene.FMRIManager.SelectedDiFuMoArea = value;
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            gameObject.SetActive(false);
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isDiFuMoDisplayed = SelectedScene.FMRIManager.DisplayDiFuMo;

            gameObject.SetActive(isDiFuMoDisplayed);
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            m_AtlasDropdown.options.Clear();
            if (ApplicationState.Module3D.DiFuMoObjects.Loaded)
            {
                int count = 0;
                foreach (var atlas in ApplicationState.Module3D.DiFuMoObjects.FMRIs.Keys.OrderBy(k => int.Parse(k)))
                {
                    m_AtlasDropdown.options.Add(new Dropdown.OptionData(atlas));
                    if (atlas == SelectedScene.FMRIManager.SelectedDiFuMoAtlas)
                        m_AtlasDropdown.value = count;
                    count++;
                }
            }
            m_AtlasDropdown.RefreshShownValue();

            m_AreaDropdown.options.Clear();
            if (ApplicationState.Module3D.DiFuMoObjects.Loaded)
            {
                foreach (var label in ApplicationState.Module3D.DiFuMoObjects.Information[SelectedScene.FMRIManager.SelectedDiFuMoAtlas].AllLabels)
                {
                    m_AreaDropdown.options.Add(new Dropdown.OptionData(label.Name));
                }
                m_AreaDropdown.value = SelectedScene.FMRIManager.SelectedDiFuMoArea;
            }
            m_AreaDropdown.RefreshShownValue();
        }
        #endregion
    }
}