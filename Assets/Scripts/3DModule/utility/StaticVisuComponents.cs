

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
    public class StaticVisuComponents : MonoBehaviour
    {
        static public HBP_3DModule_Command HBPCommand = null;
        static public HBP_3DModule_Main HBPMain = null;
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

        void Awake()
        {
            HBPCommand = GetComponent<HBP_3DModule_Command>();
            HBPMain = GetComponent<HBP_3DModule_Main>();
            ScenesManager = transform.Find("Scene").GetComponent<ScenesManager>();
            SPScene = ScenesManager.transform.Find("SP").GetComponent<SP3DScene>();
            MPScene = ScenesManager.transform.Find("MP").GetComponent<MP3DScene>();

            Transform UI = transform.Find("UI");
            UIManager = UI.GetComponent<UIManager>();
            BackgroundCamera = UI.Find("background camera").GetComponent<Camera>();
            CanvasOverlay = UI.Find("canvas").Find("overlay");
            CanvasCamera = UI.Find("canvas").Find("camera");

            MouseSceneManager = transform.Find("Events").GetComponent<Cam.InputsSceneManager>();

            Transform managers = UI.Find("managers");
            UICameraManager = managers.Find("camera").GetComponent<UICameraManager>();
            UIOverlayManager = managers.Find("overlay").GetComponent<UIOverlayManager>();
        }
    }
}