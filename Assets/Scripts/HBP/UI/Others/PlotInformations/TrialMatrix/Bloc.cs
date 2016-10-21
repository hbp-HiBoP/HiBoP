using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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

        d.Bloc m_bloc;
        public d.Bloc Data { get { return m_bloc; } }

        List<int> m_selectedLines = new List<int>();
        public int[] SelectedLines { get { return m_selectedLines.ToArray(); } }

        int m_beginDragLine;
        bool m_onDrag;

        Texture2D m_colorMap;
        RectTransform rectTransform;
        BlocInformationDisplayer m_blocDisplayer;
        #endregion

        #region Public Methods
        public void Set(d.Bloc bloc,Texture2D colorMap,Vector2 limits)
        {
            m_blocDisplayer = transform.GetChild(2).GetComponent<BlocInformationDisplayer>();
            m_bloc = bloc;
            m_blocDisplayer.Set(bloc);
            m_colorMap = colorMap;
            gameObject.name = bloc.PBloc.DisplayInformations.Name + " | " + "Bloc n°" + bloc.PBloc.DisplayInformations.Column;
            SetTexture(bloc, colorMap, limits);
            GenerateMainEventIndicator(bloc);
            GenerateSecondaryIndicator(bloc);
        }

        public void UpdateLimits(Vector2 limits)
        {
            SetTexture(Data, m_colorMap, limits);
        }

        public void SelectLines(int[] lines,bool additive)
        {
                ClearLinesSelected();
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
                for (int i = 0; i < Data.Lines.Length; i++)
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
            m_beginDragLine = LineClicked();
        }

        public void OnPointerClick(BaseEventData p)
        {
            PointerEventData pointer = (PointerEventData) p;
            if (pointer.button.ToString() == "Left")
            {
                if (!m_onDrag)
                {
                    int[] linesClicked = new int[1] { m_beginDragLine };
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        SendMessageSelectLines(linesClicked, m_bloc.PBloc, true);
                    }
                    else
                    {
                        SendMessageSelectLines(linesClicked, m_bloc.PBloc, false);
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
                SendMessageSelectLines(array, m_bloc.PBloc, false);
            }
        }

        public void OnBeginDrag()
        {
            m_onDrag = true;
        }

        public void OnEndDrag()
        {
            int l_endDragLine = Mathf.Clamp(LineClicked(),0,m_bloc.Lines.Length-1);
            int l_beginDragLine = Mathf.Clamp(m_beginDragLine, 0, m_bloc.Lines.Length-1); ;
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
                SendMessageSelectLines(linesSelected.ToArray(), m_bloc.PBloc, true);

            }
            else
            {
                SendMessageSelectLines(linesSelected.ToArray(), m_bloc.PBloc, false);
            }
            m_onDrag = false;
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
                    SendMessageSelectLines(l_linesToSelect.ToArray(), m_bloc.PBloc, true);
                }
                else
                {
                    SendMessageSelectLines(l_linesToSelect.ToArray(), m_bloc.PBloc, false);
                }
            }
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        void AddCache(int[] lines)
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            GameObject displayer = Instantiate(pref_selectionDisplay as GameObject);
            RectTransform displayerRect = displayer.GetComponent<RectTransform>();
            displayerRect.SetParent(transform.GetChild(1));
            int nbLines = m_bloc.Lines.Length;
            float top = (float)(lines[lines.Length - 1] + 1) / nbLines;
            float bot = (float)lines[0] / nbLines;
            displayerRect.anchorMin = new Vector2(0, bot);
            displayerRect.anchorMax = new Vector2(1, top);
            displayerRect.offsetMin = new Vector2(0, 0);
            displayerRect.offsetMax = new Vector2(0, 0);
        }

        void SendMessageSelectLines(int[] lines, Data.Experience.Protocol.Bloc bloc,bool additive)
        {
            Graph.GraphsGestion graphGestion = FindObjectOfType<Graph.GraphsGestion>();
            if(graphGestion)
            {
                graphGestion.HandleSelectLines(lines, bloc, additive);
            }
        }

        void SetTexture(d.Bloc bloc,Texture2D colorMap,Vector2 limits)
        {
            Texture2D texture = GenerateTexture(bloc.Lines, limits, colorMap);
            if (ApplicationState.GeneralSettings.TrialMatrixSmoothingType == HBP.Data.Settings.GeneralSettings.TrialMatrixSmoothingEnum.Line)
            {
                texture = SmoothTexture(texture, 5);
            }
            texture.mipMapBias = -5;
            texture.wrapMode = TextureWrapMode.Clamp;
            GetComponent<RawImage>().texture = texture;
        }

        Texture2D GenerateTexture(d.Line[] lines,Vector2 limits,Texture2D colorMap)
        {
            // Caculate texture size.
            int width = lines[0].DataWithCorrection.Length;
            int height = lines.Length;
            Texture2D l_texture = new Texture2D(width, height,TextureFormat.RGBA32,false);

            // Set pixels of the texture.
            for (int l = 0; l < height; l++)
            {
                for (int c = 0; c < width; c++)
                {
                    float value = lines[l].DataWithCorrection[c];
                    if(float.IsNaN(value))
                    {
                        l_texture.SetPixel(c, l, Color.black);
                    }
                    else
                    {
                        l_texture.SetPixel(c, l, GetColor(value, limits, colorMap));
                    }
                }
            }

            // Set texture.
            l_texture.filterMode = FilterMode.Point;
            l_texture.anisoLevel = 0;
            l_texture.Apply();
            return l_texture;
        }

        Texture2D SmoothTexture(Texture2D texture)
        {
            Texture2D result = new Texture2D(2 * texture.width - 1, texture.height);
            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    Color color1 = texture.GetPixel(x, y);
                    if (x < (texture.width - 1))
                    {
                        Color color2 = texture.GetPixel(x + 1, y);
                        Color newColor = Color.Lerp(color1, color2, 0.5f);
                        result.SetPixel(2 * x + 1, y, newColor);
                    }
                    result.SetPixel(2 * x, y, color1);
                }
            }
            result.Apply();
            result.anisoLevel = 0;
            result.filterMode = FilterMode.Point;
            return result;
        }

        Texture2D SmoothTexture(Texture2D texture, int pass)
        {
            Texture2D result = texture;
            for (int i = 0; i < pass; i++)
            {
                result = SmoothTexture(result);
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

        void GenerateMainEventIndicator(Data.TrialMatrix.Bloc bloc)
        {
            GameObject mainEvent = new GameObject();
            mainEvent.name = "Main event";
            Image image = mainEvent.AddComponent<Image>();
            RectTransform rect = mainEvent.GetComponent<RectTransform>();
            rect.SetParent(transform.GetChild(0));
            float X = (float)bloc.Lines[0].Main.Position / bloc.Lines[0].DataWithCorrection.Length;
            float Xstep = 1.0f / bloc.Lines[0].DataWithCorrection.Length;
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
                        float Xstep = 1.0f / bloc.Lines[l].DataWithCorrection.Length;
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
        #endregion
    }
}