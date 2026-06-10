using HBP.Core.Tools;
using System.Collections.Generic;
using System.IO;

namespace HBP.Data.BIDS
{
    /// <summary>
    /// Manager for loading, saving, and creating BIDS export configurations.
    /// </summary>
    public static class BIDSConfigurationManager
    {
        /// <summary>
        /// Load a BIDS export configuration from a JSON file.
        /// If the file doesn't exist, returns the default configuration.
        /// </summary>
        /// <param name="path">Path to the configuration JSON file</param>
        /// <returns>Loaded or default configuration</returns>
        public static BIDSExportConfiguration LoadConfiguration(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return GetDefaultConfiguration();
            }
            
            try
            {
                return ClassLoaderSaver.LoadFromJson<BIDSExportConfiguration>(path);
            }
            catch
            {
                return GetDefaultConfiguration();
            }
        }
        
        /// <summary>
        /// Save a BIDS export configuration to a JSON file.
        /// </summary>
        /// <param name="config">Configuration to save</param>
        /// <param name="path">Path where to save the JSON file</param>
        public static void SaveConfiguration(BIDSExportConfiguration config, string path)
        {
            ClassLoaderSaver.SaveToJSon(config, path, true);
        }
        
        /// <summary>
        /// Get the default BIDS export configuration that matches the previous BIDSParameters behavior.
        /// </summary>
        /// <returns>Default configuration</returns>
        public static BIDSExportConfiguration GetDefaultConfiguration()
        {
            return new BIDSExportConfiguration
            {
                Version = "1.0",
                AnatomicalRules = new List<AnatomicalDataRule>
                {
                    // Pre-implantation session
                    new() { 
                        DataType = "MRI", 
                        SourceName = "Preimplantation", 
                        BIDSSuffix = "T1w", 
                        BIDSSession = "pre" 
                    },
                    new() { 
                        DataType = "Mesh", 
                        SourceName = "Grey matter", 
                        BIDSSuffix = "pial", 
                        BIDSSession = "pre" 
                    },
                    new() { 
                        DataType = "Mesh", 
                        SourceName = "White matter", 
                        BIDSSuffix = "white", 
                        BIDSSession = "pre" 
                    },
                    
                    // Post-implantation session
                    new() { 
                        DataType = "MRI", 
                        SourceName = "Postimplantation", 
                        BIDSSuffix = "T1w", 
                        BIDSSession = "post" 
                    },
                    new() { 
                        DataType = "MRI", 
                        SourceName = "CT", 
                        BIDSSuffix = "CT", 
                        BIDSSession = "post" 
                    }
                },
                CoordinateSystemRules = new List<CoordinateSystemRule>
                {
                    new() { 
                        CoordinateSystemName = "Patient", 
                        BIDSSpace = "" 
                    },
                    new() { 
                        CoordinateSystemName = "MNI", 
                        BIDSSpace = "MNI152Lin" 
                    }
                }
            };
        }
    }
}
