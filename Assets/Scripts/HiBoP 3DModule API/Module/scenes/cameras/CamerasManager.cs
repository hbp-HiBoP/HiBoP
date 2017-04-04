
/**
 * \file    CamerasManager.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define CamerasManager class
 */

// system
using System.Collections.Generic;

// unity
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.ImageEffects;

namespace HBP.VISU3D.Cam
{
    /// <summary>
    /// A manager for all the SP and MP cameras used in the scene.
    /// </summary>
    public class CamerasManager : MonoBehaviour
    {
        #region members

        // cameras    
        private List<List<GameObject>> m_spCameras = new List<List<GameObject>>(); /**< SP cameras list */
        private List<List<GameObject>> m_mpCameras = new List<List<GameObject>>(); /**< MP cameras list */
        private GameObject m_spCamerasParent = null, m_mpCamerasParent = null; /**< hierachy cameras gameobjects */
        public Camera m_backgroundCamera = null; /**< background camera where all the manager cameras will be displayed */

        // ui 
        private List<GameObject> m_spLinesViewsPanels = new List<GameObject>(); /**< SP line view panel list*/
        private List<GameObject> m_mpLinesViewsPanels = new List<GameObject>(); /**< MP line view panel list*/

        public GameObject m_singlePatientPanel = null; /**< SP patient panel object */
        public GameObject m_multiPatientsPanel = null; /**< MP patient panel object */

        // other
        private bool m_focusModule = true;
        private int m_maxConditionsColumn = 10;  /**< maximum number columns cameras*/
        private int m_maxViewsLine = 5;          /**< maximum number lines cameras */

        #endregion members

        #region mono_behaviour

        void Awake()
        {
            // add one line of 1 view (single)
            GameObject singlePatientLineViews = Instantiate(GlobalGOPreloaded.LineViews);
            singlePatientLineViews.SetActive(true);
            singlePatientLineViews.name = "line_views_0";
            singlePatientLineViews.transform.SetParent(m_singlePatientPanel.transform, false);

            GameObject singlePatientView = Instantiate(GlobalGOPreloaded.View);
            singlePatientView.SetActive(true);
            singlePatientView.transform.SetParent(singlePatientLineViews.transform, false);
            singlePatientView.name = "conv_view_0";

            // add one line of 1 view (multi)
            GameObject multiPatientLineViews = Instantiate(GlobalGOPreloaded.LineViews);
            multiPatientLineViews.SetActive(true);
            multiPatientLineViews.name = "line_views_0";
            multiPatientLineViews.transform.SetParent(m_multiPatientsPanel.transform, false);

            GameObject multiPatientView = Instantiate(GlobalGOPreloaded.View);
            multiPatientView.SetActive(true);
            multiPatientView.transform.SetParent(multiPatientLineViews.transform, false);
            multiPatientView.name = "conv_view_0";

            // add theses in the lists
            m_spLinesViewsPanels.Add(singlePatientLineViews);
            m_mpLinesViewsPanels.Add(multiPatientLineViews);

            // cameras parents
            m_spCamerasParent = transform.Find("sp_cameras").gameObject;
            m_mpCamerasParent = transform.Find("mp_cameras").gameObject;

            // define single patient camera
            GameObject singlePatientCamera = Instantiate(GlobalGOPreloaded.SPCamera);
            init_camera(singlePatientCamera, true, 0, 0);

            // define first multi patients camera
            GameObject multiPatientsCamera = Instantiate(GlobalGOPreloaded.MPCamera);
            init_camera(multiPatientsCamera, false, 0, 0);

            // init cameras lists
            // single
            List<GameObject> initListMultiPatientsCameras = new List<GameObject>();
            initListMultiPatientsCameras.Add(multiPatientsCamera);
            m_mpCameras.Add(initListMultiPatientsCameras);
            // multi
            List<GameObject> initListSinglePatientCameras = new List<GameObject>();
            initListSinglePatientCameras.Add(singlePatientCamera);
            m_spCameras.Add(initListSinglePatientCameras);


            // init conidition columns cameras
            define_conditions_columns_cameras(true, 1);
            define_conditions_columns_cameras(false, 1);
        }

        void Update()
        {
            update_cameras_viewPort();
        }

        #endregion mono_behaviour

        #region others

        public int max_views_line() { return m_maxViewsLine; }

        public int max_conditions_col() { return m_maxConditionsColumn; }

