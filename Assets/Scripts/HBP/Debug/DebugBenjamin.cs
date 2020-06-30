using System.Linq;
using UnityEngine;
using System.IO;
using System;
using HBP.Data;
using HBP.UI;
using Tools.Unity;
using System.Collections;

public class DebugBenjamin : MonoBehaviour
{
#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ApplicationState.Module3D.SelectedScene.IsBrainTransparent = !ApplicationState.Module3D.SelectedScene.IsBrainTransparent;
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
        HBP.UI.Window window = ApplicationState.WindowsManager.WindowsReferencer.Windows.FirstOrDefault(w => w.GetComponent<Selector>().Selected);
        if (!string.IsNullOrEmpty(path))
        {
            Texture2D image = Texture2DExtension.ScreenRectToTexture(window.GetComponent<RectTransform>().ToScreenSpace());
            image.filterMode = FilterMode.Trilinear;
            image.SaveToPNG(path);
        }
    }
#endif
}
