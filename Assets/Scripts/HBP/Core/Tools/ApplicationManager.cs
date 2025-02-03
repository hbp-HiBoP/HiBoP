using System.IO;
using HBP.Core.Data;

namespace HBP.Core.Tools
{
    public class ApplicationManager : Manager<ApplicationManager>
    {
        #region Private Methods
        private void OnDestroy()
        {
            DataManager.Clear();
            string tmpDir = ApplicationState.ExtractProjectFolder;
            if (Directory.Exists(tmpDir))
            {
                Directory.Delete(tmpDir, true);
            }
        }
        #endregion
    }
}
