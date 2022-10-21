using UnityEngine;
using UnityEngine.Events;
using System.IO;
using ThirdParty.CielaSpike;
using HBP.Core.Tools;
using HBP.Core.Data;
using HBP.Data.Module3D;
using System.Collections.Generic;
using HBP.Core.Interfaces;
using System.Linq;
using System;

namespace HBP.UI.Tools
{
    public class ProjectLoaderSaver : MonoBehaviour
    {
        #region Public Methods  
        public void Load(ProjectInfo projectInfo)
        {
            UnityEngine.Profiling.Profiler.BeginSample("1");
            Project projectToLoad = new Project();

            DataManager.Clear();
            Project projectLoaded = ApplicationState.ProjectLoaded;
            string projectLoadedLocation = ApplicationState.ProjectLoadedLocation;
            ApplicationState.ProjectLoaded = projectToLoad;
            ApplicationState.ProjectLoadedLocation = Directory.GetParent(projectInfo.Path).FullName;

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("2");

            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            LoadingManager.Load(
                projectToLoad.c_Load(projectInfo, (progress, duration, text) => onChangeProgress.Invoke(progress, duration, text)),
                onChangeProgress,
                (taskState) =>
                {
                    if (taskState == TaskState.Done)
                    {
                        FindObjectOfType<MenuButtonState>().SetInteractables();
                        Dictionary<string, List<Tuple<BaseData, string>>> problematicData = ApplicationState.ProjectLoaded.CheckProjectIDs();
                        if (problematicData.Count > 0)
                        {
                            string displayedString = "";
                            foreach (var kv in problematicData)
                            {
                                displayedString += string.Format("<b>{0}</b>\n{1}\n\n", kv.Key, string.Join("\n", kv.Value.Select(t => string.Format(" - {0}", t.Item2))));
                            }
                            Debug.Log(displayedString);
                            string[] lines = displayedString.Split("\n");
                            if (lines.Length > 20)
                            {
                                string duplicateFilePath = Path.Combine(ApplicationState.ProjectLoadedLocation, string.Format("{0}_duplicate_IDs.txt", ApplicationState.ProjectLoaded.Preferences.Name));
                                displayedString = "";
                                for (int i = 0; i < 18; ++i)
                                {
                                    displayedString += lines[i];
                                    displayedString += "\n";
                                }
                                displayedString += "[...]\n";
                                using (StreamWriter sw = new StreamWriter(duplicateFilePath))
                                {
                                    for (int i = 0; i < lines.Length; ++i)
                                    {
                                        sw.WriteLine(lines[i]);
                                    }
                                }
                                displayedString += string.Format("\n<i>Full report has been saved at {0}</i>\n\n", duplicateFilePath);
                            }
                            DialogBoxManager.Open(DialogBoxManager.AlertType.WarningMultiOptions, "ID issue", string.Format("Some IDs of this project are used by multiple different objects:\n\n{0}You have two options: you can regenerate the IDs of problematic objects automatically, but this can unlink some of your objects (for example, some datasets may not be linked to the right protocol), or you can leave them as is but you may encounter issues and will need to fix the IDs manually later. If you did not unzip the project and modify files using a text editor, please send a bug report.\nWhat do you want to do?", displayedString),
                                () =>
                                {
                                    foreach (var kv in problematicData)
                                    {
                                        for (int i = 1; i < kv.Value.Count; ++i)
                                        {
                                            kv.Value[i].Item1.GenerateID();
                                        }
                                    }
                                    DialogBoxManager.Open(DialogBoxManager.AlertType.Informational, "New IDs generated", "New IDs have been generated for duplicates. Do not forget to save the project to keep the new IDs.");
                                }, "Regenerate IDs", () => { }, "Leave IDs as is");
                        }
                    }
                    else
                    {
                        ApplicationState.ProjectLoaded = projectLoaded;
                        ApplicationState.ProjectLoadedLocation = projectLoadedLocation;
                    }
                });
            UnityEngine.Profiling.Profiler.EndSample();
        }
        public void Save(string path)
        {
            Module3DMain.SaveConfigurations();
            ApplicationState.ProjectLoadedLocation = path;
            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            LoadingManager.Load(
                ApplicationState.ProjectLoaded.c_Save(path, (progress, duration, text) => onChangeProgress.Invoke(progress, duration, text)),
                onChangeProgress);
        }
        public void Save()
        {
            Save(ApplicationState.ProjectLoadedLocation);
        }
        public void SaveAndReload()
        {
            Save();
            FindObjectOfType<MenuButtonState>().SetInteractables();
        }
        #endregion
    }
}