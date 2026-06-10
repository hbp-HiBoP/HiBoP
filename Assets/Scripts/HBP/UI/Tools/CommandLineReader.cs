using HBP.Core.Data;
using HBP.Data.Module3D;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using HBP.Core.Tools;
using HBP.Core.Preferences;
using Cysharp.Threading.Tasks;

namespace HBP.UI.Tools
{
    public class CommandLineReader : MonoBehaviour
    {
        #region Properties
#if UNITY_EDITOR
        [SerializeField] private bool m_AutoLoad = false;
        [SerializeField] private string m_ProjectName;
        [SerializeField] private string m_VisualizationName;
#endif
        #endregion

        #region Private Methods
        private void Awake()
        {
            string[] args = System.Environment.GetCommandLineArgs();
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(m_ProjectName) && !string.IsNullOrEmpty(m_VisualizationName) && m_AutoLoad)
            {
                args = new string[] { "HiBoP", "-p", m_ProjectName, "-v", m_VisualizationName };
            }
#endif
            InterpreteCommandLineArguments(args).Forget();
        }
        #endregion

        #region Coroutines
        private async UniTaskVoid InterpreteCommandLineArguments(string[] args)
        {
            if (args.Length != 0)
            {
                List<string> actions = new();
                List<List<string>> arguments = new();
                for (int i = 1; i < args.Length; ++i)
                {
                    string arg = args[i];
                    if (i == 1 && new FileInfo(arg).Exists)
                    {
                        actions.Add("-pf");
                        arguments.Add(new List<string>(1) { arg });
                    }
                    else if (arg.StartsWith("-"))
                    {
                        actions.Add(arg);
                        arguments.Add(new List<string>());
                    }
                    else
                    {
                        if (arguments.Count > 0)
                        {
                            arguments.Last().Add(arg);
                        }
                    }
                }
                for (int i = 0; i < actions.Count; ++i)
                {
                    await ApplyActionAsync(actions[i], arguments[i]);
                    await UniTask.WaitForEndOfFrame();
                }
            }
            Destroy(gameObject);
        }
        private async UniTask ApplyActionAsync(string action, List<string> arguments)
        {
            if (action == "-p") // Project
            {
                if (arguments.Count == 0)
                {
                    DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Couldn't open project", "The project name has not been specified.").Forget();
                    return;
                }

                string path = Path.Combine(PersistentDataManager.UserPreferences.General.Project.DefaultLocation, arguments[0] + Project.EXTENSION);
                if (!File.Exists(path))
                {
                    DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Couldn't open project", "The project file does not exist.").Forget();
                    return;
                }

                await ProjectLoaderSaver.LoadAsync(new ProjectInfo(path));
            }
            else if (action == "-pf") // Project File
            {
                if (arguments.Count == 0)
                {
                    DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Couldn't open project", "The project name has not been specified.").Forget();
                    return;
                }

                if (!File.Exists(arguments[0]))
                {
                    DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Couldn't open project", "The project file does not exist.").Forget();
                    return;
                }

                await ProjectLoaderSaver.LoadAsync(new ProjectInfo(arguments[0]));
            }
            else if (action == "-v") // Visualization
            {
                if (ApplicationState.LoadedProject == null)
                {
                    DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Project not loaded", "You are trying to open a visualization without opening a project. This is not supported.").Forget();
                    return;
                }

                if (arguments.Count == 0)
                {
                    DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Couldn't load visualizations", "The names of the visualizations have not been specified.").Forget();
                    return;
                }

                IEnumerable<Visualization> visualizations = from visu in ApplicationState.LoadedProject.Visualizations where arguments.Contains(visu.Name) select visu;
                if (visualizations.Count() == 0 && arguments[0] == "all")
                {
                    visualizations = ApplicationState.LoadedProject.Visualizations;
                }
                LoadingManager.Load((update, token) => Module3DMain.LoadAsync(visualizations, update, token));
            }
        }
        #endregion
    }
}
