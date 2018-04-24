using UnityEngine;
using UnityEngine.EventSystems;

public class ZoneResizer : MonoBehaviour
{
    #region Properties
    [SerializeField]
    RectTransform botRect;
    public RectTransform BotRect
    {
        get { return botRect; }
        set { botRect = value; }
    }

    [SerializeField]
    RectTransform topRect;
    public RectTransform TopRect
    {
        get { return topRect; }
        set { topRect = value; }
    }

    [SerializeField]
    RectTransform handleRect;
    public RectTransform HandleRect
    {
        get { return handleRect; }
        set
        {
            RemoveEvents();
            handleRect = value;
            AddEvents();
        }
    }

    public enum DirectionType { LeftToRight,BottomToTop};
    [SerializeField]
    DirectionType direction;
    public DirectionType Direction
    {
        get { return direction; }
        set
        {
            direction = value;
            SetCursor();
        }
    }

    [SerializeField]
    float ratio;
    public float Ratio
    {
        get { return ratio; }
        set
        {
            if(value < min)
            {
                ratio = 0;
            }
            else if(value > max)
            {
                ratio = 1;
            }
            else
            {
                ratio = value;
            }
            Move(ratio);
        }
    }

    [SerializeField]
    float min;
    public float Min
    {
        get { return min; }
        set { min = value; Ratio = Ratio; }
    }

    [SerializeField]
    float max;
    public float Max
    {
        get { return max; }
        set { max = value; Ratio = Ratio; }
    }

    [SerializeField]
    Texture2D cursor;
    Vector2 hotSpot;
    #endregion
    #region private Methods
    void Move(float ratio)
    {
        if(direction == DirectionType.LeftToRight)
        {
            if(botRect)
            {
                botRect.anchorMax = new Vector2(ratio, botRect.anchorMax.y);
            }
            if(topRect)
            {
                topRect.anchorMin = new Vector2(ratio, topRect.anchorMin.y);
            }
            if(handleRect)
            {
                handleRect.anchorMin = new Vector2(ratio, handleRect.anchorMin.y);
                handleRect.anchorMax = new Vector2(ratio, handleRect.anchorMax.y);
            }
        }
        else
        {
            if(botRect)
            {
                botRect.anchorMax = new Vector2(botRect.anchorMax.x, ratio);
            }
            if (topRect)
            {
                topRect.anchorMin = new Vector2(topRect.anchorMin.x, ratio);
            }
            if (handleRect)
            {
                handleRect.anchorMin = new Vector2(handleRect.anchorMin.x, ratio);
                handleRect.anchorMax = new Vector2(handleRect.anchorMax.x, ratio);
            }
        }
        if(ratio == 0)
        {
            botRect.gameObject.SetActive(false);
        }
        else if(ratio == 1)
        {
            topRect.gameObject.SetActive(false);
        }
        else
        {
            botRect.gameObject.SetActive(true);
            topRect.gameObject.SetActive(true);
        }
    }
    void AddEvents()
    {
        if(handleRect)
        {
            EventTrigger eventTrigger = handleRect.GetComponent<EventTrigger>();
            if (!eventTrigger)
            {
                eventTrigger = handleRect.gameObject.AddComponent<EventTrigger>();
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
        if(handleRect)
        {
            EventTrigger eventTrigger = handleRect.GetComponent<EventTrigger>();
            if (eventTrigger)
            {
                DestroyImmediate(eventTrigger);
            }
        }
    }
    void OnPointerEnterDelegate(PointerEventData data)
    {
        Cursor.SetCursor(cursor, hotSpot, CursorMode.Auto);
    }
    void OnPointerExitDelegate(PointerEventData data)
    {
        if(!data.dragging)
        {
            Cursor.SetCursor(ApplicationState.UserPreferences.Theme.General.Cursor.Texture, ApplicationState.UserPreferences.Theme.General.Cursor.Offset, CursorMode.Auto);
        }
    }
    void OnDragDelegate(PointerEventData data)
    {
        RectTransform rectTransform = (RectTransform)transform;
        Vector2 localMousePosition = data.position - ((Vector2)rectTransform.position - Vector2.Scale(rectTransform.pivot, rectTransform.rect.size));
        Vector2 ratio = new Vector2(localMousePosition.x / rectTransform.rect.width, localMousePosition.y / rectTransform.rect.height);
        switch (Direction)
        {
            case DirectionType.BottomToTop: Ratio = ratio.y; break;
            case DirectionType.LeftToRight: Ratio = ratio.x; break;
        }
    }
    void OnEndDragDelegate(PointerEventData data)
    {
        Cursor.SetCursor(ApplicationState.UserPreferences.Theme.General.Cursor.Texture, ApplicationState.UserPreferences.Theme.General.Cursor.Offset, CursorMode.Auto);
    }
    void SetCursor()
    {
        switch(Direction)
        {
            case DirectionType.BottomToTop:
                cursor = ApplicationState.UserPreferences.Theme.General.TopBottomCursor.Texture;
                hotSpot = ApplicationState.UserPreferences.Theme.General.TopBottomCursor.Offset;
                break;

            case DirectionType.LeftToRight:
                cursor = ApplicationState.UserPreferences.Theme.General.LeftRightCursor.Texture;
                hotSpot = ApplicationState.UserPreferences.Theme.General.LeftRightCursor.Offset;
                break;
        }
    }
    void Start()
    {
        AddEvents();
        SetCursor();
    }
    #endregion
}
