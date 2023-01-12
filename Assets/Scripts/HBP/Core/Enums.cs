using System.ComponentModel;

namespace HBP.Core.Enums
{
    public enum AveragingType { Mean, Median }
    public enum NormalizationType { None, SubTrial, Trial, SubBloc, Bloc, Protocol, Auto }
    public enum SiteInfluenceByDistanceType { Constant, Linear, Quadratic }
    public enum BlocFormatType {[Description("Trial height")] TrialHeight, [Description("Trial ratio")] TrialRatio, [Description("Bloc ratio")] BlocRatio }
    public enum DisplayableError { LeftMeshEmpty, RightMeshEmpty, PreimplantationMRIEmpty, ImplantationEmpty }
    public enum SceneType { SinglePatient, MultiPatients }
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
    public enum CutOrientation { Axial = 0, Coronal = 1, Sagittal = 2, Custom = 3 } // Same as above
    public enum SiteInformationDisplayMode { Anatomy, IEEG, CCEP, IEEGCCEP, Light }
    public enum MeshPart { Left, Right, Both, None };
    public enum MeshType { Patient, MNI }
    public enum TriEraserMode { OneTri, Cylinder, Zone, Invert, Expand };
    public enum SiteNavigationDirection { Left, Right }
    public enum SiteType { Normal, Positive, Negative, Source, NotASource, BlackListed, NonePos, NoneNeg };
    public enum MainSecondaryEnum { Main, Secondary }
    public enum CreationType { FromScratch, FromExistingObject, FromFile, FromDatabase, FromDirectory }
    public enum RaycastHitResult { None, Cut, Mesh, Site, ROI }
    public enum LayoutDirection { Horizontal, Vertical }
}