using System.IO;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Tools;
using HBP.UI.Database;
using UnityEngine;

namespace HBP.UI.Tools
{
    public class ApplicationManager : Manager<ApplicationManager>
    {
        #region Properties

        private bool m_IsQuitting = false;

        #endregion

        #region Private Methods

        private void Awake()
        {
            Application.wantsToQuit += OnQuit;
        }

        private void Start()
        {
            DatabaseWorkflow.InitializeAsync().Forget();
        }

        private void OnDestroy()
        {
            DataManager.Clear();
            string tmpDir = ApplicationState.ExtractProjectFolder;
            if (Directory.Exists(tmpDir))
            {
                Directory.Delete(tmpDir, true);
            }
        }

        private bool OnQuit()
        {
            if (m_IsQuitting) return true;

            ShowQuitDialog().Forget();
            return false;
        }

        private async UniTaskVoid ShowQuitDialog()
        {
            if (ApplicationState.LoadedProject != null)
            {
                var choice = await DialogBoxManager.OpenAsync(DialogBoxType.Warning, "Project Open", "A project is currently open. It's recommended to save before quitting to avoid losing your progress.\n\nWhat would you like to do?", "Save & Quit", "Quit", "Cancel");
                switch (choice)
                {
                    case 0:
                        await ProjectLoaderSaver.SaveAsync();
                        m_IsQuitting = true;
                        Application.Quit();
                        break;
                    case 1:
                        m_IsQuitting = true;
                        Application.Quit();
                        break;
                    case 2:
                        return;
                }
            }
            else
            {
                int result = await DialogBoxManager.OpenAsync(DialogBoxType.Informational, "Quit HiBoP?", "Are you sure you want to quit HiBoP? Make sure all your data is saved.", "Quit", "Cancel");
                if (result == 0)
                {
                    m_IsQuitting = true;
                    Application.Quit();
                }
            }
        }

        #endregion
    }
}