        private void init_camera(GameObject camera, bool spScene, int idColumn, int idLine)
        {
            camera.tag = spScene ? "SingleCamera" : "MultiCamera";
            camera.name = (spScene ? "singlePatient_camera_tb_c" : "multiPatients_camera_tb_c") + idColumn + "_v" + idLine;
            camera.transform.SetParent(spScene ? m_spCamerasParent.transform : m_mpCamerasParent.transform);
            camera.SetActive(true);
            camera.GetComponent<TrackBallCamera>().init(spScene ? m_singlePatientPanel.transform.position : m_multiPatientsPanel.transform.position);
        }

        public void update_cameras_target(bool spScene, Vector3 target)
        {
            List<List<GameObject>> cameras = spScene ? m_spCameras : m_mpCameras;

            for (int ii = 0; ii < cameras.Count; ++ii)
                for (int jj = 0; jj < cameras[ii].Count; ++jj)
                    cameras[ii][jj].GetComponent<TrackBallCamera>().init(target);
        }


        public void stop_rotation_of_all_cameras(bool spScene)
        {
            if(spScene)
                for (int ii = 0; ii < m_spCameras.Count; ++ii)
                {
                    for (int jj = 0; jj < m_spCameras[ii].Count; jj++)
                    {
                        if (m_spCameras[ii][jj].activeInHierarchy)
                        {
                            m_spCameras[ii][jj].GetComponent<TrackBallSingleCamera>().stop_automatic_rotation();
                        }
                    }
                }
            else
                for (int ii = 0; ii < m_mpCameras.Count; ++ii)
                {
                    for (int jj = 0; jj < m_mpCameras[ii].Count; jj++)
                    {
                        if (m_mpCameras[ii][jj].activeInHierarchy)
                        {
                            m_mpCameras[ii][jj].GetComponent<TrackBallMultiCamera>().stop_automatic_rotation();
                        }
                    }
                }
        }

        /// <summary>
        /// Apply a rotation in all the cameras
        /// </summary>
        /// <param name="spScene"></param>
        public void rotate_all_cameras(bool spScene)
        {
            if(spScene)
                for (int ii = 0; ii < m_spCameras.Count; ++ii)
                {
                    for (int jj = 0; jj < m_spCameras[ii].Count; jj++)
                    {
                        if (m_spCameras[ii][jj].activeInHierarchy)
                        {
                            m_spCameras[ii][jj].GetComponent<TrackBallSingleCamera>().start_automatic_rotation();
                        }
                    }
                }
            else
                for (int ii = 0; ii < m_mpCameras.Count; ++ii)
                {
                    for (int jj = 0; jj < m_mpCameras[ii].Count; jj++)
                    {
                        if (m_mpCameras[ii][jj].activeInHierarchy)
                        {
                            m_mpCameras[ii][jj].GetComponent<TrackBallMultiCamera>().start_automatic_rotation();
                        }
                    }
                }
        }

        /// <summary>
        /// Sets the cameras rotation speed
        /// </summary>
        /// <param name="spScene"></param>
        public void set_all_cameras_rotation_speed(bool spScene, float speed)
        {
            if (spScene)
                for (int ii = 0; ii < m_spCameras.Count; ++ii)
                {
                    for (int jj = 0; jj < m_spCameras[ii].Count; jj++)
                    {
                        if (m_spCameras[ii][jj].activeInHierarchy)
                        {
                            m_spCameras[ii][jj].GetComponent<TrackBallSingleCamera>().set_camera_rotation_speed(speed);
                        }
                    }
                }
            else
                for (int ii = 0; ii < m_mpCameras.Count; ++ii)
                {
                    for (int jj = 0; jj < m_mpCameras[ii].Count; jj++)
                    {
                        if (m_mpCameras[ii][jj].activeInHierarchy)
                        {
                            m_mpCameras[ii][jj].GetComponent<TrackBallMultiCamera>().set_camera_rotation_speed(speed);
                        }
                    }
                }
        }

        public void set_module_focus(bool state)
        {
            m_focusModule = state;
            for (int ii = 0; ii < m_spCameras.Count; ++ii)
            {
                for (int jj = 0; jj < m_spCameras[ii].Count; jj++)
                {
                    if(m_spCameras[ii][jj] != null)
                        m_spCameras[ii][jj].GetComponent<TrackBallSingleCamera>().set_camera_focus(state);
                }
            }
     
            for (int ii = 0; ii < m_mpCameras.Count; ++ii)
            {
                for (int jj = 0; jj < m_mpCameras[ii].Count; jj++)
                {
                    if (m_mpCameras[ii][jj] != null)
                        m_mpCameras[ii][jj].GetComponent<TrackBallMultiCamera>().set_camera_focus(state);
                }
            }
        }

