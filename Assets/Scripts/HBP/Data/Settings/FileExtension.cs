namespace HBP.Data.Settings
{
    /// <summary>
    /// Class which define the extension of the differents files.
    /// </summary>
	public static class FileExtension
	{
		private static string m_protocolExtension = ".prov";
        /// <summary>
        /// Extension of protocol files.
        /// </summary>
		public static string Protocol{get{return m_protocolExtension;}}

        private static string m_posExtension = ".pos";
        /// <summary>
        /// Extension of protocol files.
        /// </summary>
		public static string POS { get { return m_posExtension; } }

        private static string m_patientExtension = ".patient";
        /// <summary>
        /// Extension of patient files.
        /// </summary>
        public static string Patient{get{return m_patientExtension;}}

		private static string m_groupExtension = ".group";
        /// <summary>
        /// Extension of group files.
        /// </summary>
        public static string Group{get{return m_groupExtension;}}

		private static string m_datasetExtension = ".dataset";
        /// <summary>
        /// Extension of dataset files.
        /// </summary>
        public static string Dataset{get{return m_datasetExtension; }}

		private static string m_ROIExtension = ".roi";
        /// <summary>
        /// Extension of region of interest files.
        /// </summary>
        public static string ROI{get{return m_ROIExtension;}}

		private static string m_Settings = ".settings";
        /// <summary>
        /// Extension of project settings files.
        /// </summary>
        public static string Settings{get{return m_Settings;}}

        private static string m_implantationExtension = ".pts";
        /// <summary>
        /// Extension of implantation files.
        /// </summary>
        public static string Implantation { get { return m_implantationExtension; } }


        private static string m_eegExtension = ".eeg";
        /// <summary>
        /// Extension of implantation files.
        /// </summary>
        public static string EEG { get { return m_eegExtension; } }

        private static string m_singleVisualisation = ".singleVisualisation";
        /// <summary>
        /// Extension of single visualisation files.
        /// </summary>
        public static string SingleVisualisation { get { return m_singleVisualisation; } }

        private static string m_multiVisualisation = ".multiVisualisation";
        /// <summary>
        /// Extension of multi visualisation files.
        /// </summary>
        public static string MultiVisualisation { get { return m_multiVisualisation; } }
    }
}
