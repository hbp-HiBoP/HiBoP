using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using data = HBP.Data.TrialMatrix;
using System.Collections.ObjectModel;
using Tools.Unity;
using Tools.CSharp;
using UnityEngine.Profiling;

namespace HBP.UI.TrialMatrix
{
    [RequireComponent(typeof(RawImage))]
    public class SubBloc : MonoBehaviour
    {
        #region Properties
        data.Bloc m_Data;
        public data.Bloc Data
        {
            get
            {
                return m_Data;
            }
            set
            {
                m_Data = Data;
                SetTexture();
            }
        }

        Texture2D m_ColorMap;
        public Texture2D ColorMap
        {
            get
            {
                return m_ColorMap;
            }
            set
            {
                m_ColorMap = value;
                SetTexture();
            }
        }

        Vector2 m_Limits;
        public Vector2 Limits
        {
            get { return m_Limits; }
            set
            {
                m_Limits = value;
                SetTexture();
            }
        }

        List<int> m_selectedLines = new List<int>();
        public ReadOnlyCollection<int> SelectedLines
        {
            get { return new ReadOnlyCollection<int>(m_selectedLines); }
        }

        [SerializeField] GameObject SelectionMask;
        int m_BeginDragLine;
        bool m_Initialized;
        bool m_Dragging;
        RectTransform m_RectTransform;
        LayoutElement m_LayoutElement;
        #endregion

