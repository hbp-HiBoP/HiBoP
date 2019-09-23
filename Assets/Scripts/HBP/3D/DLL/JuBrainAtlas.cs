using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Module3D.DLL
{
    public class JuBrainAtlas : Tools.DLL.CppDLLImportBase
    {
        #region Properties
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
        public bool LoadJuBrainAtlas(string leftNIIPath, string rightNIIPath, string jsonPath)
        {
            return Loaded = load_JuBrainAtlas(_handle, leftNIIPath, rightNIIPath, jsonPath) == 1;
        }
        public int GetClosestAreaIndex(Vector3 position)
        {
            return get_closest_area_index_JuBrainAtlas(_handle, -position.x, position.y, position.z);
        }
        public string[] GetInformation(int labelIndex)
        {
            string result = Marshal.PtrToStringAnsi(get_area_information_JuBrainAtlas(_handle, labelIndex));
            return result.Split(new char[1] { '?' }, StringSplitOptions.None);
        }
        public int[] GetSurfaceAreaLabels(Surface surface)
        {
            int[] result = new int[surface.NumberOfVertices];
            get_vertices_area_index_JuBrainAtlas(_handle, surface.getHandle(), result);
            return result;
        }
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
        protected override void create_DLL_class()
        {
            _handle = new HandleRef(this, create_JuBrainAtlas());
        }
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
