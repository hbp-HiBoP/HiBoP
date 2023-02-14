using HBP.Theme;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HBP.UI.Tools;
using HBP.Core.Tools;
using System.IO;
using UnityEngine;
using System;
using HBP.Core.Data;

namespace HBP.UI.Tools
{
    public static class UITools
    {
        public static void SetIEnumerableFieldInItem(this UnityEngine.UI.Text text, string title, IEnumerable<string> values, State emptyState, int max = 3)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(string.Format("{0} :", title));
            stringBuilder.Append(values.ToTooltip(max));
            var themeElement = text.GetComponent<ThemeElement>();
            if (themeElement)
            {
                if (values.Count() == 0) themeElement.Set(emptyState);
                else themeElement.Set();
            }
            var tooltip = text.GetComponent<Tooltip>();
            if (tooltip) tooltip.Text = stringBuilder.ToString();
            text.text = values.Count().ToString();
        }
        public static void SetIEnumerableFieldInItem(this UnityEngine.UI.Text text, int size, State emptyState)
        {
            var themeElement = text.GetComponent<ThemeElement>();
            if (themeElement)
            {
                if (size == 0) themeElement.Set(emptyState);
                else themeElement.Set();
            }
            text.text = size.ToString();
        }
        public static void CheckProjectIDAndAskForRegeneration()
        {
            Dictionary<string, List<Tuple<BaseData, string>>> problematicData = ApplicationState.ProjectLoaded.CheckProjectIDs();
            if (problematicData.Count > 0)
            {
                string displayedString = "";
                foreach (var kv in problematicData)
                {
                    displayedString += string.Format("<b>{0}</b>\n{1}\n\n", kv.Key, string.Join("\n", kv.Value.Select(t => string.Format(" - {0}", t.Item2))));
                }
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
    }
}