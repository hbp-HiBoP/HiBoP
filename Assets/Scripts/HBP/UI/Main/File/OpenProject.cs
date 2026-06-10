using UnityEngine;
using System.Linq;
using HBP.Core.Data;
using HBP.Data.Module3D;
using HBP.UI.Tools.Lists;
using HBP.UI.Tools;
using HBP.Core.Tools;
using HBP.Core.Preferences;
using Cysharp.Threading.Tasks;

namespace HBP.UI.Main
{
    public class OpenProject : DialogWindow 
	{
		#region Properties
		[SerializeField] FolderSelector m_LocationFolderSelector;
		[SerializeField] ProjectList m_ProjectList;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;

                m_LocationFolderSelector.interactable = value;
                m_ProjectList.Interactable = value;
                SetLoadButton();
            }
        }
        #endregion

        #region Public Methods
        public async UniTaskVoid Load(ProjectInfo info)
        {
            if (ApplicationState.LoadedProject != null)
            {
                if (ApplicationState.LoadedProject.Visualizations.Any(v => Module3DMain.Visualizations.Contains(v)))
                {
                    int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Opened visualizations", "Some visualizations of the currently loaded project are opened. Loading another project will close any opened visualization.\n\nWould you like to load another project ?", "Load project", "Cancel");
                    if (result == 0)
                    {
                        Module3DMain.RemoveAllScenes();
                        base.Close();
                        WindowsManager.CloseAll();
                        await ProjectLoaderSaver.LoadAsync(info);
                    }
                }
                else
                {
                    base.Close();
                    WindowsManager.CloseAll();
                    await ProjectLoaderSaver.LoadAsync(info);
                }
            }
            else
            {
                base.Close();
                WindowsManager.CloseAll();
                await ProjectLoaderSaver.LoadAsync(info);
            }
        }
        public override void OK()
		{
            Load(m_ProjectList.ObjectsSelected[0]).Forget();
		}
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            // Initialize project list.
            m_ProjectList.OnSelect.AddListener((project) => SetLoadButton());
            m_ProjectList.OnDeselect.AddListener((project) => SetLoadButton());
            m_ProjectList.OnAction.AddListener((info, i) => Load(info).Forget());

            // Initialise location folder selector.
            m_LocationFolderSelector.onValueChanged.AddListener((value) => DisplayProjects(value).Forget());

            // Base method.
            base.Initialize();
        }
        protected override void SetFields()
        {
            // Base method.
            base.SetFields();

            // Set location folder selector.
            m_LocationFolderSelector.Folder = PersistentDataManager.UserPreferences.General.Project.DefaultLocation;
        }
        #endregion

        #region Coroutines
        private async UniTaskVoid DisplayProjects(string path)
        {
            try
            {
                m_OKButton.interactable = false;
                m_ProjectList.Set(new ProjectInfo[0]);
                string[] paths = Project.GetProject(path).ToArray();
                foreach (string projectPath in paths)
                {
                    await UniTask.SwitchToThreadPool();
                    ProjectInfo project = new(projectPath);
                    await UniTask.SwitchToMainThread();
                    m_ProjectList.Add(project);
                }
                m_ProjectList.SortByName(BaseList.Sorting.Descending);
            }
            catch(System.Exception e)
            {
                Debug.LogException(e);
            }
        }
        void SetLoadButton()
        {
            m_OKButton.interactable = m_ProjectList.ObjectsSelected.Length == 1 && m_Interactable;
        }
        #endregion
    }
}