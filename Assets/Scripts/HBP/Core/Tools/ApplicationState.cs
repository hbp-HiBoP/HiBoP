using HBP.Core.Data;

namespace HBP.Core.Tools
{
    public static class ApplicationState
    {
        /// <summary>
        /// ID of this instance of HiBoP
        /// </summary>
        public static string InstanceID { get; private set; } = System.Guid.NewGuid().ToString();

        /// <summary>
        /// Project loaded on the application.
        /// </summary>
        public static Project ProjectLoaded { get; set; }

        /// <summary>
        /// Location of the project loaded.
        /// </summary>
        public static string ProjectLoadedLocation { get; set; }

        /// <summary>
        /// TMP folder to store the open projects
        /// </summary>
        public static string TMPFolder { get; set; }

        /// <summary>
        /// Full path to the loaded project
        /// </summary>
        public static string ExtractProjectFolder { get; set; }

        /// <summary>
        /// Path to the data folder
        /// </summary>
        static public string DataPath { get; set; }
    }
}