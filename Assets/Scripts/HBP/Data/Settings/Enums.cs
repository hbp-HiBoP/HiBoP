using System.ComponentModel;

namespace HBP.Data.Enums
{
    public enum AveragingType { Mean, Median }
    public enum NormalizationType { None, SubTrial, Trial, SubBloc, Bloc, Protocol }
    public enum SiteInfluenceByDistanceType { Constant, Linear, Quadratic }
    public enum BlocFormatType {[Description("Trial height")] TrialHeight, [Description("Trial ratio")] TrialRatio, [Description("Bloc ratio")] BlocRatio }
    public enum DisplayableError { LeftMeshEmpty, RightMeshEmpty, PreimplantationMRIEmpty, ImplantationEmpty }
    public enum SceneType { SinglePatient, MultiPatients };
    public enum ColumnType { Anatomic, iEEG }
    public enum CameraControl { Trackball, Orbital }
    public enum ColorType // For now, integers matter because of the link with the dll.
    {
        Grayscale = 0,
        Hot = 1,
        Winter = 2,
        Warm = 3,
        Surface = 4,
        Cool = 5,
        RedYellow = 6,
        BlueGreen = 7,
        ACTC = 8,
        Bone = 9,
        GEColor = 10,
        Gold = 11,
        XRain = 12,
        MatLab = 13,
        Default = 14,
        BrainColor = 15,
        White = 16,
        SoftGrayscale = 17
    }
    public enum CutOrientation { Axial = 0, Coronal = 1, Sagital = 2, Custom = 3 } // Same as above
    public enum SiteInformationDisplayMode { Anatomy, IEEG, CCEP, Light }
    public enum MeshPart { Left, Right, Both, None };
    public enum TriEraserMode { OneTri, Cylinder, Zone, Invert, Expand };
    public enum SiteNavigationDirection { Left, Right }
    public enum SiteType { Normal, Positive, Negative, Excluded, Source, NotASource, NoLatencyData, BlackListed, NonePos, NoneNeg, Marked, Suspicious };
    public enum MainSecondaryEnum { Main, Secondary }
    public enum CreationType { FromScratch, FromExistingItem, FromFile }
}