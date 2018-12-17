using UnityEngine;
using UnityEngine.UI;
using Tools.CSharp;
using data = HBP.Data.TrialMatrix;
using HBP.Data.Experience.Dataset;

namespace HBP.UI.TrialMatrix
{
    public class SubBloc : MonoBehaviour
    {
        #region Properties
        data.SubBloc m_Data;
        public data.SubBloc Data
        {
            get
            {
                return m_Data;
            }
            set
            {
                Set(value, Colors, Limits);
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
                m_Colors = value;
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

        [SerializeField] RawImage m_RawImage;
        [SerializeField] LayoutElement m_MainTextureLayoutElement;
        [SerializeField] LayoutElement m_LeftFillerLayoutElement;
        [SerializeField] LayoutElement m_RightFillerLayoutElement;
        [SerializeField] GameObject m_EventPrefab;
        [SerializeField] RectTransform m_EventContainer;
        LayoutElement m_LayoutElement;
        #endregion

        #region Public Methods
        public void Set(data.SubBloc data, Color[] colors, Vector2 limits)
        {
            UnityEngine.Profiling.Profiler.BeginSample("1");
            gameObject.name = data.SubBlocProtocol.Name;
            m_Data = data;
            m_Colors = colors;
            m_Limits = limits;
            m_LayoutElement.flexibleWidth = data.Window.Lenght;
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("2");
            SetTexture();
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("3");
            SetFillers();
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("4");
            GenerateEventIndicators(data);
            UnityEngine.Profiling.Profiler.EndSample();
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_LayoutElement = GetComponent<LayoutElement>();
        }
        void SetTexture()
        {
            UnityEngine.Profiling.Profiler.BeginSample("1");
            float[][] trials = ExtractDataTrials(m_Data.SubTrials);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("2");
            if (ApplicationState.UserPreferences.Visualization.TrialMatrix.TrialSmoothing)
            {
                trials = SmoothTrials(trials, ApplicationState.UserPreferences.Visualization.TrialMatrix.NumberOfIntermediateValues);
            }
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("3");
            Texture2D texture = GenerateTexture(trials, m_Limits, m_Colors);
            texture.mipMapBias = -5;
            texture.wrapMode = TextureWrapMode.Clamp;
            m_RawImage.texture = texture;
            UnityEngine.Profiling.Profiler.EndSample();
        }
        void SetFillers()
        {
            m_LeftFillerLayoutElement.flexibleWidth = m_Data.SubBlocProtocol.Window.Start - m_Data.Window.Start;
            m_RightFillerLayoutElement.flexibleWidth = m_Data.Window.End - m_Data.SubBlocProtocol.Window.End;
            m_MainTextureLayoutElement.flexibleWidth = m_Data.SubBlocProtocol.Window.Lenght;
        }
        Texture2D GenerateTexture(float[][] trials, Vector2 limits, Color[] colors)
        {
            UnityEngine.Profiling.Profiler.BeginSample("3.1");
            // Caculate texture size.
            int height = trials.Length;
            if (height == 0) return new Texture2D(0,0);
            int width = trials[0].Length;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            UnityEngine.Profiling.Profiler.EndSample();

            // Set pixels
            UnityEngine.Profiling.Profiler.BeginSample("3.2");
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float value = trials[height - 1 - y][x];
                    if (float.IsNaN(value))
                    {
                        pixels[y*width + x] = Color.black;
                    }
                    else
                    {
                        pixels[y * width + x] = GetColor(value, limits, colors);
                    }
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();

            // Set texture.
            UnityEngine.Profiling.Profiler.BeginSample("3.3");
            texture.SetPixels(pixels);
            texture.filterMode = FilterMode.Point;
            texture.anisoLevel = 0;
            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("3.4");
            texture.Apply();
            UnityEngine.Profiling.Profiler.EndSample();

            return texture;
        }
        float[][] SmoothTrials(float[][] trials, int pass)
        {
            float[][] result = new float[trials.Length][];
            for (int l = 0; l < result.Length; l++)
            {
                result[l] = trials[l].LinearSmooth(pass);
            }
            return result;
        }
        float[][] ExtractDataTrials(data.SubTrial[] subTrials)
        {
            float[][] result = new float[subTrials.Length][];
            for (int l = 0; l < subTrials.Length; l++)
            {
                result[l] = subTrials[l].Data.Values;
            }
            return result;
        }
        Color GetColor(float value, Vector2 limits, Color[] colors)
        {
            UnityEngine.Profiling.Profiler.BeginSample("3.2.1");
            float ratio = (value - limits.x) / (limits.y - limits.x);
            ratio = Mathf.Clamp01(ratio);
            int x = Mathf.RoundToInt(ratio * (colors.Length - 1));
            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("3.2.2");
            Color color = colors[x];
            UnityEngine.Profiling.Profiler.EndSample();
            return color;
        }
        void GenerateEventIndicators(data.SubBloc subBloc)
        {
            foreach (var _event in subBloc.SubBlocProtocol.Events)
            {
                GameObject eventGameobject = new GameObject(_event.Name, typeof(RectTransform));
                RectTransform rectTransform = eventGameobject.transform as RectTransform;
                rectTransform.SetParent(m_EventContainer);
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(1, 1);
                rectTransform.offsetMin = new Vector2(0, 0);
                rectTransform.offsetMax = new Vector2(0, 0);
                if (_event.Type == HBP.Data.Enums.MainSecondaryEnum.Main)
                {
                    data.SubTrial subTrial = subBloc.SubTrials[0];
                    EventInformation.EventOccurence occurence = subTrial.Data.InformationsByEvent[_event].Occurences[0];

                    GameObject eventGameObject = Instantiate(m_EventPrefab, rectTransform);
                    RectTransform rect = eventGameObject.transform as RectTransform;

                    float x = (float)(occurence.IndexFromStart) / (subTrial.Data.Values.Length);
                    rect.anchorMin = new Vector2(x, 0);
                    rect.anchorMax = new Vector2(x, 1);
                    rect.anchoredPosition = new Vector2(0, 0);
                }
                else if(_event.Type == HBP.Data.Enums.MainSecondaryEnum.Secondary)
                {
                    for (int i = 0; i < subBloc.SubTrials.Length; i++)
                    {
                        data.SubTrial subTrial = subBloc.SubTrials[i];
                        EventInformation eventInformation = subTrial.Data.InformationsByEvent[_event];
                        foreach (var occurence in eventInformation.Occurences)
                        {
                            GameObject eventGameObject = Instantiate(m_EventPrefab, rectTransform);
                            RectTransform rect = eventGameObject.transform as RectTransform;

                            float x = (float)(occurence.IndexFromStart) / (subTrial.Data.Values.Length);
                            float y = (float)i / subBloc.SubTrials.Length;
                            float height = 1.0f / subBloc.SubTrials.Length;

                            rect.anchorMin = new Vector2(x, y);
                            rect.anchorMax = new Vector2(x, y + height);
                            rect.anchoredPosition = new Vector2(0, 0);
                        }
                    }
                }
            }
        }
        #endregion
    }
}