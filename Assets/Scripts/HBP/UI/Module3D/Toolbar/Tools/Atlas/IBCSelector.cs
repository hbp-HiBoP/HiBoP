using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class IBCSelector : Tool, IScrollHandler
    {
        #region Properties
        /// <summary>
        /// Dropdown to select the contrast to display
        /// </summary>
        [SerializeField] private Dropdown m_Dropdown;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Dropdown.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                SelectedScene.FMRIManager.SelectedIBCContrastID = value;
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Dropdown.gameObject.SetActive(false);
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isIBC = SelectedScene.FMRIManager.DisplayIBCContrasts;

            m_Dropdown.gameObject.SetActive(isIBC);
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            m_Dropdown.options.Clear();
            if (ApplicationState.Module3D.IBCObjects.Loaded)
            {
                foreach (var contrast in ApplicationState.Module3D.IBCObjects.Contrasts)
                {
                    m_Dropdown.options.Add(new Dropdown.OptionData(contrast.Name));
                }
                m_Dropdown.value = SelectedScene.FMRIManager.SelectedIBCContrastID;
            }
            m_Dropdown.RefreshShownValue();
        }
        public void OnScroll(PointerEventData eventData)
        {
            int newValue = m_Dropdown.value + (eventData.scrollDelta.y < 0 ? 1 : -1);
            int total = m_Dropdown.options.Count;
            m_Dropdown.value = ((newValue % total) + total) % total;
            m_Dropdown.RefreshShownValue();
        }
        #endregion
    }
}