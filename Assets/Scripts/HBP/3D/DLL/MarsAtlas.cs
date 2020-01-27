using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Module3D.DLL
{
    /// <summary>
    /// Mars atlas index, used to identify sites mars IDs and areas on the brain
    /// </summary>
    public class MarsAtlas : BrainAtlas
    {
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
        /// Return all the labels of the mars atlas file
        /// </summary>
        /// <returns>Array of all labels</returns>
        public int[] Labels()
        {
            int[] labels = new int[get_label_count_MarsAtlasIndex(_handle)];
            get_all_labels_MarsAtlasIndex(_handle, labels);
            return labels;
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
        /// Generate a sites list for group CCEP depending on the MarsAtlas
        /// </summary>
        /// <param name="dimension">Maximum dimension of one direction</param>
        public RawSiteList GenerateAtlasRawSiteList(int dimension)
        {
            return new RawSiteList(generate_atlas_sites_list_MarsAtlasIndex(_handle, dimension));
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
            Loaded = load_MarsAtlasIndex(_handle, path, pathBrodmann, pathNifti) == 1;
            apply_offset_MarsAtlasIndex(_handle, 1.7f, 0f, 1f);
            return Loaded;
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
        [DllImport("hbp_export", EntryPoint = "get_label_count_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern int get_label_count_MarsAtlasIndex(HandleRef marsAtlasIndex);
        [DllImport("hbp_export", EntryPoint = "get_all_labels_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern void get_all_labels_MarsAtlasIndex(HandleRef marsAtlasIndex, int[] labels);
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
        [DllImport("hbp_export", EntryPoint = "generate_atlas_sites_list_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr generate_atlas_sites_list_MarsAtlasIndex(HandleRef marsAtlasIndex, int dimension);
        [DllImport("hbp_export", EntryPoint = "apply_offset_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr apply_offset_MarsAtlasIndex(HandleRef marsAtlasIndex, float x, float y, float z);
        #endregion

    }
}