        #region Public Methods
        public void Set(data.Bloc data,Texture2D colorMap,Vector2 limits)
        {
            m_Data = data;
            m_ColorMap = colorMap;
            m_Limits = limits;

            // TODO
            //name = data.ProtocolBloc.Name + " | " + "Bloc n°" + data.ProtocolBloc.Position.Column;

            SetSize();
            SetTexture();
            GenerateMainEventIndicator(data);
            GenerateSecondaryIndicator(data);
        }
        public void SelectLines(int[] lines,bool additive)
        {
                ClearMask();
                if (additive)
                {
                    foreach (int i in lines)
                    {
                        if (!m_selectedLines.Contains(i))
                        {
                            m_selectedLines.Add(i);
                        }
                    }
                }
                else
                {
                    m_selectedLines = new List<int>(lines);
                }
                List<int> l_notSelectedLines = new List<int>();
                for (int i = 0; i < Data.SubBlocs.Length; i++)
                {
                    if (!m_selectedLines.Contains(i)) l_notSelectedLines.Add(i);
                }

                List<List<int>> GroupOfLines = new List<List<int>>();
                List<int> group = new List<int>();
                for (int i = 0; i < l_notSelectedLines.Count; i++)
                {
                    if (group.Count != 0 && l_notSelectedLines[i] != group[group.Count - 1] + 1)
                    {
                        GroupOfLines.Add(new List<int>(group));
                        group = new List<int>();
                    }
                    group.Add(l_notSelectedLines[i]);
                    if (i == l_notSelectedLines.Count - 1)
                    {
                        GroupOfLines.Add(new List<int>(group));
                    }
                }

                foreach (List<int> g in GroupOfLines)
                {
                    AddCache(g.ToArray());
                }
        }
        public void OnPointerDown()
        {
            m_BeginDragLine = GetTrialAtPosition(Input.mousePosition);
        }
        public void OnPointerClick(BaseEventData p)
        {
            PointerEventData pointer = (PointerEventData) p;
            if (pointer.button == PointerEventData.InputButton.Left)
            {
                if (!m_Dragging)
                {
                    int[] linesClicked = new int[1] { m_BeginDragLine };
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        SendMessageSelectLines(linesClicked, true);
                    }
                    else
                    {
                        SendMessageSelectLines(linesClicked, false);
                    }
                }
            }
            else if (pointer.button == PointerEventData.InputButton.Right)
            {
                int max = Data.SubBlocs.Length;
                int[] array = new int[max];
                for (int i = 0; i < max; i++)
                {
                    array[i] = i;
                }
                SendMessageSelectLines(array, false);
            }
        }
        public void OnBeginDrag()
        {
            m_Dragging = true;
        }
        public void OnEndDrag()
        {
            int l_endDragLine = Mathf.Clamp(GetTrialAtPosition(Input.mousePosition),0,m_Data.SubBlocs.Length-1);
            int l_beginDragLine = Mathf.Clamp(m_BeginDragLine, 0, m_Data.SubBlocs.Length-1); ;
            List<int> linesSelected = new List<int>();
            if(l_beginDragLine > l_endDragLine)
            {
                for (int l = l_endDragLine; l <= l_beginDragLine; l++)
                {
                    linesSelected.Add(l);
                }
            }
            else
            {
                for (int l = l_beginDragLine; l <= l_endDragLine; l++)
                {
                    linesSelected.Add(l);
                }
            }
            if(Input.GetKey(KeyCode.LeftShift))
            {
                SendMessageSelectLines(linesSelected.ToArray(), true);

            }
            else
            {
                SendMessageSelectLines(linesSelected.ToArray(), false);
            }
            m_Dragging = false;
        }
        public void OnScroll()
        {
            float l_input = Input.GetAxis("Mouse ScrollWheel") * 10;
            int delta = Mathf.RoundToInt(l_input);
            if(delta != 0)
            {
                int[] l_lines = m_selectedLines.ToArray();
                int size = l_lines.Length - 1;
                if (size < 0) size = 0;
                List<int> l_linesToSelect = new List<int>(size);
                int newLine;
                for (int i = 0; i < l_lines.Length; i++)
                {
                    newLine = (((l_lines[i] + delta) % Data.SubBlocs.Length) + Data.SubBlocs.Length) % Data.SubBlocs.Length;
                    if (newLine >= 0 && newLine < Data.SubBlocs.Length)
                    {
                        l_linesToSelect.Add(newLine);
                    }
                }
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    SendMessageSelectLines(l_linesToSelect.ToArray(), true);
                }
                else
                {
                    SendMessageSelectLines(l_linesToSelect.ToArray(), false);
                }
            }
        }
        #endregion

        #region Private Methods
        void Awake()
        {
           if(!m_Initialized) Initialize();
        }
        void Initialize()
        {
            m_RectTransform = GetComponent<RectTransform>();
            m_LayoutElement = GetComponent<LayoutElement>();
            m_Initialized = true;
        }
        void SetSize()
        {
            switch (ApplicationState.UserPreferences.Visualization.TrialMatrix.BlocFormat)
            {
                case HBP.Data.Enums.BlocFormatType.TrialHeight:
                    m_LayoutElement.preferredHeight = ApplicationState.UserPreferences.Visualization.TrialMatrix.TrialHeight * m_Data.SubBlocs.Length;
                    break;
                case HBP.Data.Enums.BlocFormatType.TrialRatio:
                    m_LayoutElement.preferredHeight = ApplicationState.UserPreferences.Visualization.TrialMatrix.TrialRatio * m_RectTransform.rect.width * m_Data.SubBlocs.Length;
                    break;
                case HBP.Data.Enums.BlocFormatType.BlocRatio:
                    m_LayoutElement.preferredHeight = ApplicationState.UserPreferences.Visualization.TrialMatrix.BlocRatio * m_RectTransform.rect.width;
                    break;
            }
        }
        void SetTexture()
        {
            //float[][] lines = ExtractDataFromLines(m_Data.SubBlocs);

            //if(ApplicationState.UserPreferences.Visualization.TrialMatrix.TrialSmoothing)
            //{
            //    lines = SmoothLines(lines, ApplicationState.UserPreferences.Visualization.TrialMatrix.NumberOfIntermediateValues);
            //}

            //Texture2D texture = GenerateTexture(lines, m_Limits, m_ColorMap);
            //texture.mipMapBias = -5;
            //texture.wrapMode = TextureWrapMode.Clamp;
            //GetComponent<RawImage>().texture = texture;
        }

        void AddCache(int[] lines)
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            GameObject displayer = Instantiate(SelectionMask as GameObject);
            RectTransform displayerRect = displayer.GetComponent<RectTransform>();
            displayerRect.SetParent(transform.GetChild(1));
            int nbLines = m_Data.SubBlocs.Length;
            float top = (float)(lines[lines.Length - 1] + 1) / nbLines;
            float bot = (float)lines[0] / nbLines;
            displayerRect.anchorMin = new Vector2(0, bot);
            displayerRect.anchorMax = new Vector2(1, top);
            displayerRect.offsetMin = new Vector2(0, 0);
            displayerRect.offsetMax = new Vector2(0, 0);
        }
        void SendMessageSelectLines(int[] lines, bool additive)
        {
            Informations.TrialMatrixZone graphGestion = GetComponentInParent<Informations.TrialMatrixZone>();
            if (graphGestion)
            {
                graphGestion.OnSelectLines(lines, this, additive);
            }
        }
        Texture2D GenerateTexture(float[][] lines,Vector2 limits,Texture2D colorMap)
        {
            // Caculate texture size.
            int height = lines.Length;
            if (height == 0) return new Texture2D(0,0);
            int width = lines[0].Length;
            Texture2D l_texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] colors = new Color[width * height];

            // Set pixels
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float value = lines[y][x];
                    if (float.IsNaN(value))
                    {
                        colors[y*width + x] = Color.black;
                    }
                    else
                    {
                        colors[y * width + x] = GetColor(value, limits, colorMap);
                    }
                }
            }

            // Set texture.
            l_texture.SetPixels(colors);
            l_texture.filterMode = FilterMode.Point;
            l_texture.anisoLevel = 0;
            l_texture.Apply();
            return l_texture;
        }
        float[][] SmoothLines(float[][] lines, int pass)
        {
            UnityEngine.Profiling.Profiler.BeginSample("smoothing lines");
            float[][] result = new float[lines.Length][];
            for (int l = 0; l < result.Length; l++)
            {
                result[l] = lines[l].LinearSmooth(pass);
            }
            return result;
        }
        float[][] ExtractDataFromLines(data.SubTrial[] lines)
        {
            float[][] result = new float[lines.Length][];
            //for (int l = 0; l < lines.Length; l++)
            //{
            //    result[l] = lines[l].NormalizedValues;
            //}
            return result;
        }
        Color GetColor(float value,Vector2 limits,Texture2D colorMap)
        {
            Profiler.BeginSample("GetColor");
            float ratio = (value - limits.x) / (limits.y - limits.x);
            ratio = Mathf.Clamp01(ratio);
            int y = Mathf.RoundToInt(ratio * (colorMap.height - 1));
            Profiler.EndSample();
            return colorMap.GetPixel(0,y);
        }
        void GenerateMainEventIndicator(data.Bloc bloc)
        {
            // TODO
            //GameObject mainEvent = new GameObject();
            //mainEvent.name = "Main event";
            //Image image = mainEvent.AddComponent<Image>();
            //RectTransform rect = mainEvent.GetComponent<RectTransform>();
            //rect.SetParent(transform.GetChild(0));
            //float X = (float)bloc.Trials[0].Bloc.PositionByEvent[bloc.ProtocolBloc.MainEvent] / bloc.Trials[0].NormalizedValues.Length;
            //float Xstep = 0.5f / bloc.Trials[0].NormalizedValues.Length;
            //rect.pivot = new Vector2(0, 0);
            //rect.anchorMin = new Vector2(X, 0f);
            //rect.anchorMax = new Vector2(X+ Xstep, 1f);
            //rect.offsetMin = new Vector2(0, 0);
            //rect.offsetMax = new Vector2(0, 0);
            //if (rect.sizeDelta.x < 1) rect.sizeDelta = new Vector2(1.0f, rect.sizeDelta.y);
            //image.color = Color.black;
        }
        void GenerateSecondaryIndicator(data.Bloc bloc)
        {
            // TODO
            //for (int l = 0; l < bloc.Trials.Length; l++)
            //{
            //    foreach (var secondaryEvent in bloc.ProtocolBloc.SecondaryEvents)
            //    {
            //        int position = bloc.Trials[l].Bloc.PositionByEvent[secondaryEvent];
            //        if (position > -1)
            //        {
            //            GameObject mainEvent = new GameObject();
            //            mainEvent.name = secondaryEvent.Name + " - " + l;
            //            Image image = mainEvent.AddComponent<Image>();
            //            RectTransform rect = mainEvent.GetComponent<RectTransform>();
            //            rect.SetParent(transform.GetChild(0));
            //            float X = (float)position / bloc.Trials[l].NormalizedValues.Length;
            //            float Xstep = 0.5f / bloc.Trials[l].NormalizedValues.Length;
            //            float Y = (float)l / bloc.Trials.Length;
            //            float Ystep = 1.0f / bloc.Trials.Length;
            //            rect.pivot = new Vector2(0, 0);
            //            rect.anchorMin = new Vector2(X, Y);
            //            rect.anchorMax = new Vector2(X + Xstep, Y + Ystep);
            //            rect.offsetMin = new Vector2(0, 0);
            //            rect.offsetMax = new Vector2(0, 0);
            //            image.color = Color.black;
            //        }
            //    }
            //}
        }
        int GetTrialAtPosition(Vector3 position)
        {
            Vector2 ratio = m_RectTransform.GetRatioPosition(position);
            return Mathf.FloorToInt(ratio.y * Data.SubBlocs.Length);
        }
        void ClearMask()
        {
            foreach(Transform child in transform.GetChild(1)) Destroy(child.gameObject);
        }
        void OnRectTransformDimensionsChange()
        {
            if (m_RectTransform.hasChanged) SetSize();
        }
        #endregion
    }
}