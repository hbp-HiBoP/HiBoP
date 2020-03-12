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
    private void StartVideo()
    {
        if (!(ApplicationState.Module3D.SelectedColumn is Column3DDynamic))
            return;

        float fps = ApplicationState.Module3D.SelectedScene.ColumnsDynamic[0].Timeline.Step;
        m_VideoStream = new HBP.Module3D.DLL.VideoStream();
        m_VideoStream.Open("D:/TestVideo.avi", 1920, 1080, fps);
        StartCoroutine(c_Video());
    }
    private IEnumerator c_Video()
    {
        HBP.Module3D.DLL.Texture texture = new HBP.Module3D.DLL.Texture();
        Texture2D texture2D = new Texture2D(1920, 1080);
        texture.Reset(1920, 1080);

        Base3DScene scene = ApplicationState.Module3D.SelectedScene;
        int numberOfColumns = ApplicationState.Module3D.SelectedScene.Columns.Count;
        int numberOfViewLines = ApplicationState.Module3D.SelectedScene.ViewLineNumber;
        int timelineLength = scene.ColumnsDynamic[0].Timeline.Length;

        Color[] timeline = Enumerable.Repeat(Color.white, 20 * 1920).ToArray();
        Color[] timelineCursor = Enumerable.Repeat(Color.black, 20 * 5).ToArray();
        Color[] separator = Enumerable.Repeat(Color.black, 1080 * 3).ToArray();
        for (int i = 0; i < timelineLength; i++)
        {
            foreach (var column in scene.ColumnsDynamic)
            {
                column.Timeline.CurrentIndex = i;
            }
            yield return new WaitForEndOfFrame();

            int width = 1920 / numberOfColumns;
            int height = 1080 / numberOfViewLines;

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
                if (horizontalOffset != 0) texture2D.SetPixels(horizontalOffset - 1, 0, 3, 1080, separator);
                // Overlay
                Colormap colormap = ApplicationState.Module3DUI.Scenes[scene].Scene3DUI.Columns[j].Colormap;
                Texture2D colormapTexture = Texture2DExtension.ScreenRectToTexture(colormap.GetComponent<RectTransform>().ToScreenSpace());
                texture2D.SetPixels(horizontalOffset + 5, 1080 - 5 - colormapTexture.height, colormapTexture.width, colormapTexture.height, colormapTexture.GetPixels());
                Icon icon = ApplicationState.Module3DUI.Scenes[scene].Scene3DUI.Columns[j].Icon;
                Texture2D iconTexture = icon.IsActive ? icon.Sprite.texture : null;
                if (iconTexture)
                {
                    Texture2D newIconTexture = new Texture2D(iconTexture.width, iconTexture.height);
                    newIconTexture.SetPixels(iconTexture.GetPixels());
                    float resizeFactor = 1f / (Mathf.Max(newIconTexture.width, newIconTexture.height) / 200);
                    newIconTexture.Resize((int)(resizeFactor * newIconTexture.width), (int)(resizeFactor * newIconTexture.height)); // does not work
                    texture2D.SetPixels(horizontalOffset + width - 5 - newIconTexture.width, 1080 - 5 - newIconTexture.height, newIconTexture.width, newIconTexture.height, newIconTexture.GetPixels());
                }
            }
            /*
            texture2D.SetPixels(0, 0, 1920, 20, timeline);
            int cursorPosition = i * (1915 / (timelineLength - 1));
            texture2D.SetPixels(cursorPosition, 0, 5, 20, timelineCursor);
            */
            texture.FromTexture2D(texture2D);
            m_VideoStream.WriteFrame(texture);
        }
        m_VideoStream.Dispose();
        m_VideoStream = null;
    }
#endif
}
