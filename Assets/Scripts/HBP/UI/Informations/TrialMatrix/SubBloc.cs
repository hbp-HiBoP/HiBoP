using UnityEngine;
using UnityEngine.UI;
using data = HBP.Data.Informations.TrialMatrix;
using UnityEngine.Events;
using UnityEngine.UI.Extensions;
using UnityEngine.EventSystems;
using HBP.Core.Enums;
using HBP.Core.Data;
using HBP.Core.Preferences;
using System.Collections.Generic;

namespace HBP.UI.Informations.TrialMatrix
{
    public class SubBloc : MonoBehaviour
    {
        #region Properties

        [SerializeField] data.SubBloc m_Data;

        public data.SubBloc Data
        {
            get { return m_Data; }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_Data, value))
                {
                    SetData();
                }
            }
        }

        Color[] m_Colors;

        public Color[] Colors
        {
            get { return m_Colors; }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_Colors, value))
                {
                    SetColors();
                }
            }
        }

        [SerializeField] Vector2 m_Limits;

        public Vector2 Limits
        {
            get { return m_Limits; }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_Limits, value))
                {
                    SetLimits();
                }
            }
        }

        [SerializeField] bool m_Hovered = false;

        public bool Hovered
        {
            get { return m_Hovered; }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_Hovered, value))
                {
                    SetHovered();
                }
            }
        }

        [SerializeField] UnityEvent m_OnChangeHovered = new();

        public UnityEvent OnChangeHovered
        {
            get { return m_OnChangeHovered; }
        }

        [SerializeField] BaseEventData m_OnPointerDown;

        public BaseEventData OnPointerDown
        {
            get { return m_OnPointerDown; }
        }

        [SerializeField] RawImage m_RawImage;
        [SerializeField] LayoutElement m_MainTextureLayoutElement;
        [SerializeField] LayoutElement m_LeftFillerLayoutElement;
        [SerializeField] LayoutElement m_RightFillerLayoutElement;
        [SerializeField] GameObject m_EventPrefab;
        [SerializeField] RectTransform m_EventContainer;
        [SerializeField] LayoutElement m_LayoutElement;
        readonly List<RawImage> m_TileImages = new();
        readonly List<Texture2D> m_TileTextures = new();
        Color32[] m_Color32Buffer;

        #endregion

        #region Public Methods

        public void Set(data.SubBloc data, Color[] colors, Vector2 limits)
        {
            m_Data = data;
            if (m_Data != null)
            {
                m_Colors = colors;
                m_Limits = limits;
                m_LayoutElement.flexibleWidth = data.Window.Length;

                if (data.IsFiller)
                {
                    gameObject.name = "Filler";
                    ClearTextureResources();
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

        void OnDestroy()
        {
            ClearTextureResources();
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
            if (m_Data != null && !m_Data.IsFiller && m_Colors != null && m_Colors.Length > 0)
            {
                float[][] trials = ExtractDataTrials(m_Data.SubTrials);
                bool smooth = PersistentDataManager.UserPreferences.Visualization.TrialMatrix.TrialSmoothing;
                bool smooth2D = smooth && PersistentDataManager.UserPreferences.Visualization.TrialMatrix.Smooth2D;
                TrialMatrixTiles tiles = TrialMatrixTileBuilder.Build(trials, m_Limits, GetColor32Buffer(), smooth, PersistentDataManager.UserPreferences.Visualization.TrialMatrix.NumberOfIntermediateValues, smooth2D, SystemInfo.maxTextureSize);
                ApplyTiles(tiles, smooth2D ? FilterMode.Bilinear : FilterMode.Point);
            }
        }

        void SetFillers()
        {
            if (m_Data.IsFiller)
            {
                m_LeftFillerLayoutElement.flexibleWidth = 1;
                m_RightFillerLayoutElement.flexibleWidth = 0;
                m_MainTextureLayoutElement.flexibleWidth = 0;
            }
            else
            {
                m_LeftFillerLayoutElement.flexibleWidth = m_Data.SubBlocProtocol.Window.Start - m_Data.Window.Start;
                m_RightFillerLayoutElement.flexibleWidth = m_Data.Window.End - m_Data.SubBlocProtocol.Window.End;
                m_MainTextureLayoutElement.flexibleWidth = m_Data.SubBlocProtocol.Window.Length;
            }
        }

        Color32[] GetColor32Buffer()
        {
            if (m_Color32Buffer == null || m_Color32Buffer.Length != m_Colors.Length)
                m_Color32Buffer = new Color32[m_Colors.Length];
            for (int i = 0; i < m_Colors.Length; i++)
                m_Color32Buffer[i] = m_Colors[i];
            return m_Color32Buffer;
        }

        void ApplyTiles(TrialMatrixTiles tiles, FilterMode filterMode)
        {
            EnsureTileImages(tiles.Tiles.Count);
            long textureBytes = 0;
            for (int i = 0; i < tiles.Tiles.Count; i++)
            {
                TrialMatrixTile tile = tiles.Tiles[i];
                RawImage image = m_TileImages[i];
                RectTransform rect = image.rectTransform;
                rect.anchorMin = new Vector2((float)tile.CoreX / tiles.Width, (float)tile.CoreY / tiles.Height);
                rect.anchorMax = new Vector2((float)(tile.CoreX + tile.CoreWidth) / tiles.Width, (float)(tile.CoreY + tile.CoreHeight) / tiles.Height);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                image.uvRect = tile.UvRect;
                image.enabled = true;

                Texture2D texture = GetOrCreateTexture(i, tile.TextureWidth, tile.TextureHeight);
                texture.filterMode = filterMode;
                texture.SetPixelData(tile.Pixels, 0);
                texture.Apply(false, false);
                image.texture = texture;
                textureBytes += (long)tile.TextureWidth * tile.TextureHeight * 4;
            }

            TrimTextures(tiles.Tiles.Count);
            DataManager.RegisterMemoryUsage(this, MemoryCacheCategory.Texture, textureBytes, true);
        }

        void EnsureTileImages(int count)
        {
            if (m_TileImages.Count == 0)
                m_TileImages.Add(m_RawImage);
            while (m_TileImages.Count < count)
            {
                GameObject tileObject = new($"Texture Tile {m_TileImages.Count}", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                tileObject.layer = m_RawImage.gameObject.layer;
                RectTransform rect = tileObject.transform as RectTransform;
                rect.SetParent(m_RawImage.rectTransform.parent, false);
                rect.SetSiblingIndex(m_RawImage.rectTransform.GetSiblingIndex() + m_TileImages.Count);
                RawImage image = tileObject.GetComponent<RawImage>();
                image.raycastTarget = m_RawImage.raycastTarget;
                image.maskable = m_RawImage.maskable;
                image.color = m_RawImage.color;
                m_TileImages.Add(image);
            }

            while (m_TileImages.Count > System.Math.Max(1, count))
            {
                int last = m_TileImages.Count - 1;
                DestroyObject(m_TileImages[last].gameObject);
                m_TileImages.RemoveAt(last);
            }

            if (count == 0)
                m_RawImage.enabled = false;
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

        Texture2D GetOrCreateTexture(int index, int width, int height)
        {
            while (m_TileTextures.Count <= index)
                m_TileTextures.Add(null);
            Texture2D texture = m_TileTextures[index];
            if (texture == null || texture.width != width || texture.height != height)
            {
                DestroyObject(texture);
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    name = $"Trial Matrix Tile {index}",
                    mipMapBias = -5,
                    wrapMode = TextureWrapMode.Clamp,
                    anisoLevel = 0
                };
                m_TileTextures[index] = texture;
            }

            return texture;
        }

        void TrimTextures(int count)
        {
            for (int i = m_TileTextures.Count - 1; i >= count; i--)
            {
                DestroyObject(m_TileTextures[i]);
                m_TileTextures.RemoveAt(i);
            }
        }

        void ClearTextureResources()
        {
            DataManager.UnregisterMemoryUsage(this);
            foreach (Texture2D texture in m_TileTextures)
                DestroyObject(texture);
            m_TileTextures.Clear();
            for (int i = m_TileImages.Count - 1; i >= 1; i--)
                DestroyObject(m_TileImages[i].gameObject);
            m_TileImages.Clear();
            if (m_RawImage != null)
            {
                m_RawImage.texture = null;
                m_RawImage.enabled = false;
            }
        }

        static void DestroyObject(Object value)
        {
            if (value == null)
                return;
            if (Application.isPlaying)
                Destroy(value);
            else
                DestroyImmediate(value);
        }

        void GenerateEventIndicators(data.SubBloc subBloc)
        {
            foreach (var _event in subBloc.SubBlocProtocol.Events)
            {
                GameObject eventGameobject = new(_event.Name, typeof(RectTransform));
                RectTransform rectTransform = eventGameobject.transform as RectTransform;
                rectTransform.SetParent(m_EventContainer);
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(1, 1);
                rectTransform.offsetMin = new Vector2(0, 0);
                rectTransform.offsetMax = new Vector2(0, 0);
                rectTransform.localScale = new Vector3(1, 1, 1);
                if (_event.Type == MainSecondaryEnum.Main)
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
                else if (_event.Type == MainSecondaryEnum.Secondary)
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
