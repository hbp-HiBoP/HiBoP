using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Core.Tools;
using HBP.Core.Database;
using HBP.Data.Module3D;
using HBP.Core.Preferences;
using HBP.Theme;
using HBP.UI.Tools;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace HBP.Dev
{
    public class DevDebug : MonoBehaviour
    {
#if !UNITY_EDITOR
        private void Awake()
        {
            Destroy(this);
        }
#endif
        // Used by the commented debug block in Update.
        // private List<Vector3> m_InitialPositions = new();
        // private List<Vector3> m_FinalPositions = new();
        // private float m_Percent;
        // private bool m_Initialized = false;
        // private float m_TimeSinceLastAction = 0;
        /*        private void OnApplicationQuit()
                {
                    Debug.Log("quitting");
                    using StreamWriter sw = new(Path.Combine(Application.persistentDataPath, "quit.txt"));
                    sw.WriteLine("quit");
                    sw.Close();
                }*/
        private void Start()
        {
        }

        private async UniTask SaveActivityAsNifti(Action<float, float, LoadingText> onChangeProgress)
        {
            async UniTaskVoid checkProgress(CancellationToken cancellationToken)
            {
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    onChangeProgress.Invoke(Module3DMain.SelectedColumn.ActivityGenerator.Progress, 0, new LoadingText("Exporting as Nifti"));
                    await UniTask.WaitForSeconds(0.05f);
                }
            }

            CancellationTokenSource source = new();
            checkProgress(source.Token).Forget();
            await UniTask.SwitchToThreadPool();
            Module3DMain.SelectedColumn.ActivityGenerator.SaveActivityAsNifti(Path.Join(PersistentDataManager.UserPreferences.General.Project.DefaultExportLocation, "test_nifti.nii.gz"), (Module3DMain.SelectedColumn as Column3DIEEG).CurrentProjectionSubtimeline, "IEEG Activity");
            source.Cancel();
        }

        [SerializeField] private GameObject m_CubePrefab;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                //LoadingManager.Load(SaveActivityAsNifti);
                //Core.Object3D.FMRI fmri = new("FMRI", Path.Join(PersistentDataManager.UserPreferences.General.Project.DefaultExportLocation, "FRUIT.nii.gz"));
                //Vector3[] positions = Module3DMain.SelectedScene.AtlasManager.SelectedAtlas.GetAreaCoordinates(Module3DMain.SelectedScene.AtlasManager.HoveredArea);
                //foreach (var pos in positions)
                //{
                //    Instantiate(m_CubePrefab, pos, Quaternion.identity);
                //}
            }
            //if (Input.GetKeyDown(KeyCode.F1))
            //{
            //    DialogBoxManager.Open(DialogBoxType.Error, "Lorem ipsum dolor sit", "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi accumsan lacus quam, vitae vestibulum libero malesuada vitae. Fusce ornare rutrum tortor vitae bibendum. Phasellus dolor.\r\n\r\n").Forget();
            //}
            //if (Input.GetKeyDown(KeyCode.F2))
            //{
            //    DialogBoxManager.Open(DialogBoxType.Warning, "Lorem ipsum dolor sit", "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi accumsan lacus quam, vitae vestibulum libero malesuada vitae. Fusce ornare rutrum tortor vitae bibendum. Phasellus dolor.\r\n\r\n").Forget();
            //}
            //if (Input.GetKeyDown(KeyCode.F3))
            //{
            //    DialogBoxManager.Open(DialogBoxType.Informational, "Lorem ipsum dolor sit", "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi accumsan lacus quam, vitae vestibulum libero malesuada vitae. Fusce ornare rutrum tortor vitae bibendum. Phasellus dolor.\r\n\r\n").Forget();
            //}
            //if (Input.GetKeyDown(KeyCode.F4))
            //{
            //    int result = await DialogBoxManager.OpenAsync(DialogBoxType.Informational, "Test", "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi accumsan lacus quam, vitae vestibulum libero malesuada vitae. Fusce ornare rutrum tortor vitae bibendum. Phasellus dolor.\r\n\r\n", "Yes", "No");
            //    Debug.Log(result);
            //}
            //if (Input.GetKeyDown(KeyCode.F1))
            //{
            //    CheckProjectAndDatabaseIntegrity();
            //}
            //if (Input.GetKeyDown(KeyCode.F2))
            //{
            //    DatabaseManager.Database.LoadDatabase();
            //}
