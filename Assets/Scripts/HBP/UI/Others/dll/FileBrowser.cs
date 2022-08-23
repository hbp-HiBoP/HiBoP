using System.IO;
using ThirdParty.SFB;

namespace HBP.UI.Tools
{
    public class FileBrowser
    {
        #region Properties
        private static string m_LastSelectedDirectory = "";
        #endregion

        #region Public Methods
        /// <summary>
        /// Open a qt file dialog and return the path of an existing directory.
        /// </summary>
        /// <param name="message"> message to be displayed in top of the file dialog </param>
        /// <param name="directoryPath"> default directory of the file dialog </param>
        /// <returns> return an empty path if no directory has been choosen or if an error occurs </returns>
        public static string GetExistingDirectoryName(string message = "Select a directory", string directoryPath = "")
        {
            string[] paths = StandaloneFileBrowser.OpenFolderPanel(message, string.IsNullOrEmpty(directoryPath) ? m_LastSelectedDirectory : directoryPath, false);
            return paths.Length > 0 ? (m_LastSelectedDirectory = paths[0]) : string.Empty;
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
            string[] paths = StandaloneFileBrowser.OpenFilePanel(message, string.IsNullOrEmpty(filePath) ? m_LastSelectedDirectory : new FileInfo(filePath).DirectoryName, new ExtensionFilter[] { new ExtensionFilter("Files", filtersArray) }, false);
            if (paths.Length > 0)
            {
                m_LastSelectedDirectory = new FileInfo(paths[0]).DirectoryName;
                return paths[0];
            }
            else
            {
                return string.Empty;
            }
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
            string[] paths = StandaloneFileBrowser.OpenFilePanel(message, string.IsNullOrEmpty(filePath) ? m_LastSelectedDirectory : new FileInfo(filePath).DirectoryName, new ExtensionFilter[] { new ExtensionFilter("Files", filtersArray) }, true);
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
            string path = StandaloneFileBrowser.SaveFilePanel(message, string.IsNullOrEmpty(filePath) ? m_LastSelectedDirectory : new FileInfo(filePath).DirectoryName, defaultName, filtersArray == null ? null : new ExtensionFilter[] { new ExtensionFilter("Files", filtersArray) });
            return path;
        }
        /// <summary>
        /// Open a qt file dialog and return the path of an existing directory.
        /// </summary>
        /// <param name="callback">Action to be performed</param>
        /// <param name="message"> message to be displayed in top of the file dialog </param>
        /// <param name="directoryPath"> default directory of the file dialog </param>
        /// <returns> return an empty path if no directory has been choosen or if an error occurs </returns>
        public static void GetExistingDirectoryNameAsync(System.Action<string> callback, string message = "Select a directory", string directoryPath = "")
        {
            StandaloneFileBrowser.OpenFolderPanelAsync(message, string.IsNullOrEmpty(directoryPath) ? m_LastSelectedDirectory : directoryPath, false, (paths) =>
            {
                callback(paths.Length > 0 ? (m_LastSelectedDirectory = paths[0]) : string.Empty);
            });
        }
        /// <summary>
        /// Open a qt file dialog and return the path of a selected file.
        /// </summary>
        /// <param name="callback">Action to be performed</param>
        /// <param name="filtersArray"> extension filters of the files of be displayed in the file dialog (ex: filtersArray[0] = "txt", filtersArray[1] = "png" ...) </param>
        /// <param name="message">  message to be displayed in top of the file dialog  </param>
        /// <param name="filePath"> default directory of the file dialog </param>
        /// <returns> return an empty path if no file has been choosen or if an error occurs </returns>
        public static void GetExistingFileNameAsync(System.Action<string> callback, string[] filtersArray = null, string message = "Select a file", string filePath = "")
        {
            StandaloneFileBrowser.OpenFilePanelAsync(message, string.IsNullOrEmpty(filePath) ? m_LastSelectedDirectory : new FileInfo(filePath).DirectoryName, new ExtensionFilter[] { new ExtensionFilter("Files", filtersArray) }, false, (paths) =>
            {
                if (paths.Length > 0)
                {
                    if (!string.IsNullOrEmpty(paths[0]))
                    {
                        m_LastSelectedDirectory = new FileInfo(paths[0]).DirectoryName;
                        callback(paths[0]);
                    }
                    else
                    {
                        callback(string.Empty);
                    }
                }
                else
                {
                    callback(string.Empty);
                }
            });
        }
        /// <summary>
        /// Open a qt file dialog and return the list of path of the selected files.
        /// </summary>
        /// <param name="callback">Action to be performed</param>
        /// <param name="filtersArray">  extension filters of the files of be displayed in the file dialog (ex: filtersArray[0] = "txt", filtersArray[1] = "png" ...) </param>
        /// <param name="message"> message to be displayed in top of the file dialog </param>
        /// <param name="filePath"> default directory of the file dialog </param>
        /// <returns> return an empty path if no file has been choosen or if an error occurs </returns>
        public static void GetExistingFileNamesAsync(System.Action<string[]> callback, string[] filtersArray = null, string message = "Select files", string filePath = "")
        {
            StandaloneFileBrowser.OpenFilePanelAsync(message, string.IsNullOrEmpty(filePath) ? m_LastSelectedDirectory : new FileInfo(filePath).DirectoryName, new ExtensionFilter[] { new ExtensionFilter("Files", filtersArray) }, true, callback);
        }
        /// <summary>
        /// Open a qt file dialog and return the path of a saved file
        /// </summary>
        /// <param name="callback">Action to be performed</param>
        /// <param name="filtersArray"> extension filters of the files of be displayed in the file dialog (ex: filtersArray[0] = "txt", filtersArray[1] = "png" ...) </param>
        /// <param name="message">  message to be displayed in top of the file dialog  </param>
        /// <param name="filePath"> default directory of the file dialog </param>
        /// <returns> return an empty path if no file has been choosen or if an error occurs </returns>
        public static void GetSavedFileNameAsync(System.Action<string> callback, string[] filtersArray = null, string message = "Save to", string filePath = "", string defaultName = "")
        {
            StandaloneFileBrowser.SaveFilePanelAsync(message, string.IsNullOrEmpty(filePath) ? m_LastSelectedDirectory : new FileInfo(filePath).DirectoryName, defaultName, filtersArray == null ? null : new ExtensionFilter[] { new ExtensionFilter("Files", filtersArray) }, callback);
        }
        #endregion
    }
}