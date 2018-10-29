using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;
using d = HBP.Data.TrialMatrix;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine.EventSystems;

namespace HBP.UI.TrialMatrix
{
    public class Bloc : MonoBehaviour
    {
        #region Properties
        public GameObject SubBlocPrefab;
        public RectTransform SubBlocsRectTransform;

        string m_title;
        public string Title
        {
            get { return m_title; }
            set
            {
                m_title = value;
                OnChangeTitle.Invoke(value);
            }
        }
        public StringEvent OnChangeTitle;

        List<int> m_SelectedTrials = new List<int>();
        public int[] SelectedTrials
        {
            get
            {
                return m_SelectedTrials.ToArray();
            }
            set
            {
                m_SelectedTrials = value.ToList();
            }
        }
        //int m_BeginDragTrial;
        //bool m_Dragging;
        //[SerializeField] GameObject m_SelectionMask;

        List<SubBloc> m_SubBlocs = new List<SubBloc>();
        public ReadOnlyCollection<SubBloc> SubBlocs { get { return new ReadOnlyCollection<SubBloc>(m_SubBlocs); } }

        #endregion

        #region Public Methods
        public void Set(d.Bloc bloc,Texture2D colorMap,Vector2 limits)
        {
            Title = bloc.ProtocolBloc.Name;
            foreach (d.SubBloc subBloc in bloc.SubBlocs)
            {
                AddSubBloc(subBloc, colorMap, limits);
            }

            //if (bloc.Length < max)
            //{
            //    int blocEmptyToAdd = max - bloc.Length;
            //    for (int i = 0; i < blocEmptyToAdd; i++)
            //    {
            //        AddFiller();
            //    }
            //}
        }
        public void SelectTrials(int[] trials,bool additive)
        {
            //int NumberOfSubTrials = m_Data.SubTrials.Length;
            //int[] unselectedSubTrials = new int[NumberOfSubTrials - subTrials.Length];
            //for (int t = 0, i = 0; t < m_Data.SubTrials.Length; t++)
            //{
            //    if (!subTrials.Contains(t))
            //    {
            //        unselectedSubTrials[i] = t;
            //        i++;
            //    }
            //}

            //List<List<int>> subTrialsGroups = new List<List<int>>();
            //List<int> subTrialGroup = new List<int>();
            //for (int i = 0; i < unselectedSubTrials.Length; i++)
            //{
            //    int subTrial = unselectedSubTrials[i];
            //    if (subTrialGroup.Count != 0 && unselectedSubTrials[i] != subTrialGroup[subTrialGroup.Count - 1] + 1)
            //    {
            //        subTrialsGroups.Add(new List<int>(subTrialGroup));
            //        subTrialGroup = new List<int>();
            //    }
            //    subTrialGroup.Add(unselectedSubTrials[i]);
            //    if (i == unselectedSubTrials.Length - 1)
            //    {
            //        subTrialsGroups.Add(new List<int>(subTrialGroup));
            //    }
            //}

            //ClearMask();
            //foreach (var g in subTrialsGroups)
            //{
            //    AddMask(g.ToArray());
            //}
        }
        //public void OnPointerDown()
        //{
        //    m_BeginDragTrial = GetTrialAtPosition(Input.mousePosition);
        //}
        //public void OnPointerClick(BaseEventData p)
        //{
        //    PointerEventData pointer = (PointerEventData)p;
        //    if (pointer.button == PointerEventData.InputButton.Left)
        //    {
        //        if (!m_Dragging)
        //        {
        //            int[] linesClicked = new int[1] { m_BeginDragTrial };
        //            if (Input.GetKey(KeyCode.LeftShift))
        //            {
        //                SendMessageSelectLines(linesClicked, true);
        //            }
        //            else
        //            {
        //                SendMessageSelectLines(linesClicked, false);
        //            }
        //        }
        //    }
        //    else if (pointer.button == PointerEventData.InputButton.Right)
        //    {
        //        int max = Data.SubBlocs.Length;
        //        int[] array = new int[max];
        //        for (int i = 0; i < max; i++)
        //        {
        //            array[i] = i;
        //        }
        //        SendMessageSelectLines(array, false);
        //    }
        //}
        //public void OnBeginDrag()
        //{
        //    m_Dragging = true;
        //}
        //public void OnEndDrag()
        //{
        //    int l_endDragLine = Mathf.Clamp(GetTrialAtPosition(Input.mousePosition), 0, m_Data.SubBlocs.Length - 1);
        //    int l_beginDragLine = Mathf.Clamp(m_BeginDragTrial, 0, m_Data.SubBlocs.Length - 1); ;
        //    List<int> linesSelected = new List<int>();
        //    if (l_beginDragLine > l_endDragLine)
        //    {
        //        for (int l = l_endDragLine; l <= l_beginDragLine; l++)
        //        {
        //            linesSelected.Add(l);
        //        }
        //    }
        //    else
        //    {
        //        for (int l = l_beginDragLine; l <= l_endDragLine; l++)
        //        {
        //            linesSelected.Add(l);
        //        }
        //    }
        //    if (Input.GetKey(KeyCode.LeftShift))
        //    {
        //        SendMessageSelectLines(linesSelected.ToArray(), true);

