
using UnityEngine;
/**
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
    /// ID of this instance of HiBoP
    /// </summary>
    public static string InstanceID { get; private set; } = System.Guid.NewGuid().ToString();

    /// <summary>
    /// Project loaded on the application.
    /// </summary>
    public static HBP.Data.General.Project ProjectLoaded { get; set; }

    /// <summary>
    /// Location of the project loaded.
    /// </summary>
    public static string ProjectLoadedLocation { get; set; }

    /// <summary>
    /// TMP folder to store the open projects
    /// </summary>
    public static string ProjectTMPFolder { get; set; }

    /// <summary>
    /// Full path to the loaded project
    /// </summary>
    public static string ProjectLoadedTMPFullPath
    {
        get
        {
            return System.IO.Path.Combine(ProjectTMPFolder, InstanceID);
        }
    }

    /// <summary>
    /// General settings of the application.
    /// </summary>
    public static HBP.Data.Preferences.UserPreferences UserPreferences { get; set; }

    /// <summary>
    /// Coroutine manager.
    /// </summary>
    public static CoroutineManager CoroutineManager { get; set; }

    /// <summary>
    /// 3D Module manager.
    /// </summary>
    public static HBP.Module3D.HBP3DModule Module3D { get; set; }

    /// <summary>
    /// UI for the 3D Module manager.
    /// </summary>
    public static HBP.UI.Module3D.Module3DUI Module3DUI { get; set; }

    /// <summary>
    /// 3D DLL Debug Manager.
    /// </summary>
    public static HBP.Module3D.DLL.DLLDebugManager DLLDebugManager { get; set; }

    /// <summary>
    /// Dialog box manager.
    /// </summary>
    public static Tools.Unity.DialogBoxManager DialogBoxManager { get; set; }

    /// <summary>
    /// Loading circle manager.
    /// </summary>
    public static LoadingManager LoadingManager { get; set; }

    /// <summary>
    /// Tooltip manager
    /// </summary>
    public static Tools.Unity.TooltipManager TooltipManager { get; set; }

    /// <summary>
    /// Memory manager.
    /// </summary>
    public static Tools.Unity.MemoryManager MemoryManager { get; set; }

    /// <summary>
    /// Windows manager.
    /// </summary>
    public static HBP.UI.WindowsManager WindowsManager { get; set; }

    static public string DataPath
    {
        get
        {
#if UNITY_EDITOR
            return Application.dataPath + "/Data/";
#else
            return Application.dataPath +  "/../Data/";
#endif

        }
    }
}