using UnityEngine;
using Ionic.Zip;
using System.IO;
using CielaSpike;
using HBP.UI;
using System.Collections;
using HBP.Data.General;
using HBP.Data.Preferences;

public class DEBUG : MonoBehaviour
{
    private string m_TmpDir
    {
        get
        {
            string tmpDir = Application.dataPath + "/" + ".tmp";
            if (!Directory.Exists(tmpDir))
            {
                DirectoryInfo di = Directory.CreateDirectory(tmpDir);
                di.Attributes |= FileAttributes.Hidden;
            }
            return tmpDir;
        }
    }
    private string m_ProjectName = "Proj";


    private void Update()
    {
        /*
        if (Input.GetKeyDown(KeyCode.N)) NewProject();
        if (Input.GetKeyDown(KeyCode.L)) StartCoroutine(c_Load(m_ProjectName));
        if (Input.GetKeyDown(KeyCode.S)) StartCoroutine(c_Save(m_ProjectName));
        */
    }

    private void NewProject()
    {
        ProjectSettings settings = new ProjectSettings(m_ProjectName, ApplicationState.UserPreferences.General.Project.DefaultPatientDatabase, ApplicationState.UserPreferences.General.Project.DefaultLocalizerDatabase);
        ApplicationState.ProjectLoaded = new Project(settings);
        ApplicationState.ProjectLoadedLocation = m_TmpDir;
        StartCoroutine(c_Save(m_ProjectName));
        FindObjectOfType<MenuButtonState>().SetInteractables();
    }

    private IEnumerator c_Load(string projectName)
    {
        using (ZipFile zip = ZipFile.Read(ApplicationState.UserPreferences.General.Project.DefaultLocation + string.Format("/{0}.hibop", projectName)))
        {
            zip.ExtractAll(m_TmpDir + string.Format("/{0}", projectName), ExtractExistingFileAction.OverwriteSilently);
        }

        Task projectLoadingTask;
        yield return this.StartCoroutineAsync(FindObjectOfType<ProjectLoaderSaver>().c_Load(new ProjectInfo(m_TmpDir + string.Format("/{0}", projectName))), out projectLoadingTask);
        Directory.Delete(string.Format(m_TmpDir + "/{0}", projectName), true);
    }

    private IEnumerator c_Save(string projectName)
    {
        yield return this.StartCoroutineAsync(FindObjectOfType<ProjectLoaderSaver>().c_Save(ApplicationState.ProjectLoadedLocation));
        if (File.Exists(string.Format(ApplicationState.UserPreferences.General.Project.DefaultLocation + "/{0}.hibop", projectName))) File.Delete(ApplicationState.UserPreferences.General.Project.DefaultLocation + string.Format("/{0}.hibop", projectName));
        using (ZipFile zip = new ZipFile(string.Format(ApplicationState.UserPreferences.General.Project.DefaultLocation + "/{0}.hibop", projectName)))
        {
            zip.AddDirectory(ApplicationState.ProjectLoadedTMPFullPath);
            zip.Save();
        }
    }
}