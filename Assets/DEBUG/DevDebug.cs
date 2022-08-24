using UnityEditor;
using HBP.Theme.Components;
using System.Linq;
using UnityEngine;
using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using HBP.Core.Enums;
using HBP.Core.Data;
using HBP.Data.Module3D;
using HBP.Core.Tools;
using HBP.UI.Tools;

namespace HBP.Dev
{
    public class DevDebug : MonoBehaviour
    {
#if UNITY_EDITOR
        private List<Vector3> m_InitialPositions = new List<Vector3>();
        private List<Vector3> m_FinalPositions = new List<Vector3>();
        private float m_Percent;
        private bool m_Initialized = false;
        private float m_TimeSinceLastAction = 0;
        private void Update()
        {
            // FRAMRATE
            m_TimeSinceLastAction += Time.deltaTime;
            if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0 || Input.anyKey || Input.anyKeyDown)
            {
                m_TimeSinceLastAction = 0;
            }
            if (m_TimeSinceLastAction > 60)
            {
                Application.targetFrameRate = 1;
            }
            else
            {
                Application.targetFrameRate = -1;
            }

            // SITES
            if (Input.GetKeyDown(KeyCode.F2))
            {
                m_InitialPositions.Clear();
                foreach (var site in Module3DMain.SelectedColumn.Sites)
                {
                    m_InitialPositions.Add(site.transform.localPosition);
                }
            }
            if (Input.GetKeyDown(KeyCode.F3))
            {
                m_FinalPositions.Clear();
                Vector3 orientation = Module3DMain.SelectedScene.MRIManager.SelectedMRI.Volume.GetOrientationVector(CutOrientation.Sagittal, false);
                orientation = new Vector3(-orientation.x, orientation.y, orientation.z);
                Vector3 center = Module3DMain.SelectedScene.MeshManager.MeshCenter;
                center = new Vector3(-center.x, center.y, center.z);
                foreach (var site in Module3DMain.SelectedColumn.Sites)
                {
                    Vector3 vector = site.transform.localPosition - center;
                    float dot = Vector3.Dot(vector, orientation);
                    if (dot > 0)
                    {
                        m_FinalPositions.Add(site.transform.localPosition - 2f * (dot / orientation.magnitude) * orientation.normalized);
                    }
                    else
                    {
                        m_FinalPositions.Add(site.transform.localPosition);
                    }
                }
                m_Initialized = true;
            }
            if (Input.GetKeyDown(KeyCode.F1))
            {
                m_Percent = 0;
            }
            if (m_Initialized && m_Percent < 1)
            {
                int i = 0;
                foreach (var site in Module3DMain.SelectedColumn.Sites)
                {
                    site.transform.localPosition = new Vector3(Mathf.Lerp(m_InitialPositions[i].x, m_FinalPositions[i].x, m_Percent), Mathf.Lerp(m_InitialPositions[i].y, m_FinalPositions[i].y, m_Percent), Mathf.Lerp(m_InitialPositions[i].z, m_FinalPositions[i].z, m_Percent));
                    i++;
                }
                m_Percent += Time.deltaTime;
            }
        }
        private void MarsAtlasCCEP()
        {
            DirectoryInfo dir = new DirectoryInfo(@"D:\HBP\CCEP\07-bids_20190416\converted");
            FileInfo[] files = dir.GetFiles("*.vhdr");
            foreach (var file in files)
            {
                ApplicationState.ProjectLoaded.Datasets[0].AddData(new CCEPDataInfo("ccep", new Core.Data.Container.BrainVision(file.FullName, Guid.NewGuid().ToString()), ApplicationState.ProjectLoaded.Patients[0], file.Name.Replace(file.Extension, "")));
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
                    ApplicationState.ProjectLoaded.Datasets[0].AddData(new CCEPDataInfo("ccep", new Core.Data.Container.BrainVision(file.FullName, Guid.NewGuid().ToString()), patient, site));
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
            Window window = WindowsManager.WindowsReferencer.Windows.FirstOrDefault(w => w.GetComponent<Selector>().Selected);
            if (!string.IsNullOrEmpty(path))
            {
                Texture2D image = Texture2DExtension.ScreenRectToTexture(window.GetComponent<RectTransform>().ToScreenSpace());
                image.filterMode = FilterMode.Trilinear;
                image.SaveToPNG(path);
            }
        }
        private void TestOrientation()
        {
            Vector3 orientation = Module3DMain.SelectedScene.MRIManager.SelectedMRI.Volume.GetOrientationVector(CutOrientation.Sagittal, false);
            Vector3 center = Module3DMain.SelectedScene.MeshManager.MeshCenter;
            center = new Vector3(-center.x, center.y, center.z);
            foreach (var site in Module3DMain.SelectedColumn.Sites)
            {
                Vector3 vector = site.transform.localPosition - center;
                float dot = Vector3.Dot(vector, orientation);
                if (dot < 0)
                {
                    site.transform.localPosition -= 0.2f * (dot / orientation.magnitude) * orientation.normalized;
                }
            }
        }
        private static void ActiveThemeElement()
        {
            var selected = Selection.activeGameObject;
            var themeElements = selected.GetComponentsInChildren<ThemeElement>(true);
            foreach (var element in themeElements)
            {
                element.enabled = true;
            }
        }
        private static void LoadDatabase()
        {
            Patient.LoadFromBIDSDatabase(@"Z:\BrainTV\HBP\Development\BaseBidsCCEPGrenoble\07-bids_20190416\07-bids", out Patient[] patients);
        }
#endif
    }
}