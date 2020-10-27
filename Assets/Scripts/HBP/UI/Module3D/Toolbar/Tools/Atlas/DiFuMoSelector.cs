using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class DiFuMoSelector : Tool, IScrollHandler
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

                SelectedScene.FMRIManager.SelectedDiFuMoArea = value;
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
            bool isDiFuMoDisplayed = SelectedScene.FMRIManager.DisplayDiFuMo;

            m_Dropdown.gameObject.SetActive(isDiFuMoDisplayed);
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            m_Dropdown.options.Clear();
            if (ApplicationState.Module3D.DiFuMoObjects.Loaded)
            {
                foreach (var label in ApplicationState.Module3D.DiFuMoObjects.Information.AllLabels)
                {
                    m_Dropdown.options.Add(new Dropdown.OptionData(label.Name));
                }
                m_Dropdown.value = SelectedScene.FMRIManager.SelectedDiFuMoArea;
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