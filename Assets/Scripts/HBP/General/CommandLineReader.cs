using CielaSpike;
using HBP.Data.Visualization;
using HBP.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace HBP
{
    public class CommandLineReader : MonoBehaviour
    {
        #region Private Methods
        private void Awake()
        {
            string[] args = System.Environment.GetCommandLineArgs();
#if UNITY_EDITOR
            args = new string[] { "HiBoP", "-p", "Projet", "-v", "Unknown" };
#endif
            StartCoroutine(c_InterpreteCommandLineArguments(args));
        }
        #endregion

        #region Coroutines
        private IEnumerator c_InterpreteCommandLineArguments(string[] args)
        {
            if (args.Length != 0)
            {
                List<string> actions = new List<string>();
                List<List<string>> arguments = new List<List<string>>();
                for (int i = 1; i < args.Length; ++i)
                {
                    string arg = args[i];
                    if (arg.StartsWith("-"))
                    {
                        actions.Add(arg);
                        arguments.Add(new List<string>());
                    }
                    else
                    {
                        arguments.Last().Add(arg);
                    }
                }
                for (int i = 0; i < actions.Count; ++i)
                {
                    yield return StartCoroutine(c_ApplyAction(actions[i], arguments[i]));
                }
            }
        }
        private IEnumerator c_ApplyAction(string action, List<string> arguments)
        {
            if (action == "-p")
            {
                if (arguments.Count == 0)
                {
                    ApplicationState.DialogBoxManager.Open(Tools.Unity.DialogBoxManager.AlertType.Error, "Couldn't open project", "The project name has not been specified.");
                }
                else
                {
                    Task projectLoadingTask;
                    yield return this.StartCoroutineAsync(FindObjectOfType<ProjectLoaderSaver>().c_Load(new Data.General.ProjectInfo(ApplicationState.UserPreferences.General.Project.DefaultLocation + Path.DirectorySeparatorChar + arguments[0])), out projectLoadingTask);
                    if (projectLoadingTask.State == TaskState.Error)
                    {
                        ApplicationState.DialogBoxManager.Open(Tools.Unity.DialogBoxManager.AlertType.Error, "Couldn't open project", "No project named <color=red>" + arguments[0] + "</color> could be found in the default project directory (" + ApplicationState.UserPreferences.General.Project.DefaultLocation + ").\n\nPlease verify the project name or your default project directory.");
                    }
                }
            }
            else if (action == "-v")
            {
                if (ApplicationState.ProjectLoaded == null)
                {
                    ApplicationState.DialogBoxManager.Open(Tools.Unity.DialogBoxManager.AlertType.Error, "Project not loaded", "You are trying to open a visualization without opening a project. This is not supported.");
                }
                else if (arguments.Count == 0)
                {
                    ApplicationState.DialogBoxManager.Open(Tools.Unity.DialogBoxManager.AlertType.Error, "Couldn't load visualizations", "The names of the visualizations have not been specified.");
                }
                else
                {
                    IEnumerable<Visualization> visualizations;
                    if (arguments[0] == "all")
                    {
                        visualizations = ApplicationState.ProjectLoaded.Visualizations;
                    }
                    else
                    {
                        visualizations = from visu in ApplicationState.ProjectLoaded.Visualizations where arguments.Contains(visu.Name) select visu;
                    }
                    Task visualizationLoadingTask;
                    yield return this.StartCoroutineAsync(ApplicationState.Module3D.c_Load(visualizations), out visualizationLoadingTask);
                    if (visualizationLoadingTask.State == TaskState.Error)
                    {
                        ApplicationState.DialogBoxManager.Open(Tools.Unity.DialogBoxManager.AlertType.Error, "Couldn't load visualizations", "The specified visualizations could not be loaded.");
                    }
                }
            }
        }
        #endregion
    }
}