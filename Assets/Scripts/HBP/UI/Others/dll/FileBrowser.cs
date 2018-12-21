using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace HBP.UI
{
    public class FileBrowser
    {
        #region Public Methods
        private static List<string> launch_fileDialog_window(string argumentsFileDialogs)
        {
#if UNITY_EDITOR
            string filePath = Application.dataPath + "/../tools/windows/HiBoP_Tools.exe";
#else
            string filePath = Application.dataPath + "/../tools/HiBoP_Tools.exe";
            if (Application.platform == RuntimePlatform.OSXPlayer)
            {
                filePath = Application.dataPath + "/../../HiBoP_Tools.app/Contents/MacOS/HiBoP_Tools";
                //filePath = Application.dataPath + "/../tools/HiBoP_Tools";
                //UnityEngine.Debug.LogError("mac -> " + filePath);
            }
            else if(Application.platform == RuntimePlatform.LinuxPlayer)
            {
                filePath = Application.dataPath + "/../tools/HiBoP_Tools";
                UnityEngine.Debug.Log(filePath);
            }
#endif
            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    Arguments = argumentsFileDialogs, // "FileDialog get_existing_file_names \"message test\" \"Images files (*.jpg, *.png)\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            List<string> paths = new List<string>();
            while (!proc.StandardOutput.EndOfStream)
                paths.Add(proc.StandardOutput.ReadLine());

            return paths;
        }
        /// <summary>
        /// Open a qt file dialog and return the path of an existing directory.
        /// </summary>
        /// <param name="message"> message to be displayed in top of the file dialog </param>
        /// <param name="directoryPath"> default directory of the file dialog </param>
        /// <returns> return an empty path if no directory has been choosen or if an error occurs </returns>
        public static string GetExistingDirectoryName(string message = "Select a directory", string directoryPath = "")
        {
            string[] paths = SFB.StandaloneFileBrowser.OpenFolderPanel(message, directoryPath, false);
            return paths.Length > 0 ? paths[0] : string.Empty;
        }
        /// <summary>
        /// Open a qt file dialog and return the path of a selected file.
        /// </summary>
        /// <param name="filtersArray"> extension filters of the files of be displayed in the file dialog (ex: filtersArray[0] = "txt", filtersArray[1] = "png" ...) </param>
        /// <param name="message">  message to be displayed in top of the file dialog  </param>
        /// <param name="filePath"> default directory of the file dialog </param>
        /// <returns> return an empty path if no file has been choosen or if an error occurs </returns>
        public static string GetExistingFileName(string[] filtersArray = null, string message = "Select a file", string filePath = "")
        {
            string directory = string.IsNullOrEmpty(filePath) ? new DirectoryInfo(Application.dataPath).Parent.FullName : new FileInfo(filePath).DirectoryName;
            var paths = SFB.StandaloneFileBrowser.OpenFilePanel(message, directory, new SFB.ExtensionFilter[] { new SFB.ExtensionFilter("Files", filtersArray) }, false);
            return paths.Length > 0 ? paths[0] : string.Empty;
        }
        /// <summary>
        /// Open a qt file dialog and return the list of path of the selected files.
        /// </summary>
        /// <param name="filtersArray">  extension filters of the files of be displayed in the file dialog (ex: filtersArray[0] = "txt", filtersArray[1] = "png" ...) </param>
        /// <param name="message"> message to be displayed in top of the file dialog </param>
        /// <param name="filePath"> default directory of the file dialog </param>
        /// <returns> return an empty path if no file has been choosen or if an error occurs </returns>
        public static string[] GetExistingFileNames(string[] filtersArray = null, string message = "Select files", string filePath = "")
        {
            string directory = string.IsNullOrEmpty(filePath) ? new DirectoryInfo(Application.dataPath).Parent.FullName : new FileInfo(filePath).DirectoryName;
            var paths = SFB.StandaloneFileBrowser.OpenFilePanel(message, directory, new SFB.ExtensionFilter[] { new SFB.ExtensionFilter("Files", filtersArray) }, true);
            return paths;
        }
        /// <summary>
        /// Open a qt file dialog and return the path of a saved file
        /// </summary>
        /// <param name="filtersArray"> extension filters of the files of be displayed in the file dialog (ex: filtersArray[0] = "txt", filtersArray[1] = "png" ...) </param>
        /// <param name="message">  message to be displayed in top of the file dialog  </param>
        /// <param name="filePath"> default directory of the file dialog </param>
        /// <returns> return an empty path if no file has been choosen or if an error occurs </returns>
        public static string GetSavedFileName(string[] filtersArray = null, string message = "Save to", string filePath = "", string defaultName = "")
        {
            string directory = string.IsNullOrEmpty(filePath) ? new DirectoryInfo(Application.dataPath).Parent.FullName : new FileInfo(filePath).DirectoryName;
            var path = SFB.StandaloneFileBrowser.SaveFilePanel(message, directory, defaultName, new SFB.ExtensionFilter[] { new SFB.ExtensionFilter("Files", filtersArray) });
            return path;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <param name="isUnityEditor"></param>
        /// <returns></returns>
        public static int get_action_to_do_from_error_dialog_test(string errorMessage, bool isUnityEditor)
        {
            string unity = isUnityEditor ? "editor" : "build";
            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Application.dataPath + "/../tools/HiBoP_Tools.exe",
                    Arguments = "FileDialog get_action_to_do_from_error_dialog +\"" + errorMessage + "\" " + unity,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            proc.WaitForExit();
            return proc.ExitCode;
        }
        #endregion
    }
}