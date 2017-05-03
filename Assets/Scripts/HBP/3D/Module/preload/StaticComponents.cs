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
        #region Properties
        static public HBP3DModule HBPCommand = null;
        static public ScenesManager ScenesManager = null;
        static public SinglePatient3DScene SinglePatientScene = null;
        static public MultiPatients3DScene MultiPatientsScene = null;
        static public UIManager UIManager = null;
        static public MenuManager UICameraManager = null;
        static public UIOverlayManager UIOverlayManager = null;     
        static public Cam.InputsSceneManager InputsSceneManager = null;
        static public DLL.DLLDebugManager DLLDebugManager = null;
        static public Transform CanvasOverlay = null;
        #endregion

        #region Private Methods
        void Awake()
        {
            DLLDebugManager = GetComponent<DLL.DLLDebugManager>();

            HBPCommand = GetComponent<HBP3DModule>();
            ScenesManager = transform.Find("Scenes").GetComponent<ScenesManager>();
            SinglePatientScene = ScenesManager.transform.Find("SP").GetComponent<SinglePatient3DScene>();
            MultiPatientsScene = ScenesManager.transform.Find("MP").GetComponent<MultiPatients3DScene>();

            Transform Visualisation = GameObject.Find("Brain Visualisation").transform;
            UIManager = Visualisation.GetComponent<UIManager>();
            CanvasOverlay = Visualisation.FindChild("Overlay");
            InputsSceneManager = transform.Find("Scenes").GetComponent<Cam.InputsSceneManager>();
            UICameraManager = Visualisation.FindChild("Mid").GetComponent<MenuManager>();
            UIOverlayManager = CanvasOverlay.GetComponent<UIOverlayManager>();
        }
        #endregion
    }
}