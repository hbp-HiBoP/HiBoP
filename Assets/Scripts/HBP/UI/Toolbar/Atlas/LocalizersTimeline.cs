using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using HBP.Core.Object3D;

namespace HBP.UI.Toolbar
{
    public class LocalizersTimeline : Tool
    {
        #region Properties
        /// <summary>
        /// Slider to change the current sample of the timeline
        /// </summary>
        [SerializeField] private Slider m_Slider;
        /// <summary>
        /// Transform that will contain the zero marker
        /// </summary>
        [SerializeField] private RectTransform m_TimelineContainer;
        /// <summary>
        /// Prefab for the zero marker
        /// </summary>
        [SerializeField] private GameObject m_ZeroMarkerPrefab;
        [SerializeField] private Text m_StartTimeText;
        [SerializeField] private Text m_CurrentTimeText;
        [SerializeField] private Text m_EndTimeText;
        /// <summary>
        /// Reference to the instantiated zero marker
        /// </summary>
        private GameObject m_ZeroMarker;
        #endregion

        #region Private Methods
        /// <summary>
        /// Create and position the zero marker on the timeline
        /// </summary>
        private void CreateZeroMarker()
        {
            DeleteZeroMarker();

            var currentFMRI = Object3DManager.Localizers.GetCurrentFMRI(SelectedScene.FMRIManager.SelectedLocalizersProtocol, SelectedScene.FMRIManager.SelectedLocalizersData, SelectedScene.FMRIManager.SelectedLocalizersBloc);
            if (currentFMRI != null && currentFMRI.Loaded && m_ZeroMarkerPrefab != null)
            {
                // Calculate the position of time zero in the timeline
                float timeZero = 0f;
                float startTime = currentFMRI.StartTime;
                float endTime = startTime + currentFMRI.TimeStep * (currentFMRI.Volumes.Count - 1);
                
                // Only create marker if zero is within the timeline range
                if (timeZero >= startTime && timeZero <= endTime)
                {
                    float normalizedPosition = Mathf.InverseLerp(startTime, endTime, timeZero);
                    
                    m_ZeroMarker = Instantiate(m_ZeroMarkerPrefab, m_TimelineContainer);
                    RectTransform markerRect = m_ZeroMarker.GetComponent<RectTransform>();
                    
                    // Position the marker at the zero time position
                    markerRect.anchorMin = new Vector2(normalizedPosition, 0);
                    markerRect.anchorMax = new Vector2(normalizedPosition, 1);
                    markerRect.anchoredPosition = Vector2.zero;
                }
            }
        }
        /// <summary>
        /// Remove the zero marker
        /// </summary>
        private void DeleteZeroMarker()
        {
            if (m_ZeroMarker != null)
            {
                Destroy(m_ZeroMarker);
                m_ZeroMarker = null;
            }
        }
        /// <summary>
        /// Update the timeline display based on current FMRI
        /// </summary>
        private void UpdateTimeline()
        {
            var currentFMRI = Object3DManager.Localizers.GetCurrentFMRI(SelectedScene.FMRIManager.SelectedLocalizersProtocol, SelectedScene.FMRIManager.SelectedLocalizersData, SelectedScene.FMRIManager.SelectedLocalizersBloc);

            if (currentFMRI != null && currentFMRI.Loaded)
            {
                // Setup slider range based on FMRI volumes
                m_Slider.minValue = 0;
                m_Slider.maxValue = currentFMRI.Volumes.Count - 1;
                m_Slider.value = SelectedScene.FMRIManager.SelectedLocalizersTimelineIndex;
                
                // Create zero marker
                CreateZeroMarker();
                
                // Update time texts
                UpdateTimeTexts();
            }
            else
            {
                m_Slider.minValue = 0;
                m_Slider.maxValue = 0;
                m_Slider.value = 0;
                DeleteZeroMarker();
                ClearTimeTexts();
            }
        }
        /// <summary>
        /// Update the time texts with current FMRI timing information
        /// </summary>
        private void UpdateTimeTexts()
        {
            var currentFMRI = Object3DManager.Localizers.GetCurrentFMRI(SelectedScene.FMRIManager.SelectedLocalizersProtocol, SelectedScene.FMRIManager.SelectedLocalizersData, SelectedScene.FMRIManager.SelectedLocalizersBloc);
            
            if (currentFMRI != null && currentFMRI.Loaded)
            {
                float startTime = currentFMRI.StartTime;
                float endTime = startTime + currentFMRI.TimeStep * (currentFMRI.Volumes.Count - 1);
                int currentIndex = SelectedScene.FMRIManager.SelectedLocalizersTimelineIndex;
                float currentTime = startTime + currentFMRI.TimeStep * currentIndex;

                m_StartTimeText.text = $"{startTime.ToString("N0")} {currentFMRI.TimeUnit}";
                m_CurrentTimeText.text = $"{currentIndex} ({currentTime.ToString("N2")} {currentFMRI.TimeUnit})";
                m_EndTimeText.text = $"{endTime.ToString("N0")} {currentFMRI.TimeUnit}";
            }
        }
        private void ClearTimeTexts()
        {
            m_StartTimeText.text = "";
            m_CurrentTimeText.text = "";
            m_EndTimeText.text = "";
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Slider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                int timelineIndex = Mathf.RoundToInt(value);
                SelectedScene.FMRIManager.SelectedLocalizersTimelineIndex = timelineIndex;
                UpdateTimeTexts();
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            gameObject.SetActive(false);
            m_Slider.value = 0;
            m_Slider.interactable = false;
            DeleteZeroMarker();
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isLocalizersDisplayed = SelectedScene.FMRIManager.DisplayLocalizers;
            var currentFMRI = Object3DManager.Localizers.GetCurrentFMRI(SelectedScene.FMRIManager.SelectedLocalizersProtocol, SelectedScene.FMRIManager.SelectedLocalizersData, SelectedScene.FMRIManager.SelectedLocalizersBloc);
            bool hasFMRI = currentFMRI != null && currentFMRI.Loaded;

            gameObject.SetActive(isLocalizersDisplayed);
            m_Slider.interactable = isLocalizersDisplayed && hasFMRI;
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            if (SelectedScene.FMRIManager.DisplayLocalizers == true)
            {
                var currentFMRI = Object3DManager.Localizers.GetCurrentFMRI(SelectedScene.FMRIManager.SelectedLocalizersProtocol, SelectedScene.FMRIManager.SelectedLocalizersData, SelectedScene.FMRIManager.SelectedLocalizersBloc);
                if (currentFMRI != null && currentFMRI.Loaded)
                {
                    UpdateTimeline();
                }
            }
        }
        #endregion
    }
}