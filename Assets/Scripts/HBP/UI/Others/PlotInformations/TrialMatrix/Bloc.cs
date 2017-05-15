using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using HBP.Data.Settings;
using System.Collections.Generic;
using d = HBP.Data.TrialMatrix;

namespace HBP.UI.TrialMatrix
{
    [RequireComponent(typeof(RawImage))]
    public class Bloc : MonoBehaviour
    {
        #region Properties

        [SerializeField]
        GameObject pref_selectionDisplay;

        d.Bloc data;
        public d.Bloc Data { get { return data; } }

        List<int> selectedLines = new List<int>();
        public int[] SelectedLines
        {
            get { return selectedLines.ToArray(); }
        }
        const float LINE_RATIO = 0.0025f;
        int beginDragLine;
        bool onDrag;

        Texture2D colorMap;
        RectTransform rectTransform;
        BlocInformationDisplayer blocInformationsDisplayer;
        LayoutElement layoutElement;
        #endregion

        #region Public Methods
        public void Set(d.Bloc bloc,Texture2D colorMap,Vector2 limits)
        {
            data = bloc;
            this.colorMap = colorMap;
            gameObject.name = bloc.PBloc.DisplayInformations.Name + " | " + "Bloc n°" + bloc.PBloc.DisplayInformations.Position.Column;
            blocInformationsDisplayer.Set(bloc);
            SetSize();
            SetTexture(bloc, colorMap, limits);
            GenerateMainEventIndicator(bloc);
            GenerateSecondaryIndicator(bloc);
        }
        public void UpdateLimits(Vector2 limits)
        {
            SetTexture(Data, colorMap, limits);
        }
        public void SelectLines(int[] lines,bool additive)
        {
                ClearLinesSelected();
                if (additive)
                {
                    foreach (int i in lines)
                    {
                        if (!selectedLines.Contains(i))
                        {
                            selectedLines.Add(i);
                        }
                    }
                }
                else
                {
                    selectedLines = new List<int>(lines);
                }
                List<int> l_notSelectedLines = new List<int>();
                for (int i = 0; i < Data.Lines.Length; i++)
                {
                    if (!selectedLines.Contains(i)) l_notSelectedLines.Add(i);
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
            beginDragLine = LineClicked();
        }
        public void OnPointerClick(BaseEventData p)
        {
            PointerEventData pointer = (PointerEventData) p;
            if (pointer.button.ToString() == "Left")
            {
                if (!onDrag)
                {
                    int[] linesClicked = new int[1] { beginDragLine };
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        SendMessageSelectLines(linesClicked, data.PBloc, true);
                    }
                    else
                    {
                        SendMessageSelectLines(linesClicked, data.PBloc, false);
                    }
                }
            }
            else if (pointer.button.ToString() == "Right")
            {
                int max = Data.Lines.Length;
                int[] array = new int[max];
                for (int i = 0; i < max; i++)
                {
                    array[i] = i;
                }
                SendMessageSelectLines(array, data.PBloc, false);
            }
        }
        public void OnBeginDrag()
        {
            onDrag = true;
        }
        public void OnEndDrag()
        {
            int l_endDragLine = Mathf.Clamp(LineClicked(),0,data.Lines.Length-1);
            int l_beginDragLine = Mathf.Clamp(beginDragLine, 0, data.Lines.Length-1); ;
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
                SendMessageSelectLines(linesSelected.ToArray(), data.PBloc, true);

            }
            else
            {
                SendMessageSelectLines(linesSelected.ToArray(), data.PBloc, false);
            }
            onDrag = false;
        }
        public void OnScroll()
        {
            float l_input = Input.GetAxis("Mouse ScrollWheel") * 10;
            int delta = Mathf.RoundToInt(l_input);
            if(delta != 0)
            {
                int[] l_lines = SelectedLines;
                int size = l_lines.Length - 1;
                if (size < 0) size = 0;
                List<int> l_linesToSelect = new List<int>(size);
                int newLine;
                for (int i = 0; i < l_lines.Length; i++)
                {
                    newLine = l_lines[i] + delta;
                    if (newLine >= 0 && newLine < Data.Lines.Length)
                    {
                        l_linesToSelect.Add(newLine);
                    }
                }
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    SendMessageSelectLines(l_linesToSelect.ToArray(), data.PBloc, true);
                }
                else
                {
                    SendMessageSelectLines(l_linesToSelect.ToArray(), data.PBloc, false);
                }
            }
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            blocInformationsDisplayer = transform.GetChild(2).GetComponent<BlocInformationDisplayer>();
            layoutElement = GetComponent<LayoutElement>();
        }
        void AddCache(int[] lines)
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            GameObject displayer = Instantiate(pref_selectionDisplay as GameObject);
            RectTransform displayerRect = displayer.GetComponent<RectTransform>();
            displayerRect.SetParent(transform.GetChild(1));
            int nbLines = data.Lines.Length;
            float top = (float)(lines[lines.Length - 1] + 1) / nbLines;
            float bot = (float)lines[0] / nbLines;
            displayerRect.anchorMin = new Vector2(0, bot);
            displayerRect.anchorMax = new Vector2(1, top);
            displayerRect.offsetMin = new Vector2(0, 0);
            displayerRect.offsetMax = new Vector2(0, 0);
        }
        void SendMessageSelectLines(int[] lines, Data.Experience.Protocol.Bloc bloc,bool additive)
        {
            //Graph.GraphsGestion graphGestion = FindObjectOfType<Graph.GraphsGestion>();
            //if(graphGestion)
            //{
            //    graphGestion.OnSelectLines(lines, bloc, additive);
            //}
        }
        void SetTexture(d.Bloc bloc,Texture2D colorMap,Vector2 limits)
        {
            float[,] lines = ExtractDataFromLines(bloc.Lines);
            switch (ApplicationState.GeneralSettings.TrialMatrixSettings.Smoothing)
            {
                case TrialMatrixSettings.SmoothingType.None: break;
                case TrialMatrixSettings.SmoothingType.Line: lines = SmoothLines(lines,5); break;
            }

            Texture2D texture = GenerateTexture(lines, limits, colorMap);

            texture.mipMapBias = -5;
            texture.wrapMode = TextureWrapMode.Clamp;
            GetComponent<RawImage>().texture = texture;
        }
        Texture2D GenerateTexture(float[,] lines,Vector2 limits,Texture2D colorMap)
        {
            // Caculate texture size.
            int width = lines.GetLength(0);
            int height = lines.GetLength(1);
            Texture2D l_texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] colors = new Color[width * height];

            // Set pixels
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float value = lines[x, y];
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
        float[,] SmoothLines(float[,] lines, int pass)
        {
            float[,] result = new float[(lines.GetLength(0)-1)*pass + 1,lines.GetLength(1)];
            int width = lines.GetLength(0);
            int height = lines.GetLength(1);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x == width - 1)
                    {
                        result[x * pass, y] = lines[x, y];
                    }
                    else
                    {
                        for (int t = 0; t < pass; t++)
                        {
                            float value = Mathf.Lerp(lines[x, y], lines[x + 1, y], (float)t / pass);
                            result[(x * pass) + t, y] = value;
                        }
                    }
                }
            }
            return result;
        }
        float[,] ExtractDataFromLines(d.Line[] lines)
        {
            float[,] result = new float[lines[0].DataWithCorrection.Length,lines.Length];
            int width = result.GetLength(0);
            int height = result.GetLength(1);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    result[x, y] = lines[y].DataWithCorrection[x];
                }
            }
            return result;
        }
        Color GetColor(float value,Vector2 limits,Texture2D colorMap)
        {
            float ratio = (value - limits.x) / (limits.y - limits.x);
            ratio = Mathf.Clamp(ratio, 0.0f, 1.0f);
            int y = Mathf.RoundToInt(ratio * (colorMap.height - 1));
            return colorMap.GetPixel(0,y);
        }
        void GenerateMainEventIndicator(d.Bloc bloc)
        {
            GameObject mainEvent = new GameObject();
            mainEvent.name = "Main event";
            Image image = mainEvent.AddComponent<Image>();
            RectTransform rect = mainEvent.GetComponent<RectTransform>();
            rect.SetParent(transform.GetChild(0));
            float X = (float)bloc.Lines[0].Main.Position / bloc.Lines[0].DataWithCorrection.Length;
            float Xstep = 0.5f / bloc.Lines[0].DataWithCorrection.Length;
            rect.pivot = new Vector2(0, 0);
            rect.anchorMin = new Vector2(X, 0f);
            rect.anchorMax = new Vector2(X+ Xstep, 1f);
            rect.offsetMin = new Vector2(0, 0);
            rect.offsetMax = new Vector2(0, 0);
            image.color = Color.black;
        }
        void GenerateSecondaryIndicator(d.Bloc bloc)
        {
            for (int l = 0; l < bloc.Lines.Length; l++)
            {
                for (int e = 0; e < bloc.Lines[l].Secondaries.Length; e++)
                {
                    int position = bloc.Lines[l].Secondaries[e].Position;
                    if (position > -1)
                    {
                        GameObject mainEvent = new GameObject();
                        mainEvent.name = "Secondary event n°" + e + " line n°" + l;
                        Image image = mainEvent.AddComponent<Image>();
                        RectTransform rect = mainEvent.GetComponent<RectTransform>();
                        rect.SetParent(transform.GetChild(0));
                        float X = (float)position / bloc.Lines[l].DataWithCorrection.Length;
                        float Xstep = 0.5f / bloc.Lines[l].DataWithCorrection.Length;
                        float Y = (float)l / bloc.Lines.Length;
                        float Ystep = 1.0f / bloc.Lines.Length;
                        rect.pivot = new Vector2(0, 0);
                        rect.anchorMin = new Vector2(X, Y);
                        rect.anchorMax = new Vector2(X + Xstep, Y + Ystep);
                        rect.offsetMin = new Vector2(0, 0);
                        rect.offsetMax = new Vector2(0, 0);
                        image.color = Color.black;
                    }
                }
            }
        }
        int LineClicked()
        {
            Vector2 mousePosition = Input.mousePosition - rectTransform.position;
            mousePosition = new Vector2(mousePosition.x / rectTransform.rect.width, mousePosition.y / rectTransform.rect.height);
            mousePosition = new Vector2(Mathf.Clamp01(mousePosition.x), Mathf.Clamp01(mousePosition.y));
            return Mathf.FloorToInt(mousePosition.y * Data.Lines.Length);
        }
        void ClearLinesSelected()
        {
            foreach(Transform child in transform.GetChild(1))
            {
                Destroy(child.gameObject);
            }
        }
        void SetSize()
        {
            switch(ApplicationState.GeneralSettings.TrialMatrixSettings.BlocFormat)
            {
                case TrialMatrixSettings.BlocFormatType.ConstantLine:
                    layoutElement.preferredHeight = ApplicationState.GeneralSettings.TrialMatrixSettings.ConstantLineHeight * data.Lines.Length;
                    break;
                case TrialMatrixSettings.BlocFormatType.LineRatio:
                    layoutElement.preferredHeight = ApplicationState.GeneralSettings.TrialMatrixSettings.LineHeightByWidth * rectTransform.rect.width * data.Lines.Length;
                    break;
                case TrialMatrixSettings.BlocFormatType.BlocRatio:
                    layoutElement.preferredHeight = ApplicationState.GeneralSettings.TrialMatrixSettings.HeightByWidth * rectTransform.rect.width;  
                    break;
            }
        }

        void OnRectTransformDimensionsChange()
        {
            if (rectTransform.hasChanged)
            {
                SetSize();
            }
        }
        #endregion
    }
}