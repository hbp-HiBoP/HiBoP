
/**
 * \file    CamerasManager.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define CamerasManager class
 */

// system
using System.Collections;
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
        private bool m_focusModule = false;

        public int m_maxConditionsColumn = 10;  /**< maximum number columns cameras*/
        public int m_maxViewsLine = 5;          /**< maximum number lines cameras */

        #endregion members

        #region mono_behaviour

        /// <summary>
        /// This function is always called before any Start functions and also just after a prefab is instantiated. (If a GameObject is inactive during start up Awake is not called until it is made active.)
        /// </summary>
        void Awake()
        {
            // add one line of 1 view (single)
            GameObject singlePatientLineViews = Instantiate(BaseGameObjects.LineViews);
            singlePatientLineViews.SetActive(true);
            singlePatientLineViews.name = "line_views_0";
            singlePatientLineViews.transform.SetParent(m_singlePatientPanel.transform, false);

            GameObject singlePatientView = Instantiate(BaseGameObjects.View);
            singlePatientView.SetActive(true);
            singlePatientView.transform.SetParent(singlePatientLineViews.transform, false);
            singlePatientView.name = "conv_view_0";

            // add one line of 1 view (multi)
            GameObject multiPatientLineViews = Instantiate(BaseGameObjects.LineViews);
            multiPatientLineViews.SetActive(true);
            multiPatientLineViews.name = "line_views_0";
            multiPatientLineViews.transform.SetParent(m_multiPatientsPanel.transform, false);

            GameObject multiPatientView = Instantiate(BaseGameObjects.View);
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
            GameObject singlePatientCamera = Instantiate(BaseGameObjects.SPCamera);
            initCamera(singlePatientCamera, true, 0, 0);

            //singlePatientCamera.tag = "SingleCamera";
            //singlePatientCamera.name = "singlePatient_camera_tb_c0_v0";
            //singlePatientCamera.transform.parent = m_spCamerasParent.transform;
            //singlePatientCamera.SetActive(true);

            // define first multi patients camera
            GameObject multiPatientsCamera = Instantiate(BaseGameObjects.MPCamera);
            initCamera(multiPatientsCamera, false, 0, 0);
            //multiPatientsCamera.tag = "MultiCamera";
            //multiPatientsCamera.name = "multiPatients_camera_tb_c0_v0";
            //multiPatientsCamera.transform.parent = m_mpCamerasParent.transform;
            //multiPatientsCamera.SetActive(true);

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
            defineConditionColumnsCameras(true, 1);
            defineConditionColumnsCameras(false, 1);
        }

        /// <summary>
        /// Update is called once per frame. It is the main workhorse function for frame updates.
        /// </summary>
        void Update()
        {
            updateCamerasViewPort();
        }

        #endregion mono_behaviour

        #region others

        private void initCamera(GameObject camera, bool spScene, int idColumn, int idLine)
        {
            camera.tag = spScene ? "SingleCamera" : "MultiCamera";
            camera.name = (spScene ? "singlePatient_camera_tb_c" : "multiPatients_camera_tb_c") + idColumn + "_v" + idLine;
            camera.transform.SetParent(spScene ? m_spCamerasParent.transform : m_mpCamerasParent.transform);
            camera.SetActive(true);
            camera.GetComponent<TrackBallCamera>().init(spScene ? m_singlePatientPanel.transform.position : m_multiPatientsPanel.transform.position);
        }

        public void updateCamerasTarget(bool spScene, Vector3 target)
        {
            List<List<GameObject>> cameras = spScene ? m_spCameras : m_mpCameras;

            for (int ii = 0; ii < cameras.Count; ++ii)
                for (int jj = 0; jj < cameras[ii].Count; ++jj)
                    cameras[ii][jj].GetComponent<TrackBallCamera>().init(target);
        }


        public void stopRotationOfAllCameras(bool spScene)
        {
            if(spScene)
                for (int ii = 0; ii < m_spCameras.Count; ++ii)
                {
                    for (int jj = 0; jj < m_spCameras[ii].Count; jj++)
                    {
                        if (m_spCameras[ii][jj].activeInHierarchy)
                        {
                            m_spCameras[ii][jj].GetComponent<TrackBallSingleCamera>().stopAutomaticRotation();
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
                            m_mpCameras[ii][jj].GetComponent<TrackBallMultiCamera>().stopAutomaticRotation();
                        }
                    }
                }
        }

        /// <summary>
        /// Apply a coroutine rotation in all the cameras
        /// </summary>
        /// <param name="spScene"></param>
        public void rotateAllCameras(bool spScene)
        {
            if(spScene)
                for (int ii = 0; ii < m_spCameras.Count; ++ii)
                {
                    for (int jj = 0; jj < m_spCameras[ii].Count; jj++)
                    {
                        if (m_spCameras[ii][jj].activeInHierarchy)
                        {
                            m_spCameras[ii][jj].GetComponent<TrackBallSingleCamera>().automaticCameraRotation();
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
                            m_mpCameras[ii][jj].GetComponent<TrackBallMultiCamera>().automaticCameraRotation();
                        }
                    }
                }
        }

        /// <summary>
        /// Set the focus state of the module
        /// </summary>
        /// <param name="state"></param>
        public void setModuleFocus(bool state)
        {
            m_focusModule = state;
            for (int ii = 0; ii < m_spCameras.Count; ++ii)
            {
                for (int jj = 0; jj < m_spCameras[ii].Count; jj++)
                {
                    if(m_spCameras[ii][jj] != null)
                        m_spCameras[ii][jj].GetComponent<TrackBallSingleCamera>().setCameraFocus(state);
                }
            }
     
            for (int ii = 0; ii < m_mpCameras.Count; ++ii)
            {
                for (int jj = 0; jj < m_mpCameras[ii].Count; jj++)
                {
                    if (m_mpCameras[ii][jj] != null)
                        m_mpCameras[ii][jj].GetComponent<TrackBallMultiCamera>().setCameraFocus(state);
                }
            }
        }

        /// <summary>
        /// Get the camera corresponding to the column and line id
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="idColumn"></param>
        /// <param name="idLine"></param>
        /// <returns></returns>
        public TrackBallCamera getCamera(bool spScene, int idColumn, int idLine)
        {
            if (spScene)
            {
                return m_spCameras[idLine][idColumn].GetComponent<TrackBallCamera>();
            }

            return m_mpCameras[idLine][idColumn].GetComponent<TrackBallCamera>();
        }

        /// <summary>
        /// Get the background camera
        /// </summary>
        /// <returns></returns>
        public Camera getBackGroundCamera()
        {
            return m_backgroundCamera;
        }


        /// <summary>
        /// Get the screen rect of the input camera
        /// </summary>
        /// <param name="rectTransform"></param>
        /// <param name="camera"></param>
        /// <returns></returns>
        public static Rect GetScreenRect(RectTransform rectTransform, Camera camera)
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

        /// <summary>
        /// Get the column number of the scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <returns></returns>
        public int getColumnsNumber(bool spScene)
        {
            if (spScene)
                return m_spCameras[0].Count;

            return m_mpCameras[0].Count;
        }

        /// <summary>
        /// Get the line number of the scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <returns></returns>
        public int getNumberOfViews(bool spScene)
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
        public RectTransform getSceneRectT(bool spScene)
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
        public RectTransform getCameraRect(bool spScene, int columnId, int viewsId)
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
        private void updateCamerasViewPort()
        {
            Rect viewportRect = new Rect();

            // sp
            for (int ii = 0; ii < m_spCameras.Count; ++ii)
            {
                for (int jj = 0; jj < m_spCameras[0].Count; ++jj)
                {
                    GameObject convView = m_spLinesViewsPanels[ii].transform.FindChild("conv_view_" + jj).gameObject;
                    LayoutElement layout = convView.GetComponent<LayoutElement>();

                    if (m_spCameras[ii][jj].GetComponent<TrackBallCamera>().isMinimized())
                    {            
                        layout.flexibleWidth = 0.001f;
                        layout.minWidth = 20;

                        RectTransform currentRectT = convView.GetComponent<RectTransform>();
                        viewportRect = GetScreenRect(currentRectT, m_backgroundCamera);
                        m_spCameras[ii][jj].GetComponent<Camera>().rect = new Rect(viewportRect.xMin / Screen.width, viewportRect.yMin / Screen.height, viewportRect.width / Screen.width, viewportRect.height / Screen.height);
                    }
                    else
                    {
                        layout.flexibleWidth = 1f;
                        layout.minWidth = 0;

                        m_spCameras[ii][jj].SetActive(true);
                        RectTransform currentRectT = convView.GetComponent<RectTransform>();
                        viewportRect = GetScreenRect(currentRectT, m_backgroundCamera);
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

                    if (m_mpCameras[ii][jj].GetComponent<TrackBallCamera>().isMinimized())
                    {
                        layout.flexibleWidth = 0.001f;
                        layout.minWidth = 20;

                        RectTransform currentRectT = convView.GetComponent<RectTransform>();
                        viewportRect = GetScreenRect(currentRectT, m_backgroundCamera);
                        m_mpCameras[ii][jj].GetComponent<Camera>().rect = new Rect(viewportRect.xMin / Screen.width, viewportRect.yMin / Screen.height, viewportRect.width / Screen.width, viewportRect.height / Screen.height);
                    }
                    else
                    {
                        layout.flexibleWidth = 1f;
                        layout.minWidth = 0;

                        m_mpCameras[ii][jj].SetActive(true);
                        RectTransform currentRectT = convView.GetComponent<RectTransform>();
                        viewportRect = GetScreenRect(currentRectT, m_backgroundCamera);
                        m_mpCameras[ii][jj].GetComponent<Camera>().rect = new Rect(viewportRect.xMin / Screen.width, viewportRect.yMin / Screen.height, viewportRect.width / Screen.width, viewportRect.height / Screen.height);
                    }
                }
            }
        }

        /// <summary>
        /// Add a new line of cameras
        /// </summary>
        /// <param name="singlePatientScene"></param>
        public void addViewLineCameras(bool singlePatientScene)
        {
            if (singlePatientScene)
                addViewLineCameras(m_spCameras, m_spLinesViewsPanels, m_spCamerasParent.transform, "singlePatient_camera_tb_c");
            else
                addViewLineCameras(m_mpCameras, m_mpLinesViewsPanels, m_mpCamerasParent.transform, "multiPatients_camera_tb_c");
        }

        /// <summary>
        /// Remove the last line of cameras
        /// </summary>
        /// <param name="singlePatientScene"></param>
        public void removeViewLineCameras(bool singlePatientScene)
        {
            if (singlePatientScene)
            {
                removeViewLineCameras(m_spCameras, m_spLinesViewsPanels);
            }
            else
            {
                removeViewLineCameras(m_mpCameras, m_mpLinesViewsPanels);
            }
        }

        /// <summary>
        /// Define the number of columns cameras of the scene
        /// </summary>
        /// <param name="singlePatientScene"></param>
        /// <param name="nbColumns"></param>
        public void defineConditionColumnsCameras(bool singlePatientScene, int nbColumns)
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
                    addColumnCameras(singlePatientScene);
                }
            }
            else if (diff > 0)
            {
                for (int ii = 0; ii < diff; ++ii)
                {
                    removeLastColumnCameras(singlePatientScene);
                }
            }
        }


        /// <summary>
        /// Add a new column of cameras
        /// </summary>
        /// <param name="singlePatientScene"></param>
        /// <param name="IRMFColum"></param>
        public void addColumnCameras(bool singlePatientScene, bool IRMFColum = false)
        {
            if (singlePatientScene)
                addColumnCameras(m_spCameras, m_spLinesViewsPanels, m_spCamerasParent.transform, "singlePatient_camera_tb_c", singlePatientScene, IRMFColum);
            else
                addColumnCameras(m_mpCameras, m_mpLinesViewsPanels, m_mpCamerasParent.transform, "multiPatients_camera_tb_c", singlePatientScene, IRMFColum);
        }

        /// <summary>
        /// Remove the last column of cameras
        /// </summary>
        /// <param name="singlePatientScene"></param>
        public void removeLastColumnCameras(bool singlePatientScene)
        {
            if (singlePatientScene)
            {
                removeLastColumnCameras(m_spCameras, m_spLinesViewsPanels);
            }
            else
            {
                removeLastColumnCameras(m_mpCameras, m_mpLinesViewsPanels);
            }
        }

        /// <summary>
        /// Add a new line of camera to the input cameras list
        /// </summary>
        /// <param name="sceneCameras"></param>
        /// <param name="sceneLinesViewsPanels"></param>
        /// <param name="cameraParent"></param>
        /// <param name="sceneCameraBaseName"></param>
        private void addViewLineCameras(List<List<GameObject>> sceneCameras, List<GameObject> sceneLinesViewsPanels, Transform cameraParent, string sceneCameraBaseName)
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
                    camera.setLineId(currentViewsNumber);
                    camera.setColId(ii);
                    camera.setCameraFocus(m_focusModule);

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
        private void removeViewLineCameras(List<List<GameObject>> sceneCameras, List<GameObject> sceneLinesViewsPanels)
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
        private void addColumnCameras(List<List<GameObject>> sceneCameras, List<GameObject> sceneLinesViewsPanels, Transform cameraParent, string sceneCameraBaseName, bool spScene, bool IRMFColumn)
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
                    camera.setLineId(ii);
                    camera.setColId(currentConditionNumber);
                    camera.setCameraFocus(m_focusModule);
                    camera.setIRMFCamera(IRMFColumn);

                    if (spScene)
                        camera.setColumnLayer("C" + currentConditionNumber + "_SP");
                    else
                        camera.setColumnLayer("C" + currentConditionNumber + "_MP");
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
        private void removeLastColumnCameras(List<List<GameObject>> sceneCameras, List<GameObject> sceneLinesViewsPanels)
        {
            int currentConditionNumber = sceneCameras[0].Count;
            if (currentConditionNumber > 1)
            {
                // destroy last added camera column
                for (int ii = 0; ii < sceneCameras.Count; ++ii)
                {
                    if (sceneCameras[ii][sceneCameras[ii].Count - 1].GetComponent<TrackBallCamera>().isSelected())
                    {
                        sceneCameras[ii][sceneCameras[ii].Count - 1].GetComponent<TrackBallCamera>().updateSelectedColumn(0);
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
        public void updateCamerasLayerMask(bool spScene, int layerMask)
        {
            if (spScene)
            {
                for (int ii = 0; ii < m_spCameras.Count; ++ii)
                {
                    for (int jj = 0; jj < m_spCameras[ii].Count; jj++)
                    {
                        m_spCameras[ii][jj].GetComponent<TrackBallSingleCamera>().updateCullingMask(layerMask);
                    }
                }
            }
            else
            {
                for (int ii = 0; ii < m_mpCameras.Count; ++ii)
                {
                    for (int jj = 0; jj < m_mpCameras[ii].Count; jj++)
                    {
                        m_mpCameras[ii][jj].GetComponent<TrackBallMultiCamera>().updateCullingMask(layerMask);
                    }
                }
            }
        }

        // TEST
        public void applyMPCamerasSettingsToSPCameras()
        {
            if (m_spCameras.Count < m_mpCameras.Count)
            {
                int diff = m_mpCameras.Count - m_spCameras.Count;
                for(int ii = 0; ii < diff; ++ii)
                {
                    addViewLineCameras(true);
                }
            }
            else if (m_spCameras.Count > m_mpCameras.Count)
            {
                int diff = m_spCameras.Count - m_mpCameras.Count;
                for (int ii = 0; ii < diff; ++ii)
                {
                    removeViewLineCameras(true);
                }
            }

            if(getColumnsNumber(true) == getColumnsNumber(false))
            {
                for (int ii = 0; ii < m_spCameras.Count; ++ii)
                {
                    for(int jj = 0; jj < m_spCameras[ii].Count; ++jj)
                    {
                        m_spCameras[ii][jj].GetComponent<TrackBallCamera>().defineCamera(m_mpCameras[ii][jj].transform.position, 
                                                                    m_mpCameras[ii][jj].transform.rotation, m_mpCameras[ii][jj].GetComponent<TrackBallCamera>().target());
                    }
                }
            }
            
        }


        public void setEdgesMode(bool spScene, bool show)
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