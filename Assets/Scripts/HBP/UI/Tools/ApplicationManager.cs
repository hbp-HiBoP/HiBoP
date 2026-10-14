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
        private readonly UniTaskCompletionSource m_DatabaseInitialization = new();
        public UniTask DatabaseInitialization => m_DatabaseInitialization.Task;
        public static System.Exception DatabaseInitializationPresentedError { get; private set; }

        public static async UniTask WaitForDatabaseInitializationAsync(System.Threading.CancellationToken token)
        {
            await UniTask.WaitUntil(() => IsInitialized, cancellationToken: token);
            await m_Instance.DatabaseInitialization.AttachExternalCancellation(token);
        }

        #endregion

        #region Private Methods

        protected override void Initialization()
        {
            base.Initialization();
            DatabaseInitializationPresentedError = null;
            Application.wantsToQuit += OnQuit;
        }

        private void Start()
        {
            InitializeDatabaseAsync().Forget();
        }

        private async UniTask InitializeDatabaseAsync()
        {
            try
            {
                await DatabaseWorkflow.InitializeAsync(error => DatabaseInitializationPresentedError = error);
                m_DatabaseInitialization.TrySetResult();
            }
            catch (System.Exception exception)
            {
                m_DatabaseInitialization.TrySetException(exception);
                throw;
            }
        }

        private void OnDestroy()
        {
            Application.wantsToQuit -= OnQuit;
            DataManager.Clear();
            string tmpDir = ApplicationState.ExtractProjectFolder;
            if (Directory.Exists(tmpDir))
            {
                Directory.Delete(tmpDir, true);
            }
        }

        private bool OnQuit()
        {
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-displaySyncEvidenceOnce") >= 0 && System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-displaySyncEvidence") >= 0) return true;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            string[] arguments = System.Environment.GetCommandLineArgs();
            if (System.Array.IndexOf(arguments, "-questPairingSmoke") >= 0) return true;
            if (System.Array.IndexOf(arguments, "-questUsbSmoke") >= 0) return true;
            if (System.Array.IndexOf(arguments, "-captureOnce") >= 0 && System.Array.IndexOf(arguments, "-captureAnatomy") >= 0) return true;
            if (System.Array.IndexOf(arguments, "-ieegEvidenceOnce") >= 0 && System.Array.IndexOf(arguments, "-ieegEvidence") >= 0) return true;
            if (System.Array.IndexOf(arguments, "-sceneEvidenceOnce") >= 0 && System.Array.IndexOf(arguments, "-sceneEvidence") >= 0) return true;
#endif
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
