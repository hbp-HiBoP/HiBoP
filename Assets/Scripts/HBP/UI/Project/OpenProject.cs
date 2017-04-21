using UnityEngine.UI;
using System.Collections;
using CielaSpike;
using Tools.Unity;
using HBP.Data.General;

namespace HBP.UI
{
	public class OpenProject : Window 
	{
		#region Properties
		FolderSelector locationFolderSelector;
		ProjectList projectList;
        Button loadButton;
        #endregion

        #region Public Methods
        public void Load(ProjectInfo info)
        {
            FindObjectOfType<ProjectLoaderSaver>().Load(info);
            base.Close();
        }
        public void Load()
		{
           Load(projectList.GetObjectsSelected()[0]);
		}
        #endregion

        #region Private Methods
        protected override void SetWindow()
        {
            loadButton = transform.FindChild("Content").FindChild("GeneralButtons").FindChild("Load").GetComponent<Button>();
            projectList = transform.FindChild("Content").FindChild("Projects").FindChild("ProjectList").FindChild("List").FindChild("List").FindChild("Viewport").FindChild("Content").GetComponent<ProjectList>();
            projectList.SelectEvent.AddListener(() => loadButton.interactable = true);
            projectList.ActionEvent.AddListener((info, i) => Load(info));

            locationFolderSelector = transform.FindChild("Content").FindChild("Projects").FindChild("FolderSelector").GetComponent<FolderSelector>();
            locationFolderSelector.onValueChanged.AddListener((value) => this.StartCoroutineAsync(DisplayProjects(value)));
            locationFolderSelector.Path = ApplicationState.GeneralSettings.DefaultProjectLocation;
        }
        IEnumerator DisplayProjects(string path)
        {
            yield return Ninja.JumpToUnity;
            loadButton.interactable = false;
            projectList.Clear();
            yield return Ninja.JumpBack;
            string[] projectsPath = Project.GetProject(path);
            foreach(string projectPath in projectsPath)
            {
                yield return Ninja.JumpBack;
                ProjectInfo project = new ProjectInfo(projectPath);
                yield return Ninja.JumpToUnity;
                projectList.Add(project);
            }
            yield return true;
        }
        #endregion
    }
}