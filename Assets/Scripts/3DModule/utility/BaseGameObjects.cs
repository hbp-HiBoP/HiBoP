

/* \file BaseGameObjects.cs
 * \author Lance Florian
 * \date    22/04/2016
 * \brief Define BaseGameObjects
 */

// system
using System;
using System.Text;

// unity
using UnityEngine;

namespace HBP.VISU3D
{
    public class BaseGameObjects : MonoBehaviour
    {
        static public GameObject ROIBubble = null;
        static public GameObject ColormapDisplay = null;
        static public GameObject TimeDisplay = null;
        static public GameObject CutImageOverlay = null;
        static public GameObject IconeDisplay = null;
        static public GameObject MinimizeDisplay = null;
        static public GameObject SetPlanePanel = null;
        static public GameObject ChoosePlanePanel = null;
        static public GameObject Timeline = null;
        static public GameObject IEEGLeftMenu = null;
        static public GameObject ROILeftMenu = null;
        static public GameObject IRMFLeftMenu = null;
        static public GameObject PlotLeftMenu = null;
        static public GameObject SceneLeftMenu = null;
        static public GameObject Plot = null;
        static public GameObject Cut = null;
        static public GameObject Brain = null;
        static public GameObject Ring = null;
        static public GameObject LineViews = null;
        static public GameObject View = null;
        static public GameObject ScreenMessage = null;
        static public GameObject ScreenProgressBar = null;
        static public GameObject SPCamera = null;
        static public GameObject MPCamera = null;

        void Awake()
        {
            // ROIBubble
            ROIBubble = Instantiate(Resources.Load("Prefabs/scene objects/ROI bubble", typeof(GameObject))) as GameObject;
            initObject(ROIBubble, "ROI Bubble");

            // Colormap display
            ColormapDisplay = Instantiate(Resources.Load("Prefabs/ui/overlay/column colormap", typeof(GameObject))) as GameObject;
            initObject(ColormapDisplay, "Colormap Display");

            // Colormap display
            TimeDisplay = Instantiate(Resources.Load("Prefabs/ui/overlay/time display", typeof(GameObject))) as GameObject;
            initObject(TimeDisplay, "Time Display");
            
            // Cut image overlay
            CutImageOverlay = Instantiate(Resources.Load("Prefabs/ui/overlay/image_cut", typeof(GameObject))) as GameObject;
            initObject(CutImageOverlay, "Image cut overlay");

            // Icone display
            IconeDisplay = Instantiate(Resources.Load("Prefabs/ui/overlay/icone", typeof(GameObject))) as GameObject;
            initObject(IconeDisplay, "Icone display");

            // Minimize display
            MinimizeDisplay = Instantiate(Resources.Load("Prefabs/ui/overlay/minimize", typeof(GameObject))) as GameObject;
            initObject(MinimizeDisplay, "Minimize display");

            // Set plane panel
            SetPlanePanel = Instantiate(Resources.Load("Prefabs/ui/overlay/set plane", typeof(GameObject))) as GameObject;
            initObject(SetPlanePanel, "Set plane panel");

            // Choose plane panel
            ChoosePlanePanel = Instantiate(Resources.Load("Prefabs/ui/overlay/choose plane", typeof(GameObject))) as GameObject;
            initObject(ChoosePlanePanel, "Choose plane panel");

            // Timeline
            Timeline = Instantiate(Resources.Load("Prefabs/ui/overlay/timeline", typeof(GameObject))) as GameObject;
            initObject(Timeline, "Tmeline");

            // IEEG left menu
            IEEGLeftMenu = Instantiate(Resources.Load("Prefabs/ui/camera/iEEG left menu", typeof(GameObject))) as GameObject;
            initObject(IEEGLeftMenu, "IEEG left menu");

            // General left menu
            SceneLeftMenu = Instantiate(Resources.Load("Prefabs/ui/camera/Scene left menu", typeof(GameObject))) as GameObject;
            initObject(SceneLeftMenu, "Scene left menu");
            
            // Plot left menu
            PlotLeftMenu = Instantiate(Resources.Load("Prefabs/ui/camera/Plot left menu", typeof(GameObject))) as GameObject;
            initObject(PlotLeftMenu, "Plot left menu");            

            // ROI left menu
            ROILeftMenu = Instantiate(Resources.Load("Prefabs/ui/camera/ROI left menu", typeof(GameObject))) as GameObject;
            initObject(ROILeftMenu, "ROI left menu");

            // IRMF left menu
            IRMFLeftMenu = Instantiate(Resources.Load("Prefabs/ui/camera/IRMF left menu", typeof(GameObject))) as GameObject;
            initObject(IRMFLeftMenu, "IRMF left menu");
            
            // Plot
            Plot = Instantiate(Resources.Load("Prefabs/scene objects/Plot", typeof(GameObject))) as GameObject;
            initObject(Plot, "Plot");

            // Brain
            Brain = Instantiate(Resources.Load("Prefabs/scene objects/Brain", typeof(GameObject))) as GameObject;
            Brain.GetComponent<MeshFilter>().mesh.MarkDynamic();
            initObject(Brain, "Brain");

            // Ring
            Ring = Instantiate(Resources.Load("Prefabs/scene objects/Ring", typeof(GameObject))) as GameObject;
            initObject(Ring, "Ring");
            
            // Cut
            Cut = Instantiate(Resources.Load("Prefabs/scene objects/Cut", typeof(GameObject))) as GameObject;
            Cut.GetComponent<MeshFilter>().mesh.MarkDynamic();
            initObject(Cut, "Cut");

            // Line views panel
            LineViews = Instantiate(Resources.Load("Prefabs/ui/camera/Camera line views panel", typeof(GameObject))) as GameObject;
            initObject(LineViews, "LineViews");

            // View panel
            View = Instantiate(Resources.Load("Prefabs/ui/camera/Camera view panel", typeof(GameObject))) as GameObject;
            initObject(View, "View");

            // Screen message panel
            ScreenMessage = Instantiate(Resources.Load("Prefabs/ui/overlay/screen message panel", typeof(GameObject))) as GameObject;            
            initObject(ScreenMessage, "Screen message");

            // Screen progress bar panel
            ScreenProgressBar = Instantiate(Resources.Load("Prefabs/ui/overlay/progressbar", typeof(GameObject))) as GameObject;
            initObject(ScreenProgressBar, "Screen Progress Bar");

            // SP camera
            SPCamera = Instantiate(Resources.Load("Prefabs/Cameras/SPCamera", typeof(GameObject))) as GameObject;
            initObject(SPCamera, "SP Camera");

            // MP camera
            MPCamera = Instantiate(Resources.Load("Prefabs/Cameras/MPCamera", typeof(GameObject))) as GameObject;
            initObject(MPCamera, "MP Camera");
        }


        private void initObject(GameObject baseGameObject, string objectName)
        {
            baseGameObject.name = objectName;
            baseGameObject.transform.SetParent(transform);
            baseGameObject.SetActive(false);
        } 
    }
}