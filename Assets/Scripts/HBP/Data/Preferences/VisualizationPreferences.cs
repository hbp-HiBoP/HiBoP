using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HBP.Core.Enums;
using HBP.Core.Data;
using Newtonsoft.Json;

namespace HBP.Data.Preferences
{
    [JsonObject(MemberSerialization.OptIn)]
    public class VisualizationPreferences : ICloneable
    {
        #region Properties
        [JsonProperty] public _3DPreferences _3D { get; set; }
        [JsonProperty] public TrialMatrixPreferences TrialMatrix { get; set; }
        [JsonProperty] public GraphPreferences Graph { get; set; }
        [JsonProperty] public CutPreferences Cut { get; set; }
        #endregion

        #region Constructors
        public VisualizationPreferences() : this(new _3DPreferences(), new TrialMatrixPreferences(), new GraphPreferences(), new CutPreferences())
        {

        }
        public VisualizationPreferences(_3DPreferences _3d, TrialMatrixPreferences trialMatrix, GraphPreferences graph, CutPreferences cut)
        {
            _3D = _3d;
            TrialMatrix = trialMatrix;
            Graph = graph;
            Cut = cut;
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new VisualizationPreferences(_3D.Clone() as _3DPreferences, TrialMatrix.Clone() as TrialMatrixPreferences, Graph.Clone() as GraphPreferences, Cut.Clone() as CutPreferences);
        }
        #endregion
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class _3DPreferences : ICloneable
    {
        #region Properties
        [JsonProperty] public bool AutomaticEEGUpdate { get; set; }
        [JsonProperty] public bool RawCuts { get; set; }
        [JsonProperty] public LayoutDirection VisualizationsLayoutDirection { get; set; }
        [JsonProperty] public SiteInfluenceByDistanceType SiteInfluenceByDistance { get; set; }
        [JsonProperty] public string DefaultSelectedMRIInSinglePatientVisualization { get; set; }
        [JsonProperty] public string DefaultSelectedMeshInSinglePatientVisualization { get; set; }
        [JsonProperty] public string DefaultSelectedImplantationInSinglePatientVisualization { get; set; }
        [JsonProperty] public string DefaultSelectedMRIInMultiPatientsVisualization { get; set; }
        [JsonProperty] public string DefaultSelectedMeshInMultiPatientsVisualization { get; set; }
        [JsonProperty] public string DefaultSelectedImplantationInMultiPatientsVisualization { get; set; }
        #endregion

        #region Constructors
        public _3DPreferences(
            bool automaticEEGUpdate = true,
            bool rawCuts = false,
            LayoutDirection visualizationsLayoutDirection = LayoutDirection.Vertical,
            SiteInfluenceByDistanceType siteInfluenceByDistance = SiteInfluenceByDistanceType.Quadratic,
            string defaultSelectedMRIInSinglePatientVisualization = "Preimplantation",
            string defaultSelectedMeshInSinglePatientVisualization = "Grey matter",
            string defaultSelectedImplantationInSinglePatientVisualization = "Patient",
            string defaultSelectedMRIInMultiPatientsVisualization = "MNI",
            string defaultSelectedMeshInMultiPatientsVisualization = "MNI Grey matter",
            string defaultSelectedImplantationInMultiPatientsVisualization = "MNI")
        {
            AutomaticEEGUpdate = automaticEEGUpdate;
            RawCuts = rawCuts;
            VisualizationsLayoutDirection = visualizationsLayoutDirection;
            SiteInfluenceByDistance = siteInfluenceByDistance;
            DefaultSelectedMRIInSinglePatientVisualization = defaultSelectedMRIInSinglePatientVisualization;
            DefaultSelectedMeshInSinglePatientVisualization = defaultSelectedMeshInSinglePatientVisualization;
            DefaultSelectedImplantationInSinglePatientVisualization = defaultSelectedImplantationInSinglePatientVisualization;
            DefaultSelectedMRIInMultiPatientsVisualization = defaultSelectedMRIInMultiPatientsVisualization;
            DefaultSelectedMeshInMultiPatientsVisualization = defaultSelectedMeshInMultiPatientsVisualization;
            DefaultSelectedImplantationInMultiPatientsVisualization = defaultSelectedImplantationInMultiPatientsVisualization;
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new _3DPreferences(AutomaticEEGUpdate, RawCuts, VisualizationsLayoutDirection, SiteInfluenceByDistance, DefaultSelectedMRIInSinglePatientVisualization, DefaultSelectedMeshInSinglePatientVisualization, DefaultSelectedImplantationInSinglePatientVisualization, DefaultSelectedMRIInMultiPatientsVisualization, DefaultSelectedMeshInMultiPatientsVisualization, DefaultSelectedImplantationInMultiPatientsVisualization);
        }
        #endregion
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class TrialMatrixPreferences : ICloneable
    {
        #region Properties
        public const int MINIMUM_TRIAL_HEIGHT = 5;
        public const int MAXIMUM_TRIAL_HEIGHT = 50;
        public const float MINIMUM_TRIAL_RATIO = 0.001f;
        public const float MAXIMUM_TRIAL_RATIO = 0.05f;
        public const float MINIMUM_BLOC_RATIO = 0.05f;
        public const float MAXIMUM_BLOC_RATIO = 1.0f;
        public const float MINIMUM_PROTOCOL_RATIO = 0.5f;
        public const float MAXIMUM_PROTOCOL_RATIO = 2.0f;
        [JsonProperty] public bool ShowWholeProtocol { get; set; }
        [JsonProperty] public bool TrialsSynchronization { get; set; }
        [JsonProperty] public bool TrialSmoothing { get; set; }
        [JsonProperty] public int NumberOfIntermediateValues { get; set; }
        [JsonProperty] public bool Smooth2D { get; set; }
        [JsonProperty] public BlocFormatType SubBlocFormat { get; set; }
        [JsonProperty] public int TrialHeight { get; set; }
        [JsonProperty] public float TrialRatio { get; set; }
        [JsonProperty] public float BlocRatio { get; set; }
        [JsonProperty] public float ProtocolRatio { get; set; }
        #endregion

        #region Constructors
        public TrialMatrixPreferences(bool showWholeProtocol = true, bool trialsSynchronization = true, bool trialSmooting = true,
            int numberOfIntermediateValues = 3, bool smooth2D = true, BlocFormatType subBlocFormat = BlocFormatType.BlocRatio,
            int trialHeight = (int)(0.3f * (MAXIMUM_TRIAL_HEIGHT - MINIMUM_TRIAL_HEIGHT)), float trialRatio = 0.3f * (MAXIMUM_TRIAL_RATIO - MINIMUM_TRIAL_RATIO), float blocRatio = 0.3f * (MAXIMUM_BLOC_RATIO - MINIMUM_BLOC_RATIO),
            float protocolRatio = 0.3f * (MAXIMUM_PROTOCOL_RATIO - MINIMUM_PROTOCOL_RATIO))
        {
            ShowWholeProtocol = showWholeProtocol;
            TrialsSynchronization = trialsSynchronization;
            TrialSmoothing = trialSmooting;
            NumberOfIntermediateValues = numberOfIntermediateValues;
            Smooth2D = smooth2D;
            SubBlocFormat = subBlocFormat;
            TrialHeight = trialHeight;
            TrialRatio = trialRatio;
            BlocRatio = blocRatio;
            ProtocolRatio = protocolRatio;
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new TrialMatrixPreferences(ShowWholeProtocol, TrialsSynchronization, TrialSmoothing, NumberOfIntermediateValues, Smooth2D, SubBlocFormat, TrialHeight, TrialRatio, BlocRatio, ProtocolRatio);
        }
        #endregion
    }


    [JsonObject(MemberSerialization.OptIn)]
    public class GraphPreferences : ICloneable
    {
        #region Properties
        public const int MINIMUM_NUMBER_OF_SITES = 2;
        public const int MAXIMUM_NUMBER_OF_SITES = 10;
        public const int MINIMUM_NUMBER_OF_COLUMNS = 8;
        public const int MAXIMUM_NUMBER_OF_COLUMNS = 16;
        public const int MINIMUM_NUMBER_OF_GROUPS = 1;
        public const int MAXIMUM_NUMBER_OF_GROUPS = 10;

        [JsonProperty] public bool ShowCurvesOfMinimizedColumns { get; set; }
        [JsonProperty] public bool ShowSEM { get; set; }
        
        [JsonProperty] private int m_MaxSites;
        [JsonIgnore] public int MaxSites
        {
            get => m_MaxSites;
            set
            {
                if (value != m_MaxSites)
                {
                    m_MaxSites = value;
                    UpdateMaxDimensions(MaxSites, MaxColumns, MaxGroups);
                }
            }
        }
        [JsonProperty] private int m_MaxColumns;
        [JsonIgnore] public int MaxColumns
        {
            get => m_MaxColumns;
            set
            {
                if (value != m_MaxColumns)
                {
                    m_MaxColumns = value;
                    UpdateMaxDimensions(MaxSites, MaxColumns, MaxGroups);
                }
            }
        }
        [JsonProperty] private int m_MaxGroups;
        [JsonIgnore] public int MaxGroups
        {
            get => m_MaxGroups;
            set
            {
                if (value != m_MaxGroups)
                {
                    m_MaxGroups = value;
                    UpdateMaxDimensions(MaxSites, MaxColumns, MaxGroups);
                }
            }
        }

        [JsonProperty] public ColorGrid SiteColors { get; set; }
        [JsonProperty] public ColorGrid ROIColors { get; set; }
        [JsonProperty] public ColorGrid GroupColors { get; set; }
        #endregion

        #region Constructors
        public GraphPreferences(bool showCurvesOfMinimizedColumns = true, bool showSEM = true, int maxSites = 2, int maxColumns = 8, int maxGroups = 1)
        {
            ShowCurvesOfMinimizedColumns = showCurvesOfMinimizedColumns;
            ShowSEM = showSEM;
            m_MaxSites = maxSites;
            m_MaxColumns = maxColumns;
            m_MaxGroups = maxGroups;
            InitializeDefaultColors();
        }
        #endregion

        #region Public Methods
        public void SetDefaultColors()
        {
            InitializeDefaultColors();
        }
        public void UpdateMaxDimensions(int maxSites, int maxColumns, int maxGroups)
        {
            if (maxSites > MaxSites || maxColumns > MaxColumns)
            {
                // Redimensionner la grille des sites si nécessaire
                ColorGrid newSiteColors = new ColorGrid(maxSites, maxColumns, ColorGrid.ColorGridType.Site);
                for (int r = 0; r < Math.Min(MaxSites, maxSites); r++)
                {
                    for (int c = 0; c < Math.Min(MaxColumns, maxColumns); c++)
                    {
                        newSiteColors.SetColor(r, c, SiteColors.GetColor(r, c));
                    }
                }
                SiteColors = newSiteColors;
            }

            if (maxColumns > MaxColumns)
            {
                // Redimensionner la grille des ROI si nécessaire
                ColorGrid newROIColors = new ColorGrid(1, maxColumns, ColorGrid.ColorGridType.ROI);
                for (int c = 0; c < Math.Min(MaxColumns, maxColumns); c++)
                {
                    newROIColors.SetColor(0, c, ROIColors.GetColor(0, c));
                }
                ROIColors = newROIColors;
            }

            if (maxGroups > MaxGroups || maxColumns > MaxColumns)
            {
                // Redimensionner la grille des groupes si nécessaire
                ColorGrid newGroupColors = new ColorGrid(maxGroups, maxColumns, ColorGrid.ColorGridType.Group);
                for (int r = 0; r < Math.Min(MaxGroups, maxGroups); r++)
                {
                    for (int c = 0; c < Math.Min(MaxColumns, maxColumns); c++)
                    {
                        newGroupColors.SetColor(r, c, GroupColors.GetColor(r, c));
                    }
                }
                GroupColors = newGroupColors;
            }

            MaxSites = maxSites;
            MaxColumns = maxColumns;
            MaxGroups = maxGroups;
        }
        public object Clone()
        {
            GraphPreferences clone = new GraphPreferences(ShowCurvesOfMinimizedColumns, ShowSEM);
            clone.MaxSites = MaxSites;
            clone.MaxColumns = MaxColumns;
            clone.MaxGroups = MaxGroups;
            clone.SiteColors = SiteColors?.Clone() as ColorGrid;
            clone.ROIColors = ROIColors?.Clone() as ColorGrid;
            clone.GroupColors = GroupColors?.Clone() as ColorGrid;
            return clone;
        }
        #endregion

        #region Private Methods
        private void InitializeDefaultColors()
        {
            SiteColors = new ColorGrid(MaxSites, MaxColumns, ColorGrid.ColorGridType.Site);
            ROIColors = new ColorGrid(1, MaxColumns, ColorGrid.ColorGridType.ROI);
            GroupColors = new ColorGrid(MaxGroups, MaxColumns, ColorGrid.ColorGridType.Group);

            SiteColors.InitializeWithColors(new Color[]
            {
                new Color(171f / 255f, 61f / 255f, 58f / 255f),
                new Color(171f / 255f, 152f / 255f, 58f / 255f),
                new Color(46f / 255f, 135f / 255f, 52f / 255f),
                new Color(66f / 255f, 49f / 255f, 118f / 255f),
                new Color(171f / 255f, 109f / 255f, 58f / 255f),
                new Color(171f / 255f, 171f / 255f, 58f / 255f),
                new Color(35f / 255f, 103f / 255f, 103f / 255f),
                new Color(89f / 255f, 43f / 255f, 114f / 255f),
                new Color(171f / 255f, 133f / 255f, 58f / 255f),
                new Color(123f / 255f, 160f / 255f, 54f / 255f),
                new Color(47f / 255f, 66f / 255f, 115f / 255f),
                new Color(137f / 255f, 47f / 255f, 98f / 255f),
                new Color(129f / 255f, 22f / 255f, 22f / 255f),
                new Color(129f / 255f, 111f / 255f, 22f / 255f),
                new Color(18f / 255f, 103f / 255f, 18f / 255f),
                new Color(39f / 255f, 24f / 255f, 89f / 255f),
            });
            ROIColors.InitializeWithColors(new Color[]
            {
                new Color(129f / 255f, 71f / 255f, 22f / 255f),
                new Color(129f / 255f, 129f / 255f, 22f / 255f),
                new Color(13f / 255f, 77f / 255f, 77f / 255f),
                new Color(62f / 255f, 19f / 255f, 86f / 255f),
                new Color(129f / 255f, 93f / 255f, 22f / 255f),
                new Color(87f / 255f, 120f / 255f, 21f / 255f),
                new Color(23f / 255f, 42f / 255f, 86f / 255f),
                new Color(103f / 255f, 18f / 255f, 66f / 255f)
            });
            GroupColors.InitializeWithColors(new Color[]
            {
                new Color(180f / 255f, 100f / 255f, 100f / 255f),
                new Color(100f / 255f, 180f / 255f, 120f / 255f),
                new Color(120f / 255f, 140f / 255f, 180f / 255f),
                new Color(180f / 255f, 160f / 255f, 100f / 255f),
                new Color(160f / 255f, 100f / 255f, 180f / 255f),
                new Color(100f / 255f, 160f / 255f, 160f / 255f),
                new Color(180f / 255f, 130f / 255f, 80f / 255f),
                new Color(140f / 255f, 140f / 255f, 140f / 255f)
            });
        }
        #endregion
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class CutPreferences : ICloneable
    {
        #region Properties
        [JsonProperty] public bool ShowCutLines { get; set; }
        #endregion

        #region Constructors
        public CutPreferences(bool showCutLines = true)
        {
            ShowCutLines = showCutLines;
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new CutPreferences(ShowCutLines);
        }
        #endregion
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ColorGrid : ICloneable
    {
        #region Enums
        public enum ColorGridType { Site, ROI, Group }
        #endregion

        #region Properties
        [JsonProperty] private SerializableColor[,] m_Colors;
        [JsonProperty] private ColorGridType m_Type;

        public int Rows => m_Colors?.GetLength(0) ?? 0;
        public int Columns => m_Colors?.GetLength(1) ?? 0;
        #endregion

        #region Constructors
        public ColorGrid(int rows, int columns, ColorGridType type)
        {
            m_Colors = new SerializableColor[rows, columns];
            m_Type = type;
        }
        #endregion

        #region Public Methods
        public Color GetColor(int row, int column)
        {
            if (row >= 0 && row < Rows && column >= 0 && column < Columns)
            {
                return m_Colors[row, column].ToColor();
            }
            
            // Si on dépasse les bornes, générer une couleur aléatoire et l'ajouter à la grille
            Color randomColor = GenerateRandomColor(row, column);
            SetColor(row, column, randomColor);
            return randomColor;
        }
        public void SetColor(int row, int column, Color color)
        {
            EnsureSize(row + 1, column + 1);
            m_Colors[row, column] = new SerializableColor(color);
        }
        public void InitializeWithColors(Color[] colors)
        {
            int colorIndex = 0;
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    if (colorIndex < colors.Length)
                    {
                        m_Colors[r, c] = new SerializableColor(colors[colorIndex]);
                        colorIndex++;
                    }
                    else
                    {
                        m_Colors[r, c] = new SerializableColor(GenerateRandomColor(r, c));
                    }
                }
            }
        }
        public object Clone()
        {
            ColorGrid clone = new ColorGrid(Rows, Columns, m_Type);
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    clone.m_Colors[r, c] = m_Colors[r, c];
                }
            }
            return clone;
        }
        #endregion

