using System.Linq;
using UnityEngine;
using System.IO;
using System;
using HBP.Data;
using HBP.UI;
using Tools.Unity;
using System.Collections;
using HBP.Module3D;
using HBP.UI.Module3D;
using HBP.Data.Visualization;

public class DebugBenjamin : MonoBehaviour
{
#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            StartVideo();
        }
    }
    private void MarsAtlasCCEP()
    {
        DirectoryInfo dir = new DirectoryInfo(@"D:\HBP\CCEP\07-bids_20190416\converted");
        FileInfo[] files = dir.GetFiles("*.vhdr");
        foreach (var file in files)
        {
            ApplicationState.ProjectLoaded.Datasets[0].AddData(new HBP.Data.Experience.Dataset.CCEPDataInfo("ccep", new HBP.Data.Container.BrainVision(file.FullName, Guid.NewGuid().ToString()), ApplicationState.ProjectLoaded.Patients[0], file.Name.Replace(file.Extension, "")));
        }
    }
    private void GetAllCCEPData()
    {
        string ccepDB = @"D:\HBP\CCEP\07-bids_20190416\07-bids";
        DirectoryInfo baseDir = new DirectoryInfo(ccepDB);
        DirectoryInfo[] patientDirs = baseDir.GetDirectories("sub-*");
        foreach (var dir in patientDirs)
        {
            string patientName = dir.Name.Substring(4);
            Patient patient = ApplicationState.ProjectLoaded.Patients.FirstOrDefault(p => p.Name == patientName);
            if (patient == null) continue;
            DirectoryInfo ieegDir = new DirectoryInfo(Path.Combine(dir.FullName, "ses-postimp01", "ieeg"));
            FileInfo[] files = ieegDir.GetFiles("*.vhdr").Where(f => f.FullName.Contains("ccep")).ToArray();
            foreach (var file in files)
            {
                string site = file.Name.Split('_')[3].Substring(4, 8);
                if (!site.Contains("p")) site = site.Substring(0, 6);
                site = site.Insert(site.Length / 2, "-");
                ApplicationState.ProjectLoaded.Datasets[0].AddData(new HBP.Data.Experience.Dataset.CCEPDataInfo("ccep", new HBP.Data.Container.BrainVision(file.FullName, Guid.NewGuid().ToString()), patient, site));
            }
        }
    }
    private void ScreenshotWindow()
    {
        string path = FileBrowser.GetSavedFileName();
        StartCoroutine(c_ScreenshotWindow(path));
    }
    private IEnumerator c_ScreenshotWindow(string path)
    {
        yield return new WaitForEndOfFrame();
        Window window = ApplicationState.WindowsManager.WindowsReferencer.Windows.FirstOrDefault(w => w.GetComponent<Selector>().Selected);
        if (!string.IsNullOrEmpty(path))
        {
            Texture2D image = Texture2DExtension.ScreenRectToTexture(window.GetComponent<RectTransform>().ToScreenSpace());
            image.filterMode = FilterMode.Trilinear;
            image.SaveToPNG(path);
        }
    }

    private HBP.Module3D.DLL.VideoStream m_VideoStream;
    [SerializeField] private Canvas m_Canvas;
    private int m_TotalWidth = 1920;
    private int m_TotalHeight = 1080;
    private int m_TimelineHeight = 20;
    private void StartVideo()
    {
        if (!(ApplicationState.Module3D.SelectedColumn is Column3DDynamic))
            return;

        float fps = ApplicationState.Module3D.SelectedScene.ColumnsDynamic[0].Timeline.Step;
        m_VideoStream = new HBP.Module3D.DLL.VideoStream();
        m_VideoStream.Open("D:/HBP/TestVideo.avi", m_TotalWidth, m_TotalHeight, fps);
        StartCoroutine(c_Video());
    }
    private IEnumerator c_Video()
    {
        HBP.Module3D.DLL.Texture texture = new HBP.Module3D.DLL.Texture();
        Texture2D texture2D = new Texture2D(m_TotalWidth, m_TotalHeight);
        texture.Reset(m_TotalWidth, m_TotalHeight);

        Base3DScene scene = ApplicationState.Module3D.SelectedScene;
        int numberOfColumns = ApplicationState.Module3D.SelectedScene.Columns.Count;
        int numberOfViewLines = ApplicationState.Module3D.SelectedScene.ViewLineNumber;
        Timeline timeline = scene.ColumnsDynamic[0].Timeline;
        int timelineLength = timeline.Length;

        Color[] timelineColors = Enumerable.Repeat(Color.white, m_TimelineHeight * m_TotalWidth).ToArray();
        Color[] timelineCursorColors = Enumerable.Repeat(Color.black, m_TimelineHeight * 5).ToArray();
        Color[] verticalSeparatorColors = Enumerable.Repeat(Color.black, m_TotalHeight * 3).ToArray();
        Color[] horizontalSeparatorColors = Enumerable.Repeat(Color.black, m_TotalWidth * 3).ToArray();

        for (int i = 0; i < timelineLength; i++)
        {
            foreach (var column in scene.ColumnsDynamic)
                column.Timeline.CurrentIndex = i;

            yield return new WaitForEndOfFrame();

            int width = m_TotalWidth / numberOfColumns;
            int height = m_TotalHeight / numberOfViewLines;

            for (int j = 0; j < numberOfColumns; ++j)
            {
                int horizontalOffset = j * width;
                // 3D
                for (int k = 0; k < numberOfViewLines; ++k)
                {
                    int verticalOffset = (numberOfViewLines - 1 - k) * height;
                    Texture2D subTexture = scene.Columns[j].Views[k].GetTexture(width, height, new Color((float)40 / 255, (float)40 / 255, (float)40 / 255, 1.0f));
                    texture2D.SetPixels(horizontalOffset, verticalOffset, width, height, subTexture.GetPixels());
                }
                // Overlay
                Colormap colormap = ApplicationState.Module3DUI.Scenes[scene].Scene3DUI.Columns[j].Colormap;
                Texture2D colormapTexture = Texture2DExtension.ScreenRectToTexture(colormap.GetComponent<RectTransform>().ToScreenSpace());
                texture2D.SetPixels(horizontalOffset + 5, m_TotalHeight - 5 - colormapTexture.height, colormapTexture.width, colormapTexture.height, colormapTexture.GetPixels());
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
                texture2D.SetPixels(j * width - 1, 0, 3, m_TotalHeight, verticalSeparatorColors);

            for (int j = 1; j < numberOfViewLines; ++j)
                texture2D.SetPixels(0, j * height - 1, m_TotalWidth, 3, horizontalSeparatorColors);
            
            /*
            texture2D.SetPixels(0, 0, 1920, m_TimelineHeight, timeline);
            int cursorPosition = i * (1915 / (timelineLength - 1));
            texture2D.SetPixels(cursorPosition, 0, 5, m_TimelineHeight, timelineCursor);
            */

            texture.FromTexture2D(texture2D);

            for (int j = 0; j < numberOfColumns; j++)
                texture.WriteText(scene.Columns[j].Name, j * width + (width / 2), 20);

            texture.WriteText(string.Format("{0}ms", timeline.CurrentSubtimeline.GetLocalTime(timeline.CurrentIndex).ToString("N2")), m_TotalWidth / 2, m_TotalHeight - 20);

            m_VideoStream.WriteFrame(texture);
        }
        m_VideoStream.Dispose();
        m_VideoStream = null;
    }
#endif
}
