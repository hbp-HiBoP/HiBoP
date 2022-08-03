using ThirdParty.CielaSpike;
using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tools.Unity;
using Tools.Unity.Components;
using Tools.Unity.ResizableGrid;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HBP.Core.Enums;
using HBP.Core.Tools;

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
        public void Screenshot(bool multipleFiles = false)
        {
            StartCoroutine(c_Screenshot(m_Scene.GenerateExportDirectory(), multipleFiles));
        }
        /// <summary>
        /// Take a video of the timeline of the scene
        /// </summary>
        /// <param name="path">Path to the directory to save the video</param>
        public void Video()
        {
            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            ApplicationState.LoadingManager.Load(c_Video(m_Scene.GenerateExportDirectory(), onChangeProgress), onChangeProgress);
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
                                    string viewFilePath = Path.Combine(path, string.Format("{0}_{1}_{2}_Brain.png", openedProjectName, m_Scene.Name, column.Name));
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
                Tuple<CutOrientation, Texture2D>[] cutTextures = cutUI.CutTextures;
                for (int i = 0; i < cutTextures.Length; i++)
                {
                    try
                    {
                        string cutFilePath = Path.Combine(path, string.Format("{0}_{1}_{2}_Cut.png", openedProjectName, m_Scene.Name, cutTextures[i].Item1.ToString()));
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
                Informations.GridInformations gridInformations = informations.GetComponentInChildren<Informations.GridInformations>();
                if (!informations.Minimized)
                {
                    if (channelInformations != null && channelInformations.isActiveAndEnabled)
                    {
                        if (!Mathf.Approximately(channelInformations.GetComponent<ZoneResizer>().Ratio, 1.0f))
                        {
                            global::Tools.Unity.Graph.Graph graph = channelInformations.transform.GetComponentInChildren<global::Tools.Unity.Graph.Graph>();
                            Texture2D graphTexture = Texture2DExtension.ScreenRectToTexture(graph.GetComponent<RectTransform>().ToScreenSpace());
                            var curvesName = graph.GetEnabledCurvesName();
                            try
                            {
                                string graphFilePath = Path.Combine(path, string.Format("{0}_{1}_{2}_Graph.png", openedProjectName, m_Scene.Name, string.Join("-", curvesName)));
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
                                string graphFilePath = Path.Combine(path, string.Format("{0}_{1}_{2}_Graph.svg", openedProjectName, m_Scene.Name, string.Join("-", curvesName)));
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
                                    string curveFilePath = Path.Combine(path, string.Format("{0}_{1}_{2}_Curve.csv", openedProjectName, m_Scene.Name, curve.Key));
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
                                CanvasScalerHandler canvasScalerHandler = GetComponentInParent<CanvasScalerHandler>();
                                float scale = canvasScalerHandler.Scale;
                                trialMatrixTexture = new Texture2D((int)(trialMatrixScrollRect.content.rect.width / scale), (int)(trialMatrixScrollRect.content.rect.height / scale));
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
                                List<string> names = new List<string>();
                                Core.Data.Patient currentPatient = null;
                                foreach (var channelStruct in informations.ChannelStructs.OrderBy(cs => cs.Patient.Name))
                                {
                                    if (currentPatient != channelStruct.Patient)
                                    {
                                        currentPatient = channelStruct.Patient;
                                        names.Add(currentPatient.Name);
                                    }
                                    names.Add(channelStruct.Channel);
                                }
                                string trialMatrixFilePath = Path.Combine(path, string.Format("{0}_{1}_{2}_TrialMatrix.png", openedProjectName, m_Scene.Name, string.Join("-", names)));
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
                    else if (gridInformations != null && gridInformations.isActiveAndEnabled)
                    {
                        foreach (var graph in gridInformations.Grid.Graphs.Where(g => g.IsSelected))
                        {
                            try
                            {
                                string graphFilePath = Path.Combine(path, string.Format("{0}_{1}_{2}_Graph.svg", openedProjectName, m_Scene.Name, string.Format("{0}-{1}", graph.ChannelStruct.Patient.Name, graph.ChannelStruct.Channel)));
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
                        }
                    }
                }
                // Feedback
                ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Informational, "Screenshots saved", "Screenshots have been saved in " + path);
            }
            else
            {
                Rect sceneRect = GetComponent<RectTransform>().ToScreenSpace();
                Texture2D sceneTexture = Texture2DExtension.ScreenRectToTexture(sceneRect);
                string screenshotPath = Path.Combine(path, string.Format("{0}_{1}_fullscene.png", openedProjectName, m_Scene.Name));
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
        /// <summary>
        /// Take a video of the current timeline
        /// </summary>
        /// <param name="path">Path to where to save the video</param>
        /// <returns>Coroutine return</returns>
        private IEnumerator c_Video(string path, GenericEvent<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpToUnity;

            int totalWidth = 1920;
            int totalHeight = 1080;
            int timelineSize = 10;
            int separatorSize = 3;

            Column3DDynamic selectedColumnDynamic = m_Scene.SelectedColumn as Column3DDynamic;
            Core.Data.Timeline timeline = selectedColumnDynamic.Timeline;
            float fps = timeline.Step;
            int numberOfColumns = m_Scene.Columns.Count;
            int numberOfViewLines = m_Scene.ViewLineNumber;
            int timelineLength = timeline.Length;

            string videoPath = path + string.Format("{0}_{1}.avi", ApplicationState.ProjectLoaded.Preferences.Name, m_Scene.Name);
            ClassLoaderSaver.GenerateUniqueSavePath(ref videoPath);

            Core.DLL.VideoStream videoStream = new Core.DLL.VideoStream();
            videoStream.Open(videoPath, totalWidth, totalHeight, fps);

            Core.DLL.Texture texture = new Core.DLL.Texture();
            Texture2D texture2D = new Texture2D(totalWidth, totalHeight);
            texture.Reset(totalWidth, totalHeight);
            
            Color[] timelineColors = Enumerable.Repeat(new Color((float)220 / 255, (float)220 / 255, (float)220 / 255, 1.0f), timelineSize * totalWidth).ToArray();
            Color[] mainEventColors = Enumerable.Repeat(new Color(1, 0, 0, 1.0f), timelineSize * timelineSize).ToArray();
            Color[] timelineCursorColors = Enumerable.Repeat(Color.black, timelineSize * timelineSize).ToArray();
            Color[] verticalSeparatorColors = Enumerable.Repeat(Color.black, totalHeight * separatorSize).ToArray();
            Color[] horizontalSeparatorColors = Enumerable.Repeat(Color.black, totalWidth * separatorSize).ToArray();

            for (int i = 0; i < timelineLength; i++)
            {
                onChangeProgress.Invoke((float)i / (timelineLength - 1), 0, new LoadingText("Taking video of the timeline"));

                foreach (var column in m_Scene.ColumnsDynamic)
                    column.Timeline.CurrentIndex = i;

                yield return new WaitForEndOfFrame();

                int width = totalWidth / numberOfColumns;
                int height = totalHeight / numberOfViewLines;

                for (int j = 0; j < numberOfColumns; ++j)
                {
                    int horizontalOffset = j * width;
                    // 3D
                    for (int k = 0; k < numberOfViewLines; ++k)
                    {
                        int verticalOffset = (numberOfViewLines - 1 - k) * height;
                        Texture2D subTexture = m_Scene.Columns[j].Views[k].GetTexture(width, height, new Color((float)40 / 255, (float)40 / 255, (float)40 / 255, 1.0f));
                        texture2D.SetPixels(horizontalOffset, verticalOffset, width, height, subTexture.GetPixels());
                    }
                    // Overlay - Not very good: needs a way to be drawn on its own
                    //Colormap colormap = Scene3DUI.Columns[j].Colormap;
                    //Texture2D colormapTexture = Texture2DExtension.ScreenRectToTexture(colormap.GetComponent<RectTransform>().ToScreenSpace());
                    //texture2D.SetPixels(horizontalOffset + 5, totalHeight - 5 - colormapTexture.height, colormapTexture.width, colormapTexture.height, colormapTexture.GetPixels());
                    //Icon icon = ApplicationState.Module3DUI.Scenes[scene].Scene3DUI.Columns[j].Icon;
                    //Texture2D iconTexture = icon.IsActive ? icon.Sprite.texture : null;
                    //if (iconTexture)
                    //{
                    //    Texture2D newIconTexture = new Texture2D(iconTexture.width, iconTexture.height);
                    //    newIconTexture.SetPixels(iconTexture.GetPixels());
                    //    float resizeFactor = 1f / (Mathf.Max(newIconTexture.width, newIconTexture.height) / 200);
                    //    newIconTexture.Resize((int)(resizeFactor * newIconTexture.width), (int)(resizeFactor * newIconTexture.height)); // does not work
                    //    texture2D.SetPixels(horizontalOffset + width - 5 - newIconTexture.width, 1080 - 5 - newIconTexture.height, newIconTexture.width, newIconTexture.height, newIconTexture.GetPixels());
                    //}
                }

                for (int j = 1; j < numberOfColumns; ++j)
                    texture2D.SetPixels(j * width - (separatorSize / 2), 0, separatorSize, totalHeight, verticalSeparatorColors);

                for (int j = 1; j < numberOfViewLines; ++j)
                    texture2D.SetPixels(0, j * height - (separatorSize / 2), totalWidth, separatorSize, horizontalSeparatorColors);

                texture2D.SetPixels(0, 0, totalWidth, timelineSize, timelineColors);
                foreach (var subTimeline in timeline.SubTimelinesBySubBloc.Values)
                {
                    int mainEventIndex = subTimeline.GlobalMinIndex + subTimeline.Frequency.ConvertToFlooredNumberOfSamples(subTimeline.StatisticsByEvent.FirstOrDefault(e => e.Key.Type == MainSecondaryEnum.Main).Value.RoundedTimeFromStart);
                    int mainEventPosition = mainEventIndex * ((totalWidth - timelineSize) / (timelineLength - 1));
                    texture2D.SetPixels(mainEventPosition, 0, timelineSize, timelineSize, mainEventColors);
                }
                int cursorPosition = i * ((totalWidth - timelineSize) / (timelineLength - 1));
                texture2D.SetPixels(cursorPosition, 0, timelineSize, timelineSize, timelineCursorColors);

                texture.FromTexture2D(texture2D);

                for (int j = 0; j < numberOfColumns; j++)
                    texture.WriteText(m_Scene.Columns[j].Name, j * width + (width / 2), 20);

                texture.WriteText(string.Format("{0}ms", timeline.CurrentSubtimeline.GetLocalTime(timeline.CurrentIndex).ToString("N2")), totalWidth / 2, totalHeight - 20);

                videoStream.WriteFrame(texture);
            }
            videoStream.Dispose();
            onChangeProgress.Invoke(1, 0, new LoadingText("Finished"));
            ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Informational, "Video saved", "A video of the scene has been saved at " + videoPath);
        }
        #endregion
    }
}