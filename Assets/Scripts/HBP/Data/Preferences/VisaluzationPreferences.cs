using System;
using System.Runtime.Serialization;

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
        [DataMember] public Enums.SiteInfluenceByDistanceType SiteInfluenceByDistance { get; set; }
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
            Enums.SiteInfluenceByDistanceType siteInfluenceByDistance = Enums.SiteInfluenceByDistanceType.Quadratic,
            string defaultSelectedMRIInSinglePatientVisualization = "Preimplantation",
            string defaultSelectedMeshInSinglePatientVisualization = "Grey matter",
            string defaultSelectedImplantationInSinglePatientVisualization = "Patient",
            string defaultSelectedMRIInMultiPatientsVisualization = "MNI",
            string defaultSelectedMeshInMultiPatientsVisualization = "MNI Grey matter",
            string defaultSelectedImplantationInMultiPatientsVisualization = "MNI")
        {
            AutomaticEEGUpdate = automaticEEGUpdate;
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
            return new _3DPreferences(AutomaticEEGUpdate, SiteInfluenceByDistance, DefaultSelectedMRIInSinglePatientVisualization, DefaultSelectedMeshInSinglePatientVisualization, DefaultSelectedImplantationInSinglePatientVisualization, DefaultSelectedMRIInMultiPatientsVisualization, DefaultSelectedMeshInMultiPatientsVisualization, DefaultSelectedImplantationInMultiPatientsVisualization);
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
        [DataMember] public Enums.BlocFormatType SubBlocFormat { get; set; }
        [DataMember] public int TrialHeight { get; set; }
        [DataMember] public float TrialRatio { get; set; }
        [DataMember] public float BlocRatio { get; set; }
        #endregion

        #region Constructors
        public TrialMatrixPreferences(bool showWholeProtocol = false, bool trialsSynchronization = true, bool trialSmooting = true,
            int numberOfIntermediateValues = 3, Enums.BlocFormatType subBlocFormat = Enums.BlocFormatType.BlocRatio,
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
        [DataMember] public bool ShowCurvesOfMinimizedColumns { get; set; }
        #endregion

        #region Constructors
        public GraphPreferences(bool showCurvesOfMinimizedColumns = false)
        {
            ShowCurvesOfMinimizedColumns = showCurvesOfMinimizedColumns;
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new GraphPreferences(ShowCurvesOfMinimizedColumns);
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