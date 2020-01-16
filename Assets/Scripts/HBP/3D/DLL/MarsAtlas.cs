using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Module3D.DLL
{
    /// <summary>
    /// Mars atlas index, used to identify sites mars IDs and areas on the brain
    /// </summary>
    public class MarsAtlas : Tools.DLL.CppDLLImportBase
    {
        #region Properties
        /// <summary>
        /// Is the MarsAtlas loaded ?
        /// </summary>
        public bool Loaded { get; private set; }
        #endregion

        #region Constructors
        public MarsAtlas(string path, string pathBrodmann, string pathNifti) : base()
        {
            if (!Load(path, pathBrodmann, pathNifti))
            {
                Debug.LogError("Can't load mars atlas index.");
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Get the label of the MarsAtlas region given its shortened name
        /// </summary>
        /// <param name="name">Shortened name of the MarsAtlas region</param>
        /// <returns></returns>
        public int Label(string name)
        {
            return get_label_MarsAtlasIndex(_handle, name);
        }
        /// <summary>
        /// Return the name of the hemisphere given a mars atlas label ID
        /// </summary>
        /// <param name="id">ID of mars atlas label</param>
        /// <returns>Name of the hemipshere</returns>
        public string Hemisphere(int id)
        {
            if (id < 0) return "not found";

            IntPtr result = hemisphere_MarsAtlasIndex(_handle, id);
            return Marshal.PtrToStringAnsi(result);
        }
        /// <summary>
        /// Return the name of the lobe given a mars atlas label ID
        /// </summary>
        /// <param name="id">ID of mars atlas label</param>
        /// <returns>Name of the lobe</returns>
        public string Lobe(int label)
        {
            if (label < 0) return "not found";

            IntPtr result = lobe_MarsAtlasIndex(_handle, label);
            return Marshal.PtrToStringAnsi(result);
        }
        /// <summary>
        /// Return the name of the name fs given a mars atlas label ID
        /// </summary>
        /// <param name="id">ID of mars atlas label</param>
        /// <returns>Name of the name fs</returns>
        public string NameFS(int label)
        {
            if (label < 0) return "not found";

            IntPtr result = nameFS_MarsAtlasIndex(_handle, label);
            return Marshal.PtrToStringAnsi(result);
        }
        /// <summary>
        /// Return the name of a mars atlas area given a mars atlas label ID
        /// </summary>
        /// <param name="id">ID of mars atlas label</param>
        /// <returns>Name of the mars atlas area</returns>
        public string Name(int label)
        {
            if (label < 0) return "not found";

            IntPtr result = name_MarsAtlasIndex(_handle, label);
            return Marshal.PtrToStringAnsi(result);
        }
        /// <summary>
        /// Return the full name of a mars atlas area given a mars atlas label ID
        /// </summary>
        /// <param name="id">ID of mars atlas label</param>
        /// <returns>Full name of the mars atlas area</returns>
        public string FullName(int label)
        {
            if (label < 0) return "not found";

            IntPtr result = fullName_MarsAtlasIndex(_handle, label);
            return Marshal.PtrToStringAnsi(result);
        }
        /// <summary>
        /// Return the name of the brodmann area given a mars atlas label ID
        /// </summary>
        /// <param name="id">ID of mars atlas label</param>
        /// <returns>Name of the brodmann area</returns>
        public string BrodmannArea(int label)
        {
            if (label < 0) return "not found";

            IntPtr result = BA_MarsAtlasIndex(_handle, label);
            return Marshal.PtrToStringAnsi(result);
        }
        /// <summary>
        /// Get the index of the area closest to a position
        /// </summary>
        /// <param name="position">Position to consider</param>
        /// <returns>Index of the closest area</returns>
        public int GetClosestAreaIndex(Vector3 position)
        {
            return get_closest_area_index_MarsAtlasIndex(_handle, -position.x, position.y, position.z);
        }
        /// <summary>
        /// Get information about an area given its index
        /// </summary>
        /// <param name="labelIndex">Index of the label</param>
        /// <returns>Array of strings containing information (name, location, arealabel, status, doi) about an area</returns>
        public string[] GetInformation(int labelIndex)
        {
            string result = Marshal.PtrToStringAnsi(get_area_information_MarsAtlasIndex(_handle, labelIndex));
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
            get_vertices_area_index_MarsAtlasIndex(_handle, surface.getHandle(), result);
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
            get_colors_from_indices_MarsAtlasIndex(_handle, indices, indices.Length, selectedArea, result);
            for (int i = 0; i < colors.Length; ++i)
            {
                colors[i] = new Color(result[4 * i] / 255, result[4 * i + 1] / 255, result[4 * i + 2] / 255, result[4 * i + 3] / 255);
            }
            return colors;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Load the mars atlas
        /// </summary>
        /// <param name="path">Path to mars atlas csv file</param>
        /// <param name="pathBrodmann">Path to brodmann txt file</param>
        /// <param name="pathNifti">Path to mars atlas nifti file</param>
        /// <returns>True if the mars atlas is correctly loaded</returns>
        private bool Load(string path, string pathBrodmann, string pathNifti)
        {
            return Loaded = load_MarsAtlasIndex(_handle, path, pathBrodmann, pathNifti) == 1;
        }
        #endregion

        #region Memory Management
        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            _handle = new HandleRef(this, create_MarsAtlasIndex());
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            delete_MarsAtlasIndex(_handle);
        }
        #endregion

        #region DLLImport
        [DllImport("hbp_export", EntryPoint = "create_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_MarsAtlasIndex();
        [DllImport("hbp_export", EntryPoint = "delete_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_MarsAtlasIndex(HandleRef marsAtlasIndex);
        [DllImport("hbp_export", EntryPoint = "load_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern int load_MarsAtlasIndex(HandleRef marsAtlasIndex, string pathFile, string pathBrodmannFile, string pathNiftiFile);
        [DllImport("hbp_export", EntryPoint = "get_label_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern int get_label_MarsAtlasIndex(HandleRef marsAtlasIndex, string name);
        [DllImport("hbp_export", EntryPoint = "hemisphere_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr hemisphere_MarsAtlasIndex(HandleRef marsAtlasIndex, int label);
        [DllImport("hbp_export", EntryPoint = "lobe_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr lobe_MarsAtlasIndex(HandleRef marsAtlasIndex, int label);
        [DllImport("hbp_export", EntryPoint = "nameFS_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr nameFS_MarsAtlasIndex(HandleRef marsAtlasIndex, int label);
        [DllImport("hbp_export", EntryPoint = "name_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr name_MarsAtlasIndex(HandleRef marsAtlasIndex, int label);
        [DllImport("hbp_export", EntryPoint = "fullName_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr fullName_MarsAtlasIndex(HandleRef marsAtlasIndex, int label);
        [DllImport("hbp_export", EntryPoint = "BA_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr BA_MarsAtlasIndex(HandleRef marsAtlasIndex, int label);
        [DllImport("hbp_export", EntryPoint = "get_closest_area_index_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern int get_closest_area_index_MarsAtlasIndex(HandleRef juBrainAtlas, float x, float y, float z);
        [DllImport("hbp_export", EntryPoint = "get_area_information_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr get_area_information_MarsAtlasIndex(HandleRef juBrainAtlas, int labelIndex);
        [DllImport("hbp_export", EntryPoint = "get_vertices_area_index_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern void get_vertices_area_index_MarsAtlasIndex(HandleRef juBrainAtlas, HandleRef surfaceHandle, int[] indices);
        [DllImport("hbp_export", EntryPoint = "get_colors_from_indices_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern void get_colors_from_indices_MarsAtlasIndex(HandleRef juBrainAtlas, int[] indices, int size, int selectedArea, float[] colors);
        #endregion

    }
}