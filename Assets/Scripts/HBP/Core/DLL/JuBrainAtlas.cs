using HBP.Core.Tools;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Core.DLL
{
    /// <summary>
    /// Class containing information about the different areas of the JuBrain Cytoarchitectonic Atlas
    /// </summary>
    public class JuBrainAtlas : BrainAtlas
    {
        #region Properties
        private static string LeftNIIPath => Path.Combine(ApplicationState.DataPath, "Atlases", "JuBrain", "JulichBrainAtlas_3.1_207areas_MPM_lh_Colin27.nii.gz");
        private static string RightNIIPath => Path.Combine(ApplicationState.DataPath, "Atlases", "JuBrain", "JulichBrainAtlas_3.1_207areas_MPM_rh_Colin27.nii.gz");
        private static string JsonPath => Path.Combine(ApplicationState.DataPath, "Atlases", "JuBrain", "jubrain_labels_3.1.json");
        #endregion

        #region Constructors
        public JuBrainAtlas() : base() { }
        #endregion

        #region Public Methods
        /// <summary>
        /// Load the JuBrain atlas DLL object
        /// </summary>
        /// <param name="leftNIIPath">Path of the NIFTI file for the left side of the atlas</param>
        /// <param name="rightNIIPath">Path of the NIFTI file for the right side of the atlas</param>
        /// <param name="jsonPath">Path to the json containing information about the areas of the atlas</param>
        /// <returns></returns>
        public override void Load()
        {
            GetAreaNames();
            Loading = true;
            Loaded = load_JuBrainAtlas(_handle, LeftNIIPath, RightNIIPath, JsonPath) == 1;
            Loading = false;
        }
        public override string GetAreaName(int index)
        {
            string[] areaInformation = GetInformation(index);
            if (areaInformation.Length == 1)
                return areaInformation[0];
            return string.Empty;
        }
        #endregion

        #region Private Methods
        protected override void GetAreaNames()
        {
            m_AreaNames = new List<string>();
            var names = new List<string>();

            if (!File.Exists(JsonPath)) return;

            string json = File.ReadAllText(JsonPath);
            JObject root = JObject.Parse(json);

            var structures = root["JulichBrainAtlas"]?["Structures"]?["Structure"];
            if (structures != null)
            {
                foreach (var structure in structures)
                {
                    var name = structure["name"]?.ToString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        names.Add(name);
                    }
                }
            }

            m_AreaNames.AddRange(names.OrderBy(n => n).Distinct());
        }
        #endregion

        #region Memory Management
        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            GetAreaNames();
            _handle = new HandleRef(this, create_JuBrainAtlas());
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            delete_JuBrainAtlas(_handle);
        }
        #endregion

        #region DLLImport
        [DllImport("hbp_export", EntryPoint = "create_JuBrainAtlas", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_JuBrainAtlas();
        [DllImport("hbp_export", EntryPoint = "delete_JuBrainAtlas", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_JuBrainAtlas(HandleRef juBrainAtlas);
        [DllImport("hbp_export", EntryPoint = "load_JuBrainAtlas", CallingConvention = CallingConvention.Cdecl)]
        static private extern int load_JuBrainAtlas(HandleRef juBrainAtlas, string leftNIIPath, string rightNIIPath, string jsonPath);
        #endregion
    }
}