/*            if (Input.GetKeyDown(KeyCode.A))
            {
                Core.Object3D.Cut cut = Module3DMain.SelectedScene.Cuts[0];
                cut.Position -= 1.0f / cut.NumberOfCuts;
                Module3DMain.SelectedScene.UpdateCutPlane(cut, true);
            }
            if (Input.GetKeyDown(KeyCode.B))
            {
                Core.Object3D.Cut cut = Module3DMain.SelectedScene.Cuts[0];
                cut.Flip = !cut.Flip;
                Module3DMain.SelectedScene.UpdateCutPlane(cut, true);
            }
            if (Input.GetKeyDown(KeyCode.C))
            {
                Core.Object3D.Cut cut = Module3DMain.SelectedScene.Cuts[0];
                cut.Orientation = (CutOrientation)(((int)cut.Orientation + 1) % 3);
                Module3DMain.SelectedScene.UpdateCutPlane(cut, true);
            }*/
            /*
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
                Vector3 center = Module3DMain.SelectedScene.MeshManager.MeshCenter;
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
            */
        }

        private void TestLoadCancel()
        {
            LoadingManager.Load(TestLoadCancelAsync);
        }

        private async UniTask TestLoadCancelAsync(Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            for (int i = 0; i < 10; i++)
            {
                if (token.IsCancellationRequested) return;
                updateProgress((float)(i + 1) / 10, 3, new LoadingText("Loading ", "", $"{i + 1} / 10"));
                await UniTask.WaitForSeconds(3);
            }
        }

        private async UniTaskVoid TestLoadPatients1()
        {
            System.Diagnostics.Stopwatch watch = new();
            watch.Start();
            List<Patient> patients = new();
            DirectoryInfo patientDirectory = new DirectoryInfo(@"C:\HBP\Projects\VISU_full").GetDirectories("Patients", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] patientFiles = patientDirectory.GetFiles("*" + Patient.EXTENSION, SearchOption.TopDirectoryOnly);
            foreach (var file in patientFiles)
            {
                patients.Add(await ClassLoaderSaver.LoadFromJsonAsync<Patient>(file.FullName));
            }

            watch.Stop();
            Debug.Log("Time : " + watch.ElapsedMilliseconds);
        }

        private async UniTaskVoid TestLoadPatients2()
        {
            System.Diagnostics.Stopwatch watch = new();
            watch.Start();
            await UniTask.SwitchToThreadPool();
            List<Patient> patients = new();
            DirectoryInfo patientDirectory = new DirectoryInfo(@"C:\HBP\Projects\VISU_full").GetDirectories("Patients", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] patientFiles = patientDirectory.GetFiles("*" + Patient.EXTENSION, SearchOption.TopDirectoryOnly);
            foreach (var file in patientFiles)
            {
                patients.Add(ClassLoaderSaver.LoadFromJson<Patient>(file.FullName));
            }

            watch.Stop();
            Debug.Log("Time : " + watch.ElapsedMilliseconds);
        }

        private async UniTaskVoid TestLoadPatients4()
        {
            System.Diagnostics.Stopwatch watch = new();
            watch.Start();
            List<Patient> patients = new();
            DirectoryInfo patientDirectory = new DirectoryInfo(@"C:\HBP\Projects\VISU_full").GetDirectories("Patients", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] patientFiles = patientDirectory.GetFiles("*" + Patient.EXTENSION, SearchOption.TopDirectoryOnly);
            patients = (await UniTask.WhenAll(patientFiles.Select(pf => ClassLoaderSaver.LoadFromJsonAsync<Patient>(pf.FullName)))).ToList();
            watch.Stop();
            Debug.Log("Time : " + watch.ElapsedMilliseconds);
        }

        private async UniTaskVoid TestLoadPatients5()
        {
            await UniTask.SwitchToThreadPool();
            System.Diagnostics.Stopwatch watch = new();
            watch.Start();
            List<Patient> patients = new();
            DirectoryInfo patientDirectory = new DirectoryInfo(@"C:\HBP\Projects\VISU_full").GetDirectories("Patients", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] patientFiles = patientDirectory.GetFiles("*" + Patient.EXTENSION, SearchOption.TopDirectoryOnly);
            var tasks = patientFiles.Select(file => (Func<UniTask<Patient>>)(async () => await ClassLoaderSaver.LoadFromJsonAsync<Patient>(file.FullName)));
            await Core.Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "tutu", (a, b, c) => { }, LoadingConcurrencyPolicy.Current.GetLimit(LoadingWorkCategory.JsonAndZip), true);
            watch.Stop();
            Debug.Log("Time : " + watch.ElapsedMilliseconds);
        }

        private async UniTaskVoid TestLoadPatients6()
        {
            await UniTask.SwitchToThreadPool();
            System.Diagnostics.Stopwatch watch = new();
            watch.Start();
            List<Patient> patients = new();
            DirectoryInfo patientDirectory = new DirectoryInfo(@"C:\HBP\Projects\VISU_full").GetDirectories("Patients", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] patientFiles = patientDirectory.GetFiles("*" + Patient.EXTENSION, SearchOption.TopDirectoryOnly);
            System.Random rand = new();
            var tasks = patientFiles.Select(file => (Func<UniTask<Patient>>)(async () =>
            {
                await UniTask.SwitchToThreadPool();
                await UniTask.WaitForSeconds((float)rand.NextDouble() * 10);
                using StreamReader streamReader = new(file.FullName);
                var str = streamReader.ReadToEnd();
                var result = JsonConvert.DeserializeObject<Patient>(str, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
                return new Patient();

                /*return await ClassLoaderSaver.LoadFromJsonAsync<Patient>(file.FullName)*/
            }));
            await LoadingManager.LoadAsync(update => Core.Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "tutu", update, 0, true));
            watch.Stop();
            Debug.Log("Time : " + watch.ElapsedMilliseconds);
        }

        private T LoadFromJson<T>(string path) where T : new()
        {
            T result = new();
            using (StreamReader streamReader = new(path))
            {
                result = JsonConvert.DeserializeObject<T>(streamReader.ReadToEnd(), new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
            }

            return result;
        }

        private async UniTaskVoid ThrowError()
        {
            await ThrowErrorAsync();
        }

        private async UniTask ThrowErrorAsync()
        {
            await ThrowErrorAsync2();
            throw new Exception("Test");
        }

        private async UniTask ThrowErrorAsync2()
        {
            await UniTask.WaitForSeconds(1);
            throw new Exception("Test2");
        }

        private void CheckProjectAndDatabaseIntegrity()
        {
            // Database
            foreach (var dataInfo in DatabaseManager.Database.DataInfos)
            {
                if (dataInfo is PatientDataInfo patientDataInfo && !DatabaseManager.Database.Patients.Contains(patientDataInfo.Patient))
                {
                    Debug.LogError(string.Format("Patient of {0} not found in database", patientDataInfo.Name));
                }
            }

            // Project
            if (ApplicationState.LoadedProject == null) return;
            foreach (var dataset in ApplicationState.LoadedProject.Datasets)
            {
                foreach (var data in dataset.Data.OfType<PatientDataInfo>())
                {
                    if (!ApplicationState.LoadedProject.Patients.Contains(data.Patient))
                    {
                        Debug.LogError(string.Format("Patient of {0}-{1} not found in project", dataset.Name, data.Name));
                    }
                }
            }
        }

        private void MarsAtlasCCEP()
        {
            //DirectoryInfo dir = new DirectoryInfo(@"D:\HBP\CCEP\07-bids_20190416\converted");
            //FileInfo[] files = dir.GetFiles("*.vhdr");
            //foreach (var file in files)
            //{
            //    ApplicationState.LoadedProject.Datasets[0].AddData(new CCEPDataInfo("ccep", new Core.Data.Container.BrainVision(file.FullName, Guid.NewGuid().ToString()), ApplicationState.LoadedProject.Patients[0], file.Name.Replace(file.Extension, ""), ""));
            //}
        }

        private void GetAllCCEPData()
        {
            //string ccepDB = @"D:\HBP\CCEP\07-bids_20190416\07-bids";
            //DirectoryInfo baseDir = new DirectoryInfo(ccepDB);
            //DirectoryInfo[] patientDirs = baseDir.GetDirectories("sub-*");
            //foreach (var dir in patientDirs)
            //{
            //    string patientName = dir.Name.Substring(4);
            //    Patient patient = ApplicationState.LoadedProject.Patients.FirstOrDefault(p => p.Name == patientName);
            //    if (patient == null) continue;
            //    DirectoryInfo ieegDir = new DirectoryInfo(Path.Combine(dir.FullName, "ses-postimp01", "ieeg"));
            //    FileInfo[] files = ieegDir.GetFiles("*.vhdr").Where(f => f.FullName.Contains("ccep")).ToArray();
            //    foreach (var file in files)
            //    {
            //        string site = file.Name.Split('_')[3].Substring(4, 8);
            //        if (!site.Contains("p")) site = site.Substring(0, 6);
            //        site = site.Insert(site.Length / 2, "-");
            //        ApplicationState.LoadedProject.Datasets[0].AddData(new CCEPDataInfo("ccep", new Core.Data.Container.BrainVision(file.FullName, Guid.NewGuid().ToString()), patient, site, ""));
            //    }
            //}
        }

        private async void ScreenshotWindow()
        {
            string path = await FileBrowser.GetSavedFileNameAsync();
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
    }
}
