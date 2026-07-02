using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.UI.Tools;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    public class SiteStateExport : Tool
    {
        #region Properties
        /// <summary>
        /// Import states from a file
        /// </summary>
        [SerializeField] private Button m_Import;
        /// <summary>
        /// Export states to a file
        /// </summary>
        [SerializeField] private Button m_Export;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Import.onClick.AddListener(() =>
            {
                if (ListenerLock) return;
                
                LoadSiteStatesToSelectedColumn();
            });
            m_Export.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SaveSiteStatesOfSelectedColumn();
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Import.interactable = false;
            m_Export.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            m_Import.interactable = true;
            m_Export.interactable = true;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Save the sites of the selected column
        /// </summary>
        private async void SaveSiteStatesOfSelectedColumn()
        {
            string savePath = await ToolbarExternalActions.GetSavedFileNameAsync(new string[] { "csv" }, "Save site states to");
            if (!string.IsNullOrEmpty(savePath))
            {
                SaveSiteStates(savePath);
            }
        }
        /// <summary>
        /// Save the state of the sites of the selected column to a file
        /// </summary>
        /// <param name="path">Path where to save the data</param>
        private void SaveSiteStates(string path)
        {
            try
            {
                using (StreamWriter sw = new(path))
                {
                    sw.WriteLine("ID,Blacklisted,Highlighted,Color,Labels");
                    foreach (var site in SelectedColumn.SiteStateBySiteID)
                    {
                        sw.WriteLine("{0},{1},{2},{3},{4}", site.Key, site.Value.IsBlackListed, site.Value.IsHighlighted, site.Value.Color.ToHexString(), string.Join(";", site.Value.Labels));
                    }
                }
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Informational, "Site states saved", "Site states of the selected column have been saved to <color=#3080ffff>" + path + "</color>").Forget();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Can not save site states", "Please verify your rights.").Forget();
            }
        }
        /// <summary>
        /// Load the sites of the selected column
        /// </summary>
        private async void LoadSiteStatesToSelectedColumn()
        {
            string loadPath = await ToolbarExternalActions.GetExistingFileNameAsync(new string[] { "csv" }, "Load site states");
            if (!string.IsNullOrEmpty(loadPath))
            {
                var result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Informational, "Override Site Attributes", "Would you like to override the site attributes for all columns or just the selected one?\n\nReminder: a site's attributes consist of its blacklisted status, highlighted status, color and labels.", "All columns", "Selected only", "Cancel");
                if (result == 2) return;
                LoadSiteStates(loadPath, result == 0);
            }
        }
        /// <summary>
        /// Load the states of the sites to this column from a file
        /// </summary>
        /// <param name="path">Path of the file to load data from</param>
        public void LoadSiteStates(string path, bool allColumns)
        {
            try
            {
                List<Column3D> columns = allColumns ? SelectedScene.Columns : new List<Column3D> { SelectedColumn };
                using StreamReader sr = new(path);
                // Find which column of the csv corresponds to which argument
                string firstLine = sr.ReadLine();
                string[] firstLineSplits = firstLine.Split(',');
                int[] indices = new int[5];
                for (int i = 0; i < indices.Length; ++i)
                {
                    string split = firstLineSplits[i];
                    indices[i] = split == "ID" ? 0 : split == "Blacklisted" ? 1 : split == "Highlighted" ? 2 : split == "Color" ? 3 : split == "Labels" ? 4 : i;
                }
                // Fill states
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] args = line.Split(',');
                    Core.Object3D.SiteState state = new();

                    if (bool.TryParse(args[indices[1]], out bool stateValue))
                    {
                        state.IsBlackListed = stateValue;
                    }
                    else
                    {
                        state.IsBlackListed = false;
                    }

                    if (bool.TryParse(args[indices[2]], out stateValue))
                    {
                        state.IsHighlighted = stateValue;
                    }
                    else
                    {
                        state.IsHighlighted = false;
                    }

                    if (ColorUtility.TryParseHtmlString(args[indices[3]], out Color color))
                    {
                        state.Color = color;
                    }
                    else
                    {
                        state.Color = Core.Object3D.SiteState.DefaultColor;
                    }

                    string[] labels = args[indices[4]].Split(new char[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
                    foreach (var label in labels)
                    {
                        state.AddLabel(label);
                    }

                    foreach (var column in columns)
                    {
                        if (column.SiteStateBySiteID.TryGetValue(args[indices[0]], out Core.Object3D.SiteState existingState))
                        {
                            existingState.ApplyState(state);
                        }
                        else
                        {
                            column.SiteStateBySiteID.Add(args[indices[0]], state);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Can not load site states", "Please verify your files and try again.").Forget();
            }
        }
        #endregion
    }
}
