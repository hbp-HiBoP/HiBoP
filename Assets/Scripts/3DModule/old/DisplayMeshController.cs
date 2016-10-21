

//// sytem
//using System.Collections;
//using System.Globalization;
//using System.Collections.Generic;

//// unity
//using UnityEngine;
//using UnityEngine.UI;

//// hbp
//using HBP.VISU3D.Cam;

//namespace HBP.VISU3D
//{

//    public class DisplayMeshController : MonoBehaviour
//    {
//        private bool initialized = false;

//        public Transform scenePanel;
//        public Transform displayMeshControllerOverlay;

//        private Camera m_backGroundCamera;
//        private CamerasManager m_camerasManager;
//        private SceneOverlay displayMeshControllerSceneOverlay = new SceneOverlay();

//        private GameObject displayMeshElementsParent; // loaded prefab        

//        private Base3DScene m_scene;

//        public bool isVisibleFromScene = true;

//        void Awake()
//        {
//            displayMeshControllerSceneOverlay.mainUITransform = displayMeshControllerOverlay;
//        }

//        public void setUIVisibleFromScene(bool visible)
//        {
//            displayMeshControllerSceneOverlay.setActivity(visible);
//            isVisibleFromScene = visible;
//        }


//        public void setUIActivity(bool activity, int state = 0)
//        {
//            if (!isVisibleFromScene)
//                return;

//            displayMeshControllerSceneOverlay.setActivity(activity);
//        }

//        public void init(Base3DScene scene, CamerasManager camerasManager, bool spScene)
//        {
//            // init visibility
//            setUIActivity(true);

//            m_scene = scene;
//            m_camerasManager = camerasManager;
//            m_backGroundCamera = m_camerasManager.getBackGroundCamera();

//            // init base (plane list)
//            displayMeshElementsParent = Instantiate(Resources.Load("Prefabs/ui/setDisplayMesh_controller", typeof(GameObject))) as GameObject;
//            displayMeshElementsParent.name = "elements";
//            displayMeshElementsParent.transform.SetParent(displayMeshControllerOverlay, false);
//            displayMeshElementsParent.SetActive(true);

//            displayMeshElementsParent.transform.Find("hemi_button").GetComponent<Button>().onClick.AddListener(delegate { UIListeners.setMeshToDisplay(m_scene, "other"); });
//            displayMeshElementsParent.transform.Find("white_button").GetComponent<Button>().onClick.AddListener(delegate { UIListeners.setMeshToDisplay(m_scene, "white"); });
//            displayMeshElementsParent.transform.Find("inflated_button").GetComponent<Button>().onClick.AddListener(delegate { UIListeners.setMeshToDisplay(m_scene, "white_inflated"); });

//            initialized = true;
//        }


//        // Update is called once per frame
//        void Update()
//        {
//            if (!initialized)
//                return;

//            RectTransform rectTransform;
//            Rect screenRect = CamerasManager.GetScreenRect(scenePanel.gameObject.GetComponent<RectTransform>(), m_backGroundCamera);

//            rectTransform = displayMeshElementsParent.gameObject.GetComponent<RectTransform>();
//            rectTransform.position = screenRect.position + new Vector2(90, screenRect.height - 30);

//            //rectTransform.position = new Vector2(-1000, -1000);
//            //rectTransform.sizeDelta = new Vector2(40, 100)
//        }
//    }
//}