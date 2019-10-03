using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using T = Tools.Unity;

namespace HBP.UI.Module3D.Tools
{
    public class TriangleErasingLoaderSaver : Tool
    {
        #region Properties
        [SerializeField] private Button m_Save;
        [SerializeField] private Button m_Load;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Save.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                try
                {
                    string file = FileBrowser.GetSavedFileName(new string[] { "trimask" }, "Save brain state to");
                    if (!string.IsNullOrEmpty(file))
                    {
                        string fileContent = string.Join("\n", SelectedScene.TriangleEraser.CurrentMasks.Select(m => string.Join(" ", m)));
                        File.WriteAllText(file, fileContent);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    ApplicationState.DialogBoxManager.Open(T.DialogBoxManager.AlertType.Error, "Save Error", "The file could not be saved.");
                }
            });
            m_Load.onClick.AddListener(() =>
            {
                if (ListenerLock) return;
                try
                {
                    string file = FileBrowser.GetSavedFileName(new string[] { "trimask" }, "Load brain state from");
                    if (!string.IsNullOrEmpty(file))
                    {
                        string fileContent = File.ReadAllText(file);
                        SelectedScene.TriangleEraser.CurrentMasks = fileContent.Split('\n').Select(s => s.Split(' ').Select(split => int.Parse(split)).ToArray()).ToList();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    ApplicationState.DialogBoxManager.Open(T.DialogBoxManager.AlertType.Error, "Load Error", "The file could not be loaded.");
                }
            });
        }
        public override void DefaultState()
        {
            m_Save.interactable = false;
            m_Load.interactable = false;
        }
        public override void UpdateInteractable()
        {
            m_Save.interactable = true;
            m_Load.interactable = true;
        }
        #endregion
    }
}