﻿/**
* \class ApplicationState
* \author Adrien Gannerie
* \version 1.0
* \date 12 janvier 2017
* \brief Class which define the state of the application.
* 
* \details Class which define the state of the application like the project loaded, the current project location, the general settings and the UI skin.
*/
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
    /// Theme of the application.
    /// </summary>
    public static HBP.UI.Theme.Theme Theme;

    public static CoroutineManager CoroutineManager;

    public static HBP.Module3D.HBP3DModule Module3D;
}
