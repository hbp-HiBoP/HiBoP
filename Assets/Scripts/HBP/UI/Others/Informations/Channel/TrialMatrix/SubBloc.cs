using UnityEngine;
using UnityEngine.UI;
using Tools.CSharp;
using data = HBP.Data.TrialMatrix;
using HBP.Data.Experience.Dataset;
using UnityEngine.Events;
using UnityEngine.UI.Extensions;
using UnityEngine.EventSystems;

namespace HBP.UI.TrialMatrix
{
    public class SubBloc : MonoBehaviour
    {
        #region Properties
        [SerializeField] data.SubBloc m_Data;
        public data.SubBloc Data
        {
            get
            {
                return m_Data;
            }
            set
            {
                if(SetPropertyUtility.SetClass(ref m_Data, value))
                {
                    SetData();
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
                if(SetPropertyUtility.SetClass(ref m_Colors, value))
                {
                    SetColors();
                }
            }
        }

        [SerializeField] Vector2 m_Limits;
        public Vector2 Limits
        {
            get
            {
                return m_Limits;
            }
            set
            {
                if(SetPropertyUtility.SetStruct(ref m_Limits, value))
                {
                    SetLimits();
                }
            }
        }

        [SerializeField] bool m_Hovered = false;
        public bool Hovered
        {
            get
            {
                return m_Hovered;
            }
            set
            {
                if(SetPropertyUtility.SetStruct(ref m_Hovered, value))
                {
                    SetHovered();
                }
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

        [SerializeField] BaseEventData m_OnPointerDown;
        public BaseEventData OnPointerDown
        {
            get
            {
                return m_OnPointerDown;
            }
        }

        [SerializeField] RawImage m_RawImage;
        [SerializeField] LayoutElement m_MainTextureLayoutElement;
        [SerializeField] LayoutElement m_LeftFillerLayoutElement;
        [SerializeField] LayoutElement m_RightFillerLayoutElement;
        [SerializeField] GameObject m_EventPrefab;
        [SerializeField] RectTransform m_EventContainer;
        [SerializeField] LayoutElement m_LayoutElement;
        #endregion

        #region Public Methods
        public void Set(data.SubBloc data, Color[] colors, Vector2 limits)
        {
            m_Data = data;
            if(m_Data != null)
            {
                m_Colors = colors;
                m_Limits = limits;
                m_LayoutElement.flexibleWidth = data.Window.Lenght;

                if (data.IsFiller)
                {
                    gameObject.name = "Filler";
                }
                else
                {
                    gameObject.name = data.SubBlocProtocol.Name;
                    SetTexture();
                    GenerateEventIndicators(data);
                }
                SetFillers();
            }
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            m_LayoutElement = GetComponent<LayoutElement>();
        }
        void OnValidate()
        {
            SetColors();
            SetHovered();
            SetLimits();
            SetData();
        }

        void SetTexture()
        {
            if(m_Data != null)
            {
                float[][] trials = ExtractDataTrials(m_Data.SubTrials);
                if (ApplicationState.UserPreferences.Visualization.TrialMatrix.TrialSmoothing)
                {
                    trials = SmoothTrials(trials, ApplicationState.UserPreferences.Visualization.TrialMatrix.NumberOfIntermediateValues);
                }

                Texture2D texture = GenerateTexture(trials, m_Limits, m_Colors);
                texture.mipMapBias = -5;
                texture.wrapMode = TextureWrapMode.Clamp;
                m_RawImage.texture = texture;
            }
        }
        void SetFillers()
        {
            if(m_Data.IsFiller)
            {
                m_LeftFillerLayoutElement.flexibleWidth = 1;
                m_RightFillerLayoutElement.flexibleWidth = 0;
                m_MainTextureLayoutElement.flexibleWidth = 0;
            }
            else
            {
                m_LeftFillerLayoutElement.flexibleWidth = m_Data.SubBlocProtocol.Window.Start - m_Data.Window.Start;
                m_RightFillerLayoutElement.flexibleWidth = m_Data.Window.End - m_Data.SubBlocProtocol.Window.End;
                m_MainTextureLayoutElement.flexibleWidth = m_Data.SubBlocProtocol.Window.Lenght;
            }
        }
        Texture2D GenerateTexture(float[][] trials, Vector2 limits, Color[] colors)
        {
            // Caculate texture size.
            int height = trials.Length;
            if (height == 0) return new Texture2D(0,0);
            int width = trials[0].Length;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];

            // Set pixels
            float[] trial;
            int start = 0;
            for (int y = 0; y < height; y++)
            {
                trial = trials[height - 1 - y];
                for (int x = 0; x < width; x++)
                {
                    pixels[start + x] = GetColor(trial[x], limits, colors);
                }
                start += width;
            }

            // Set texture.
            texture.SetPixels(pixels);
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
        Color GetColor(float value, Vector2 limits, Color[] colors)
        {
            float ratio = 0.5f;
            if(limits.y != limits.x)
            {
                ratio = (value - limits.x) / (limits.y - limits.x);
                ratio = Mathf.Clamp01(ratio);
            }
            int x = Mathf.RoundToInt(ratio * (colors.Length - 1));
            return colors[x];
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

                    float x = (occurence.IndexFromStart + 0.5f) / (subTrial.Data.Values.Length);
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

                            float x = (float)(occurence.IndexFromStart + 0.5f) / (subTrial.Data.Values.Length);
                            float y = 1 - (float)i / subBloc.SubTrials.Length;
                            float height = 1.0f / subBloc.SubTrials.Length;

                            rect.anchorMin = new Vector2(x, y - height);
                            rect.anchorMax = new Vector2(x, y);
                            rect.anchoredPosition = new Vector2(0, 0);
                        }
                    }
                }
            }
        }

        void SetColors()
        {
            SetTexture();
        }
        void SetHovered()
        {
            m_OnChangeHovered.Invoke();
        }
        void SetLimits()
        {
            SetTexture();
        }
        void SetData()
        {
            Set(m_Data, m_Colors, m_Limits);
        }
        #endregion
    }
}