        //    }
        //    else
        //    {
        //        SendMessageSelectLines(linesSelected.ToArray(), false);
        //    }
        //    m_Dragging = false;
        //}
        //public void OnScroll()
        //{
        //    float l_input = Input.GetAxis("Mouse ScrollWheel") * 10;
        //    int delta = Mathf.RoundToInt(l_input);
        //    if (delta != 0)
        //    {
        //        int[] l_lines = m_selectedLines.ToArray();
        //        int size = l_lines.Length - 1;
        //        if (size < 0) size = 0;
        //        List<int> l_linesToSelect = new List<int>(size);
        //        int newLine;
        //        for (int i = 0; i < l_lines.Length; i++)
        //        {
        //            newLine = (((l_lines[i] + delta) % Data.SubBlocs.Length) + Data.SubBlocs.Length) % Data.SubBlocs.Length;
        //            if (newLine >= 0 && newLine < Data.SubBlocs.Length)
        //            {
        //                l_linesToSelect.Add(newLine);
        //            }
        //        }
        //        if (Input.GetKey(KeyCode.LeftShift))
        //        {
        //            SendMessageSelectLines(l_linesToSelect.ToArray(), true);
        //        }
        //        else
        //        {
        //            SendMessageSelectLines(l_linesToSelect.ToArray(), false);
        //        }
        //    }
        //}
        #endregion

        #region Private Methods     
        void SetSize()
        {
            //switch (ApplicationState.UserPreferences.Visualization.TrialMatrix.SubBlocFormat)
            //{
            //    case HBP.Data.Enums.BlocFormatType.TrialHeight:
            //        m_LayoutElement.preferredHeight = ApplicationState.UserPreferences.Visualization.TrialMatrix.TrialHeight * m_Data.SubTrials.Length;
            //        break;
            //    case HBP.Data.Enums.BlocFormatType.TrialRatio:
            //        m_LayoutElement.preferredHeight = ApplicationState.UserPreferences.Visualization.TrialMatrix.TrialRatio * m_RectTransform.rect.width * m_Data.SubTrials.Length;
            //        break;
            //    case HBP.Data.Enums.BlocFormatType.BlocRatio:
            //        m_LayoutElement.preferredHeight = ApplicationState.UserPreferences.Visualization.TrialMatrix.BlocRatio * m_RectTransform.rect.width;
            //        break;
            //}
        }
        void AddSubBloc(d.SubBloc data, Texture2D colorMap, Vector2 limits)
        {
            SubBloc subBloc = (Instantiate(SubBlocPrefab, SubBlocsRectTransform) as GameObject).GetComponent<SubBloc>();
            subBloc.Set(data, colorMap, limits);
            m_SubBlocs.Add(subBloc);
        }
        //void AddFiller()
        //{
        //    GameObject filler = Instantiate(new GameObject("Filler", new System.Type[] { typeof(Image) }), SubBlocsRectTransform);
        //    filler.GetComponent<Image>().color = Color.black;
        //}
        //void AddMask(int[] lines)
        //{
        //    RectTransform rectTransform = GetComponent<RectTransform>();
        //    GameObject displayer = Instantiate(m_SelectionMask as GameObject);
        //    RectTransform displayerRect = displayer.GetComponent<RectTransform>();
        //    displayerRect.SetParent(transform.GetChild(1));
        //    int nbLines = m_Data.SubBlocs.Length;
        //    float top = (float)(lines[lines.Length - 1] + 1) / nbLines;
        //    float bot = (float)lines[0] / nbLines;
        //    displayerRect.anchorMin = new Vector2(0, bot);
        //    displayerRect.anchorMax = new Vector2(1, top);
        //    displayerRect.offsetMin = new Vector2(0, 0);
        //    displayerRect.offsetMax = new Vector2(0, 0);
        //}
        //void ClearMask()
        //{
        //    foreach (Transform child in transform.GetChild(1)) Destroy(child.gameObject);
        //}
        #endregion
    }
}
