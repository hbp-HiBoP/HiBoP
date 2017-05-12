﻿public enum SceneType { SinglePatient, MultiPatients };
public enum CameraType { EEG, fMRI };
public enum ColorType // For now, integers matter because of the link with the dll. FIXME or don't
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
public enum SiteFilter
{
    Specific,
    Electrode,
    Patient,
    Highlighted,
    Unhighlighted,
    All,
    InRegionOfInterest,
    OutOfRegionOfInterest,
    Name,
    MarsAtlas,
    Broadman
}
public enum SiteAction
{
    Include,
    Exclude,
    Blacklist,
    Unblacklist,
    Highlight,
    Unhighlight,
    Mark,
    Unmark
}