        public TrackBallCamera get_camera(bool spScene, int idColumn, int idLine)
        {
            if (spScene)
            {
                return m_spCameras[idLine][idColumn].GetComponent<TrackBallCamera>();
            }

            return m_mpCameras[idLine][idColumn].GetComponent<TrackBallCamera>();
        }

        public Camera background_camera()
        {
            return m_backgroundCamera;
        }


        /// <summary>
        /// Get the screen rect of the input camera
        /// </summary>
        /// <param name="rectTransform"></param>
        /// <param name="camera"></param>
        /// <returns></returns>
        public static Rect screen_rectangle(RectTransform rectTransform, Camera camera)
        {
            if (rectTransform == null)
                return new Rect();

            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            float xMin = float.PositiveInfinity;
            float xMax = float.NegativeInfinity;
            float yMin = float.PositiveInfinity;
            float yMax = float.NegativeInfinity;

            for (int i = 0; i < 4; i++)
            {
                Vector3 screenCoord = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);

                if (screenCoord.x < xMin)
                    xMin = screenCoord.x;
                if (screenCoord.x > xMax)
                    xMax = screenCoord.x;
                if (screenCoord.y < yMin)
                    yMin = screenCoord.y;
                if (screenCoord.y > yMax)
                    yMax = screenCoord.y;
            }

            Rect result = new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
            return result;
        }

        public int columns_nb(bool spScene)
        {
            if (spScene)
                return m_spCameras[0].Count;

            return m_mpCameras[0].Count;
        }

        public int views_nb(bool spScene)
        {
            if (spScene)
                return m_spLinesViewsPanels.Count;

            return m_mpLinesViewsPanels.Count;
        }

        /// <summary>
        /// Retrieve the rectranform of a scene panel
        /// </summary>
        /// <param name="spScene"></param>
        /// <returns></returns>
        public RectTransform screen_rectangleT(bool spScene)
        {
            if (spScene)
                return m_singlePatientPanel.GetComponent<RectTransform>();

            return m_multiPatientsPanel.GetComponent<RectTransform>();
        }

        /// <summary>
        /// Get the Rect transform of the camera corresponding to the colum and line id of the scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="columnId"></param>
        /// <param name="viewsId"></param>
        /// <returns></returns>
        public RectTransform camera_rectangle(bool spScene, int columnId, int viewsId)
        {
            RectTransform rectT = null;// new RectTransform();
            if (spScene)
            {
                if (viewsId < m_spLinesViewsPanels.Count)
                {
                    Transform convView = m_spLinesViewsPanels[viewsId].transform.Find("conv_view_" + columnId);
                    if (convView != null)
                        rectT = convView.gameObject.GetComponent<RectTransform>();
                }
            }
            else
            {
                if (viewsId < m_mpLinesViewsPanels.Count)
                {
                    Transform convView = m_mpLinesViewsPanels[viewsId].transform.Find("conv_view_" + columnId);
                    if (convView != null)
                        rectT = convView.gameObject.GetComponent<RectTransform>();
                }
            }

            return rectT;
        }

