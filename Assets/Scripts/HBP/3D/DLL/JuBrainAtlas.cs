using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Module3D.DLL
{
    /// <summary>
    /// Class containing information about the different areas of the JuBrain Cytoarchitectonic Atlas
    /// </summary>
    public class JuBrainAtlas : BrainAtlas
    {
        #region Constructors
        public JuBrainAtlas() : base() { }
        #endregion

        #region Public Methods
        public void Load()
        {
            string leftNIIPath = Path.Combine(ApplicationState.DataPath, "Atlases", "JuBrain", "jubrain_left_nlin2Stdcolin27.nii.gz");
            string rightNIIPath = Path.Combine(ApplicationState.DataPath, "Atlases", "JuBrain", "jubrain_right_nlin2Stdcolin27.nii.gz");
            string jsonPath = Path.Combine(ApplicationState.DataPath, "Atlases", "JuBrain", "jubrain.json");
            Load(leftNIIPath, rightNIIPath, jsonPath);
        }
        /// <summary>
        /// Load the JuBrain atlas DLL object
        /// </summary>
        /// <param name="leftNIIPath">Path of the NIFTI file for the left side of the atlas</param>
        /// <param name="rightNIIPath">Path of the NIFTI file for the right side of the atlas</param>
        /// <param name="jsonPath">Path to the json containing information about the areas of the atlas</param>
        /// <returns></returns>
        public bool Load(string leftNIIPath, string rightNIIPath, string jsonPath)
        {
            Loading = true;
            Loaded = load_JuBrainAtlas(_handle, leftNIIPath, rightNIIPath, jsonPath) == 1;
            Loading = false;
            return Loaded;
        }
        #endregion

        #region Memory Management
        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
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
