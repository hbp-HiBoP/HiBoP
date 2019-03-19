using System.Runtime.Serialization;

namespace HBP.Data.Preferences
{
    [DataContract]
    public class VisualizationPreferences
    {
        #region Properties
        [DataMember] public _3DPreferences _3D { get; set; }
        [DataMember] public TrialMatrixPreferences TrialMatrix { get; set; }
        [DataMember] public GraphPreferences Graph { get; set; }
        [DataMember] public CutPreferences Cut { get; set; }
        #endregion

        #region Constructors
        public VisualizationPreferences()
        {
            _3D = new _3DPreferences();
            TrialMatrix = new TrialMatrixPreferences();
            Graph = new GraphPreferences();
            Cut = new CutPreferences();
        }
        #endregion
    }

    [DataContract]
    public class _3DPreferences
    {
        [DataMember] public bool AutomaticEEGUpdate { get; set; }
        [DataMember] public Enums.SiteInfluenceByDistanceType SiteInfluenceByDistance { get; set; }
        [DataMember] public string DefaultSelectedMRIInSinglePatientVisualization { get; set; }
        [DataMember] public string DefaultSelectedMeshInSinglePatientVisualization { get; set; }
        [DataMember] public string DefaultSelectedImplantationInSinglePatientVisualization { get; set; }
        [DataMember] public string DefaultSelectedMRIInMultiPatientsVisualization { get; set; }
        [DataMember] public string DefaultSelectedMeshInMultiPatientsVisualization { get; set; }
        [DataMember] public string DefaultSelectedImplantationInMultiPatientsVisualization { get; set; }

        public _3DPreferences()
        {
            AutomaticEEGUpdate = true;
            SiteInfluenceByDistance = Enums.SiteInfluenceByDistanceType.Quadratic;
            DefaultSelectedMRIInSinglePatientVisualization = "Preimplantation";
            DefaultSelectedMeshInSinglePatientVisualization = "Grey matter";
            DefaultSelectedImplantationInSinglePatientVisualization = "Patient";
            DefaultSelectedMRIInMultiPatientsVisualization = "MNI";
            DefaultSelectedMeshInMultiPatientsVisualization = "MNI Grey matter";
            DefaultSelectedImplantationInMultiPatientsVisualization = "MNI";
        }
    }

    [DataContract]
    public class TrialMatrixPreferences
    {
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
    }

    [DataContract]
    public class GraphPreferences
    {
        [DataMember] public bool ShowCurvesOfMinimizedColumns { get; set; }
    }

    [DataContract]
    public class CutPreferences
    {
        [DataMember] public bool ShowCutLines { get; set; }
        //[DataMember] public bool SimplifiedMeshes { get; set; }
    }
}