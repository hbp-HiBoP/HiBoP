using Cysharp.Threading.Tasks;
using HBP.Core.Data;

namespace HBP.UI.Tools
{
    public static class ProjectLoaderSaver
    {
        #region Public Methods
        public async static UniTaskVoid Load(ProjectInfo projectInfo)
        {
            await LoadAsync(projectInfo);
        }

        public async static UniTask LoadAsync(ProjectInfo projectInfo)
        {
            await ProjectWorkflowService.Default.LoadProjectAsync(projectInfo);
        }

        public async static UniTaskVoid Save(string path)
        {
            await SaveAsync(path);
        }

        public async static UniTaskVoid Save()
        {
            await SaveAsync();
        }

        public async static UniTaskVoid SaveAndReload()
        {
            await ProjectWorkflowService.Default.SaveProjectAndReloadAsync();
        }

        public async static UniTask SaveAsync()
        {
            await ProjectWorkflowService.Default.SaveProjectAsync();
        }

        public async static UniTask SaveAsync(string path)
        {
            await ProjectWorkflowService.Default.SaveProjectAsync(path);
        }
        #endregion
    }
}
