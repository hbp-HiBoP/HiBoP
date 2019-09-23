using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Module3D.DLL
{
    /// <summary>
    /// Class containing information about the different areas of the JuBrain Cytoarchitectonic Atlas
    /// </summary>
    public class JuBrainAtlas : Tools.DLL.CppDLLImportBase
    {
        #region Properties
        /// <summary>
        /// Is the atlas completely loaded ?
        /// </summary>
        public bool Loaded { get; private set; }
        #endregion

        #region Constructors
        public JuBrainAtlas(string leftNIIPath, string rightNIIPath, string jsonPath) : base()
        {
            if (!LoadJuBrainAtlas(leftNIIPath, rightNIIPath, jsonPath))
            {
                Debug.LogError("Can't load JuBrain Atlas.");
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Load the JuBrain atlas DLL object
        /// </summary>
        /// <param name="leftNIIPath">Path of the NIFTI file for the left side of the atlas</param>
        /// <param name="rightNIIPath">Path of the NIFTI file for the right side of the atlas</param>
        /// <param name="jsonPath">Path to the json containing information about the areas of the atlas</param>
        /// <returns></returns>
        public bool LoadJuBrainAtlas(string leftNIIPath, string rightNIIPath, string jsonPath)
        {
            return Loaded = load_JuBrainAtlas(_handle, leftNIIPath, rightNIIPath, jsonPath) == 1;
        }
        /// <summary>
        /// Get the index of the area closest to a position
        /// </summary>
        /// <param name="position">Position to consider</param>
        /// <returns>Index of the closest area</returns>
        public int GetClosestAreaIndex(Vector3 position)
        {
            return get_closest_area_index_JuBrainAtlas(_handle, -position.x, position.y, position.z);
        }
        /// <summary>
        /// Get information about an area given its index
        /// </summary>
        /// <param name="labelIndex">Index of the label</param>
        /// <returns>Array of strings containing information (name, location, arealabel, status, doi) about an area</returns>
        public string[] GetInformation(int labelIndex)
        {
            string result = Marshal.PtrToStringAnsi(get_area_information_JuBrainAtlas(_handle, labelIndex));
            return result.Split(new char[1] { '?' }, StringSplitOptions.None);
        }
        /// <summary>
        /// Get the labels of the area for each vertex of a surface
        /// </summary>
        /// <param name="surface">Surface to consider</param>
        /// <returns>Array which size is the number of vertices containing the index of the area and -1 if there is no area</returns>
        public int[] GetSurfaceAreaLabels(Surface surface)
        {
            int[] result = new int[surface.NumberOfVertices];
            get_vertices_area_index_JuBrainAtlas(_handle, surface.getHandle(), result);
            return result;
        }
        /// <summary>
        /// Convert an array of indices to an array of color
        /// </summary>
        /// <param name="indices">Array of indices of atlas areas</param>
        /// <param name="selectedArea">Currently selected area (to highlight it)</param>
        /// <returns></returns>
        public Color[] ConvertIndicesToColors(int[] indices, int selectedArea)
        {
            Color[] colors = new Color[indices.Length];
            float[] result = new float[indices.Length * 4];
            get_colors_from_indices_JuBrainAtlas(_handle, indices, indices.Length, selectedArea, result);
            for (int i = 0; i < colors.Length; ++i)
            {
                colors[i] = new Color(result[4 * i] / 255, result[4 * i + 1] / 255, result[4 * i + 2] / 255, result[4 * i + 3] / 255);
            }
            return colors;
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
        [DllImport("hbp_export", EntryPoint = "get_closest_area_index_JuBrainAtlas", CallingConvention = CallingConvention.Cdecl)]
        static private extern int get_closest_area_index_JuBrainAtlas(HandleRef juBrainAtlas, float x, float y, float z);
        [DllImport("hbp_export", EntryPoint = "get_area_information_JuBrainAtlas", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr get_area_information_JuBrainAtlas(HandleRef juBrainAtlas, int labelIndex);
        [DllImport("hbp_export", EntryPoint = "get_vertices_area_index_JuBrainAtlas", CallingConvention = CallingConvention.Cdecl)]
        static private extern void get_vertices_area_index_JuBrainAtlas(HandleRef juBrainAtlas, HandleRef surfaceHandle, int[] indices);
        [DllImport("hbp_export", EntryPoint = "get_colors_from_indices_JuBrainAtlas", CallingConvention = CallingConvention.Cdecl)]
        static private extern void get_colors_from_indices_JuBrainAtlas(HandleRef juBrainAtlas, int[] indices, int size, int selectedArea, float[] colors);
        #endregion
    }
}
