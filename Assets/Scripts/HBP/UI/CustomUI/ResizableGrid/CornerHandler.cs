using UnityEngine;
using UnityEngine.EventSystems;

namespace HBP.UI.Tools.ResizableGrids
{
    public class CornerHandler : Handler
    {
        #region Properties
        [SerializeField] protected Theme.State m_CornerState;

        /// <summary>
        /// Associated vertical handler
        /// </summary>
        private VerticalHandler m_VerticalHandler;

        /// <summary>
        /// Associated horizontal handler
        /// </summary>
        private HorizontalHandler m_HorizontalHandler;
        #endregion

        #region Private Methods
        private void Update()
        {
            if (m_VerticalHandler != null && m_HorizontalHandler != null)
            {
                GetComponent<RectTransform>().anchorMin = new Vector2(m_VerticalHandler.Position, m_HorizontalHandler.Position);
                GetComponent<RectTransform>().anchorMax = new Vector2(m_VerticalHandler.Position, m_HorizontalHandler.Position);
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize(ResizableGrid resizableGrid)
        {
            base.Initialize(resizableGrid);
        }
        /// <summary>
        /// Initialize the corner handler with the corresponding handlers
        /// </summary>
        /// <param name="verticalHandler">Associated vertical handler</param>
        /// <param name="horizontalHandler">Associated horizontal handler</param>
        public void SetCorrespondingHandlers(VerticalHandler verticalHandler, HorizontalHandler horizontalHandler)
        {
            m_VerticalHandler = verticalHandler;
            m_HorizontalHandler = horizontalHandler;
            GetComponent<RectTransform>().anchorMin = new Vector2(m_VerticalHandler.Position, m_HorizontalHandler.Position);
            GetComponent<RectTransform>().anchorMax = new Vector2(m_VerticalHandler.Position, m_HorizontalHandler.Position);
        }
        /// <summary>
        /// Callback event when clicking on the handler
        /// </summary>
        /// <param name="eventData">Data of the pointer when the event occurs</param>
        public override void OnPointerDown(PointerEventData data)
        {
            base.OnPointerDown(data);
            m_HorizontalHandler.IsClicked = true;
            m_VerticalHandler.IsClicked = true;
            m_ThemeElement.Set(m_CornerState);
        }
        /// <summary>
        /// Callback event when releasing the click on the handler
        /// </summary>
        /// <param name="eventData">Data of the pointer when the event occurs</param>
        public override void OnPointerUp(PointerEventData data)
        {
            base.OnPointerUp(data);
            m_HorizontalHandler.IsClicked = false;
            m_VerticalHandler.IsClicked = false;
        }
        /// <summary>
        /// Callback event when entering in the handler
        /// </summary>
        /// <param name="data">Data of the pointer when the event occurs</param>
        public override void OnPointerEnter(PointerEventData data)
        {
            if (!m_ResizableGrid.IsHandlerClicked)
            {
                m_ThemeElement.Set(m_CornerState);
            }
        }
        /// <summary>
        /// Callback event when dragging the handler
        /// </summary>
        /// <param name="data">Data of the pointer when the event occurs</param>
        public override void OnDrag(PointerEventData data)
        {
            m_HorizontalHandler.OnDrag(data);
            m_VerticalHandler.OnDrag(data);
            GetComponent<RectTransform>().anchorMin = new Vector2(m_VerticalHandler.Position, m_HorizontalHandler.Position);
            GetComponent<RectTransform>().anchorMax = new Vector2(m_VerticalHandler.Position, m_HorizontalHandler.Position);
        }
        #endregion
    }
}