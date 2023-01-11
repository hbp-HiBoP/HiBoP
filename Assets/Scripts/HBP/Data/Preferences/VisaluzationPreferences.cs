using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using HBP.Core.Enums;
using HBP.Core.Data;

namespace HBP.Data.Preferences
{
    [DataContract]
    public class VisualizationPreferences : ICloneable
    {
        #region Properties
        [DataMember] public _3DPreferences _3D { get; set; }
        [DataMember] public TrialMatrixPreferences TrialMatrix { get; set; }
        [DataMember] public GraphPreferences Graph { get; set; }
        [DataMember] public CutPreferences Cut { get; set; }
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

    [DataContract]
    public class _3DPreferences : ICloneable
    {
        #region Properties
        [DataMember] public bool AutomaticEEGUpdate { get; set; }
        [DataMember] public bool RawCuts { get; set; }
        [DataMember] public LayoutDirection VisualizationsLayoutDirection { get; set; }
        [DataMember] public SiteInfluenceByDistanceType SiteInfluenceByDistance { get; set; }
        [DataMember] public string DefaultSelectedMRIInSinglePatientVisualization { get; set; }
        [DataMember] public string DefaultSelectedMeshInSinglePatientVisualization { get; set; }
        [DataMember] public string DefaultSelectedImplantationInSinglePatientVisualization { get; set; }
        [DataMember] public string DefaultSelectedMRIInMultiPatientsVisualization { get; set; }
        [DataMember] public string DefaultSelectedMeshInMultiPatientsVisualization { get; set; }
        [DataMember] public string DefaultSelectedImplantationInMultiPatientsVisualization { get; set; }
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

    [DataContract]
    public class TrialMatrixPreferences : ICloneable
    {
        #region Properties
        public const int MINIMUM_TRIAL_HEIGHT = 5;
        public const int MAXIMUM_TRIAL_HEIGHT = 50;
        public const float MINIMUM_TRIAL_RATIO = 0.02f;
        public const float MAXIMUM_TRIAL_RATIO = 0.2f;
        public const float MINIMUM_BLOC_RATIO = 0.05f;
        public const float MAXIMUM_BLOC_RATIO = 1.0f;
        [DataMember] public bool ShowWholeProtocol { get; set; }
        [DataMember] public bool TrialsSynchronization { get; set; }
        [DataMember] public bool TrialSmoothing { get; set; }
        [DataMember] public int NumberOfIntermediateValues { get; set; }
        [DataMember] public BlocFormatType SubBlocFormat { get; set; }
        [DataMember] public int TrialHeight { get; set; }
        [DataMember] public float TrialRatio { get; set; }
        [DataMember] public float BlocRatio { get; set; }
        #endregion

        #region Constructors
        public TrialMatrixPreferences(bool showWholeProtocol = false, bool trialsSynchronization = true, bool trialSmooting = true,
            int numberOfIntermediateValues = 3, BlocFormatType subBlocFormat = BlocFormatType.BlocRatio,
            int trialHeight = (int)(0.3f * (MAXIMUM_TRIAL_HEIGHT - MINIMUM_TRIAL_HEIGHT)), float trialRatio = 0.3f * (MAXIMUM_TRIAL_RATIO - MINIMUM_TRIAL_RATIO), float blocRatio = 0.3f * (MAXIMUM_BLOC_RATIO - MINIMUM_BLOC_RATIO))
        {
            ShowWholeProtocol = showWholeProtocol;
            TrialsSynchronization = trialsSynchronization;
            TrialSmoothing = trialSmooting;
            NumberOfIntermediateValues = numberOfIntermediateValues;
            SubBlocFormat = subBlocFormat;
            TrialHeight = trialHeight;
            TrialRatio = trialRatio;
            BlocRatio = blocRatio;
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new TrialMatrixPreferences(ShowWholeProtocol, TrialsSynchronization, TrialSmoothing, NumberOfIntermediateValues, SubBlocFormat, TrialHeight, TrialRatio, BlocRatio);
        }
        #endregion
    }

    [DataContract]
    public class GraphPreferences : ICloneable
    {
        #region Properties
        [IgnoreDataMember] const int NUMBER_OF_COLORS = 24;
        [DataMember] public bool ShowCurvesOfMinimizedColumns { get; set; }
        [DataMember] private SerializableColor[] m_Colors;
        public Color[] Colors
        {
            get
            {
                return m_Colors.Select(c => c.ToColor()).ToArray();
            }
            set
            {
                if (value.Length == NUMBER_OF_COLORS)
                {
                    m_Colors = value.Select(c => new SerializableColor(c)).ToArray();
                }
            }
        }
        [IgnoreDataMember] private Dictionary<int, Color> m_AdditionalColors = new Dictionary<int, Color>();
        #endregion

        #region Constructors
        public GraphPreferences(bool showCurvesOfMinimizedColumns = false, Color[] colors = null)
        {
            ShowCurvesOfMinimizedColumns = showCurvesOfMinimizedColumns;
            if (colors == null || colors.Length != NUMBER_OF_COLORS)
                SetDefaultColors();
            else
                Colors = colors;
        }
        #endregion

        #region Public Methods
        public void SetDefaultColors()
        {
            Colors = new Color[NUMBER_OF_COLORS]
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
                    new Color(129f / 255f, 71f / 255f, 22f / 255f),
                    new Color(129f / 255f, 129f / 255f, 22f / 255f),
                    new Color(13f / 255f, 77f / 255f, 77f / 255f),
                    new Color(62f / 255f, 19f / 255f, 86f / 255f),
                    new Color(129f / 255f, 93f / 255f, 22f / 255f),
                    new Color(87f / 255f, 120f / 255f, 21f / 255f),
                    new Color(23f / 255f, 42f / 255f, 86f / 255f),
                    new Color(103f / 255f, 18f / 255f, 66f / 255f)
                };
        }
        public void SetColor(int position, Color color)
        {
            if (position >= 0 && position < 24)
            {
                m_Colors[position] = new SerializableColor(color);
            }
        }
        public Color GetColor(int position)
        {
            if (position >= 0 && position < 24)
            {
                return m_Colors[position].ToColor();
            }
            else
            {
                if (m_AdditionalColors.TryGetValue(position, out Color color))
                {
                    return color;
                }
                else
                {
                    color = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
                    m_AdditionalColors.Add(position, color);
                    return color;
                }
            }
        }
        public Color GetColor(int row, int column)
        {
            int position = row * 8 + column;
            return GetColor(position);
        }
        public object Clone()
        {
            return new GraphPreferences(ShowCurvesOfMinimizedColumns, Colors);
        }
        #endregion
    }

    [DataContract]
    public class CutPreferences : ICloneable
    {
        #region Properties
        [DataMember] public bool ShowCutLines { get; set; }
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
}