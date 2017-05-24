/**
 * \file    CamerasManager.cs
 * \author  Lance Florian - Adrien Gannerie
 * \date    2015 - 2017
 * \brief   Define CamerasManager class
 */
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.ImageEffects;
using System.Collections.Generic;

namespace HBP.Module3D.Cam
{
    /// <summary>
    /// A manager for all the single patient and multi-patients cameras used in the scene.
    /// </summary>
    public class CamerasManager : MonoBehaviour
    {
        #region Properties
        // Cameras.    
        List<List<TrackBallCamera>> m_SinglePatientCameras = new List<List<TrackBallCamera>>(); /**< SP cameras list */
        List<List<TrackBallCamera>> m_MultiPatientsCameras = new List<List<TrackBallCamera>>(); /**< MP cameras list */
        Transform m_SinglePatientCamerasContainer = null;
        public Transform SinglePatientCamerasContainer { get { return m_SinglePatientCamerasContainer; } }
        Transform m_MultiPatientsCamerasContainer = null;
        public Transform MultiPatientsCamerasContainer { get { return m_MultiPatientsCamerasContainer; } }

        // UI. 
        List<RectTransform> m_SinglePatientLines = new List<RectTransform>(); /**< SP line view panel list*/
        List<RectTransform> m_MultiPatientsLines = new List<RectTransform>(); /**< MP line view panel list*/

        [SerializeField, Candlelight.PropertyBackingField]
        private RectTransform m_SinglePatientPanel;
        public RectTransform SinglePatientPanel
        {
            get { return m_SinglePatientPanel; }
            set { m_SinglePatientPanel = value; }
        }
        [SerializeField, Candlelight.PropertyBackingField]
        private RectTransform m_MultiPatientsPanel;
        public RectTransform MultiPatientsPanel
        {
            get { return m_MultiPatientsPanel; }
            set { m_MultiPatientsPanel = value; }
        }

        // other
        bool m_FocusedOn3D = true;

