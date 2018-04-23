using System.Runtime.Serialization;

[DataContract]
public struct VisaluzationPreferences
{
    [DataMember] public _3DPreferences _3D { get; set; }
    [DataMember] public TrialMatrixPreferences TrialMatrix { get; set; }
    [DataMember] public GraphPreferences Graph { get; set; }
    [DataMember] public CutPreferences Cut { get; set; }
}

[DataContract]
public struct _3DPreferences
{
    [DataMember] public bool AutomaticEEGUpdate { get; set; }
    [DataMember] public SiteInfluenceType SiteInfluence { get; set; }
    [DataMember] public string DefaultSelectedMRI { get; set; }
    [DataMember] public string DefaultSelectedMesh { get; set; }
    [DataMember] public string DefaultSelectedImplantation { get; set; }
}

[DataContract]
public struct TrialMatrixPreferences
{
    public enum BlocFormatType { ConstantLine, LineRatio, BlocRatio }
    [DataMember] public bool ShowWholeProtocol { get; set; }
    [DataMember] public bool TrialSynchronization { get; set; }
    [DataMember] public bool SmoothLine { get; set; }
    [DataMember] public int NumberOfIntermediateValues { get; set; }
    [DataMember] public BlocFormatType BlocFormat { get; set; }
    [DataMember] public int LineHeight { get; set; }
    [DataMember] public float LineRatio { get; set; }
    [DataMember] public float BlocRatio { get; set; }
}

[DataContract]
public struct GraphPreferences
{
    [DataMember] public bool ShowCurvesOfMinimizedColumns { get; set; }
}

[DataContract]
public struct CutPreferences
{
    [DataMember] public bool ShowCutLines { get; set; }
    [DataMember] public bool SimplifiedMeshes { get; set; }
}