        /// <summary>
        /// Update the viewport rectangle of all the cameras
        /// </summary>
        private void update_cameras_viewPort()
        {
            Rect viewportRect = new Rect();

            // sp
            for (int ii = 0; ii < m_spCameras.Count; ++ii)
            {
                for (int jj = 0; jj < m_spCameras[0].Count; ++jj)
                {
                    GameObject convView = m_spLinesViewsPanels[ii].transform.FindChild("conv_view_" + jj).gameObject;
                    LayoutElement layout = convView.GetComponent<LayoutElement>();

                    if (m_spCameras[ii][jj].GetComponent<TrackBallCamera>().is_minimized())
                    {            
                        layout.flexibleWidth = 0.001f;
                        layout.minWidth = 20;

                        RectTransform currentRectT = convView.GetComponent<RectTransform>();
                        viewportRect = screen_rectangle(currentRectT, m_backgroundCamera);
                        m_spCameras[ii][jj].GetComponent<Camera>().rect = new Rect(viewportRect.xMin / Screen.width, viewportRect.yMin / Screen.height, viewportRect.width / Screen.width, viewportRect.height / Screen.height);
                    }
                    else
                    {
                        layout.flexibleWidth = 1f;
                        layout.minWidth = 0;

                        m_spCameras[ii][jj].SetActive(true);
                        RectTransform currentRectT = convView.GetComponent<RectTransform>();
                        viewportRect = screen_rectangle(currentRectT, m_backgroundCamera);
                        m_spCameras[ii][jj].GetComponent<Camera>().rect = new Rect(viewportRect.xMin / Screen.width, viewportRect.yMin / Screen.height, viewportRect.width / Screen.width, viewportRect.height / Screen.height);
                    }
                }
            }

            // mp
            for (int ii = 0; ii < m_mpCameras.Count; ++ii)
            {
                for (int jj = 0; jj < m_mpCameras[0].Count; ++jj)
                {
                    GameObject convView = m_mpLinesViewsPanels[ii].transform.FindChild("conv_view_" + jj).gameObject;
                    LayoutElement layout = convView.GetComponent<LayoutElement>();

                    if (m_mpCameras[ii][jj].GetComponent<TrackBallCamera>().is_minimized())
                    {
                        layout.flexibleWidth = 0.001f;
                        layout.minWidth = 20;

                        RectTransform currentRectT = convView.GetComponent<RectTransform>();
                        viewportRect = screen_rectangle(currentRectT, m_backgroundCamera);
                        m_mpCameras[ii][jj].GetComponent<Camera>().rect = new Rect(viewportRect.xMin / Screen.width, viewportRect.yMin / Screen.height, viewportRect.width / Screen.width, viewportRect.height / Screen.height);
                    }
                    else
                    {
                        layout.flexibleWidth = 1f;
                        layout.minWidth = 0;

                        m_mpCameras[ii][jj].SetActive(true);
                        RectTransform currentRectT = convView.GetComponent<RectTransform>();
                        viewportRect = screen_rectangle(currentRectT, m_backgroundCamera);
                        m_mpCameras[ii][jj].GetComponent<Camera>().rect = new Rect(viewportRect.xMin / Screen.width, viewportRect.yMin / Screen.height, viewportRect.width / Screen.width, viewportRect.height / Screen.height);
                    }
                }
            }
        }

        public void add_view_line_cameras(bool singlePatientScene)
        {
            if (singlePatientScene)
                add_view_line_cameras(m_spCameras, m_spLinesViewsPanels, m_spCamerasParent.transform, "singlePatient_camera_tb_c");
            else
                add_view_line_cameras(m_mpCameras, m_mpLinesViewsPanels, m_mpCamerasParent.transform, "multiPatients_camera_tb_c");
        }

        public void remove_view_line_cameras(bool singlePatientScene)
        {
            if (singlePatientScene)
            {
                remove_view_line_cameras(m_spCameras, m_spLinesViewsPanels);
            }
            else
            {
                remove_view_line_cameras(m_mpCameras, m_mpLinesViewsPanels);
            }
        }

        /// <summary>
        /// Define the number of columns cameras of the scene
        /// </summary>
        /// <param name="singlePatientScene"></param>
        /// <param name="nbColumns"></param>
        public void define_conditions_columns_cameras(bool singlePatientScene, int nbColumns)
        {
            int diff;
            if (singlePatientScene)
            {
                diff = m_spCameras[0].Count - nbColumns;
            }
            else
            {
                diff = m_mpCameras[0].Count - nbColumns;
            }

            if (diff < 0)
            {
                diff = -diff;
                for (int ii = 0; ii < diff; ++ii)
                {
                    add_column_cameras(singlePatientScene);
                }
            }
            else if (diff > 0)
            {
                for (int ii = 0; ii < diff; ++ii)
                {
                    remove_last_column_cameras(singlePatientScene);
                }
            }
        }

        public void add_column_cameras(bool singlePatientScene, bool IRMFColum = false)
        {
            if (singlePatientScene)
                add_columns_cameras(m_spCameras, m_spLinesViewsPanels, m_spCamerasParent.transform, "singlePatient_camera_tb_c", singlePatientScene, IRMFColum);
            else
                add_columns_cameras(m_mpCameras, m_mpLinesViewsPanels, m_mpCamerasParent.transform, "multiPatients_camera_tb_c", singlePatientScene, IRMFColum);
        }

        public void remove_last_column_cameras(bool singlePatientScene)
        {
            if (singlePatientScene)
            {
                remove_last_column_cameras(m_spCameras, m_spLinesViewsPanels);
            }
            else
            {
                remove_last_column_cameras(m_mpCameras, m_mpLinesViewsPanels);
            }
        }

