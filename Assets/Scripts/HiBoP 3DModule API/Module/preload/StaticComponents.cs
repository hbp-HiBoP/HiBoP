

/* \file StaticVisuComponents.cs
 * \author Lance Florian
 * \date    29/04/2016
 * \brief Define StaticVisuComponents
 */

// system
using System;
using System.Text;

// unity
using UnityEngine;

namespace HBP.VISU3D
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
        static public SP3DScene SPScene = null;
        static public MP3DScene MPScene = null;

        static public Camera BackgroundCamera = null;
        static public UIManager UIManager = null;
        static public UICameraManager UICameraManager = null;
        static public UIOverlayManager UIOverlayManager = null;

        static public Transform CanvasOverlay = null;
        static public Transform CanvasCamera = null;
        
        static public Cam.InputsSceneManager MouseSceneManager = null;
        static public DLL.DLLDebugManager DLLDebugManager = null;
        //static public QtWidgets Qt = null;

        void Awake()
        {
            DLLDebugManager = GetComponent<DLL.DLLDebugManager>();

            HBPCommand = GetComponent<HiBoP_3DModule_API>();
            HBPMain = GetComponent<HiBoP_3DModule_Main>();
            ScenesManager = transform.Find("Scenes").GetComponent<ScenesManager>();
            SPScene = ScenesManager.transform.Find("SP").GetComponent<SP3DScene>();
            MPScene = ScenesManager.transform.Find("MP").GetComponent<MP3DScene>();

            Transform UI = transform.Find("UI");
            UIManager = UI.GetComponent<UIManager>();
            BackgroundCamera = UI.Find("Background camera").GetComponent<Camera>();
            CanvasOverlay = UI.Find("canvas").Find("overlay");
            CanvasCamera = UI.Find("canvas").Find("camera");

            MouseSceneManager = transform.Find("Scenes").GetComponent<Cam.InputsSceneManager>();

            Transform managers = UI.Find("managers");
            UICameraManager = managers.Find("camera").GetComponent<UICameraManager>();
            UIOverlayManager = managers.Find("overlay").GetComponent<UIOverlayManager>();
        }
    }
}