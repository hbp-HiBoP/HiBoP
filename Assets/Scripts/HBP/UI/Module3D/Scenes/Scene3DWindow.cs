using HBP.Module3D;
using Tools.Unity;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tools.Unity.ResizableGrid;
using UnityEngine;
using UnityEngine.UI;

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
            // Information
            grid.AddColumn(null, GraphsUIPrefab);
            Informations.Informations informations = grid.Columns.Last().Views.Last().GetComponent<Informations.Informations>();
            informations.Scene = scene;
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
            informations.OnOpenInformationsWindow.AddListener(() =>
            {
                grid.VerticalHandlers[0].Position = grid.VerticalHandlers[0].MagneticPosition;
                grid.SetVerticalHandlersPosition(1);
                grid.UpdateAnchors();
            });
            informations.OnCloseInformationsWindow.AddListener(() =>
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
                                try
                                {
                                    view.ScreenshotTexture.SaveToPNG(screenshotsPath + "C" + c + "V" + v + ".png");
                                }
                                catch
                                {
                                    ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Screenshots could not be saved", "Please verify your rights");
                                    yield break;
                                }
                            }
                        }
                    }
                }
                // Cuts
                CutController cutUI = GetComponentInChildren<CutController>();
                Texture2D[] cutTextures = cutUI.CutTextures;
                for (int i = 0; i < cutTextures.Length; i++)
                {
                    try
                    {
                        cutTextures[i].SaveToPNG(screenshotsPath + "Cut" + i + ".png");
                    }
                    catch
                    {
                        ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Screenshots could not be saved", "Please verify your rights");
                        yield break;
                    }
                }
                // Graph and Trial Matrix
                Informations.Informations informations = GetComponentInChildren<Informations.Informations>();
                if (!informations.IsMinimized)
                {
                    if (!Mathf.Approximately(informations.GetComponent<ZoneResizer>().Ratio, 1.0f))
                    {
                        global::Tools.Unity.Graph.Graph graph = informations.transform.GetComponentInChildren<global::Tools.Unity.Graph.Graph>();
                        Texture2D graphTexture = Texture2DExtension.ScreenRectToTexture(graph.GetComponent<RectTransform>().ToScreenSpace());
                        try
                        {
                            graphTexture.SaveToPNG(screenshotsPath + "Graph.png");
                        }
                        catch
                        {
                            ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Screenshots could not be saved", "Please verify your rights");
                            yield break;
                        }
                        try
                        {
                            using (StreamWriter sw = new StreamWriter(screenshotsPath + "Graph.svg"))
                            {
                                sw.Write(graph.ToSVG());
                            }
                        }
                        catch
                        {
                            ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Screenshots could not be saved", "Please verify your rights");
                            yield break;
                        }
                        Dictionary<string, string> curveValues = graph.ToCSV();
                        try
                        {
                            foreach (var curve in curveValues)
                            {
                                using (StreamWriter sw = new StreamWriter(screenshotsPath + curve.Key + ".csv"))
                                {
                                    sw.Write(curve.Value);
                                }
                            }
                        }
                        catch
                        {
                            ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Screenshots could not be saved", "Please verify your rights");
                            yield break;
                        }
                    }
                    if (!Mathf.Approximately(informations.GetComponent<ZoneResizer>().Ratio, 0.0f))
                    {
                        ScrollRect trialMatrixScrollRect = informations.transform.Find("TrialZone").Find("TrialMatrix").GetComponent<ScrollRect>();
                        informations.ChangeOverlayState(false);
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
                            trialMatrixTexture.SaveToPNG(screenshotsPath + "TrialMatrix.png");
                        }
                        catch
                        {
                            ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Screenshots could not be saved", "Please verify your rights");
                            yield break;
                        }
                        informations.ChangeOverlayState(true);
                        trialMatrixScrollRect.viewport.GetComponent<Image>().sprite = mask;
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
                try
                {
                    sceneTexture.SaveToPNG(screenshotPath);
                }
                catch
                {
                    ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Screenshots could not be saved", "Please verify your rights");
                    yield break;
                }
                ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Informational, "Screenshot saved", "A screenshot of the scene has been saved at " + screenshotPath);
            }
        }
        #endregion
    }
}