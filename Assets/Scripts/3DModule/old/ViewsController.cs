
//// system
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

//    public class ViewsController : MonoBehaviour
//    {
//        private bool initialized = false;

//        public Transform scenePanel;
//        public Transform viewsControllerOverlay;


//        private Camera m_backGroundCamera;
//        private SceneOverlay viewsControllerSceneOverlay = new SceneOverlay();

//        private GameObject viewsElementsParent; // loaded prefab        

//        //private Base3DScene m_scene;

//        public bool isVisibleFromScene = true;

//        void Awake()
//        {
//            viewsControllerSceneOverlay.mainUITransform = viewsControllerOverlay;
//        }

//        public void setUIVisibleFromScene(bool visible)
//        {
//            viewsControllerSceneOverlay.setActivity(visible);
//            isVisibleFromScene = visible;
//        }


//        public void setUIActivity(bool activity, int state = 0)
//        {
//            if (!isVisibleFromScene)
//                return;

//            viewsControllerSceneOverlay.setActivity(activity);
//        }

//        public void init(Base3DScene scene, CamerasManager camerasManager, bool spScene)
//        {
//            // init visibility
//            setUIActivity(true);

//            //m_scene = scene;
//            m_backGroundCamera = camerasManager.getBackGroundCamera();

//            // init base (plane list)
//            viewsElementsParent = Instantiate(Resources.Load("Prefabs/ui/views_controller_overlay", typeof(GameObject))) as GameObject;
//            viewsElementsParent.name = "elements";
//            viewsElementsParent.transform.SetParent(viewsControllerOverlay, false);
//            viewsElementsParent.SetActive(true);

//            // set listeners
//            //viewsElementsParent.transform.Find("value_slider").gameObject.GetComponent<Slider>().onValueChanged.AddListener(scene.setTime);
//            //viewsElementsParent.transform.Find("compute_button").gameObject.GetComponent<Button>().onClick.AddListener(delegate { UIListeners.computeInfluences(scene); });

//            // add / remove views

//            //camerasManager
//            viewsElementsParent.transform.Find("add_view_button").GetComponent<Button>().onClick.AddListener(delegate { UIListeners.addView(camerasManager, spScene); });
//            viewsElementsParent.transform.Find("remove_view_button").GetComponent<Button>().onClick.AddListener(delegate { UIListeners.removeView(camerasManager, spScene); });

//            initialized = true;
//        }


//        // Update is called once per frame
//        void Update()
//        {
//            if (!initialized)
//                return;

//            RectTransform rectTransform;
//            Rect screenRect = CamerasManager.GetScreenRect(scenePanel.gameObject.GetComponent<RectTransform>(), m_backGroundCamera);

//            rectTransform = viewsElementsParent.gameObject.GetComponent<RectTransform>();
//            rectTransform.position = screenRect.position + new Vector2(15, screenRect.height - 30);
//            //rectTransform.sizeDelta = new Vector2(40, 100)
//        }
//    }
//}