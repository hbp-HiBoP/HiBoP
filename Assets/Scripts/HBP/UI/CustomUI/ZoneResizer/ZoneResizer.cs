using HBP.Theme.Components;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HBP.UI.Tools
{
    public class ZoneResizer : MonoBehaviour
    {
        #region Properties
        public Theme.State LeftRight;
        public Theme.State TopBottom;
        public ThemeElement ThemeElement;
        public float MarginWidth;

        private CanvasScalerHandler m_CanvasScalerHandler;

        [SerializeField]
        RectTransform m_BotRect;
        public RectTransform BotRect
        {
            get { return m_BotRect; }
            set { m_BotRect = value; }
        }

        [SerializeField]
        RectTransform m_TopRect;
        public RectTransform TopRect
        {
            get { return m_TopRect; }
            set { m_TopRect = value; }
        }

        [SerializeField]
        RectTransform m_HandleRect;
        public RectTransform HandleRect
        {
            get { return m_HandleRect; }
            set
            {
                RemoveEvents();
                m_HandleRect = value;
                AddEvents();
            }
        }

        public enum DirectionType { LeftToRight, BottomToTop };
        [SerializeField]
        DirectionType m_Direction;
        public DirectionType Direction
        {
            get { return m_Direction; }
            set
            {
                m_Direction = value;
            }
        }

        [SerializeField]
        float m_Ratio;
        public float Ratio
        {
            get { return m_Ratio; }
            set
            {
                if (value < m_Min)
                {
                    m_Ratio = 0;
                }
                else if (value > m_Max)
                {
                    m_Ratio = 1;
                }
                else
                {
                    m_Ratio = value;
                }
                Move(m_Ratio);
            }
        }

        [SerializeField]
        float m_Min;
        public float Min
        {
            get { return m_Min; }
            set { m_Min = value; Ratio = Ratio; }
        }

        [SerializeField]
        float m_Max;
        public float Max
        {
            get { return m_Max; }
            set { m_Max = value; Ratio = Ratio; }
        }

        [SerializeField]
        Texture2D m_Cursor;
        #endregion

        #region Private Methods
        void Awake()
        {
            m_CanvasScalerHandler = GetComponentInParent<CanvasScalerHandler>();
            AddEvents();
            TestIfRectIsActive();
        }
        void Move(float ratio)
        {
            if (m_Direction == DirectionType.LeftToRight)
            {
                if (m_BotRect)
                {
                    m_BotRect.anchorMax = new Vector2(ratio, m_BotRect.anchorMax.y);
                }
                if (m_TopRect)
                {
                    m_TopRect.anchorMin = new Vector2(ratio, m_TopRect.anchorMin.y);
                }
                if (m_HandleRect)
                {
                    m_HandleRect.anchorMin = new Vector2(ratio, m_HandleRect.anchorMin.y);
                    m_HandleRect.anchorMax = new Vector2(ratio, m_HandleRect.anchorMax.y);
                }
            }
            else
            {
                if (m_BotRect)
                {
                    m_BotRect.anchorMax = new Vector2(m_BotRect.anchorMax.x, ratio);
                }
                if (m_TopRect)
                {
                    m_TopRect.anchorMin = new Vector2(m_TopRect.anchorMin.x, ratio);
                }
                if (m_HandleRect)
                {
                    m_HandleRect.anchorMin = new Vector2(m_HandleRect.anchorMin.x, ratio);
                    m_HandleRect.anchorMax = new Vector2(m_HandleRect.anchorMax.x, ratio);
                }
            }
            TestIfRectIsActive();
        }
        void AddEvents()
        {
            if (m_HandleRect)
            {
                EventTrigger eventTrigger = m_HandleRect.GetComponent<EventTrigger>();
                if (!eventTrigger)
                {
                    eventTrigger = m_HandleRect.gameObject.AddComponent<EventTrigger>();
                }
                eventTrigger.hideFlags = HideFlags.HideInInspector;

                EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
                pointerEnter.eventID = EventTriggerType.PointerEnter;
                pointerEnter.callback.AddListener((data) => OnPointerEnterDelegate((PointerEventData)data));
                eventTrigger.triggers.Add(pointerEnter);

                EventTrigger.Entry pointerExit = new EventTrigger.Entry();
                pointerExit.eventID = EventTriggerType.PointerExit;
                pointerExit.callback.AddListener((data) => OnPointerExitDelegate((PointerEventData)data));
                eventTrigger.triggers.Add(pointerExit);

                EventTrigger.Entry drag = new EventTrigger.Entry();
                drag.eventID = EventTriggerType.Drag;
                drag.callback.AddListener((data) => OnDragDelegate((PointerEventData)data));
                eventTrigger.triggers.Add(drag);

                EventTrigger.Entry endDrag = new EventTrigger.Entry();
                endDrag.eventID = EventTriggerType.EndDrag;
                endDrag.callback.AddListener((data) => OnEndDragDelegate((PointerEventData)data));
                eventTrigger.triggers.Add(endDrag);
            }
        }
        void RemoveEvents()
        {
            if (m_HandleRect)
            {
                EventTrigger eventTrigger = m_HandleRect.GetComponent<EventTrigger>();
                if (eventTrigger)
                {
                    DestroyImmediate(eventTrigger);
                }
            }
        }
        void OnPointerEnterDelegate(PointerEventData data)
        {
            switch (Direction)
            {
                case DirectionType.LeftToRight:
                    ThemeElement.Set(LeftRight);
                    break;
                case DirectionType.BottomToTop:
                    ThemeElement.Set(TopBottom);
                    break;
            }
        }
        void OnPointerExitDelegate(PointerEventData data)
        {
            if (!data.dragging)
            {
                ThemeElement.Set();
            }
        }
        void OnDragDelegate(PointerEventData data)
        {
            float scale = m_CanvasScalerHandler.Scale;
            Vector2 scaledDataPosition = new Vector2(scale * data.position.x, scale * data.position.y);
            RectTransform rectTransform = (RectTransform)transform;
            Vector2 scaledRectTransformPosition = new Vector2(scale * rectTransform.position.x, scale * rectTransform.position.y);
            Vector2 localMousePosition = scaledDataPosition - (scaledRectTransformPosition - Vector2.Scale(rectTransform.pivot, rectTransform.rect.size));
            Vector2 ratio = new Vector2(localMousePosition.x / rectTransform.rect.width, localMousePosition.y / rectTransform.rect.height);
            switch (Direction)
            {
                case DirectionType.BottomToTop: Ratio = ratio.y; break;
                case DirectionType.LeftToRight: Ratio = ratio.x; break;
            }
        }
        void OnEndDragDelegate(PointerEventData data)
        {
            ThemeElement.Set();
        }
        void TestIfRectIsActive()
        {
            if (m_Ratio == 0)
            {
                m_BotRect.gameObject.SetActive(false);
                if (Direction == DirectionType.BottomToTop)
                {
                    m_TopRect.offsetMin = new Vector2(m_TopRect.offsetMin.x, 0);
                }
                else
                {
                    m_TopRect.offsetMin = new Vector2(0, m_TopRect.offsetMin.y);
                }
            }
            else if (m_Ratio == 1)
            {
                m_TopRect.gameObject.SetActive(false);
                if (Direction == DirectionType.BottomToTop)
                {
                    m_BotRect.offsetMax = new Vector2(m_BotRect.offsetMax.x, 0);
                }
                else
                {
                    m_BotRect.offsetMax = new Vector2(0, m_BotRect.offsetMax.y);
                }
            }
            else
            {
                m_BotRect.gameObject.SetActive(true);
                m_TopRect.gameObject.SetActive(true);
                if (Direction == DirectionType.BottomToTop)
                {
                    m_BotRect.offsetMax = new Vector2(m_BotRect.offsetMax.x, -MarginWidth / 2);
                    m_TopRect.offsetMin = new Vector2(m_TopRect.offsetMin.x, MarginWidth / 2);
                }
                else
                {
                    m_BotRect.offsetMax = new Vector2(MarginWidth / 2, -m_BotRect.offsetMax.y);
                    m_TopRect.offsetMin = new Vector2(MarginWidth / 2, m_TopRect.offsetMin.y);
                }
            }
        }
        #endregion
    }
}