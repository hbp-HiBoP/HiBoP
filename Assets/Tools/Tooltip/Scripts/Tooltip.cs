using UnityEngine;
using UnityEngine.EventSystems;

namespace Tools.Unity
{
    public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Properties
        [Multiline(10), SerializeField]
        private string m_Text;
        /// <summary>
        /// Text to be displayed
        /// </summary>
        public string Text
        {
            get
            {
                return m_Text;
            }
            set
            {
                m_Text = value;
            }
        }

        [SerializeField]
        private bool m_FollowMouse;
        public bool FollowMouse
        {
            get { return m_FollowMouse; }
            set { m_FollowMouse = value; }
        }


        private bool m_Entered = false;
        private float m_TimeSinceEntered = 0.0f;
        #endregion

        #region Private Methods
        private void Update()
        {
            if (m_Entered)
            {
                m_TimeSinceEntered += Time.deltaTime;
                if ((m_TimeSinceEntered > TooltipManager.TIME_TO_DISPLAY || (ApplicationState.TooltipManager.TooltipHasBeenDisplayedRecently && m_TimeSinceEntered > TooltipManager.TIME_TO_DISPLAY/3)) && !ApplicationState.TooltipManager.IsTooltipDisplayed)
                {
                    ApplicationState.TooltipManager.ShowTooltip(m_Text, m_FollowMouse);
                }
                if (Input.GetAxis("Mouse X") !=0 && Input.GetAxis("Mouse Y") != 0)
                {
                    m_TimeSinceEntered = 0;
                }
            }
        }
        #endregion

        #region Public Methods
        public void OnPointerEnter(PointerEventData data)
        {
            m_Entered = true;
        }
        public void OnPointerExit(PointerEventData data)
        {
            if(Application.isPlaying) ApplicationState.TooltipManager.HideTooltip();
            m_Entered = false;
            m_TimeSinceEntered = 0.0f;
        }
        public void OnDestroy()
        {
            if(ApplicationState.TooltipManager != null) ApplicationState.TooltipManager.HideTooltip();
            m_Entered = false;
            m_TimeSinceEntered = 0.0f;
        }
        #endregion
    }
}