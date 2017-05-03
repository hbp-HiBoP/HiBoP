

/* \file GlobalGOPreloaded.cs
 * \author Lance Florian
 * \date    22/04/2016
 * \brief Define GlobalGOPreloaded
 */

// system
using System;
using System.Text;

// unity
using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// Preloaded GO to be instancied later
    /// </summary>
    public class GlobalGOPreloaded : MonoBehaviour
    {
        public enum Cursors : int
        {
            normal, roiNew, roiSelect, triErasing
        }; 

        public Texture2D cursorTriErasing = null;
        public Texture2D cursorRoiSelect = null;
        public Texture2D cursorRoiNew = null;
        public Texture2D cursorNormal = null;

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
        static public GameObject globalLeftMenu = null;
        static public GameObject ROILeftMenu = null;
        static public GameObject fMRILeftMenu = null;
        static public GameObject SiteLeftMenu = null;
        static public GameObject SceneLeftMenu = null;
        static public GameObject Plot = null;
        static public GameObject Cut = null;
        static public GameObject Brain = null;
        static public GameObject SelectRing = null;
        static public GameObject Line = null;
        static public GameObject View = null;
        static public GameObject ScreenMessage = null;
        static public GameObject ScreenProgressBar = null;
        static public GameObject SinglePatientCamera = null;
        static public GameObject MultiPatientsCamera = null;

        static public GameObject InvisibleBrain = null;

        static public DLL.MarsAtlasIndex MarsAtlasIndex = null;

        void Awake()
        {
            // ROIBubble
            ROIBubble = Instantiate(Resources.Load("Prefabs/scene objects/ROI bubble", typeof(GameObject))) as GameObject;
            init_GO(ROIBubble, "ROI Bubble");

            // Colormap display
            ColormapDisplay = Instantiate(Resources.Load("Prefabs/ui/overlay/column colormap", typeof(GameObject))) as GameObject;
            init_GO(ColormapDisplay, "Colormap Display");

            // Colormap display
            TimeDisplay = Instantiate(Resources.Load("Prefabs/ui/overlay/time display", typeof(GameObject))) as GameObject;
            init_GO(TimeDisplay, "Time Display");
            
            // Cut image overlay
            CutImageOverlay = Instantiate(Resources.Load("Prefabs/ui/overlay/image_cut", typeof(GameObject))) as GameObject;
            init_GO(CutImageOverlay, "Image cut overlay");

            // Icone display
            IconeDisplay = Instantiate(Resources.Load("Prefabs/ui/overlay/icone", typeof(GameObject))) as GameObject;
            init_GO(IconeDisplay, "Icone display");

            // Minimize display
            MinimizeDisplay = Instantiate(Resources.Load("Prefabs/ui/overlay/minimize", typeof(GameObject))) as GameObject;
            init_GO(MinimizeDisplay, "Minimize display");

            // Set plane panel
            SetPlanePanel = Instantiate(Resources.Load("Prefabs/ui/overlay/set plane", typeof(GameObject))) as GameObject;
            init_GO(SetPlanePanel, "Set plane panel");

            // Choose plane panel
            ChoosePlanePanel = Instantiate(Resources.Load("Prefabs/ui/overlay/choose plane", typeof(GameObject))) as GameObject;
            init_GO(ChoosePlanePanel, "Choose plane panel");

            // Timeline
            Timeline = Instantiate(Resources.Load("Prefabs/ui/overlay/timeline", typeof(GameObject))) as GameObject;
            init_GO(Timeline, "Tmeline");

            // IEEG left menu
            IEEGLeftMenu = Instantiate(Resources.Load("Prefabs/ui/camera/iEEG left menu", typeof(GameObject))) as GameObject;
            init_GO(IEEGLeftMenu, "IEEG left menu");

            // General left menu
            SceneLeftMenu = Instantiate(Resources.Load("Prefabs/ui/camera/Scene left menu", typeof(GameObject))) as GameObject;
            init_GO(SceneLeftMenu, "Scene left menu");
            
            // Site left menu
            SiteLeftMenu = Instantiate(Resources.Load("Prefabs/ui/camera/Site left menu", typeof(GameObject))) as GameObject;
            init_GO(SiteLeftMenu, "Site left menu");            

            // ROI left menu
            ROILeftMenu = Instantiate(Resources.Load("Prefabs/ui/camera/ROI left menu", typeof(GameObject))) as GameObject;
            init_GO(ROILeftMenu, "ROI left menu");

            // fMRI left menu
            fMRILeftMenu = Instantiate(Resources.Load("Prefabs/ui/camera/fMRI left menu", typeof(GameObject))) as GameObject;
            init_GO(fMRILeftMenu, "fMRI left menu");

            // global left menu
            globalLeftMenu = Instantiate(Resources.Load("Prefabs/ui/camera/Global left menu", typeof(GameObject))) as GameObject;
            init_GO(globalLeftMenu, "Global left menu");

            // site
            Plot = Instantiate(Resources.Load("Prefabs/scene objects/Plot", typeof(GameObject))) as GameObject;
            init_GO(Plot, "Plot");

            // Brain
            Brain = Instantiate(Resources.Load("Prefabs/scene objects/Brain", typeof(GameObject))) as GameObject;
            Brain.GetComponent<MeshFilter>().mesh.MarkDynamic();
            init_GO(Brain, "Brain");

            // Ring
            SelectRing = Instantiate(Resources.Load("Prefabs/scene objects/Select ring", typeof(GameObject))) as GameObject;
            init_GO(SelectRing, "Ring");

            // Cut
            Cut = Instantiate(Resources.Load("Prefabs/scene objects/Cut", typeof(GameObject))) as GameObject;
            Cut.GetComponent<MeshFilter>().mesh.MarkDynamic();
            init_GO(Cut, "Cut");

            // Line views panel
            Line = Instantiate(Resources.Load("Prefabs/ui/camera/Camera line views panel", typeof(GameObject))) as GameObject;
            init_GO(Line, "LineViews");

            // View panel
            View = Instantiate(Resources.Load("Prefabs/ui/camera/Camera view panel", typeof(GameObject))) as GameObject;
            init_GO(View, "View");

            // Screen message panel
            ScreenMessage = Instantiate(Resources.Load("Prefabs/ui/overlay/screen message panel", typeof(GameObject))) as GameObject;            
            init_GO(ScreenMessage, "Screen message");

            // Screen progress bar panel
            ScreenProgressBar = Instantiate(Resources.Load("Prefabs/ui/overlay/progressbar", typeof(GameObject))) as GameObject;
            init_GO(ScreenProgressBar, "Screen Progress Bar");

            // SP camera
            SinglePatientCamera = Instantiate(Resources.Load("Prefabs/Cameras/SPCamera", typeof(GameObject))) as GameObject;
            init_GO(SinglePatientCamera, "SP Camera");

            // MP camera
            MultiPatientsCamera = Instantiate(Resources.Load("Prefabs/Cameras/MPCamera", typeof(GameObject))) as GameObject;
            init_GO(MultiPatientsCamera, "MP Camera");

            // tri center position
            InvisibleBrain = Instantiate(Resources.Load("Prefabs/scene objects/Invisible brain", typeof(GameObject))) as GameObject;
            init_GO(InvisibleBrain, "Invisible brain part");


            // ### DLL
            // MarsAtlas index
            string baseDir = Application.dataPath + "/../Data/";
            #if UNITY_EDITOR
                    baseDir = Application.dataPath + "/Data/";
            #endif
            MarsAtlasIndex = new DLL.MarsAtlasIndex();
            if(!MarsAtlasIndex.LoadMarsAtlasIndexFile(baseDir + "MarsAtlas/mars_atlas_index.csv"))
            {
                Debug.LogError("Can't load mars atlas index.");
            }
        }

        private void init_GO(GameObject baseGameObject, string objectName)
        {
            baseGameObject.name = objectName;
            baseGameObject.transform.SetParent(transform);
            baseGameObject.SetActive(false);
        } 
    }
}