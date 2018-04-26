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
        [DataMember] public SiteInfluenceType SiteInfluence { get; set; }
        [DataMember] public string DefaultSelectedMRI { get; set; }
        [DataMember] public string DefaultSelectedMesh { get; set; }
        [DataMember] public string DefaultSelectedImplantation { get; set; }
    }

    [DataContract]
    public class TrialMatrixPreferences
    {
        public enum BlocFormatType { HeightLine, LineRatio, BlocRatio }
        [DataMember] public bool ShowWholeProtocol { get; set; }
        [DataMember] public bool TrialsSynchronization { get; set; }
        [DataMember] public bool SmoothLine { get; set; }
        [DataMember] public int NumberOfIntermediateValues { get; set; }
        [DataMember] public BlocFormatType BlocFormat { get; set; }
        [DataMember] public int LineHeight { get; set; }
        [DataMember] public float LineRatio { get; set; }
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
        [DataMember] public bool SimplifiedMeshes { get; set; }
    }
}