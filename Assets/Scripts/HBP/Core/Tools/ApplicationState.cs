using HBP.Core.Data;
using System.IO;
using UnityEngine;

namespace HBP.Core.Tools
{
    public static class ApplicationState
    {
        public static string Version { get; private set; } = Application.version;

        /// <summary>
        /// ID of this instance of HiBoP
        /// </summary>
        public static string InstanceID { get; private set; } = System.Guid.NewGuid().ToString();

        /// <summary>
        /// Project loaded on the application.
        /// </summary>
        public static Project LoadedProject { get; set; } = null;

        /// <summary>
        /// Location of the project loaded.
        /// </summary>
        public static string LoadedProjectLocation { get; set; } = string.Empty;

        /// <summary>
        /// TMP folder to store the open projects
        /// </summary>
        public static string TMPFolder { get; private set; } = Path.Combine(Application.persistentDataPath, "tmp");

        /// <summary>
        /// Full path to the loaded project
        /// </summary>
        public static string ExtractProjectFolder { get; private set; } = Path.Combine(Application.persistentDataPath, InstanceID);

        /// <summary>
        /// Path to the data folder
        /// </summary>
#if UNITY_EDITOR
        public static string DataPath { get; private set; } = Path.Combine(Application.dataPath, "Data");
#else
        public static string DataPath { get; private set; } = Path.Combine(Application.dataPath, "..", "Data");
#endif

        /// <summary>
        /// Path to the database folder
        /// </summary>
        public static string DatabasePath { get; private set; } = Path.Combine(Application.persistentDataPath, "Database");
    }
}