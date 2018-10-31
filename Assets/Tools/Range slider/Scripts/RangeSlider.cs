using System;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI.Extensions;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/RangeSlider", 34)]
    [RequireComponent(typeof(RectTransform))]
    public class RangeSlider : Selectable, IDragHandler, IInitializePotentialDragHandler , ICanvasElement
    {
        #region Intern Class/Struts
        public enum Direction
        {
            LeftToRight,
            RightToLeft,
            BottomToTop,
            TopToBottom,
        }
        public enum SliderType
        {
            Simplified,
            Complete
        }
        enum Handle
        {
            None,
            Min,
            Fill,
            Max
        }
        enum Axis
        {
            Horizontal = 0,
            Vertical = 1
        }
        [Serializable]
        public class RangeSliderEvent : UnityEvent<float, float> { }
        #endregion

        #region Properties
        [SerializeField] SliderType m_Type = SliderType.Complete;
        public SliderType type { get { return m_Type; } set { if (SetPropertyUtility.SetStruct(ref m_Type, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        [SerializeField] RectTransform m_FillRect;
        public RectTransform fillRect { get { return m_FillRect; } set { if (SetPropertyUtility.SetClass(ref m_FillRect, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        [SerializeField] RectTransform m_MinHandleRect;
        public RectTransform minHandleRect { get { return m_MinHandleRect; } set { if (SetPropertyUtility.SetClass(ref m_MinHandleRect, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        [SerializeField] RectTransform m_MaxHandleRect;
        public RectTransform maxHandleRect { get { return m_MaxHandleRect; } set { if (SetPropertyUtility.SetClass(ref m_MaxHandleRect, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        [SerializeField] Graphic m_MinHandleTargetGraphic;
        public Graphic minHandleTargetGraphic { get { return m_MinHandleTargetGraphic; } set { if(SetPropertyUtility.SetClass(ref m_MinHandleTargetGraphic, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        [SerializeField] Graphic m_MaxHandleTargetGraphic;
        public Graphic maxHandleTargetGraphic { get { return m_MaxHandleTargetGraphic; } set { if (SetPropertyUtility.SetClass(ref m_MaxHandleTargetGraphic, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        [SerializeField] Graphic m_FillTargetGraphic;
        public Graphic fillTargetGraphic { get { return m_FillTargetGraphic; } set { if (SetPropertyUtility.SetClass(ref m_FillTargetGraphic, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        [SerializeField] AnimationTriggers m_FillAnimationTriggers;
        public AnimationTriggers fillAnimationTriggers { get { return m_FillAnimationTriggers; } set { if (SetPropertyUtility.SetClass(ref m_FillAnimationTriggers, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        [SerializeField] AnimationTriggers m_HandleAnimationTriggers;
        public AnimationTriggers handleanimationTriggers { get { return m_HandleAnimationTriggers; } set { if (SetPropertyUtility.SetClass(ref m_HandleAnimationTriggers, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        [SerializeField] SpriteState m_FillSpriteState;
        public SpriteState fillSpriteState { get { return m_FillSpriteState; } set { if (SetPropertyUtility.SetStruct(ref m_FillSpriteState, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        [SerializeField] SpriteState m_HandleSpriteState;
        public SpriteState handleSpriteState { get { return m_HandleSpriteState; } set { if (SetPropertyUtility.SetStruct(ref m_HandleSpriteState, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        [SerializeField] ColorBlock m_FillColors;
        public ColorBlock fillColors { get { return m_FillColors; } set { if (SetPropertyUtility.SetStruct(ref m_FillColors, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        [SerializeField] ColorBlock m_HandleColors;
        public ColorBlock handleColors { get { return m_HandleColors; } set { if (SetPropertyUtility.SetStruct(ref m_HandleColors, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        [SerializeField] Transition m_FillTransition;
        public Transition FillTransition { get { return m_FillTransition; } set { if (SetPropertyUtility.SetStruct(ref m_FillTransition, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        [SerializeField] Transition m_HandleTransition;
        public Transition handleTransition { get { return m_HandleTransition; } set { if (SetPropertyUtility.SetStruct(ref m_HandleTransition, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        public Animator FillAnimator { get { return m_FillRect != null ? m_FillRect.GetComponent<Animator>() : null; } }

        public Animator minHandleAnimator { get { return m_MinHandleRect != null ? m_MinHandleRect.GetComponent<Animator>() : null; } } 

        public Animator maxHandleAnimator { get { return m_MaxHandleRect != null ? m_MaxHandleRect.GetComponent<Animator>() : null; }  }

        [SerializeField] Direction m_Direction = Direction.LeftToRight;
        public Direction direction { get { return m_Direction; } set { if (SetPropertyUtility.SetStruct(ref m_Direction, value)) UpdateVisuals(); } }

        [SerializeField] float m_MinLimit = 0;
        public float minLimit { get { return m_MinLimit; } set { if (SetPropertyUtility.SetStruct(ref m_MinLimit, value)) { Set(m_MinValue, m_MaxValue, Handle.Min); UpdateVisuals(); } } }

        [SerializeField] float m_MaxLimit = 1;
        public float maxLimit { get { return m_MaxLimit; } set { if (SetPropertyUtility.SetStruct(ref m_MaxLimit, value)) { Set(m_MinValue, m_MaxValue, Handle.Max); UpdateVisuals(); } } }

        [SerializeField] float m_MinValue = 0f;
        public float minValue
        {
            get
            {
                return m_MinValue;
            }
            set
            {
                Set(value, m_MaxValue, Handle.Min);
            }
        }
        public float normalizedMinValue
        {
            get
            {
                if (Mathf.Approximately(minLimit, maxLimit))
                    return 0;
                return Mathf.InverseLerp(minLimit, maxLimit, minValue);
            }
            set
            {
                this.minValue = Mathf.Lerp(minLimit, maxLimit, value);
            }
        }

        [SerializeField] float m_MaxValue = 1f;
        public float maxValue
        {
            get
            {
                return m_MaxValue;
            }
            set
            {
                Set(m_MinValue, value, Handle.Max);
            }
        }
        public float normalizedMaxValue
        {
            get
            {
                if (Mathf.Approximately(minLimit, maxLimit))
                    return 0;
                return Mathf.InverseLerp(minLimit, maxLimit, maxValue);
            }
            set
            {
                this.maxValue = Mathf.Lerp(minLimit, maxLimit, value);
            }
        }

        public Vector2 Values
        {
            get { return new Vector2(m_MinValue, m_MaxValue); }
            set { Set(value.x, value.y, Handle.None); }
        }

        [SerializeField] float m_Step = 0.1f;
        public float step { get { return m_Step; } set { m_Step = value; } }
        public float normalizedStep
        {
            get
            {
                if (Mathf.Approximately(minLimit, maxLimit))
                    return 0;
                return Mathf.InverseLerp(0, maxLimit - minLimit, step);
            }
            set
            {
                step = Mathf.Lerp(0, maxLimit - minLimit, value);
            }
        }

        [SerializeField] RangeSliderEvent m_OnValueChanged = new RangeSliderEvent();
        public RangeSliderEvent onValueChanged { get { return m_OnValueChanged; } set { m_OnValueChanged = value; } }

        Axis axis { get { return (m_Direction == Direction.LeftToRight || m_Direction == Direction.RightToLeft) ? Axis.Horizontal : Axis.Vertical; } }

        bool reverseValue { get { return m_Direction == Direction.RightToLeft || m_Direction == Direction.TopToBottom; } }

        // Private fields
        RectTransform m_FillContainerRect;
        RectTransform m_HandlesContainerRect;
        bool m_IsHovered;
        bool m_IsClicked;
        bool m_IsSelected;

        bool m_VisualUpdateRequired;
        SelectionState m_MinHandleSelectionState;
        SelectionState minHandleSelectionSate { get { return m_MinHandleSelectionState; } set { if (SetPropertyUtility.SetStruct(ref m_MinHandleSelectionState, value)) { m_VisualUpdateRequired = true; } } }

        SelectionState m_MaxHandleSelectionState;
        SelectionState maxHandleSelectionSate { get { return m_MaxHandleSelectionState; } set { if (SetPropertyUtility.SetStruct(ref m_MaxHandleSelectionState, value)) { m_VisualUpdateRequired = true; } } }

        SelectionState m_FillSelectionState;
        SelectionState fillSelectionSate { get { return m_FillSelectionState; } set { if (SetPropertyUtility.SetStruct(ref m_FillSelectionState, value)) { m_VisualUpdateRequired = true; } } }

        Handle m_SelectedHandle = Handle.None;
        Vector2 m_Offset = Vector2.zero;
        DrivenRectTransformTracker m_Tracker;
        #endregion

        #region Public Methods
        public virtual void Rebuild(CanvasUpdate executing)
        {
#if UNITY_EDITOR
            if (executing == CanvasUpdate.Prelayout)
                onValueChanged.Invoke(minValue, maxValue);
#endif
        }
        public override void OnPointerEnter(PointerEventData eventData)
        {
            m_IsHovered = true;
        }
        public override void OnPointerExit(PointerEventData eventData)
        {
            m_IsHovered = false;
        }
        public override void OnPointerDown(PointerEventData eventData)
        {
            m_IsClicked = true;
            if (!MayDrag(eventData))
                return;

            base.OnPointerDown(eventData);

            Vector3 localMousePosition;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(transform as RectTransform, eventData.position, eventData.pressEventCamera, out localMousePosition);
            bool isOnMinHandle = RectTransformUtility.RectangleContainsScreenPoint(m_MinHandleRect, eventData.position, eventData.enterEventCamera);
            bool isOnMaxHandle = RectTransformUtility.RectangleContainsScreenPoint(m_MaxHandleRect, eventData.position, eventData.enterEventCamera);
            bool isOnFill = RectTransformUtility.RectangleContainsScreenPoint(m_FillRect, eventData.position, eventData.enterEventCamera);
            float distanceBetweenPointerAndMinHandle = (localMousePosition - m_MinHandleRect.position).sqrMagnitude;
            float distanceBetweenPointerAndMaxHandle = (localMousePosition - m_MaxHandleRect.position).sqrMagnitude;
            switch (type)
            {
                case SliderType.Simplified:
                    m_SelectedHandle = distanceBetweenPointerAndMaxHandle > distanceBetweenPointerAndMinHandle ? Handle.Min : Handle.Max;
                    break;
                case SliderType.Complete:
                    if (isOnFill && !(isOnMaxHandle || isOnMinHandle))
                    {
                        m_SelectedHandle = Handle.Fill;
                    }
                    else
                    {
                        m_SelectedHandle = distanceBetweenPointerAndMaxHandle > distanceBetweenPointerAndMinHandle ? Handle.Min : Handle.Max;
                    }
                    break;
                default:
                    break;
            }

            m_Offset = Vector2.zero;
            switch (m_SelectedHandle)
            {
                case Handle.Min:
                    if (m_HandlesContainerRect != null && isOnMinHandle)
                    {
                        Vector2 localMousePos;
                        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_MinHandleRect, eventData.position, eventData.pressEventCamera, out localMousePos))
                            m_Offset = localMousePos;
                    }
                    else
                    {
                        // Outside the slider handle - jump to this point instead
                        UpdateDrag(eventData, eventData.pressEventCamera);
                    }
                    break;
                case Handle.Max:
                    if (m_HandlesContainerRect != null && isOnMaxHandle)
                    {
                        Vector2 localMousePos;
                        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_MaxHandleRect, eventData.position, eventData.pressEventCamera, out localMousePos))
                            m_Offset = localMousePos;
                    }
                    else
                    {
                        // Outside the slider handle - jump to this point instead
                        UpdateDrag(eventData, eventData.pressEventCamera);
                    }
                    break;
                case Handle.Fill:
                    if (m_FillContainerRect != null && isOnFill)
                    {
                        Vector2 localMousePos;
                        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_FillRect, eventData.position, eventData.pressEventCamera, out localMousePos))
                            m_Offset = localMousePos;
                    }
                    break;
                default:
                    break;
            }
        }
        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            UpdateDrag(eventData, eventData.pressEventCamera);
        }
        public override void OnMove(AxisEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
            {
                base.OnMove(eventData);
                return;
            }

            float movement;
            float localStep = step;
            if (Mathf.Approximately(localStep, 0)) localStep = 0.1f * (maxLimit - minLimit);

            switch (eventData.moveDir)
            {
                case MoveDirection.Left:
                    if (axis == Axis.Horizontal && FindSelectableOnLeft() == null)
                    {
                        movement = reverseValue ? localStep : -localStep;
                        Set(movement);
                    }
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Right:
                    if (axis == Axis.Horizontal && FindSelectableOnRight() == null)
                    {
                        movement = reverseValue ? -localStep : localStep;
                        Set(movement);
                    }
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Up:
                    if (axis == Axis.Vertical && FindSelectableOnUp() == null)
                    {
                        movement = reverseValue ? -localStep : localStep;
                        Set(movement);
                    }
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Down:
                    if (axis == Axis.Vertical && FindSelectableOnDown() == null)
                    {
                        movement = reverseValue ? localStep : -localStep;
                        Set(movement);
                    }
                    else
                        base.OnMove(eventData);
                    break;
            }
        }
        public override void OnPointerUp(PointerEventData eventData)
        {
            m_IsClicked = false;
            m_SelectedHandle = Handle.None;
        }
        public virtual void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }
        public override void OnSelect(BaseEventData eventData)
        {
            m_IsSelected = true;
        }
        public override void OnDeselect(BaseEventData eventData)
        {
            m_IsSelected = false;
        }
        public void SetDirection(Direction direction, bool includeRectLayouts)
        {
            Axis oldAxis = axis;
            bool oldReverse = reverseValue;
            this.direction = direction;

            if (!includeRectLayouts)
                return;

            if (axis != oldAxis)
                RectTransformUtility.FlipLayoutAxes(transform as RectTransform, true, true);

            if (reverseValue != oldReverse)
                RectTransformUtility.FlipLayoutOnAxis(transform as RectTransform, (int)axis, true, true);
        }
        public void LayoutComplete()
        {
            m_MinHandleTargetGraphic.LayoutComplete();
            m_MaxHandleTargetGraphic.LayoutComplete();
            m_FillTargetGraphic.LayoutComplete();
        }
        public void GraphicUpdateComplete()
        {
            m_MinHandleTargetGraphic.GraphicUpdateComplete();
            m_MaxHandleTargetGraphic.GraphicUpdateComplete();
            m_FillTargetGraphic.GraphicUpdateComplete();
        }
        public override Selectable FindSelectableOnLeft()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Horizontal)
                return null;
            return base.FindSelectableOnLeft();
        }
        public override Selectable FindSelectableOnRight()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Horizontal)
                return null;
            return base.FindSelectableOnRight();
        }
        public override Selectable FindSelectableOnUp()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Vertical)
                return null;
            return base.FindSelectableOnUp();
        }
        public override Selectable FindSelectableOnDown()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Vertical)
                return null;
            return base.FindSelectableOnDown();
        }
        #endregion
        
        #region Private Methods
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            UpdateCachedReferences();
            Set(m_MinValue, m_MaxValue, Handle.None, false);
            // Update rects since other things might affect them even if value didn't change.
            UpdateVisuals();
            UpdateSelectionState();
            TransitionToSelectionState(true);
        }
#endif
        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateCachedReferences();
            Set(m_MinValue, m_MaxValue, Handle.None, false);
            // Update rects since they need to be initialized correctly.
            UpdateVisuals();
            UpdateSelectionState();
            TransitionToSelectionState(true);
        }
        protected override void OnDisable()
        {
            m_Tracker.Clear();
            base.OnDisable();
        }
        protected void Update()
        {
            EvaluateAndTransitionToSelectionState();
        }
        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            UpdateVisuals();
        }
        void Set(float movement)
        {
            if (minValue + movement < minLimit) movement = minLimit - minValue;
            if (maxValue + movement > maxLimit) movement = maxLimit - maxValue;
            Set(minValue + movement, maxValue + movement, Handle.None);
        }
        void Set(float minInput, float maxInput, Handle commanderHandle, bool sendCallback = true)
        {
            // Clamp the input
            float newMinValue = Mathf.Clamp(minInput, minLimit, maxLimit);
            float newMaxValue = Mathf.Clamp(maxInput, minLimit, maxLimit);

            // If the stepped value doesn't match the last one, it's time to update
            if (m_MinValue == newMinValue && m_MaxValue == newMaxValue)
                return;

            switch (commanderHandle)
            {
                case Handle.Min:
                    m_MinValue = newMinValue;
                    m_MaxValue = Mathf.Max(newMaxValue, newMinValue);
                    break;
                case Handle.Max:
                    m_MaxValue = newMaxValue;
                    m_MinValue = Mathf.Min(newMinValue, newMaxValue);
                    break;
                default:
                    m_MinValue = newMinValue;
                    m_MaxValue = newMaxValue;
                    break;
            }
            Debug.Log(m_MinValue);
            UpdateVisuals();
            if (sendCallback)
                m_OnValueChanged.Invoke(m_MinValue, m_MaxValue);
        }
        void UpdateCachedReferences()
        {
            if (m_FillRect != null && m_FillRect.parent != null)
            {
                m_FillContainerRect = m_FillRect.parent.GetComponent<RectTransform>();
            }
            else
            {
                m_FillContainerRect = null;
            }

            if (m_MinHandleRect != null && m_MinHandleRect.parent != null)
            {
                m_HandlesContainerRect = m_MinHandleRect.parent.GetComponent<RectTransform>();
            }
            else if (m_MaxHandleRect != null && m_MaxHandleRect.parent != null)
            {
                m_HandlesContainerRect = m_MaxHandleRect.parent.GetComponent<RectTransform>();
            }
            else
            {
                m_HandlesContainerRect = null;
            }
        }
        void UpdateVisuals()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UpdateCachedReferences();
#endif

            m_Tracker.Clear();

            if (m_FillContainerRect != null)
            {
                m_Tracker.Add(this, m_FillRect, DrivenTransformProperties.Anchors);
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;

                if (reverseValue)
                {
                    anchorMin[(int)axis] = 1 - normalizedMaxValue;
                    anchorMax[(int)axis] = 1 - normalizedMinValue;
                }
                else
                {
                    anchorMin[(int)axis] = normalizedMinValue;
                    anchorMax[(int)axis] = normalizedMaxValue;
                }
                m_FillRect.anchorMin = anchorMin;
                m_FillRect.anchorMax = anchorMax;
            }

            if (m_HandlesContainerRect != null)
            {
                m_Tracker.Add(this, m_MinHandleRect, DrivenTransformProperties.Anchors);
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;
                anchorMin[(int)axis] = anchorMax[(int)axis] = (reverseValue ? (1 - normalizedMaxValue) : normalizedMinValue);
                m_MinHandleRect.anchorMin = anchorMin;
                m_MinHandleRect.anchorMax = anchorMax;

                m_Tracker.Add(this, m_MaxHandleRect, DrivenTransformProperties.Anchors);
                anchorMin = Vector2.zero;
                anchorMax = Vector2.one;
                anchorMin[(int)axis] = anchorMax[(int)axis] = (reverseValue ? (1 - normalizedMinValue) : normalizedMaxValue);
                m_MaxHandleRect.anchorMin = anchorMin;
                m_MaxHandleRect.anchorMax = anchorMax;
            }
        }
        void UpdateDrag(PointerEventData eventData, Camera cam)
        {
            RectTransform clickRect = m_HandlesContainerRect ?? m_FillContainerRect;
            if (clickRect != null && clickRect.rect.size[(int)axis] > 0)
            {
                Vector2 localCursor;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(clickRect, eventData.position, cam, out localCursor))
                    return;
                localCursor -= clickRect.rect.position;

                float val = Mathf.Clamp01((localCursor - m_Offset)[(int)axis] / clickRect.rect.size[(int)axis]);
                switch (m_SelectedHandle)
                {
                    case Handle.Min:
                        if (Mathf.Approximately(val, 0)) normalizedMinValue = 0;
                        else normalizedMinValue = RoundAtPrecision(reverseValue ? 1f - val : val, normalizedStep);
                        break;
                    case Handle.Max:
                        if (Mathf.Approximately(val, 1)) normalizedMaxValue = 1;
                        else normalizedMaxValue = RoundAtPrecision(reverseValue ? 1f - val : val, normalizedStep);
                        break;
                    case Handle.Fill:
                        Vector2 delta = eventData.position - ((Vector2) m_FillRect.position + m_Offset);
                        float normalizedMovement = (reverseValue ? -1 : 1) * delta[(int)axis] / clickRect.rect.size[(int)axis];
                        normalizedMovement = RoundAtPrecision(normalizedMovement, normalizedStep);
                        float absMovement = Mathf.Lerp(0, m_MaxLimit - m_MinLimit, Mathf.Abs(normalizedMovement));
                        float movement = Mathf.Sign(normalizedMovement) * absMovement;
                        Set(movement);
                        break;
                    default:
                        break;
                }
            }
        }
        bool MayDrag(PointerEventData eventData)
        {
            return IsActive() && IsInteractable() && eventData.button == PointerEventData.InputButton.Left;
        }
        float RoundAtPrecision(float number, float precision)
        {
            if (Mathf.Approximately(precision, 0f)) return number;
            float result = precision * Mathf.Round(number / precision);
            return result;
        }

        void EvaluateAndTransitionToSelectionState()
        {
            if (IsActive())
            {
                UpdateSelectionState();
                if (m_VisualUpdateRequired)
                {
                    TransitionToSelectionState(false);
                    m_VisualUpdateRequired = false;
                }
            }
        }
        void UpdateSelectionState()
        {
            if (!IsInteractable())
            {
                minHandleSelectionSate = SelectionState.Disabled;
                maxHandleSelectionSate = SelectionState.Disabled;
                fillSelectionSate = SelectionState.Disabled;
            }
            else if (m_IsClicked)
            {
                switch (m_SelectedHandle)
                {
                    case Handle.None:
                        minHandleSelectionSate = SelectionState.Normal;
                        maxHandleSelectionSate = SelectionState.Normal;
                        fillSelectionSate = SelectionState.Normal;
                        break;
                    case Handle.Min:
                        minHandleSelectionSate = m_IsHovered ? SelectionState.Pressed : SelectionState.Highlighted;
                        maxHandleSelectionSate = SelectionState.Normal;
                        fillSelectionSate = SelectionState.Normal;
                        break;
                    case Handle.Fill:
                        minHandleSelectionSate = SelectionState.Normal;
                        maxHandleSelectionSate = SelectionState.Normal;
                        fillSelectionSate = m_IsHovered ? SelectionState.Pressed : SelectionState.Highlighted;
                        break;
                    case Handle.Max:
                        minHandleSelectionSate = SelectionState.Normal;
                        maxHandleSelectionSate = m_IsHovered ? SelectionState.Pressed : SelectionState.Highlighted;
                        fillSelectionSate = SelectionState.Normal;
                        break;
                    default:
                        minHandleSelectionSate = SelectionState.Normal;
                        maxHandleSelectionSate = SelectionState.Normal;
                        fillSelectionSate = SelectionState.Normal;
                        break;
                }
            }
            else if (m_IsHovered)
            {
                Vector2 mousePosition = Input.mousePosition;
                if (RectTransformUtility.RectangleContainsScreenPoint(m_MinHandleRect, mousePosition))
                {
                    minHandleSelectionSate = SelectionState.Highlighted;
                    maxHandleSelectionSate = SelectionState.Normal;
                    fillSelectionSate = SelectionState.Normal;
                }
                else if (RectTransformUtility.RectangleContainsScreenPoint(m_MaxHandleRect, mousePosition))
                {
                    minHandleSelectionSate = SelectionState.Normal;
                    maxHandleSelectionSate = SelectionState.Highlighted;
                    fillSelectionSate = SelectionState.Normal;
                }
                else if (RectTransformUtility.RectangleContainsScreenPoint(m_FillRect, mousePosition))
                {
                    minHandleSelectionSate = SelectionState.Normal;
                    maxHandleSelectionSate = SelectionState.Normal;
                    fillSelectionSate = SelectionState.Highlighted;
                }
                else
                {
                    minHandleSelectionSate = SelectionState.Highlighted;
                    maxHandleSelectionSate = SelectionState.Highlighted;
                    fillSelectionSate = SelectionState.Highlighted;
                }
            }
            else if (m_IsSelected)
            {
                minHandleSelectionSate = SelectionState.Highlighted;
                maxHandleSelectionSate = SelectionState.Highlighted;
                fillSelectionSate = SelectionState.Highlighted;
            }
            else
            {
                minHandleSelectionSate = SelectionState.Normal;
                maxHandleSelectionSate = SelectionState.Normal;
                fillSelectionSate = SelectionState.Normal;
            }
        }
        void TransitionToSelectionState(bool instant)
        {
            DoStateTransition(Handle.Min, m_MinHandleSelectionState, instant);
            DoStateTransition(Handle.Fill, m_FillSelectionState, instant);
            DoStateTransition(Handle.Max, m_MaxHandleSelectionState, instant);
        }
        void DoStateTransition(Handle handle, SelectionState state, bool instant)
        {
            Graphic targetGraphic;
            ColorBlock colorBlock;
            SpriteState spriteState;
            AnimationTriggers animationTriggers;
            Animator animator;
            Transition transition;
            switch (handle)
            {
                case Handle.None:
                    return;
                case Handle.Min:
                    targetGraphic = m_MinHandleTargetGraphic;
                    colorBlock = m_HandleColors;
                    spriteState = m_HandleSpriteState;
                    animationTriggers = m_HandleAnimationTriggers;
                    animator = minHandleAnimator;
                    transition = m_HandleTransition;
                    break;
                case Handle.Fill:
                    targetGraphic = m_FillTargetGraphic;
                    colorBlock = m_FillColors;
                    spriteState = m_FillSpriteState;
                    animationTriggers = m_FillAnimationTriggers;
                    animator = FillAnimator;
                    transition = m_FillTransition;
                    break;
                case Handle.Max:
                    targetGraphic = m_MaxHandleTargetGraphic;
                    colorBlock = m_HandleColors;
                    spriteState = m_HandleSpriteState;
                    animationTriggers = m_HandleAnimationTriggers;
                    animator = maxHandleAnimator;
                    transition = m_HandleTransition;
                    break;
                default:
                    return;
            }
            if(gameObject.activeInHierarchy)
            {
                switch (transition)
                {
                    case Transition.None:
                        return;
                    case Transition.ColorTint:
                        StartColorTween(targetGraphic, colorBlock, state, instant);
                        break;
                    case Transition.SpriteSwap:
                        DoSpriteSwap(targetGraphic, spriteState, state);
                        break;
                    case Transition.Animation:
                        TriggerAnimation(animator, animationTriggers, state);
                        break;
                    default:
                        return;
                }
            }
        }
        void StartColorTween(Graphic graphic, ColorBlock colors, SelectionState state, bool instant)
        {
            if (graphic != null)
            {
                Color targetColor;
                switch (state)
                {
                    case SelectionState.Normal:
                        targetColor = colors.normalColor;
                        break;
                    case SelectionState.Highlighted:
                        targetColor = colors.highlightedColor;
                        break;
                    case SelectionState.Pressed:
                        targetColor = colors.pressedColor;
                        break;
                    case SelectionState.Disabled:
                        targetColor = colors.disabledColor;
                        break;
                    default:
                        targetColor = colors.normalColor;
                        break;
                }
                graphic.CrossFadeColor(targetColor * colors.colorMultiplier, instant ? 0f : colors.fadeDuration, true, true);
            }

        }
        void DoSpriteSwap(Graphic graphic, SpriteState spriteState, SelectionState state)
        {
            if(graphic != null && graphic is Image)
            {
                Image image = graphic as Image;
                Sprite targetSprite;
                switch (state)
                {
                    case SelectionState.Normal:
                        targetSprite = null;
                        break;
                    case SelectionState.Highlighted:
                        targetSprite = spriteState.highlightedSprite;
                        break;
                    case SelectionState.Pressed:
                        targetSprite = spriteState.pressedSprite;
                        break;
                    case SelectionState.Disabled:
                        targetSprite = spriteState.disabledSprite;
                        break;
                    default:
                        targetSprite = null;
                        break;
                }
                image.overrideSprite = targetSprite;
            }
        }
        void TriggerAnimation(Animator animator, AnimationTriggers animationTriggers, SelectionState state)
        {
            string triggerName;
            switch (state)
            {
                case SelectionState.Normal:
                    triggerName = animationTriggers.normalTrigger;
                    break;
                case SelectionState.Highlighted:
                    triggerName = animationTriggers.highlightedTrigger;
                    break;
                case SelectionState.Pressed:
                    triggerName = animationTriggers.pressedTrigger;
                    break;
                case SelectionState.Disabled:
                    triggerName = animationTriggers.disabledTrigger;
                    break;
                default:
                    triggerName = animationTriggers.normalTrigger;
                    break;
            }

            if (animator != null && animator.enabled && animator.runtimeAnimatorController != null && !string.IsNullOrEmpty(triggerName))
            {
                animator.ResetTrigger(animationTriggers.normalTrigger);
                animator.ResetTrigger(animationTriggers.pressedTrigger);
                animator.ResetTrigger(animationTriggers.highlightedTrigger);
                animator.ResetTrigger(animationTriggers.disabledTrigger);
                animator.SetTrigger(triggerName);
            }
        }
        #endregion
    }
}