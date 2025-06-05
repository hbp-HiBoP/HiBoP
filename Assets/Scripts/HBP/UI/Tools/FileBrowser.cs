using Cysharp.Threading.Tasks;
using HBP.Core.Tools;
using System;
using System.IO;
using ThirdParty.SFB;
using System.Linq;
using UnityEngine;

namespace HBP.UI.Tools
{
    public class FileBrowser
    {
        #region Properties
        private static string m_LastSelectedDirectory = "";
        #endregion

        #region Public Methods
        public static async UniTask<string> GetExistingDirectoryNameAsync(string message = "Select a directory", string directoryPath = "")
        {
            bool done = false;
            string[] result;

#if UNITY_STANDALONE_OSX
            StandaloneFileBrowser.OpenFolderPanelAsync(message, string.IsNullOrEmpty(directoryPath) ? m_LastSelectedDirectory : directoryPath, false, (paths) =>
            {
                result = paths;
                done = true;
            });
#else
            result = StandaloneFileBrowser.OpenFolderPanel(message, string.IsNullOrEmpty(directoryPath) ? m_LastSelectedDirectory : directoryPath, false);
            done = true;
#endif
            await UniTask.WaitUntil(() => done);
            return result.Length > 0 ? (m_LastSelectedDirectory = result[0].StandardizeToEnvironement()) : string.Empty;
        }
        public static async UniTask<string[]> GetExistingDirectoryNamesAsync(string message = "Select a directory", string directoryPath = "")
        {
            bool done = false;
            string[] result;

#if UNITY_STANDALONE_OSX
            StandaloneFileBrowser.OpenFolderPanelAsync(message, string.IsNullOrEmpty(directoryPath) ? m_LastSelectedDirectory : directoryPath, true, (paths) =>
            {
                result = paths;
                done = true;
            });
#else
            result = StandaloneFileBrowser.OpenFolderPanel(message, string.IsNullOrEmpty(directoryPath) ? m_LastSelectedDirectory : directoryPath, true);
            done = true;
#endif
            await UniTask.WaitUntil(() => done);
            return result.Select(r => r.StandardizeToEnvironement()).ToArray();
        }
        public static async UniTask<string> GetExistingFileNameAsync(string[] filtersArray = null, string message = "Select a file", string filePath = "")
        {
            bool done = false;
            string result = string.Empty;

#if UNITY_STANDALONE_OSX
            StandaloneFileBrowser.OpenFilePanelAsync(message, string.IsNullOrEmpty(filePath) ? m_LastSelectedDirectory : new FileInfo(filePath).DirectoryName, new ExtensionFilter[] { new ExtensionFilter("Files", filtersArray) }, false, (paths) =>
            {
                if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
                {
                    m_LastSelectedDirectory = new FileInfo(paths[0]).DirectoryName;
                    result = paths[0];
                }
                done = true;
            });
#else
            string[] paths = StandaloneFileBrowser.OpenFilePanel(message, string.IsNullOrEmpty(filePath) ? m_LastSelectedDirectory : new FileInfo(filePath).DirectoryName, new ExtensionFilter[] { new ExtensionFilter("Files", filtersArray) }, false);
            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                m_LastSelectedDirectory = new FileInfo(paths[0]).DirectoryName;
                result = paths[0];
            }
            done = true;
#endif
            await UniTask.WaitUntil(() => done);
            return result.StandardizeToEnvironement();
        }
        public static async UniTask<string[]> GetExistingFileNamesAsync(string[] filtersArray = null, string message = "Select files", string filePath = "")
        {
            bool done = false;
            string[] result = Array.Empty<string>();

#if UNITY_STANDALONE_OSX
            StandaloneFileBrowser.OpenFilePanelAsync(message, string.IsNullOrEmpty(filePath) ? m_LastSelectedDirectory : new FileInfo(filePath).DirectoryName, new ExtensionFilter[] { new ExtensionFilter("Files", filtersArray) }, true, (paths) =>
            {
                if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
                {
                    m_LastSelectedDirectory = new FileInfo(paths[0]).DirectoryName;
                    result = paths;
                }
                done = true;
            });
#else
            string[] paths = StandaloneFileBrowser.OpenFilePanel(message, string.IsNullOrEmpty(filePath) ? m_LastSelectedDirectory : new FileInfo(filePath).DirectoryName, new ExtensionFilter[] { new ExtensionFilter("Files", filtersArray) }, true);
            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                m_LastSelectedDirectory = new FileInfo(paths[0]).DirectoryName;
                result = paths;
            }
            done = true;
#endif
            await UniTask.WaitUntil(() => done);
            return result.Select(r => r.StandardizeToEnvironement()).ToArray();
        }
        public static async UniTask<string> GetSavedFileNameAsync(string[] filtersArray = null, string message = "Save to", string filePath = "", string defaultName = "")
        {
            bool done = false;
            string result = string.Empty;

#if UNITY_STANDALONE_OSX
            StandaloneFileBrowser.SaveFilePanelAsync(message, string.IsNullOrEmpty(filePath) ? m_LastSelectedDirectory : new FileInfo(filePath).DirectoryName, defaultName, filtersArray == null ? null : new ExtensionFilter[] { new ExtensionFilter("Files", filtersArray) }, (path) =>
            {
                if (!string.IsNullOrEmpty(path))
                {
                    m_LastSelectedDirectory = new FileInfo(path).DirectoryName;
                    result = path;
                }
                done = true;
            });
#else
            string path = StandaloneFileBrowser.SaveFilePanel(message, string.IsNullOrEmpty(filePath) ? m_LastSelectedDirectory : new FileInfo(filePath).DirectoryName, defaultName, filtersArray == null ? null : new ExtensionFilter[] { new ExtensionFilter("Files", filtersArray) });
            if (!string.IsNullOrEmpty(path))
            {
                m_LastSelectedDirectory = new FileInfo(path).DirectoryName;
                result = path;
            }
            done = true;
#endif
            await UniTask.WaitUntil(() => done);
            return result.StandardizeToEnvironement();
        }
        #endregion
    }
}