        /// <summary>
        /// Add a new line of camera to the input cameras list
        /// </summary>
        /// <param name="sceneCameras"></param>
        /// <param name="sceneLinesViewsPanels"></param>
        /// <param name="cameraParent"></param>
        /// <param name="sceneCameraBaseName"></param>
        private void add_view_line_cameras(List<List<GameObject>> sceneCameras, List<GameObject> sceneLinesViewsPanels, Transform cameraParent, string sceneCameraBaseName)
        {
            int currentViewsNumber = sceneCameras.Count;
            if (currentViewsNumber < m_maxViewsLine)
            {
                // add new camera line
                List<GameObject> newLine = new List<GameObject>();

                for (int ii = 0; ii < sceneCameras[0].Count; ++ii)
                {
                    newLine.Add(Instantiate(sceneCameras[0][ii]));

                    TrackBallCamera camera = newLine[ii].GetComponent<TrackBallCamera>();
                    camera.set_line_ID(currentViewsNumber);
                    camera.set_column_ID(ii);
                    camera.set_camera_focus(m_focusModule);

                    newLine[ii].GetComponent<Camera>().depth = ii;
                    newLine[ii].name = sceneCameraBaseName + ii + "_v" + currentViewsNumber;
                    newLine[ii].transform.parent = cameraParent;
                }


                sceneCameras.Add(newLine);
                sceneLinesViewsPanels.Add(Instantiate(sceneLinesViewsPanels[0]));
                sceneLinesViewsPanels[sceneLinesViewsPanels.Count - 1].name = "line_views_" + (sceneLinesViewsPanels.Count - 1);
                sceneLinesViewsPanels[sceneLinesViewsPanels.Count - 1].transform.SetParent(sceneLinesViewsPanels[0].transform.parent, false);
            }
        }

        /// <summary>
        /// Remove a line of cameras to the input cameras list
        /// </summary>
        /// <param name="sceneCameras"></param>
        /// <param name="sceneLinesViewsPanels"></param>
        private void remove_view_line_cameras(List<List<GameObject>> sceneCameras, List<GameObject> sceneLinesViewsPanels)
        {
            int currentViewsNumber = sceneCameras.Count;
            if (currentViewsNumber > 1)
            {
                // destroy last added camera line
                for (int ii = 0; ii < sceneCameras[0].Count; ++ii)
                {
                    Destroy(sceneCameras[sceneCameras.Count - 1][ii]);
                }
                sceneCameras.RemoveAt(sceneCameras.Count - 1);

                Destroy(sceneLinesViewsPanels[sceneLinesViewsPanels.Count - 1]);
                sceneLinesViewsPanels.RemoveAt(sceneLinesViewsPanels.Count - 1);
            }
        }

        /// <summary>
        /// Add a column of cameras to the input cameras list
        /// </summary>
        /// <param name="sceneCameras"></param>
        /// <param name="sceneLinesViewsPanels"></param>
        /// <param name="cameraParent"></param>
        /// <param name="sceneCameraBaseName"></param>
        private void add_columns_cameras(List<List<GameObject>> sceneCameras, List<GameObject> sceneLinesViewsPanels, Transform cameraParent, string sceneCameraBaseName, bool spScene, bool IRMFColumn)
        {
            int currentConditionNumber = sceneCameras[0].Count;
            if (currentConditionNumber < m_maxConditionsColumn)
            {
                // add new camera column
                for (int ii = 0; ii < sceneCameras.Count; ++ii)
                {
                    sceneCameras[ii].Add(Instantiate(sceneCameras[ii][0]));
                    sceneCameras[ii][currentConditionNumber].GetComponent<Camera>().depth = currentConditionNumber;
                    sceneCameras[ii][currentConditionNumber].name = sceneCameraBaseName + currentConditionNumber + "_v" + ii;
                    sceneCameras[ii][currentConditionNumber].transform.parent = cameraParent;

                    TrackBallCamera camera = sceneCameras[ii][currentConditionNumber].GetComponent<TrackBallCamera>();
                    camera.set_line_ID(ii);
                    camera.set_column_ID(currentConditionNumber);
                    camera.set_camera_focus(m_focusModule);
                    camera.set_fMRI_camera(IRMFColumn);

                    if (spScene)
                        camera.set_column_layer("C" + currentConditionNumber + "_SP");
                    else
                        camera.set_column_layer("C" + currentConditionNumber + "_MP");
                }

                for (int ii = 0; ii < sceneLinesViewsPanels.Count; ++ii)
                {
                    GameObject newView = Instantiate(sceneLinesViewsPanels[ii].transform.Find("conv_view_0").gameObject);
                    newView.name = "conv_view_" + (currentConditionNumber);
                    newView.transform.SetParent(sceneLinesViewsPanels[ii].transform, false);
                }
            }
        }

