using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.UI;
using dg = HBP.Data.TrialMatrix.Grid;
using data = HBP.Data.TrialMatrix;
using UnityEngine.Events;
using System.Linq;
using UnityEngine.UI.Extensions;
using System;
using UnityEngine.EventSystems;
using Tools.Unity;

namespace HBP.UI.TrialMatrix.Grid
{
    public class ChannelBloc : MonoBehaviour
    {
        #region Properties
        [SerializeField] string m_Title;
        public string Title
        {
            get
            {
                return m_Title;
            }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_Title, value))
                {
                    SetTitle();
                }
            }
        }

        [SerializeField, ReadOnly] bool m_Hovered;
        public bool Hovered
        {
            get
            {
                return m_Hovered;
            }
            private set
            {
                if (SetPropertyUtility.SetStruct(ref m_Hovered, value))
                {
                    SetHovered();
                }
            }
        }

        Color[] m_Colors;
        public Color[] Colors
        {
            get
            {
                return m_Colors;
            }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_Colors, value))
                {
                    SetColors();
                }
            }
        }

        public dg.ChannelBloc Data { private set; get; }

        [SerializeField] bool[] m_TrialIsSelected;
        public bool[] TrialIsSelected
        {
            get
            {
                return m_TrialIsSelected;
            }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_TrialIsSelected, value))
                {
                    SetSelections();
                }
            }
        }
        [SerializeField] UnityEvent m_OnChangeTrialSelected;
        public UnityEvent OnChangeTrialSelected
        {
            get
            {
                return m_OnChangeTrialSelected;
            }
        }

        public SubBloc SubBlocHovered
        {
            get
            {
                return SubBlocs.FirstOrDefault(c => c.Hovered);
            }
        }
        List<SubBloc> m_SubBlocs = new List<SubBloc>();
        public ReadOnlyCollection<SubBloc> SubBlocs { get { return new ReadOnlyCollection<SubBloc>(m_SubBlocs); } }
        public SubBloc MainSubBloc
        {
            get
            {
                return m_SubBlocs.First(s => s.Data.SubBlocProtocol == Data.Bloc.MainSubBloc);
            }
        }

        [SerializeField] GameObject m_SubBlocPrefab;
        [SerializeField] RectTransform m_SubBlocContainer;

        [SerializeField] GameObject m_SelectionPrefab;
        [SerializeField] RectTransform m_SelectionContainer;
        List<GameObject> m_SelectionMasks = new List<GameObject>();

        RectTransform m_RectTransform;
        LayoutElement m_LayoutElement;
        int m_OnPointerDownTrial;
        int m_LastPointerDownTrial;
        int m_AnchorTrial;
        int m_LastDragTrial;
        bool[] m_OnBeginDragStates;

        [SerializeField] StringEvent m_OnChangeTitle;
        public StringEvent OnChangeTitle
        {
            get
            {
                return m_OnChangeTitle;
            }
        }

        [SerializeField] UnityEvent m_OnChangeHovered;
        public UnityEvent OnChangeHovered
        {
            get
            {
                return m_OnChangeHovered;
            }
        }
        #endregion

        #region Public Methods
        public void Set(dg.ChannelBloc data, Color[] colors, Vector2 limits)
        {
            m_SelectionMasks = new List<GameObject>();
            Title = data.Channel.Channel + " (" + data.Channel.Patient.Name + ")";
            Colors = colors;
            Data = data;
            Clear();
            foreach (var subBloc in data.SubBlocs) AddSubBloc(subBloc, colors, limits);
            SetSize();
            m_TrialIsSelected = Enumerable.Repeat(true, SubBlocs.First(s => s.Data.SubBlocProtocol == Data.Bloc.MainSubBloc).Data.SubTrials.Length).ToArray();
        }

        public void OnPointerDown(BaseEventData baseEventData)
        {
            PointerEventData pointerEventData = (PointerEventData) baseEventData;
            m_OnPointerDownTrial = GetTrialAtPosition(pointerEventData.pressPosition);
            m_LastDragTrial = m_OnPointerDownTrial;
            if (!pointerEventData.dragging)
            {
                if (pointerEventData.button == PointerEventData.InputButton.Left)
                {
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        int start, end;
                        if (m_AnchorTrial < m_OnPointerDownTrial)
                        {
                            start = m_AnchorTrial;
                            end = m_OnPointerDownTrial;
                        }
                        else
                        {
                            start = m_OnPointerDownTrial;
                            end = m_AnchorTrial;
                        }
                        // LeftClick and Shift and Ctrl
                        if (Input.GetKey(KeyCode.LeftControl))
                        {
                            SelectTrials(start, end, true, true);
                        }
                        // LeftClick and Shift
                        else
                        {
                            SelectTrials(start, end, true, false);
                        }
                    }
                    else
                    {
                        m_AnchorTrial = m_OnPointerDownTrial;
                        // LeftClick and Ctrl
                        if (Input.GetKey(KeyCode.LeftControl))
                        {
                            SelectTrials(m_OnPointerDownTrial, m_OnPointerDownTrial, !m_TrialIsSelected[m_OnPointerDownTrial], true);
                        }
                        // LeftClick
                        else
                        {
                            SelectTrials(m_OnPointerDownTrial, m_OnPointerDownTrial, true, false);
                        }
                    }
                }
                // RightClick
                else if (pointerEventData.button == PointerEventData.InputButton.Right)
                {
                    TrialIsSelected = Enumerable.Repeat(true, m_TrialIsSelected.Length).ToArray();
                }
                m_LastPointerDownTrial = m_OnPointerDownTrial;
            }
        }
        public void OnBeginDrag(BaseEventData baseEventData)
        {
            m_OnBeginDragStates = m_TrialIsSelected;
        }
        public void OnDrag(BaseEventData baseEventData)
        {
            PointerEventData pointerEventData = (PointerEventData) baseEventData;          
            int dragTrial = GetTrialAtPosition(pointerEventData.position);

            if (m_LastDragTrial != dragTrial)
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    // Shift and Ctrl
                    if (Input.GetKey(KeyCode.LeftControl))
                    {
                        bool[] selectedTrials = m_OnBeginDragStates.ToArray();
                        int start, end;
                        if (m_OnPointerDownTrial < dragTrial)
                        {
                            start = m_OnPointerDownTrial;
                            end = dragTrial;
                        }
                        else
                        {
                            start = dragTrial;
                            end = m_OnPointerDownTrial;
                        }
                        for (int i = start; i <= end; i++)
                        {
                            selectedTrials[i] = !selectedTrials[i];
                        }
                        TrialIsSelected = selectedTrials;
                    }
                    // Shift
                    else
                    {
                        int start, end;
                        if (m_LastPointerDownTrial < dragTrial)
                        {
                            start = m_LastPointerDownTrial;
                            end = dragTrial;
                        }
                        else
                        {
                            start = dragTrial;
                            end = m_LastPointerDownTrial;
                        }
                        SelectTrials(start, end, true, true);
                    }
                }
                else
                {
                    // Ctrl
                    if (Input.GetKey(KeyCode.LeftControl))
                    {
                        bool[] selectedTrials = m_OnBeginDragStates.ToArray();
                        int start, end;
                        if (m_OnPointerDownTrial < dragTrial)
                        {
                            start = m_OnPointerDownTrial + 1;
                            end = dragTrial;
                        }
                        else
                        {
                            start = dragTrial;
                            end = m_OnPointerDownTrial - 1;
                        }
                        for (int i = start; i <= end; i++)
                        {
                            selectedTrials[i] = !selectedTrials[i];
                        }
                        TrialIsSelected = selectedTrials;
                    }
                    // Nothing
                    else
                    {
                        int start, end;
                        if (m_LastPointerDownTrial < dragTrial)
                        {
                            start = m_LastPointerDownTrial;
                            end = dragTrial;
                        }
                        else
                        {
                            start = dragTrial;
                            end = m_LastPointerDownTrial;
                        }
                        SelectTrials(start, end, true, false);
                    }
                }
                m_LastDragTrial = dragTrial;
            }
        }
        public void OnScroll(BaseEventData baseEventData)
        {
            int delta = Mathf.RoundToInt(Input.GetAxis("Mouse ScrollWheel") * 10);
            if (delta < 0)
            {
                for (int i = 0; i < -delta; i++)
                {
                    if (m_TrialIsSelected[m_TrialIsSelected.Length - 1 - i])
                    {
                        delta = -i;
                    }
                }
            }
            else if (delta > 0)
            {
                for (int i = 0; i < delta; i++)
                {
                    if (m_TrialIsSelected[i])
                    {
                        delta = i;
                    }
                }
            }
            if (delta != 0)
            {
                bool[] trialIsSelected = new bool[m_TrialIsSelected.Length];
                for (int i = 0; i < trialIsSelected.Length; i++)
                {
                    int index = i + delta;
                    if (index >= 0 && index < trialIsSelected.Length)
                    {
                        trialIsSelected[i] = m_TrialIsSelected[index];
                    }
                }
                TrialIsSelected = trialIsSelected;
            }
        }
        #endregion

        #region Private Methods    
        void OnRectTransformDimensionsChange()
        {
            SetSize();
        }
        void Awake()
        {
            m_LayoutElement = GetComponent<LayoutElement>();
            m_RectTransform = GetComponent<RectTransform>();
        }
        void OnValidate()
        {
            SetTitle();
            SetColors();
            SetSelections();
        }
        void SetSize()
        {
            if (Data.IsFound)
            {
                switch (ApplicationState.UserPreferences.Visualization.TrialMatrix.SubBlocFormat)
                {
                    case HBP.Data.Enums.BlocFormatType.TrialHeight:
                        m_LayoutElement.preferredHeight = ApplicationState.UserPreferences.Visualization.TrialMatrix.TrialHeight * Data.SubBlocs.First(s => s.SubBlocProtocol == Data.Bloc.MainSubBloc).SubTrials.Length;
                        break;
                    case HBP.Data.Enums.BlocFormatType.TrialRatio:
                        m_LayoutElement.preferredHeight = ApplicationState.UserPreferences.Visualization.TrialMatrix.TrialRatio * m_RectTransform.rect.width * Data.SubBlocs.First(s => s.SubBlocProtocol == Data.Bloc.MainSubBloc).SubTrials.Length;
                        break;
                    case HBP.Data.Enums.BlocFormatType.BlocRatio:
                        m_LayoutElement.preferredHeight = ApplicationState.UserPreferences.Visualization.TrialMatrix.BlocRatio * m_RectTransform.rect.width;
                        break;
                }
            }
            else
            {
                m_LayoutElement.flexibleHeight = 1;
            }
        }
        void AddSubBloc(data.SubBloc data, Color[] colors, Vector2 limits)
        {
            SubBloc subBloc = Instantiate(m_SubBlocPrefab, m_SubBlocContainer).GetComponent<SubBloc>();
            subBloc.Set(data, colors, limits);
            subBloc.OnChangeHovered.AddListener(() => Hovered = SubBlocs.Any(s => s.Hovered));

            EventTrigger eventTrigger = subBloc.GetComponent<EventTrigger>();

            EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
            pointerDownEntry.eventID = EventTriggerType.PointerDown;
            pointerDownEntry.callback.AddListener(OnPointerDown);
            eventTrigger.triggers.Add(pointerDownEntry);

            EventTrigger.Entry beginDragEntry = new EventTrigger.Entry();
            beginDragEntry.eventID = EventTriggerType.BeginDrag;
            beginDragEntry.callback.AddListener(OnBeginDrag);
            eventTrigger.triggers.Add(beginDragEntry);

            EventTrigger.Entry dragEntry = new EventTrigger.Entry();
            dragEntry.eventID = EventTriggerType.Drag;
            dragEntry.callback.AddListener(OnDrag);
            eventTrigger.triggers.Add(dragEntry);

            EventTrigger.Entry endDragEntry = new EventTrigger.Entry();
            endDragEntry.eventID = EventTriggerType.EndDrag;
            endDragEntry.callback.AddListener(OnDrag);
            eventTrigger.triggers.Add(endDragEntry);

            EventTrigger.Entry scrollEntry = new EventTrigger.Entry();
            scrollEntry.eventID = EventTriggerType.Scroll;
            scrollEntry.callback.AddListener(OnScroll);
            eventTrigger.triggers.Add(scrollEntry);

            m_SubBlocs.Add(subBloc);
        }
        void Clear()
        {
            foreach (var subBloc in m_SubBlocs)
            {
                Destroy(subBloc.gameObject);
            }
            m_SubBlocs = new List<SubBloc>();
        }
        void AddMask(int start, int end)
        {
            RectTransform rectTransform = Instantiate(m_SelectionPrefab, m_SelectionContainer).GetComponent<RectTransform>();
            int numberOfTrials = MainSubBloc.Data.SubTrials.Length;
            float yMax = 1 - (float)start / numberOfTrials;
            float yMin = 1 - (float)end / numberOfTrials;
            rectTransform.anchorMin = new Vector2(0, yMin);
            rectTransform.anchorMax = new Vector2(1, yMax);
            rectTransform.offsetMin = new Vector2(0, 0);
            rectTransform.offsetMax = new Vector2(0, 0);
            m_SelectionMasks.Add(rectTransform.gameObject);
        }
        void ClearMasks()
        {
            foreach (var selectionMask in m_SelectionMasks) Destroy(selectionMask);
            m_SelectionMasks.Clear();
        }
        int GetTrialAtPosition(Vector3 position)
        {
            Vector2 ratio = m_RectTransform.GetRatioPosition(position);
            return Mathf.FloorToInt((1 - ratio.y) * MainSubBloc.Data.SubTrials.Length);
        }
        void SelectTrials(int startIndex, int endIndex, bool select, bool additive = false)
        {
            if (m_TrialIsSelected.Length == 0) return;

            startIndex = Mathf.Clamp(startIndex, 0, m_TrialIsSelected.Length - 1);
            endIndex = Mathf.Clamp(endIndex, 0, m_TrialIsSelected.Length - 1);
            bool[] selection = additive ? m_TrialIsSelected.ToArray() : Enumerable.Repeat(false, m_TrialIsSelected.Length).ToArray();

            for (int i = startIndex; i <= endIndex; i++)
            {
                selection[i] = select;
            }
            TrialIsSelected = selection;
        }
        void InverseSelectionTrials(int startIndex, int endIndex, bool additive = false)
        {
            bool[] selection = additive ? m_TrialIsSelected.ToArray() : Enumerable.Repeat(false, m_TrialIsSelected.Length).ToArray();
            for (int i = startIndex; i <= endIndex; i++)
            {
                selection[i] = !selection[i];
            }
            TrialIsSelected = selection;
        }
        #endregion

        #region Setters
        void SetTitle()
        {
            OnChangeTitle.Invoke(m_Title);
        }
        void SetColors()
        {
            foreach (var subBloc in m_SubBlocs)
            {
                subBloc.Colors = m_Colors;
            }
        }
        void SetSelections()
        {
            List<Tuple<int, int>> masks = new List<Tuple<int, int>>();
            bool inside = false;
            int startIndex = -1;
            for (int i = 0; i < m_TrialIsSelected.Length; i++)
            {
                if (m_TrialIsSelected[i])
                {
                    if (inside)
                    {
                        masks.Add(new Tuple<int, int>(startIndex, i));
                        inside = false;
                    }
                }
                else
                {
                    if (!inside)
                    {
                        startIndex = i;
                        inside = true;
                    }
                    if (i == m_TrialIsSelected.Length - 1)
                    {
                        masks.Add(new Tuple<int, int>(startIndex, i + 1));
                        inside = false;
                    }
                }
            }

            ClearMasks();
            foreach (var mask in masks)
            {
                AddMask(mask.Item1, mask.Item2);
            }
            m_OnChangeTrialSelected.Invoke();
        }
        void SetHovered()
        {
            m_OnChangeHovered.Invoke();
        }
        #endregion
    }
}