using UnityEngine.UI;
using System.Collections;
using CielaSpike;
using Tools.Unity;
using HBP.Data.General;
using System.Linq;

namespace HBP.UI
{
	public class OpenProject : Window 
	{
		#region Properties
		FolderSelector m_LocationFolderSelector;
		ProjectList m_ProjectList;
        Button m_LoadingButton;
        #endregion

        #region Public Methods
        public void Load(ProjectInfo info)
        {
            FindObjectOfType<ProjectLoaderSaver>().Load(info);
            base.Close();
        }
        public void Load()
		{
           Load(m_ProjectList.GetObjectsSelected()[0]);
		}
        #endregion

        #region Private Methods
        protected override void SetWindow()
        {
            m_LoadingButton = transform.Find("Content").Find("GeneralButtons").Find("Load").GetComponent<Button>();
            m_ProjectList = transform.Find("Content").Find("Projects").Find("ProjectList").Find("List").Find("List").Find("Viewport").Find("Content").GetComponent<ProjectList>();
            m_ProjectList.SelectEvent.AddListener(() => m_LoadingButton.interactable = true);
            m_ProjectList.ActionEvent.AddListener((info, i) => Load(info));

            m_LocationFolderSelector = transform.Find("Content").Find("Projects").Find("FolderSelector").GetComponent<FolderSelector>();
            m_LocationFolderSelector.onValueChanged.AddListener((value) => this.StartCoroutineAsync(DisplayProjects(value)));
            m_LocationFolderSelector.Folder = ApplicationState.GeneralSettings.DefaultProjectLocation;
        }
        IEnumerator DisplayProjects(string path)
        {
            yield return Ninja.JumpToUnity;
            m_LoadingButton.interactable = false;
            m_ProjectList.Clear();
            yield return Ninja.JumpBack;
            string[] paths = Project.c_GetProject(path).ToArray();
            foreach (string projectPath in paths)
            {
                ProjectInfo project = new ProjectInfo(projectPath);
                yield return Ninja.JumpToUnity;
                m_ProjectList.Add(project);
                yield return Ninja.JumpBack;
            }
        }
        #endregion
    }
}