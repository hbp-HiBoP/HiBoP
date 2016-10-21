/// <summary>
/// Class which define the state of the application.
///     - Project loaded.
///     - Location of the projet loaded.
///     - General settings.
/// </summary>
public static class ApplicationState
{
    /// <summary>
    /// Project loaded on the application.
    /// </summary>
    public static HBP.Data.General.Project ProjectLoaded;

    /// <summary>
    /// Location of the project loaded.
    /// </summary>
    public static string ProjectLoadedLocation;

    /// <summary>
    /// General settings of the application.
    /// </summary>
    public static HBP.Data.Settings.GeneralSettings GeneralSettings;

    /// <summary>
    /// Skin of the application.
    /// </summary>
    public static HBP.UI.Skin.Skin Skin;
}