        #region Private Methods
        private void EnsureSize(int minRows, int minColumns)
        {
            if (Rows < minRows || Columns < minColumns)
            {
                int newRows = Math.Max(Rows, minRows);
                int newColumns = Math.Max(Columns, minColumns);

                SerializableColor[,] newColors = new SerializableColor[newRows, newColumns];

                // Copier les couleurs existantes
                for (int r = 0; r < Rows; r++)
                {
                    for (int c = 0; c < Columns; c++)
                    {
                        newColors[r, c] = m_Colors[r, c];
                    }
                }

                // Initialiser les nouvelles couleurs avec des couleurs aléatoires
                for (int r = 0; r < newRows; r++)
                {
                    for (int c = 0; c < newColumns; c++)
                    {
                        if (r >= Rows || c >= Columns)
                        {
                            newColors[r, c] = new SerializableColor(GenerateRandomColor(r, c));
                        }
                    }
                }

                m_Colors = newColors;
            }
        }
        private Color GenerateRandomColor(int row, int column)
        {
            int baseSeed = m_Type switch
            {
                ColorGridType.Site => 2000000,
                ColorGridType.ROI => 3000000,
                ColorGridType.Group => 4000000,
                _ => 1000000
            };
            int seed = baseSeed + (row * 1000) + column;
            UnityEngine.Random.State oldState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(seed);
            Color randomColor = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
            UnityEngine.Random.state = oldState;
            return randomColor;
        }
        #endregion
    }
}