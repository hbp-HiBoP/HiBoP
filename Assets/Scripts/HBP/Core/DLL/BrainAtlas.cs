using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Core.DLL
{
    /// <summary>
    /// Base class for anatomical brain atlases
    /// </summary>
    public abstract class BrainAtlas : CppDLLImportBase
    {
        #region Properties
        /// <summary>
        /// Is the atlas completely loaded ?
        /// </summary>
        public bool Loaded { get; protected set; }
        public bool Loading { get; protected set; }
        protected List<string> m_AreaNames = new();
        public ReadOnlyCollection<string> AreaNames => new(m_AreaNames);
        #endregion

        #region Public Methods
        public abstract void Load();
        /// <summary>
        /// Get the index of the area closest to a position
        /// </summary>
        /// <param name="position">Position to consider</param>
        /// <returns>Index of the closest area</returns>
        public int GetClosestAreaIndex(Vector3 position, int radius)
        {
            return get_closest_area_index_BrainAtlas(_handle, -position.x, position.y, position.z, radius);
        }
        /// <summary>
        /// Get information about an area given its index
        /// </summary>
        /// <param name="labelIndex">Index of the label</param>
        /// <returns>Array of strings containing information (name, location, arealabel, status, doi) about an area</returns>
        public string[] GetInformation(int labelIndex)
        {
            lock (typeof(Marshal))
            {
                string result = Marshal.PtrToStringAnsi(get_area_information_BrainAtlas(_handle, labelIndex));
                return result.Split(new char[1] { '?' }, StringSplitOptions.None);
            }
        }
        /// <summary>
        /// Get the labels of the area for each vertex of a surface
        /// </summary>
        /// <param name="surface">Surface to consider</param>
        /// <returns>Array which size is the number of vertices containing the index of the area and -1 if there is no area</returns>
        public int[] GetSurfaceAreaLabels(Surface surface)
        {
            int[] result = new int[surface.NumberOfVertices];
            if (Loaded) get_vertices_area_index_BrainAtlas(_handle, surface.getHandle(), result);
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
            get_colors_from_indices_BrainAtlas(_handle, indices, indices.Length, selectedArea, result);
            for (int i = 0; i < colors.Length; ++i)
            {
                colors[i] = new Color(result[4 * i] / 255, result[4 * i + 1] / 255, result[4 * i + 2] / 255, result[4 * i + 3] / 255);
            }
            return colors;
        }
        public abstract string GetAreaName(int index);
        public Vector3[] GetAreaCoordinates(int labelIndex)
        {
            int numCoords = get_region_coordinates_BrainAtlas(_handle, labelIndex, null, 0); // Get number of coordinates
            float[] coords = new float[numCoords * 3];
            get_region_coordinates_BrainAtlas(_handle, labelIndex, coords, coords.Length / 3);
            Vector3[] result = new Vector3[numCoords];
            for (int i = 0; i < numCoords; i++)
            {
                result[i] = new Vector3(-coords[3 * i], coords[3 * i + 1], coords[3 * i + 2]);
            }
            return result;
        }
        #endregion

        #region Private Methods
        protected abstract void GetAreaNames();
        #endregion

        #region DLLImport
        [DllImport("hbp_export", EntryPoint = "get_closest_area_index_BrainAtlas", CallingConvention = CallingConvention.Cdecl)]
        static private extern int get_closest_area_index_BrainAtlas(HandleRef atlas, float x, float y, float z, int radius);
        [DllImport("hbp_export", EntryPoint = "get_area_information_BrainAtlas", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr get_area_information_BrainAtlas(HandleRef atlas, int labelIndex);
        [DllImport("hbp_export", EntryPoint = "get_vertices_area_index_BrainAtlas", CallingConvention = CallingConvention.Cdecl)]
        static private extern void get_vertices_area_index_BrainAtlas(HandleRef atlas, HandleRef surfaceHandle, int[] indices);
        [DllImport("hbp_export", EntryPoint = "get_colors_from_indices_BrainAtlas", CallingConvention = CallingConvention.Cdecl)]
        static private extern void get_colors_from_indices_BrainAtlas(HandleRef atlas, int[] indices, int size, int selectedArea, float[] colors);
        [DllImport("hbp_export", EntryPoint = "get_region_coordinates_BrainAtlas", CallingConvention = CallingConvention.Cdecl)]
        private static extern int get_region_coordinates_BrainAtlas(HandleRef atlas, int labelIndex, float[] coordinates, int maxCoordinates);
        #endregion
    }
}