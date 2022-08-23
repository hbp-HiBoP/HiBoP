using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HBP.UI.Tools;

namespace HBP.UI.Toolbar
{
    public class TriangleErasingLoaderSaver : Tool
    {
        #region Properties
        /// <summary>
        /// Save the erased area to a mask
        /// </summary>
        [SerializeField] private Button m_Save;
        /// <summary>
        /// Load a mask to an erased area
        /// </summary>
        [SerializeField] private Button m_Load;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Save.onClick.AddListener(() =>
            {
                if (ListenerLock) return;
#if UNITY_STANDALONE_OSX
                FileBrowser.GetSavedFileNameAsync((file) =>
                {
                    if (!string.IsNullOrEmpty(file))
                    {
                        try
                        {
                            string fileContent = string.Join("\n", SelectedScene.TriangleEraser.CurrentMasks.Select(m => string.Join(" ", m)));
                            File.WriteAllText(file, fileContent);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Save Error", "The file could not be saved.");
                        }
                    }
                }, new string[] { "trimask" }, "Save brain state to");
#else
                string file = FileBrowser.GetSavedFileName(new string[] { "trimask" }, "Save brain state to");
                if (!string.IsNullOrEmpty(file))
                {
                    try
                    {
                        string fileContent = string.Join("\n", SelectedScene.TriangleEraser.CurrentMasks.Select(m => string.Join(" ", m)));
                        File.WriteAllText(file, fileContent);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Save Error", "The file could not be saved.");
                    }
                }
#endif
            });
            m_Load.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

#if UNITY_STANDALONE_OSX
                FileBrowser.GetExistingFileNameAsync((file) =>
                {
                    if (!string.IsNullOrEmpty(file))
                    {
                        try
                        {
                            string fileContent = File.ReadAllText(file);
                            SelectedScene.TriangleEraser.CurrentMasks = fileContent.Split('\n').Select(s => s.Split(' ').Select(split => int.Parse(split)).ToArray()).ToList();
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Load Error", "The file could not be loaded.");
                        }
                    }
                }, new string[] { "trimask" }, "Load brain state from");
#else
                string file = FileBrowser.GetExistingFileName(new string[] { "trimask" }, "Load brain state from");
                if (!string.IsNullOrEmpty(file))
                {
                    try
                    {
                        string fileContent = File.ReadAllText(file);
                        SelectedScene.TriangleEraser.CurrentMasks = fileContent.Split('\n').Select(s => s.Split(' ').Select(split => int.Parse(split)).ToArray()).ToList();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Load Error", "The file could not be loaded.");
                    }
                }
#endif
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Save.interactable = false;
            m_Load.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            m_Save.interactable = true;
            m_Load.interactable = true;
        }
        #endregion
    }
}