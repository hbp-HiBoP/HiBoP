using System.Linq;
using UnityEngine;
using System.IO;
using System;
using HBP.Data;
using HBP.UI;
using Tools.Unity;
using System.Collections;
using HBP.Module3D;

public class DebugBenjamin : MonoBehaviour
{
#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            StartVideo();
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            EndVideo();
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
    private Coroutine m_VideoCoroutine = null;
    [SerializeField] private Canvas m_Canvas;
    private void StartVideo()
    {
        Rect sceneRect = m_Canvas.GetComponent<RectTransform>().ToScreenSpace();
        m_VideoStream = new HBP.Module3D.DLL.VideoStream();
        m_VideoStream.Open("D:/TestVideo.avi", (int)sceneRect.width, (int)sceneRect.height);
        m_VideoCoroutine = StartCoroutine(c_Video(sceneRect));
    }
    private void EndVideo()
    {
        StopCoroutine(m_VideoCoroutine);
        m_VideoCoroutine = null;
        m_VideoStream.Dispose();
        m_VideoStream = null;
    }
    private IEnumerator c_Video(Rect rect)
    {
        HBP.Module3D.DLL.Texture texture = new HBP.Module3D.DLL.Texture();
        texture.Reset((int)rect.width, (int)rect.height);

        float fps = 30;
        Base3DScene scene = ApplicationState.Module3D.SelectedScene;
        Column3DIEEG column = ApplicationState.Module3D.SelectedColumn as Column3DIEEG;

        while (true)
        {
            yield return new WaitForEndOfFrame();

            Texture2D sceneTexture = Texture2DExtension.ScreenRectToTexture(rect);
            texture.FromTexture2D(sceneTexture);
            m_VideoStream.WriteFrame(texture);
        }
    }
#endif
}
