using HBP.Module3D;
using Tools.Unity;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tools.Unity.ResizableGrid;
using UnityEngine;

namespace HBP.UI.Module3D
{
    public class Scene3DWindow : MonoBehaviour
    {
        #region Properties
        private Base3DScene m_Scene;
        public GameObject SceneUIPrefab;
        public GameObject CutUIPrefab;
        public GameObject GraphsUIPrefab;
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
            grid.AddViewLine(SceneUIPrefab);
            grid.Columns.Last().Views.Last().GetComponent<Scene3DUI>().Initialize(scene);
            // Graphs
            grid.AddColumn(null, GraphsUIPrefab);
            Graph.GraphsGestion graphsGestion = grid.Columns.Last().Views.Last().GetComponent<Graph.GraphsGestion>();
            graphsGestion.Scene = scene;
            // Cuts
            grid.AddColumn(null, CutUIPrefab);
            grid.Columns.Last().Views.Last().GetComponent<CutController>().Initialize(scene);
            // Positions
            grid.VerticalHandlers[0].MagneticPosition = 0.45f;
            grid.VerticalHandlers[0].Position = 1.0f;
            grid.VerticalHandlers[1].MagneticPosition = 0.9f;
            grid.VerticalHandlers[1].Position = 0.9f;
            grid.SetVerticalHandlersPosition(1);

            ApplicationState.Module3D.OnRemoveScene.AddListener((s) =>
            {
                if (s == scene)
                {
                    Destroy(gameObject);
                }
            });
            scene.OnChangeVisibleState.AddListener((value) =>
            {
                gameObject.SetActive(value);
            });
            scene.OnRequestScreenshot.AddListener((multipleFiles) =>
            {
                string screenshotsPath = ApplicationState.GeneralSettings.DefaultScreenshotsLocation;
                if (string.IsNullOrEmpty(screenshotsPath))
                {
                    screenshotsPath = Path.GetFullPath(Application.dataPath + "/../Screenshots/");
                }
                else if (!screenshotsPath.EndsWith("/"))
                {
                    screenshotsPath += "/";
                }
                if (!Directory.Exists(screenshotsPath))
                {
                    Directory.CreateDirectory(screenshotsPath);
                }
                SaveSceneToPNG(screenshotsPath, multipleFiles);
            });
            graphsGestion.OnOpenGraphsWindow.AddListener(() =>
            {
                grid.VerticalHandlers[0].Position = grid.VerticalHandlers[0].MagneticPosition;
                grid.SetVerticalHandlersPosition(1);
                grid.UpdateAnchors();
            });
            graphsGestion.OnCloseGraphsWindow.AddListener(() =>
            {
                grid.VerticalHandlers[0].Position = grid.VerticalHandlers[1].Position - (grid.MinimumViewWidth / grid.RectTransform.rect.width);
                grid.SetVerticalHandlersPosition(1);
                grid.UpdateAnchors();
            });
        }

        public void SaveSceneToPNG(string path, bool multipleFiles = false)
        {
            StartCoroutine(c_SaveSceneToPNG(path, multipleFiles));
        }
        #endregion

        #region Coroutines
        private IEnumerator c_SaveSceneToPNG(string path, bool multipleFiles)
        {
            yield return new WaitForEndOfFrame();
            try
            {
                if (multipleFiles) // TODO : add iconic scenario and / or scales
                {
                    string screenshotsPath = path + m_Scene.Name;
                    ClassLoaderSaver.GenerateUniqueDirectoryPath(ref screenshotsPath);
                    Directory.CreateDirectory(screenshotsPath);
                    screenshotsPath += "/";
                    // Scene
                    for (int c = 0; c < m_Scene.ColumnManager.Columns.Count; c++)
                    {
                        Column3D column = m_Scene.ColumnManager.Columns[c];
                        if (!column.IsMinimized)
                        {
                            for (int v = 0; v < column.Views.Count; v++)
                            {
                                View3D view = column.Views[v];
                                if (!view.IsMinimized)
                                {
                                    view.ScreenshotTexture.SaveToPNG(screenshotsPath + "C" + c + "V" + v + ".png");
                                }
                            }
                        }
                    }
                    // Cuts
                    CutController cutUI = GetComponentInChildren<CutController>();
                    Texture2D[] cutTextures = cutUI.CutTextures;
                    for (int i = 0; i < cutTextures.Length; i++)
                    {
                        cutTextures[i].SaveToPNG(screenshotsPath + "Cut" + i + ".png");
                    }
                    // Graph and Trial Matrix
                    Graph.GraphsGestion graphsGestion = GetComponentInChildren<Graph.GraphsGestion>();
                    if (!graphsGestion.IsMinimized)
                    {
                        if (!Mathf.Approximately(graphsGestion.GetComponent<ZoneResizer>().Ratio, 1.0f))
                        {
                            global::Tools.Unity.Graph.Graph graph = graphsGestion.transform.GetComponentInChildren<global::Tools.Unity.Graph.Graph>();
                            Texture2D graphTexture = Texture2DExtension.ScreenRectToTexture(graph.GetComponent<RectTransform>().ToScreenSpace());
                            graphTexture.SaveToPNG(screenshotsPath + "Graph.png");
                        }
                        if (!Mathf.Approximately(graphsGestion.GetComponent<ZoneResizer>().Ratio, 0.0f)) // FIXME : trial matrix is not completely screenshoted because it is part of a scrollrect
                        {
                            RectTransform trialMatrixRectTransform = graphsGestion.transform.Find("TrialZone").Find("TrialMatrix").GetComponent<RectTransform>();
                            Texture2D trialMatrixTexture = Texture2DExtension.ScreenRectToTexture(trialMatrixRectTransform.ToScreenSpace());
                            trialMatrixTexture.SaveToPNG(screenshotsPath + "TrialMatrix.png");
                        }
                    }
                    // Feedback
                    ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Informational, "Screenshots saved", "Screenshots have been saved in " + screenshotsPath);
                }
                else
                {
                    Rect sceneRect = GetComponent<RectTransform>().ToScreenSpace();
                    Texture2D sceneTexture = Texture2DExtension.ScreenRectToTexture(sceneRect);
                    string screenshotPath = path + m_Scene.Name + ".png";
                    ClassLoaderSaver.GenerateUniqueSavePath(ref screenshotPath);
                    sceneTexture.SaveToPNG(screenshotPath);
                    ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Informational, "Screenshot saved", "A screenshot of the scene has been saved at " + screenshotPath);
                }
            }
            catch
            {
                ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Screenshots could not be saved", "Please verify your rights");
            }
        }
        #endregion
    }
}