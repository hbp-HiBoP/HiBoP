using System.IO;

namespace HBP.UI
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
            string[] paths = SFB.StandaloneFileBrowser.OpenFolderPanel(message, string.IsNullOrEmpty(directoryPath) ? m_LastSelectedDirectory : directoryPath, false);
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
            string[] paths = SFB.StandaloneFileBrowser.OpenFilePanel(message, string.IsNullOrEmpty(filePath) ? m_LastSelectedDirectory : new FileInfo(filePath).DirectoryName, new SFB.ExtensionFilter[] { new SFB.ExtensionFilter("Files", filtersArray) }, false);
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
            string[] paths = SFB.StandaloneFileBrowser.OpenFilePanel(message, string.IsNullOrEmpty(filePath) ? "" : new FileInfo(filePath).DirectoryName, new SFB.ExtensionFilter[] { new SFB.ExtensionFilter("Files", filtersArray) }, true);
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
            string path = SFB.StandaloneFileBrowser.SaveFilePanel(message, string.IsNullOrEmpty(filePath) ? "" : new FileInfo(filePath).DirectoryName, defaultName, filtersArray == null ? null : new SFB.ExtensionFilter[] { new SFB.ExtensionFilter("Files", filtersArray) });
            return path;
        }
        #endregion
    }
}