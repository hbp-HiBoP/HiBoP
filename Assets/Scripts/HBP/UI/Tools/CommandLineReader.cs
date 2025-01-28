using HBP.Core.Data;
using HBP.Data.Module3D;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using HBP.Core.Tools;
using HBP.Data.Preferences;
using System.Threading.Tasks;

namespace HBP.UI.Tools
{
    public class CommandLineReader : MonoBehaviour
    {
        #region Private Methods
        private void Awake()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            #if UNITY_EDITOR
            //args = new string[] { "HiBoP", "-p", "VISU", "-v", "VISU"};
            #endif
            InterpreteCommandLineArguments(args);
        }
        #endregion

        #region Coroutines
        private async void InterpreteCommandLineArguments(string[] args)
        {
            if (args.Length != 0)
            {
                List<string> actions = new List<string>();
                List<List<string>> arguments = new List<List<string>>();
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
                }
            }
            Destroy(gameObject);
        }
        private async Task ApplyActionAsync(string action, List<string> arguments)
        {
            if (action == "-p") // Project
            {
                if (arguments.Count == 0)
                {
                    DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Couldn't open project", "The project name has not been specified.");
                }
                else
                {
                    await ProjectLoaderSaver.LoadAsync(new ProjectInfo(PersistentDataManager.UserPreferences.General.Project.DefaultLocation + Path.DirectorySeparatorChar + arguments[0] + Project.EXTENSION));
                }
            }
            else if (action == "-pf") // Project File
            {
                if (arguments.Count == 0)
                {
                    DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Couldn't open project", "The project name has not been specified.");
                }
                else
                {
                    await ProjectLoaderSaver.LoadAsync(new ProjectInfo(arguments[0]));
                }
            }
            else if (action == "-v") // Visualization
            {
                if (ApplicationState.LoadedProject == null)
                {
                    DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Project not loaded", "You are trying to open a visualization without opening a project. This is not supported.");
                }
                else if (arguments.Count == 0)
                {
                    DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Couldn't load visualizations", "The names of the visualizations have not been specified.");
                }
                else
                {
                    IEnumerable<Visualization> visualizations;
                    if (arguments[0] == "all")
                    {
                        visualizations = ApplicationState.LoadedProject.Visualizations;
                    }
                    else
                    {
                        visualizations = from visu in ApplicationState.LoadedProject.Visualizations where arguments.Contains(visu.Name) select visu;
                    }
                    Module3DMain.LoadScenes(visualizations);
                }
            }
        }
        #endregion
    }
}