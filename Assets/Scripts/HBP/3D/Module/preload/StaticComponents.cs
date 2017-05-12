/* \file StaticVisuComponents.cs
 * \author Lance Florian - Adrien Gannerie
 * \date    29/04/2016 - 2017
 * \brief Define StaticVisuComponents
 */
using UnityEngine;
using HBP.UI.Module3D;

namespace HBP.Module3D
{
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
        static public HiBoP_3DModule_API HBPCommand = null;
        static public HiBoP_3DModule_Main HBPMain = null;
        static public ScenesManager ScenesManager = null;
        static public SP3DScene SinglePatientScene = null;
        static public MP3DScene MultiPatientsScene = null;
        static public UIManager UIManager = null;
        static public MenuManager UICameraManager = null;
        static public UIOverlayManager UIOverlayManager = null;     
        static public Cam.InputsSceneManager InputsSceneManager = null;
        static public DLL.DLLDebugManager DLLDebugManager = null;
        static public Transform CanvasOverlay = null;

        void Awake()
        {
            DLLDebugManager = GetComponent<DLL.DLLDebugManager>();

            HBPCommand = GetComponent<HiBoP_3DModule_API>();
            HBPMain = GetComponent<HiBoP_3DModule_Main>();
            ScenesManager = transform.Find("Scenes").GetComponent<ScenesManager>();
            SinglePatientScene = ScenesManager.transform.Find("SP").GetComponent<SP3DScene>();
            MultiPatientsScene = ScenesManager.transform.Find("MP").GetComponent<MP3DScene>();

            Transform Visualization = GameObject.Find("Brain Visualization").transform;
            UIManager = Visualization.GetComponent<UIManager>();
            CanvasOverlay = Visualization.FindChild("Overlay");
            InputsSceneManager = transform.Find("Scenes").GetComponent<Cam.InputsSceneManager>();
            UICameraManager = Visualization.FindChild("Mid").GetComponent<MenuManager>();
            UIOverlayManager = CanvasOverlay.GetComponent<UIOverlayManager>();
        }
    }
}