        [SerializeField, Candlelight.PropertyBackingField]
        private int m_MaximumNumberOfColumns = 10;
        public int MaximumNumberOfColumns
        {
            get { return m_MaximumNumberOfColumns; }
            set { m_MaximumNumberOfColumns = value; }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private int m_MaximumNumberOfLines = 5;
        public int MaximumNumberOfLines
        {
            get { return m_MaximumNumberOfLines; }
            set { m_MaximumNumberOfLines = value; }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="target"></param>
        public void UpdateCamerasTarget(SceneType scene, Vector3 target)
        {
            switch(scene)
            {
                case SceneType.SinglePatient:
                    foreach (var camerasLine in m_SinglePatientCameras) foreach (var camera in camerasLine) camera.Initialize(target);
                    break;
                case SceneType.MultiPatients:
                    foreach (var camerasLine in m_MultiPatientsCameras) foreach (var camera in camerasLine) camera.Initialize(target);
                    break;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="scene"></param>
        public void StopRotationOfAllCameras(SceneType scene)
        {
            switch(scene)
            {
                case SceneType.SinglePatient:
                    foreach (var camerasLine in m_SinglePatientCameras) foreach (var camera in camerasLine) camera.StopAutomaticRotation();
                    break;
                case SceneType.MultiPatients:
                    foreach (var camerasLine in m_MultiPatientsCameras) foreach (var camera in camerasLine) camera.StopAutomaticRotation();
                    break;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="scene"></param>
        public void RotateAllCameras(SceneType scene)
        {
            switch(scene)
            {
                case SceneType.SinglePatient:
                    foreach (var camerasLine in m_SinglePatientCameras) foreach (var camera in camerasLine) camera.AutomaticCameraRotation();
                    break;
                case SceneType.MultiPatients:
                    foreach (var camerasLine in m_MultiPatientsCameras) foreach (var camera in camerasLine) camera.AutomaticCameraRotation();
                    break;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="focusedOn3D"></param>
        public void SetModuleFocus(bool focusedOn3D)
        {
            m_FocusedOn3D = focusedOn3D;
            foreach (var camerasLine in m_SinglePatientCameras) foreach (var camera in camerasLine) camera.IsFocusedOn3DModule = focusedOn3D;
            foreach (var camerasLine in m_MultiPatientsCameras) foreach (var camera in camerasLine) camera.IsFocusedOn3DModule = focusedOn3D;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="columnNumber"></param>
        /// <param name="lineNumber"></param>
        /// <returns></returns>
        public TrackBallCamera GetCamera(SceneType scene, int columnNumber, int lineNumber)
        {
            TrackBallCamera result = null;
            switch(scene)
            {
                case SceneType.SinglePatient:
                    result = m_SinglePatientCameras[lineNumber][columnNumber];
                    break;
                case SceneType.MultiPatients:
                    result = m_MultiPatientsCameras[lineNumber][columnNumber];
                    break;
            }
            return result;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rectTransform"></param>
        /// <param name="camera"></param>
        /// <returns></returns>
        public static Rect GetScreenRectangle(RectTransform rectTransform, Camera camera)
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
        /// 
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public int GetNumberOfColumns(SceneType scene)
        {
            int result = -1;
            switch(scene)
            {
                case SceneType.SinglePatient:
                    result = m_SinglePatientCameras[0].Count;
                    break;
                case SceneType.MultiPatients:
                    result = m_MultiPatientsCameras[0].Count;
                    break;
            }
            return result;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public int GetNumberOfLines(SceneType scene)
        {
            int result = -1;
            switch (scene)
            {
                case SceneType.SinglePatient:
                    result = m_SinglePatientLines.Count;
                    break;
                case SceneType.MultiPatients:
                    result = m_MultiPatientsLines.Count;
                    break;
            }
            return result;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public RectTransform GetSceneRectTransform(SceneType scene)
        {
            RectTransform result = null;
            switch(scene)
            {
                case SceneType.SinglePatient:
                    result = m_SinglePatientPanel;
                    break;
                case SceneType.MultiPatients:
                    result = m_MultiPatientsPanel;
                    break;
            }
            return result;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="columnNumber"></param>
        /// <param name="lineNumber"></param>
        /// <returns></returns>
        public RectTransform GetCameraRectTransform(SceneType scene, int columnNumber, int lineNumber)
        {
            RectTransform result = null;
            switch(scene)
            {
                case SceneType.SinglePatient:
                    if (lineNumber < m_SinglePatientLines.Count)
                    {
                        Transform convView = m_SinglePatientLines[lineNumber].transform.Find("conv_view_" + columnNumber);
                        if (convView != null)
                            result = convView.GetComponent<RectTransform>();
                    }
                    break;
                case SceneType.MultiPatients:
                    if (lineNumber < m_MultiPatientsLines.Count)
                    {
                        Transform convView = m_MultiPatientsLines[lineNumber].transform.Find("conv_view_" + columnNumber);
                        if (convView != null)
                            result = convView.GetComponent<RectTransform>();
                    }
                    break;
            }
            return result;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="scene"></param>
        public void AddLine(SceneType scene)
        {
            switch(scene)
            {
                case SceneType.SinglePatient:
                    AddLine(m_SinglePatientCameras, m_SinglePatientLines, m_SinglePatientCamerasContainer, "Single patient");
                    break;
                case SceneType.MultiPatients:
                    AddLine(m_MultiPatientsCameras, m_MultiPatientsLines, m_MultiPatientsCamerasContainer, "Multi patients");
                    break;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="scene"></param>
        public void RemoveLine(SceneType scene)
        {
            switch(scene)
            {
                case SceneType.SinglePatient:
                    RemoveViewLineCameras(m_SinglePatientCameras, m_SinglePatientLines);
                    break;
                case SceneType.MultiPatients:
                    RemoveViewLineCameras(m_MultiPatientsCameras, m_MultiPatientsLines);
                    break;
            }
        }
        /// <summary>
        /// Define the number of columns cameras of the scene
        /// </summary>
        /// <param name="singlePatientScene"></param>
        /// <param name="numberOfColumns"></param>
        public void SetUpCameras(Base3DScene scene, int numberOfColumns)
        {
            for (int i = 0; i < numberOfColumns; i++)
            {
                AddColumnCamera(scene, CameraType.EEG);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="column"></param>
        public void AddColumnCamera(Base3DScene scene, CameraType column)
        {
            switch(scene.Type)
            {
                case SceneType.SinglePatient:
                    AddColumnCameras(m_SinglePatientCameras, m_SinglePatientLines, m_SinglePatientCamerasContainer.transform, "singlePatient_camera_tb_c", scene.Type, column);
                    break;
                case SceneType.MultiPatients:
                    AddColumnCameras(m_MultiPatientsCameras, m_MultiPatientsLines, m_MultiPatientsCamerasContainer.transform, "multiPatients_camera_tb_c", scene.Type, column);
                    break;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="scene"></param>
        public void RemoveLastColumnCamera(Base3DScene scene)
        {
            switch(scene.Type)
            {
                case SceneType.SinglePatient:
                    RemoveLastColumnCameras(m_SinglePatientCameras, m_SinglePatientLines);
                    break;
                case SceneType.MultiPatients:
                    RemoveLastColumnCameras(m_MultiPatientsCameras, m_MultiPatientsLines);
                    break;
            }
        }
        /// <summary>
        /// Update the layer mask of all the cameras of a scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="layerMask"></param>
        public void UpdateCamerasLayerMask(SceneType scene, int layerMask)
        {
            List<List<TrackBallCamera>> collection = new List<List<TrackBallCamera>>();
            switch(scene)
            {
                case SceneType.SinglePatient:
                    collection = m_SinglePatientCameras;
                    break;
                case SceneType.MultiPatients:
                    collection = m_MultiPatientsCameras;
                    break;
            }
            foreach (var item in collection) foreach (var camera in item) camera.UpdateCullingMask(layerMask);
        }
        /// <summary>
        /// 
        /// </summary>
        public void ApplyMultiPatientsCamerasSettingsToSinglePatientCameras()
        {
            if (m_SinglePatientCameras.Count < m_MultiPatientsCameras.Count)
            {
                int diff = m_MultiPatientsCameras.Count - m_SinglePatientCameras.Count;
                for (int ii = 0; ii < diff; ++ii)
                {
                    AddLine(SceneType.SinglePatient);
                }
            }
            else if (m_SinglePatientCameras.Count > m_MultiPatientsCameras.Count)
            {
                int diff = m_SinglePatientCameras.Count - m_MultiPatientsCameras.Count;
                for (int ii = 0; ii < diff; ++ii)
                {
                    RemoveLine(SceneType.SinglePatient);
                }
            }

            if (GetNumberOfColumns(SceneType.SinglePatient) == GetNumberOfColumns(SceneType.MultiPatients))
            {
                for (int ii = 0; ii < m_SinglePatientCameras.Count; ++ii)
                {
                    for (int jj = 0; jj < m_SinglePatientCameras[ii].Count; ++jj)
                    {
                        m_SinglePatientCameras[ii][jj].DefineCamera(m_MultiPatientsCameras[ii][jj].transform.position,
                                                                    m_MultiPatientsCameras[ii][jj].transform.rotation, m_MultiPatientsCameras[ii][jj].Target);
                    }
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="show"></param>
        public void SetEdgesMode(SceneType scene, bool show)
        {
            List<List<TrackBallCamera>> collection = new List<List<TrackBallCamera>>();
            switch (scene)
            {
                case SceneType.SinglePatient:
                    collection = m_SinglePatientCameras;
                    break;
                case SceneType.MultiPatients:
                    collection = m_MultiPatientsCameras;
                    break;
            }
            foreach (var item in collection)
            {
                foreach (var camera in item)
                {
                    camera.GetComponent<EdgeDetection>().enabled = show;
                }
            }
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            // Cameras Containers.
            m_SinglePatientCamerasContainer = transform.Find("Single patient container");
            m_MultiPatientsCamerasContainer = transform.Find("Multi patients container");

            // Single patient
            GameObject singlePatientLine = Instantiate(GlobalGOPreloaded.Line);
            singlePatientLine.SetActive(true);
            singlePatientLine.name = "Line n°0";
            singlePatientLine.transform.SetParent(m_SinglePatientPanel, false);
            m_SinglePatientLines.Add(singlePatientLine.GetComponent<RectTransform>());

            GameObject singlePatientColumn = Instantiate(GlobalGOPreloaded.View);
            singlePatientColumn.SetActive(true);
            singlePatientColumn.name = "Column n°0";
            singlePatientColumn.transform.SetParent(singlePatientLine.transform, false);

            TrackBallCamera singlePatientCamera = Instantiate(GlobalGOPreloaded.SinglePatientCamera).GetComponent<TrackBallCamera>();
            InitializeCamera(singlePatientCamera, SceneType.SinglePatient, 0, 0);

            List<TrackBallCamera> initListSinglePatientCameras = new List<TrackBallCamera>();
            initListSinglePatientCameras.Add(singlePatientCamera);
            m_SinglePatientCameras.Add(initListSinglePatientCameras);
            //SetUpCameras(SceneType.SinglePatient, 1);

            // Multi patients
            GameObject multiPatientLine = Instantiate(GlobalGOPreloaded.Line);
            multiPatientLine.SetActive(true);
            multiPatientLine.name = "Line n°0";
            multiPatientLine.transform.SetParent(m_MultiPatientsPanel.transform, false);
            m_MultiPatientsLines.Add(multiPatientLine.GetComponent<RectTransform>());

            GameObject multiPatientColumn = Instantiate(GlobalGOPreloaded.View);
            multiPatientColumn.SetActive(true);
            multiPatientColumn.transform.SetParent(multiPatientLine.transform, false);
            multiPatientColumn.name = "Column n°0";

            m_MultiPatientsCamerasContainer = transform.Find("Multi patients container");
            TrackBallCamera multiPatientsCamera = Instantiate(GlobalGOPreloaded.MultiPatientsCamera).GetComponent<TrackBallCamera>();
            InitializeCamera(multiPatientsCamera, SceneType.MultiPatients, 0, 0);

            List<TrackBallCamera> initListMultiPatientsCameras = new List<TrackBallCamera>();
            initListMultiPatientsCameras.Add(multiPatientsCamera.GetComponent<TrackBallCamera>());
            m_MultiPatientsCameras.Add(initListMultiPatientsCameras);
            //SetUpCameras(SceneType.MultiPatients, 1);
        }
        private void Update()
        {
            UpdateCamerasViewPort();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="scene"></param>
        /// <param name="idColumn"></param>
        /// <param name="idLine"></param>
        private void InitializeCamera(TrackBallCamera camera, SceneType scene, int idColumn, int idLine)
        {
            switch(scene)
            {
                case SceneType.SinglePatient:
                    camera.tag = "SingleCamera";
                    camera.name = "Single Patient Camera Column n°" + idColumn + " Line n°" + idLine;
                    camera.transform.SetParent(m_SinglePatientCamerasContainer);
                    camera.Initialize(m_SinglePatientPanel.position);
                    break;
                case SceneType.MultiPatients:
                    camera.tag = "MultiCamera";
                    camera.name = "Multi Patients Camera Column n°" + idColumn + " Line n°" + idLine;
                    camera.transform.SetParent(m_MultiPatientsCamerasContainer);
                    camera.GetComponent<TrackBallCamera>().Initialize(m_MultiPatientsPanel.position);
                    break;
            }
            camera.gameObject.SetActive(true);
        }
        /// <summary>
        /// Update the viewport rectangle of all the cameras
        /// </summary>
        private void UpdateCamerasViewPort()
        {
            Rect viewportRect = new Rect();

            // Single patient
            for (int l = 0; l < m_SinglePatientCameras.Count; ++l)
            {
                for (int c = 0; c < m_SinglePatientCameras[l].Count; ++c)
                {
                    Transform column = m_SinglePatientLines[l].transform.Find("Column n°" + c);
                    LayoutElement layout = column.GetComponent<LayoutElement>();

                    if (m_SinglePatientCameras[l][c].GetComponent<TrackBallCamera>().IsMinimized)
                    {
                        layout.preferredWidth = 20;

                        RectTransform currentRectT = column.GetComponent<RectTransform>();
                        //viewportRect = GetScreenRectangle(currentRectT, m_backgroundCamera);
                        m_SinglePatientCameras[l][c].GetComponent<Camera>().rect = new Rect(viewportRect.xMin / Screen.width, viewportRect.yMin / Screen.height, viewportRect.width / Screen.width, viewportRect.height / Screen.height);
                    }
                    else
                    {
                        layout.flexibleWidth = 1f;
                        layout.minWidth = 0;

                        m_SinglePatientCameras[l][c].gameObject.SetActive(true);
                        RectTransform currentRectT = column.GetComponent<RectTransform>();
                        //viewportRect = GetScreenRectangle(currentRectT, m_backgroundCamera);
                        m_SinglePatientCameras[l][c].GetComponent<Camera>().rect = new Rect(viewportRect.xMin / Screen.width, viewportRect.yMin / Screen.height, viewportRect.width / Screen.width, viewportRect.height / Screen.height);
                    }
                }
            }

            // mp
            for (int ii = 0; ii < m_MultiPatientsCameras.Count; ++ii)
            {
                for (int jj = 0; jj < m_MultiPatientsCameras[0].Count; ++jj)
                {
                    GameObject convView = m_MultiPatientsLines[ii].transform.Find("conv_view_" + jj).gameObject;
                    LayoutElement layout = convView.GetComponent<LayoutElement>();

                    if (m_MultiPatientsCameras[ii][jj].GetComponent<TrackBallCamera>().IsMinimized)
                    {
                        layout.flexibleWidth = 0.001f;
                        layout.minWidth = 20;

                        RectTransform currentRectT = convView.GetComponent<RectTransform>();
                        //viewportRect = screen_rectangle(currentRectT, m_backgroundCamera);
                        m_MultiPatientsCameras[ii][jj].GetComponent<Camera>().rect = new Rect(viewportRect.xMin / Screen.width, viewportRect.yMin / Screen.height, viewportRect.width / Screen.width, viewportRect.height / Screen.height);
                    }
                    else
                    {
                        layout.flexibleWidth = 1f;
                        layout.minWidth = 0;

                        m_MultiPatientsCameras[ii][jj].gameObject.SetActive(true);
                        RectTransform currentRectT = convView.GetComponent<RectTransform>();
                        //viewportRect = screen_rectangle(currentRectT, m_backgroundCamera);
                        m_MultiPatientsCameras[ii][jj].GetComponent<Camera>().rect = new Rect(viewportRect.xMin / Screen.width, viewportRect.yMin / Screen.height, viewportRect.width / Screen.width, viewportRect.height / Screen.height);
                    }
                }
            }
        }
        /// <summary>
        /// Add a new line of camera to the input cameras list
        /// </summary>
        /// <param name="cameras"></param>
        /// <param name="lines"></param>
        /// <param name="container"></param>
        /// <param name="baseName"></param>
        private void AddLine(List<List<TrackBallCamera>> cameras, List<RectTransform> lines, Transform container, string baseName)
        {
            int line = cameras.Count;
            if (line < m_MaximumNumberOfLines)
            {
                // add new camera line
                List<TrackBallCamera> newLine = new List<TrackBallCamera>();

                for (int column = 0; column < cameras[0].Count; ++column)
                {
                    TrackBallCamera camera = Instantiate(cameras[0][column]);
                    newLine.Add(camera);
                    camera.Line = line;
                    camera.Column = column;
                    camera.IsFocusedOn3DModule = m_FocusedOn3D;
                    camera.GetComponent<Camera>().depth = column;
                    camera.name = baseName + "Column n°" + column + " Line n°" + line;
                    camera.transform.SetParent(container);
                }
                cameras.Add(newLine);
                lines.Add(Instantiate(lines[0]));
                lines[lines.Count - 1].name = "Line n°" + (lines.Count - 1);
                lines[lines.Count - 1].transform.SetParent(lines[0].transform.parent, false);
            }
        }
        /// <summary>
        /// Remove a line of cameras to the input cameras list
        /// </summary>
        /// <param name="sceneCameras"></param>
        /// <param name="sceneLinesViewsPanels"></param>
        private void RemoveViewLineCameras(List<List<TrackBallCamera>> sceneCameras, List<RectTransform> sceneLinesViewsPanels)
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
        private void AddColumnCameras(List<List<TrackBallCamera>> sceneCameras, List<RectTransform> sceneLinesViewsPanels, Transform cameraParent, string sceneCameraBaseName, SceneType scene, CameraType column)
        {
            int currentConditionNumber = sceneCameras[0].Count;
            if (currentConditionNumber < m_MaximumNumberOfColumns)
            {
                // add new camera column
                for (int ii = 0; ii < sceneCameras.Count; ++ii)
                {
                    sceneCameras[ii].Add(Instantiate(sceneCameras[ii][0]));
                    sceneCameras[ii][currentConditionNumber].GetComponent<Camera>().depth = currentConditionNumber;
                    sceneCameras[ii][currentConditionNumber].name = sceneCameraBaseName + currentConditionNumber + "_v" + ii;
                    sceneCameras[ii][currentConditionNumber].transform.parent = cameraParent;

                    TrackBallCamera camera = sceneCameras[ii][currentConditionNumber].GetComponent<TrackBallCamera>();
                    camera.Line = ii;
                    camera.Column = currentConditionNumber;
                    camera.IsFocusedOn3DModule = m_FocusedOn3D;
                    camera.Type = column;

                    switch(scene)
                    {
                        case SceneType.SinglePatient:
                            camera.ColumnLayer = "C" + currentConditionNumber + "_SP";
                            break;
                        case SceneType.MultiPatients:
                            camera.ColumnLayer = "C" + currentConditionNumber + "_MP";
                            break;
                    }
                }

                for (int ii = 0; ii < sceneLinesViewsPanels.Count; ++ii)
                {
                    GameObject newView = Instantiate(sceneLinesViewsPanels[ii].transform.Find("Column n°0").gameObject);
                    newView.name = "Column n°" + (currentConditionNumber);
                    newView.transform.SetParent(sceneLinesViewsPanels[ii].transform, false);
                }
            }
        }
        /// <summary>
        /// Remove a column of cameras to the input cameras list
        /// </summary>
        /// <param name="sceneCameras"></param>
        /// <param name="sceneLinesViewsPanels"></param>
        private void RemoveLastColumnCameras(List<List<TrackBallCamera>> sceneCameras, List<RectTransform> sceneLinesViewsPanels)
        {
            int currentConditionNumber = sceneCameras[0].Count;
            if (currentConditionNumber > 1)
            {
                // destroy last added camera column
                for (int ii = 0; ii < sceneCameras.Count; ++ii)
                {
                    if (sceneCameras[ii][sceneCameras[ii].Count - 1].GetComponent<TrackBallCamera>().IsSelected())
                    {
                        sceneCameras[ii][sceneCameras[ii].Count - 1].GetComponent<TrackBallCamera>().UpdateSelectedColumn(0);
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
        #endregion
    }
}