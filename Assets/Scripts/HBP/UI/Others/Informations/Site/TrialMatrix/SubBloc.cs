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
                m_Data = Data;
                SetTexture();
                SetFillers();
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

        [SerializeField] RawImage m_RawImage;
        [SerializeField] LayoutElement m_MainTextureLayoutElement;
        [SerializeField] LayoutElement m_LeftFillerLayoutElement;
        [SerializeField] LayoutElement m_RightFillerLayoutElement;
        LayoutElement m_LayoutElement;
        #endregion

        #region Public Methods
        public void Set(data.SubBloc data,Texture2D colorMap,Vector2 limits, Tools.CSharp.Window window)
        {
            m_Data = data;
            m_ColorMap = colorMap;
            m_Limits = limits;
            m_LayoutElement.flexibleWidth = window.End - window.Start;
            SetTexture();
            SetFillers();
            GenerateEventIndicators(data);
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_LayoutElement = GetComponent<LayoutElement>();
        }
        void SetTexture()
        {
            float[][] trials = ExtractDataTrials(m_Data.SubTrials);
            if(ApplicationState.UserPreferences.Visualization.TrialMatrix.TrialSmoothing)
            {
                trials = SmoothTrials(trials, ApplicationState.UserPreferences.Visualization.TrialMatrix.NumberOfIntermediateValues);
            }

            Texture2D texture = GenerateTexture(trials, m_Limits, m_ColorMap);
            texture.mipMapBias = -5;
            texture.wrapMode = TextureWrapMode.Clamp;
            m_RawImage.texture = texture;
        }
        void SetFillers()
        {
            int totalLenght = m_Data.SubTrials[0].Data.Values.Length + m_Data.SpacesBefore + m_Data.SpacesAfter;
            m_LeftFillerLayoutElement.flexibleWidth = (float) m_Data.SpacesBefore / totalLenght;
            m_RightFillerLayoutElement.flexibleWidth = (float) m_Data.SpacesAfter / totalLenght;
            m_MainTextureLayoutElement.flexibleWidth = (float) m_Data.SubTrials[0].Data.Values.Length / totalLenght;
        }
        Texture2D GenerateTexture(float[][] trials,Vector2 limits,Texture2D colorMap)
        {
            // Caculate texture size.
            int height = trials.Length;
            if (height == 0) return new Texture2D(0,0);
            int width = trials[0].Length;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] colors = new Color[width * height];

            // Set pixels
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float value = trials[height - 1 - y][x];
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
            texture.SetPixels(colors);
            texture.filterMode = FilterMode.Point;
            texture.anisoLevel = 0;
            texture.Apply();
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
        Color GetColor(float value,Vector2 limits,Texture2D colorMap)
        {
            float ratio = (value - limits.x) / (limits.y - limits.x);
            ratio = Mathf.Clamp01(ratio);
            int y = Mathf.RoundToInt(ratio * (colorMap.height - 1));
            return colorMap.GetPixel(0,y);
        }
        void GenerateEventIndicators(data.SubBloc subBloc)
        {
            foreach (var _event in subBloc.SubBlocProtocol.Events)
            {

                for (int i = 0; i < subBloc.SubTrials.Length; i++)
                {
                    data.SubTrial subTrial = subBloc.SubTrials[i];
                    EventInformation eventInformation = subTrial.Data.InformationsByEvent[_event];
                    foreach (var occurence in eventInformation.Occurences)
                    {
                        GameObject eventGameObject = new GameObject(_event.Name + " - " + i);
                        eventGameObject.transform.SetParent(transform.GetChild(0));
                        eventGameObject.AddComponent<Image>().color = Color.black;

                        float x = (float)(occurence.IndexFromStart + subBloc.SpacesBefore) / (subBloc.SubTrials[i].Data.Values.Length + subBloc.SpacesBefore + subBloc.SpacesAfter);
                        float width = 0.5f / subBloc.SubTrials[i].Data.Values.Length;
                        float y = (float)i / subBloc.SubTrials.Length;
                        float height = 1.0f / subBloc.SubTrials.Length;

                        RectTransform rect = eventGameObject.GetComponent<RectTransform>();
                        rect.pivot = new Vector2(0, 0);
                        rect.anchorMin = new Vector2(x, y);
                        rect.anchorMax = new Vector2(x + width, y + height);
                        rect.offsetMin = new Vector2(0, 0);
                        rect.offsetMax = new Vector2(0, 0);
                    }
                }
            }
        }
        #endregion
    }
}