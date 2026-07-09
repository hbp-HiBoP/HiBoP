using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;
using HBP.Core.DLL.HbpCore;
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
        protected NativeBackend m_Backend = NativeBackendOptions.ExperimentalBackend;
        internal NativeBackend Backend => m_Backend;
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
            if (m_Backend == NativeBackend.HbpCore)
            {
                Vec3 nativePosition = Vec3.FromVector3(position);
                ThrowIfFailed(hbp_brain_atlas_get_closest_area_index(_handle.Handle, ref nativePosition, radius, out int label));
                return label;
            }

            return get_closest_area_index_BrainAtlas(_handle, -position.x, position.y, position.z, radius);
        }
        /// <summary>
        /// Get information about an area given its index
        /// </summary>
        /// <param name="labelIndex">Index of the label</param>
        /// <returns>Array of strings containing information (name, location, arealabel, status, doi) about an area</returns>
        public string[] GetInformation(int labelIndex)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                if (TryGetCachedInformation(labelIndex, out string[] information))
                {
                    return information;
                }
                return CopyHbpCoreAreaInformation(labelIndex).Split(new char[1] { '?' }, StringSplitOptions.None);
            }

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
            if (!Loaded) return result;

            if (m_Backend == NativeBackend.HbpCore)
            {
                EnsureSameBackend(surface, nameof(GetSurfaceAreaLabels));
                ThrowIfFailed(hbp_brain_atlas_copy_surface_area_labels(_handle.Handle, surface.getHandle().Handle, result, result.Length));
                return result;
            }

            get_vertices_area_index_BrainAtlas(_handle, surface.getHandle(), result);
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
            if (m_Backend == NativeBackend.HbpCore)
            {
                if (TryConvertCachedIndicesToColors(indices, selectedArea, colors))
                {
                    return colors;
                }

                Color4[] nativeColors = new Color4[indices.Length];
                ThrowIfFailed(hbp_brain_atlas_copy_colors_from_indices(_handle.Handle, indices, indices.Length, selectedArea, nativeColors, nativeColors.Length));
                for (int i = 0; i < colors.Length; ++i)
                {
                    colors[i] = nativeColors[i].ToColor();
                }
                return colors;
            }

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
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_brain_atlas_get_region_coordinate_count(_handle.Handle, labelIndex, out int coordinateCount));
                Vec3[] nativeCoordinates = new Vec3[coordinateCount];
                if (coordinateCount > 0)
                {
                    ThrowIfFailed(hbp_brain_atlas_copy_region_coordinates(_handle.Handle, labelIndex, nativeCoordinates, nativeCoordinates.Length));
                }

                Vector3[] hbpCoreResult = new Vector3[coordinateCount];
                for (int i = 0; i < coordinateCount; ++i)
                {
                    hbpCoreResult[i] = nativeCoordinates[i].ToVector3();
                }
                return hbpCoreResult;
            }

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

        protected virtual bool TryGetCachedInformation(int labelIndex, out string[] information)
        {
            information = null;
            return false;
        }

        protected virtual bool TryConvertCachedIndicesToColors(int[] indices, int selectedArea, Color[] colors)
        {
            return false;
        }

        private string CopyHbpCoreAreaInformation(int labelIndex)
        {
            int capacity = 256;
            while (capacity <= 4096)
            {
                StringBuilder builder = new(capacity);
                HbpCoreStatus status = hbp_brain_atlas_copy_area_information(_handle.Handle, labelIndex, builder, capacity);
                if (status == HbpCoreStatus.Ok)
                {
                    return builder.ToString();
                }
                if (status != HbpCoreStatus.BufferTooSmall)
                {
                    ThrowIfFailed(status);
                }
                capacity *= 2;
            }

            throw new InvalidOperationException("hbp_core BrainAtlas area information is too large.");
        }

        private void EnsureSameBackend(Surface surface, string methodName)
        {
            if (surface.Backend != m_Backend)
            {
                throw new InvalidOperationException($"BrainAtlas.{methodName} cannot mix {m_Backend} atlas with {surface.Backend} surface.");
            }
        }

        protected static void ThrowIfFailed(HbpCoreStatus status)
        {
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core BrainAtlas call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }
        #endregion

        #region DLLImport
        [DllImport(NativeDll.HbpExport, EntryPoint = "get_closest_area_index_BrainAtlas", CallingConvention = CallingConvention.Cdecl)]
        static private extern int get_closest_area_index_BrainAtlas(HandleRef atlas, float x, float y, float z, int radius);
        [DllImport(NativeDll.HbpExport, EntryPoint = "get_area_information_BrainAtlas", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr get_area_information_BrainAtlas(HandleRef atlas, int labelIndex);
        [DllImport(NativeDll.HbpExport, EntryPoint = "get_vertices_area_index_BrainAtlas", CallingConvention = CallingConvention.Cdecl)]
        static private extern void get_vertices_area_index_BrainAtlas(HandleRef atlas, HandleRef surfaceHandle, int[] indices);
        [DllImport(NativeDll.HbpExport, EntryPoint = "get_colors_from_indices_BrainAtlas", CallingConvention = CallingConvention.Cdecl)]
        static private extern void get_colors_from_indices_BrainAtlas(HandleRef atlas, int[] indices, int size, int selectedArea, float[] colors);
        [DllImport(NativeDll.HbpExport, EntryPoint = "get_region_coordinates_BrainAtlas", CallingConvention = CallingConvention.Cdecl)]
        private static extern int get_region_coordinates_BrainAtlas(HandleRef atlas, int labelIndex, float[] coordinates, int maxCoordinates);

        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_brain_atlas_get_closest_area_index", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_brain_atlas_get_closest_area_index(IntPtr atlas, ref Vec3 position, int radius, out int label);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_brain_atlas_copy_area_information", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_brain_atlas_copy_area_information(IntPtr atlas, int label, StringBuilder text, int textCapacity);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_brain_atlas_copy_surface_area_labels", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_brain_atlas_copy_surface_area_labels(IntPtr atlas, IntPtr surface, [Out] int[] labels, int labelCapacity);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_brain_atlas_copy_colors_from_indices", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_brain_atlas_copy_colors_from_indices(IntPtr atlas, [In] int[] indices, int indexCount, int selectedArea, [Out] Color4[] colors, int colorCapacity);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_brain_atlas_get_region_coordinate_count", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_brain_atlas_get_region_coordinate_count(IntPtr atlas, int label, out int count);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_brain_atlas_copy_region_coordinates", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_brain_atlas_copy_region_coordinates(IntPtr atlas, int label, [Out] Vec3[] coordinates, int coordinateCapacity);
        #endregion
    }
}
