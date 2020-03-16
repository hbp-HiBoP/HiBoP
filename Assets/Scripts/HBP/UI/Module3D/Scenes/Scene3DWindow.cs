using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tools.Unity;
using Tools.Unity.ResizableGrid;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// This class is used to properly display a full visualization (3D, information, cuts, site list)
    /// </summary>
    public class Scene3DWindow : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Associated logical scene
        /// </summary>
        private Base3DScene m_Scene;
        /// <summary>
        /// Reference the RectTransform of this object
        /// </summary>
        [SerializeField] private RectTransform m_RectTransform;

        /// <summary>
        /// Scene UI of this scene window
        /// </summary>
        public Scene3DUI Scene3DUI { get; private set; }
        /// <summary>
        /// Informations panel (graphs and trial matrices)
        /// </summary>
        public Informations.InformationsWrapper Informations { get; private set; }
        /// <summary>
        /// Sites informations panel (list and filter)
        /// </summary>
        public SitesInformations SitesInformations { get; private set; }
        /// <summary>
        /// Cut controller of this scene
        /// </summary>
        public CutController CutController { get; private set; }

        /// <summary>
        /// Prefab for the 3D scene column
        /// </summary>
        [SerializeField] private GameObject m_SceneUIPrefab;
        /// <summary>
        /// Prefab for the cuts panel column
        /// </summary>
        [SerializeField] private GameObject m_CutUIPrefab;
        /// <summary>
        /// Prefab for the informations panel column
        /// </summary>
        [SerializeField] private GameObject m_InformationsUIPrefab;
        /// <summary>
        /// Prefab for the site list column
        /// </summary>
        [SerializeField] private GameObject m_SitesInformationsPrefab;
        #endregion

        #region Private Methods
        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Rect rect = m_RectTransform.ToScreenSpace();
                Vector3 mousePosition = Input.mousePosition;
                if (mousePosition.x >= rect.x && mousePosition.x <= rect.x + rect.width && mousePosition.y >= rect.y && mousePosition.y <= rect.y + rect.height)
                {
                    PointerEventData pointerData = new PointerEventData(EventSystem.current)
                    {
                        pointerId = -1,
                        position = Input.mousePosition
                    };
                    List<RaycastResult> raycastResults = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(pointerData, raycastResults);
                    if (raycastResults.Count > 0)
                    {
                        if (raycastResults[0].gameObject.GetComponentInParent<Scene3DWindow>() == this)
                        {
                            m_Scene.IsSelected = true;
                        }
                    }
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the scene window
        /// </summary>
        /// <param name="scene">Associated Base3DScene</param>
        public void Initialize(Base3DScene scene)
        {
            m_Scene = scene;

            ResizableGrid grid = GetComponent<ResizableGrid>();
            // 3D
            grid.AddColumn();
            grid.AddViewLine(m_SceneUIPrefab);
            Scene3DUI = grid.Columns.Last().Views.Last().GetComponent<Scene3DUI>();
            Scene3DUI.Initialize(scene);
            // Information
            grid.AddColumn(null, m_InformationsUIPrefab);
            Informations = grid.Columns.Last().Views.Last().GetComponent<Informations.InformationsWrapper>();
            Informations.Scene = scene;
            // Sites
            grid.AddColumn(null, m_SitesInformationsPrefab);
            SitesInformations = grid.Columns.Last().Views.Last().GetComponent<SitesInformations>();
            SitesInformations.Initialize(scene);
            // Cuts
            grid.AddColumn(null, m_CutUIPrefab);
            CutController = grid.Columns.Last().Views.Last().GetComponent<CutController>();
            CutController.Initialize(scene);
            // Positions
            grid.VerticalHandlers[0].MagneticPosition = 0.45f;
            grid.VerticalHandlers[1].MagneticPosition = 0.75f;
            grid.VerticalHandlers[2].MagneticPosition = 0.9f;
            grid.VerticalHandlers[0].Position = 1.0f;
            grid.SetVerticalHandlersPosition(0);

            ApplicationState.Module3D.OnRemoveScene.AddSafeListener((s) =>
            {
                if (s == scene)
                {
                    Destroy(gameObject);
                }
            }, gameObject);
            scene.OnChangeVisibleState.AddListener((value) =>
            {
                gameObject.SetActive(value);
            });
            scene.OnRequestScreenshot.AddListener((multipleFiles) =>
            {
                string screenshotsPath = ApplicationState.UserPreferences.General.Project.DefaultExportLocation;
                if (string.IsNullOrEmpty(screenshotsPath))
                {
                    screenshotsPath = Path.GetFullPath(Application.dataPath + "/../Screenshots/");
                }
                else if (!screenshotsPath.EndsWith("/"))
                {
                    screenshotsPath += "/";
                }
                if (!Directory.Exists(screenshotsPath)) Directory.CreateDirectory(screenshotsPath);
                screenshotsPath += ApplicationState.ProjectLoaded.Preferences.Name + "/";
                if (!Directory.Exists(screenshotsPath)) Directory.CreateDirectory(screenshotsPath);
                screenshotsPath += m_Scene.Name + "/";
                if (!Directory.Exists(screenshotsPath)) Directory.CreateDirectory(screenshotsPath);
                Screenshot(screenshotsPath, multipleFiles);
            });
            Informations.OnExpand.AddListener(() =>
            {
                grid.VerticalHandlers[0].Position = grid.VerticalHandlers[0].MagneticPosition;
                grid.SetVerticalHandlersPosition(1);
                grid.UpdateAnchors();
            });
            Informations.OnMinimize.AddListener(() =>
            {
                grid.VerticalHandlers[0].Position = grid.VerticalHandlers[1].Position - (grid.MinimumViewWidth / grid.RectTransform.rect.width);
                grid.SetVerticalHandlersPosition(1);
                grid.UpdateAnchors();
            });
        }
        /// <summary>
        /// Take a screenshot of this scene
        /// </summary>
        /// <param name="path">Path to the directory to save the screenshot</param>
        /// <param name="multipleFiles">If true, multiple files (images, csv, svg ...) will be saved; if false, a simple screenshot of the whole window will be taken</param>
        public void Screenshot(string path, bool multipleFiles = false)
        {
            StartCoroutine(c_Screenshot(path, multipleFiles));
        }
        #endregion

        #region Coroutines
        /// <summary>
        /// Coroutine to take a screenshot of this scene
        /// </summary>
        /// <param name="path">Path to the directory to save the screenshot</param>
        /// <param name="multipleFiles">If true, multiple files (images, csv, svg ...) will be saved; if false, a simple screenshot of the whole window will be taken</param>
        /// <returns>Coroutine return</returns>
        private IEnumerator c_Screenshot(string path, bool multipleFiles)
        {
            yield return new WaitForEndOfFrame();

            string openedProjectName = ApplicationState.ProjectLoaded.Preferences.Name;

            if (multipleFiles) // TODO : add iconic scenario and / or scales
            {
                // Scene
                for (int c = 0; c < m_Scene.Columns.Count; c++)
                {
                    Column3D column = m_Scene.Columns[c];
                    if (!column.IsMinimized)
                    {
                        for (int v = 0; v < column.Views.Count; v++)
                        {
                            View3D view = column.Views[v];
                            if (!view.IsMinimized)
                            {
                                try
                                {
                                    string viewFilePath = path + string.Format("{0}_{1}_{2}_Brain.png", openedProjectName, m_Scene.Name, column.Name);
                                    ClassLoaderSaver.GenerateUniqueSavePath(ref viewFilePath);
                                    view.GetTexture(2048, 2048, new Color(0.0f, 0.0f, 0.0f, 0.0f)).SaveToPNG(viewFilePath);
                                }
                                catch (Exception e)
                                {
                                    Debug.LogException(e);
                                    ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Screenshots could not be saved", "Please verify your rights");
                                    yield break;
                                }
                            }
                        }
                    }
                }
                // Cuts
                CutController cutUI = GetComponentInChildren<CutController>();
                Tuple<Data.Enums.CutOrientation, Texture2D>[] cutTextures = cutUI.CutTextures;
                for (int i = 0; i < cutTextures.Length; i++)
                {
                    try
                    {
                        string cutFilePath = path + string.Format("{0}_{1}_{2}_Cut.png", openedProjectName, m_Scene.Name, cutTextures[i].Item1.ToString());
                        ClassLoaderSaver.GenerateUniqueSavePath(ref cutFilePath);
                        cutTextures[i].Item2.SaveToPNG(cutFilePath);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Screenshots could not be saved", "Please verify your rights");
                        yield break;
                    }
                }
                // Graph and Trial Matrix
                Informations.InformationsWrapper informations = GetComponentInChildren<Informations.InformationsWrapper>();
                Informations.ChannelInformations channelInformations = informations.GetComponentInChildren<Informations.ChannelInformations>();
                if (!informations.Minimized)
                {
                    if (!Mathf.Approximately(channelInformations.GetComponent<ZoneResizer>().Ratio, 1.0f))
                    {
                        global::Tools.Unity.Graph.Graph graph = channelInformations.transform.GetComponentInChildren<global::Tools.Unity.Graph.Graph>();
                        Texture2D graphTexture = Texture2DExtension.ScreenRectToTexture(graph.GetComponent<RectTransform>().ToScreenSpace());
                        try
                        {
                            string graphFilePath = path + string.Format("{0}_{1}_[{2}]_Graph.png", openedProjectName, m_Scene.Name, string.Join("-", informations.ChannelStructs.Select(cs => cs.Patient.Name + "_" + cs.Channel)));
                            ClassLoaderSaver.GenerateUniqueSavePath(ref graphFilePath);
                            graphTexture.SaveToPNG(graphFilePath);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Screenshots could not be saved", "Please verify your rights");
                            yield break;
                        }
                        try
                        {
                            string graphFilePath = path + string.Format("{0}_{1}_[{2}]_Graph.svg", openedProjectName, m_Scene.Name, string.Join("-", informations.ChannelStructs.Select(cs => cs.Patient.Name + "_" + cs.Channel)));
                            ClassLoaderSaver.GenerateUniqueSavePath(ref graphFilePath);
                            using (StreamWriter sw = new StreamWriter(graphFilePath))
                            {
                                sw.Write(graph.ToSVG());
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Screenshots could not be saved", "Please verify your rights");
                            yield break;
                        }
                        Dictionary<string, string> curveValues = graph.ToCSV();
                        try
                        {
                            foreach (var curve in curveValues)
                            {
                                string curveFilePath = path + string.Format("{0}_{1}_{2}_Curve.csv", openedProjectName, m_Scene.Name, curve.Key);
                                ClassLoaderSaver.GenerateUniqueSavePath(ref curveFilePath);
                                using (StreamWriter sw = new StreamWriter(curveFilePath))
                                {
                                    sw.Write(curve.Value);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Screenshots could not be saved", "Please verify your rights");
                            yield break;
                        }
                    }
                    if (!Mathf.Approximately(channelInformations.GetComponent<ZoneResizer>().Ratio, 0.0f))
                    {
                        ScrollRect trialMatrixScrollRect = channelInformations.GetComponentInChildren<TrialMatrix.Grid.TrialMatrixGrid>().GetComponent<ScrollRect>();
                        Sprite mask = trialMatrixScrollRect.viewport.GetComponent<Image>().sprite;
                        trialMatrixScrollRect.viewport.GetComponent<Image>().sprite = null;
                        Texture2D trialMatrixTexture;
                        if (trialMatrixScrollRect.content.rect.height > trialMatrixScrollRect.viewport.rect.height)
                        {
                            trialMatrixTexture = new Texture2D((int)trialMatrixScrollRect.content.rect.width, (int)trialMatrixScrollRect.content.rect.height);
                            float step = trialMatrixScrollRect.viewport.rect.height / trialMatrixScrollRect.content.rect.height;
                            float position = 0.0f;
                            bool isFinished = false;
                            while (!isFinished)
                            {
                                if (position > 1.0f)
                                {
                                    position = 1.0f;
                                    isFinished = true;
                                }
                                trialMatrixScrollRect.verticalNormalizedPosition = position;
                                yield return new WaitForEndOfFrame();
                                Texture2D trialMatrixTextureFragment = Texture2DExtension.ScreenRectToTexture(trialMatrixScrollRect.viewport.ToScreenSpace());
                                trialMatrixTexture.SetPixels(0, (int)(position * trialMatrixTexture.height - position * trialMatrixTextureFragment.height), trialMatrixTextureFragment.width, trialMatrixTextureFragment.height, trialMatrixTextureFragment.GetPixels());
                                position += step;
                            }
                        }
                        else
                        {
                            trialMatrixTexture = Texture2DExtension.ScreenRectToTexture(trialMatrixScrollRect.content.ToScreenSpace());
                        }
                        try
                        {
                            string trialMatrixFilePath = path + string.Format("{0}_{1}_[{2}]_TrialMatrix.png", openedProjectName, m_Scene.Name, string.Join("-", informations.ChannelStructs.Select(cs => cs.Patient.Name + "_" + cs.Channel)));
                            ClassLoaderSaver.GenerateUniqueSavePath(ref trialMatrixFilePath);
                            trialMatrixTexture.SaveToPNG(trialMatrixFilePath);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Screenshots could not be saved", "Please verify your rights");
                            yield break;
                        }
                        trialMatrixScrollRect.viewport.GetComponent<Image>().sprite = mask;
                    }
                }
                // Feedback
                ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Informational, "Screenshots saved", "Screenshots have been saved in " + path);
            }
            else
            {
                Rect sceneRect = GetComponent<RectTransform>().ToScreenSpace();
                Texture2D sceneTexture = Texture2DExtension.ScreenRectToTexture(sceneRect);
                string screenshotPath = path + string.Format("{0}_{1}_fullscene.png", openedProjectName, m_Scene.Name);
                ClassLoaderSaver.GenerateUniqueSavePath(ref screenshotPath);
                try
                {
                    sceneTexture.SaveToPNG(screenshotPath);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Screenshots could not be saved", "Please verify your rights");
                    yield break;
                }
                ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Informational, "Screenshot saved", "A screenshot of the scene has been saved at " + screenshotPath);
            }
        }
        #endregion
    }
}