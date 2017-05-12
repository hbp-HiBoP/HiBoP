/* \file StaticVisuComponents.cs
 * \author Lance Florian - Adrien Gannerie
 * \date    29/04/2016 - 2017
 * \brief Define StaticVisuComponents
 */
using UnityEngine;
using HBP.UI.Module3D;

namespace HBP.Module3D
{
    /// <summary>
    /// Paths to different directories, whether we are working in the release or in the editor
    /// </summary>
    public class GlobalPaths
    {
        #if UNITY_EDITOR
        static public string Data = Application.dataPath + "/Data/";
        #else
        static public string Data = Application.dataPath + "/../Data/";
        #endif
    }

    /// <summary>
    /// Commodity access to main classes of the program
    /// </summary>
    public class StaticComponents : MonoBehaviour
    {
        #region Properties
        static public HBP3DModule HBP3DModule = null;
        static public ScenesManager ScenesManager = null;
        static public DLL.DLLDebugManager DLLDebugManager = null;
        #endregion

        #region Private Methods
        void Awake()
        {
            HBP3DModule = GetComponent<HBP3DModule>();
            ScenesManager = GetComponentInChildren<ScenesManager>();
            DLLDebugManager = GetComponent<DLL.DLLDebugManager>();
        }
        #endregion
    }
}