        /// <summary>
        /// Remove a column of cameras to the input cameras list
        /// </summary>
        /// <param name="sceneCameras"></param>
        /// <param name="sceneLinesViewsPanels"></param>
        private void remove_last_column_cameras(List<List<GameObject>> sceneCameras, List<GameObject> sceneLinesViewsPanels)
        {
            int currentConditionNumber = sceneCameras[0].Count;
            if (currentConditionNumber > 1)
            {
                // destroy last added camera column
                for (int ii = 0; ii < sceneCameras.Count; ++ii)
                {
                    if (sceneCameras[ii][sceneCameras[ii].Count - 1].GetComponent<TrackBallCamera>().is_selected())
                    {
                        sceneCameras[ii][sceneCameras[ii].Count - 1].GetComponent<TrackBallCamera>().update_selected_column(0);
                    }

                    Destroy(sceneCameras[ii][sceneCameras[ii].Count - 1]);
                    sceneCameras[ii].RemoveAt(sceneCameras[ii].Count - 1);
                }

                for (int ii = 0; ii < sceneLinesViewsPanels.Count; ++ii)
                {
                    Destroy(sceneLinesViewsPanels[ii].transform.Find("conv_view_" + (currentConditionNumber - 1)).gameObject);
                }
            }
        }

        /// <summary>
        /// Update the layer mask of all the cameras of a scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="layerMask"></param>
        public void update_cameras_layer_mask(bool spScene, int layerMask)
        {
            if (spScene)
            {
                for (int ii = 0; ii < m_spCameras.Count; ++ii)
                {
                    for (int jj = 0; jj < m_spCameras[ii].Count; jj++)
                    {
                        m_spCameras[ii][jj].GetComponent<TrackBallSingleCamera>().update_culling_mask(layerMask);
                    }
                }
            }
            else
            {
                for (int ii = 0; ii < m_mpCameras.Count; ++ii)
                {
                    for (int jj = 0; jj < m_mpCameras[ii].Count; jj++)
                    {
                        m_mpCameras[ii][jj].GetComponent<TrackBallMultiCamera>().update_culling_mask(layerMask);
                    }
                }
            }
        }

        public void apply_MP_cameras_settings_to_SP_cameras()
        {
            if (m_spCameras.Count < m_mpCameras.Count)
            {
                int diff = m_mpCameras.Count - m_spCameras.Count;
                for(int ii = 0; ii < diff; ++ii)
                {
                    add_view_line_cameras(true);
                }
            }
            else if (m_spCameras.Count > m_mpCameras.Count)
            {
                int diff = m_spCameras.Count - m_mpCameras.Count;
                for (int ii = 0; ii < diff; ++ii)
                {
                    remove_view_line_cameras(true);
                }
            }

            if(columns_nb(true) == columns_nb(false))
            {
                for (int ii = 0; ii < m_spCameras.Count; ++ii)
                {
                    for(int jj = 0; jj < m_spCameras[ii].Count; ++jj)
                    {
                        m_spCameras[ii][jj].GetComponent<TrackBallCamera>().define_camera(m_mpCameras[ii][jj].transform.position, 
                                                                    m_mpCameras[ii][jj].transform.rotation, m_mpCameras[ii][jj].GetComponent<TrackBallCamera>().target());
                    }
                }
            }
            
        }


        public void set_edges_mode(bool spScene, bool show)
        {
            if (spScene)
            {
                for (int ii = 0; ii < m_spCameras.Count; ++ii)
                {
                    for (int jj = 0; jj < m_spCameras[ii].Count; ++jj)
                    {
                        m_spCameras[ii][jj].GetComponent<EdgeDetection>().enabled = show;
                    }
                }
            }
            else
            {
                for (int ii = 0; ii < m_mpCameras.Count; ++ii)
                {
                    for (int jj = 0; jj < m_mpCameras[ii].Count; ++jj)
                    {
                        m_mpCameras[ii][jj].GetComponent<EdgeDetection>().enabled = show;
                    }
                }
            }
        }

    #endregion others
}
}