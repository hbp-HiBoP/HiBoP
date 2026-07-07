using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HBP.Core.DLL.HbpCore;
using UnityEngine;
using HBP.Core.Enums;

namespace HBP.Core.DLL
{
    /// <summary>
    /// Class representing a volumr loaded from a NIFTI file
    /// </summary>
    public class Volume : CppDLLImportBase
    {
        private NativeBackend m_Backend = NativeBackendOptions.ExperimentalBackend;

        #region Properties
        /// <summary>
        /// Is the volume completely loaded ?
        /// </summary>
        public bool IsLoaded { get; private set; }
        public bool UsesHbpCore => m_Backend == NativeBackend.HbpCore;
        /// <summary>
        /// Center point of the volume
        /// </summary>
        public Vector3 Center
        {
            get
            {
                if (m_Backend == NativeBackend.HbpCore)
                {
                    ThrowIfFailed(hbp_volume_get_center(_handle.Handle, out Vec3 value));
                    return value.ToVector3();
                }

                float[] center = new float[3];
                center_Volume(_handle, center);
                return new Vector3(center[0], center[1], center[2]);
            }
        }
        /// <summary>
        /// Space between two voxels in x, y and z directions
        /// </summary>
        public Vector3 Spacing
        {
            get
            {
                if (m_Backend == NativeBackend.HbpCore)
                {
                    ThrowIfFailed(hbp_volume_get_spacing(_handle.Handle, out Vec3 value));
                    return value.ToVector3();
                }

                float[] spacing = new float[3];
                spacing_Volume(_handle, spacing);
                return new Vector3(spacing[0], spacing[1], spacing[2]);
            }
        }
        /// <summary>
        /// Get the calibration values of the loaded MRI
        /// </summary>
        public Tools.MRICalValues ExtremeValues
        {
            get
            {
                if (m_Backend == NativeBackend.HbpCore)
                {
                    ThrowIfFailed(hbp_volume_get_extrema(_handle.Handle, out VolumeExtrema nativeValues));
                    return ToMRICalValues(nativeValues);
                }

                Tools.MRICalValues values = new();

                float[] valuesF = new float[6];
                retrieveExtremeValues_Volume(_handle, valuesF);

                values.Min = valuesF[0];
                values.Max = valuesF[1];
                values.LoadedCalMin = valuesF[2];
                values.LoadedCalMax = valuesF[3];
                values.ComputedCalMin = valuesF[4];
                values.ComputedCalMax = valuesF[5];

                return values;
            }
        }
        /// <summary>
        /// Bounding box of this volume
        /// </summary>
        public BBox BoundingBox
        {
            get
            {
                if (m_Backend == NativeBackend.HbpCore)
                {
                    ThrowIfFailed(hbp_volume_get_bounding_box(_handle.Handle, out IntPtr bbox));
                    return new BBox(bbox, NativeBackend.HbpCore);
                }

                return new BBox(boundingBox_Volume(_handle));
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Load a NIFTI file to a DLL Volume
        /// </summary>
        /// <param name="path">Path to the NIFTI file</param>
        /// <returns></returns>
        public bool LoadNIFTIFile(string path)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                IsLoaded = hbp_volume_load_nifti(_handle.Handle, path) == HbpCoreStatus.Ok;
                return IsLoaded;
            }

            IsLoaded = (loadNiiFile_Volume(_handle, path) == 1);
            return IsLoaded;
        }
        /// <summary>
        /// Get the offset value for a cut plane given the number of cuts
        /// </summary>
        /// <param name="cutPlane">Cut plane to compute the offset for</param>
        /// <param name="nbCuts">Number of desired cuts</param>
        /// <returns>Value of the offset</returns>
        public float SizeOffsetCutPlane(Plane cutPlane, int nbCuts)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_volume_size_offset_cut_plane(_handle.Handle, cutPlane.getHandle().Handle, nbCuts, out float offset));
                return offset;
            }

            return sizeOffsetCutPlane_Volume(_handle, cutPlane.ConvertToArray(), nbCuts);
        }
        /// <summary>
        /// Get information for a plane depending on the volume and on the input orientation
        /// </summary>
        /// <param name="plane">Plane to update</param>
        /// <param name="orientation">Orientation of the cut</param>
        /// <param name="flip">Is the cut flipped ?</param>
        public void SetPlaneWithOrientation(Plane plane, CutOrientation orientation, bool flip)
        {
            plane.Normal = GetOrientationVector(orientation, flip);
        }
        public Vector3 GetOrientationVector(CutOrientation orientation, bool flip)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_volume_get_orientation_vector(_handle.Handle, (int)orientation, flip ? 1 : 0, out Vec3 value));
                return value.ToVector3();
            }

            float[] normal = new float[3];
            definePlaneWithOrientation_Volume(_handle, normal, (int)orientation, flip);
            return new Vector3(normal[0], normal[1], normal[2]);
        }
        /// <summary>
        /// Returns a cube bbox around the volume depending on the used cuts
        /// </summary>
        /// <param name="cuts">List of the cuts of the scene</param>
        /// <returns>The cube bounding box around the volume</returns>
        public BBox GetCubeBoundingBox(List<Object3D.Cut> cuts)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                throw new NotSupportedException("hbp_core does not expose Volume.GetCubeBoundingBox in step 6.");
            }

            float[] planes = new float[cuts.Count * 6];
            int planesCount = 0;
            for (int ii = 0; ii < cuts.Count; ++ii)
            {
                if (cuts[ii].Orientation != CutOrientation.Custom)
                {
                    for (int jj = 0; jj < 3; ++jj)
                    {
                        planes[ii * 6 + jj] = cuts[ii].Point[jj];
                        planes[ii * 6 + jj + 3] = cuts[ii].Normal[jj];
                    }
                    planesCount++;
                }
            }
            return new BBox(cube_bounding_box_Volume(_handle, planes, planesCount));
        }
        /// <summary>
        /// Get values of the closest voxel of the Volume for each vertex of the input surface
        /// </summary>
        /// <param name="surface"></param>
        /// <returns></returns>
        public float[] GetVerticesValues(Surface surface)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                throw new NotSupportedException("hbp_core does not expose Volume.GetVerticesValues in step 6.");
            }

            float[] result = new float[surface.NumberOfVertices];
            get_vertices_values_Volume(_handle, surface.getHandle(), result);
            return result;
        }
        public Color[] ConvertValuesToColors(float[] values, float negativeMin, float negativeMax, float positiveMin, float positiveMax, float alpha)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                throw new NotSupportedException("hbp_core does not expose Volume.ConvertValuesToColors in step 6.");
            }

            Color[] colors = new Color[values.Length];
            float[] result = new float[values.Length * 4];
            get_colors_from_values_Volume(_handle, values, values.Length, negativeMin, negativeMax, positiveMin, positiveMax, alpha, result);
            for (int i = 0; i < colors.Length; ++i)
            {
                colors[i] = new Color(result[4 * i], result[4 * i + 1], result[4 * i + 2], result[4 * i + 3]);
            }
            return colors;
        }
        public Color[] ConvertValuesToColors(float[] values, int[] mask, float min, float middle, float max, Texture texture)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                throw new NotSupportedException("hbp_core does not expose Volume.ConvertValuesToColors with Texture in step 6.");
            }

            Color[] colors = new Color[values.Length];
            float[] result = new float[values.Length * 4];
            get_colors_from_values_texture_Volume(_handle, values, mask, values.Length, min, middle, max, texture.getHandle(), result);
            for (int i = 0; i < colors.Length; ++i)
            {
                colors[i] = new Color(result[4 * i], result[4 * i + 1], result[4 * i + 2], result[4 * i + 3]);
            }
            return colors;
        }
        public float GetValueFromPosition(Vector3 position)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                Vec3 nativePosition = Vec3.FromVector3(new Vector3(-position.x, position.y, position.z));
                ThrowIfFailed(hbp_volume_sample_value(_handle.Handle, ref nativePosition, out float value));
                return value;
            }

            return get_value_from_position_Volume(_handle, -position.x, position.y, position.z);
        }
        public float GetAverageValueAroundPositionWithMask(Vector3 position, int precision, Volume maskVolume, ref float[] rawValues, ref int actualLength)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                throw new NotSupportedException("hbp_core does not expose Volume.GetAverageValueAroundPositionWithMask in step 6.");
            }

            return get_average_value_around_position_with_mask_Volume(_handle, -position.x, position.y, position.z, precision, maskVolume.getHandle(), rawValues, rawValues.Length, ref actualLength);
        }
        public int[] GetHistogramBins(int binCount, float min = 0.0f, float max = 0.0f)
        {
            if (binCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(binCount));
            }
            if (m_Backend != NativeBackend.HbpCore)
            {
                return null;
            }

            int[] bins = new int[binCount];
            ThrowIfFailed(hbp_volume_copy_histogram_bins(_handle.Handle, bins, bins.Length, min, max));
            return bins;
        }
        #endregion

        #region Memory Management
        internal NativeBackend Backend => m_Backend;

        public Volume()
        {
        }

        internal Volume(IntPtr volumePointer, NativeBackend backend) : base(volumePointer)
        {
            m_Backend = backend;
        }

        internal static Volume CreateHbpCore()
        {
            ThrowIfFailed(hbp_volume_create(out IntPtr volume));
            return new Volume(volume, NativeBackend.HbpCore);
        }

        internal void MarkLoaded()
        {
            IsLoaded = true;
        }

        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_volume_create(out IntPtr volume));
                _handle = new HandleRef(this, volume);
                return;
            }

            _handle = new HandleRef(this, create_Volume());
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_volume_destroy(_handle.Handle));
                return;
            }

            delete_Volume(_handle);
        }
        #endregion

        private static Tools.MRICalValues ToMRICalValues(VolumeExtrema nativeValues)
        {
            return new Tools.MRICalValues
            {
                Min = nativeValues.min,
                Max = nativeValues.max,
                LoadedCalMin = nativeValues.loadedCalMin,
                LoadedCalMax = nativeValues.loadedCalMax,
                ComputedCalMin = nativeValues.recomputedCalMin,
                ComputedCalMax = nativeValues.recomputedCalMax
            };
        }

        private static void ThrowIfFailed(HbpCoreStatus status)
        {
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core Volume call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }

        #region DLLimport
        [DllImport("hbp_export", EntryPoint = "create_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_Volume();
        [DllImport("hbp_export", EntryPoint = "delete_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_Volume(HandleRef handleVolume);
        [DllImport("hbp_export", EntryPoint = "center_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern void center_Volume(HandleRef handleVolume, float[] center);
        [DllImport("hbp_export", EntryPoint = "bBox_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern void bBox_Volume(HandleRef handleVolume, float[] minMax);
        [DllImport("hbp_export", EntryPoint = "diagonalLenght_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern float diagonalLenght_Volume(HandleRef handleVolume);
        [DllImport("hbp_export", EntryPoint = "boundingBox_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr boundingBox_Volume(HandleRef handleVolume);
        [DllImport("hbp_export", EntryPoint = "spacing_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern void spacing_Volume(HandleRef handleVolume, float[] spacing);
        [DllImport("hbp_export", EntryPoint = "definePlaneWithOrientation_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern void definePlaneWithOrientation_Volume(HandleRef handleVolume, float[] planeNormal, int idOrientation, bool flip);
        [DllImport("hbp_export", EntryPoint = "sizeOffsetCutPlane_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern float sizeOffsetCutPlane_Volume(HandleRef handleVolume, float[] planeCut, int nbCuts);
        [DllImport("hbp_export", EntryPoint = "retrieveExtremeValues_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern void retrieveExtremeValues_Volume(HandleRef handleVolume, float[] extremeValues);
        [DllImport("hbp_export", EntryPoint = "cube_bounding_box_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr cube_bounding_box_Volume(HandleRef handleSurface, float[] planes, int planesCount);
        [DllImport("hbp_export", EntryPoint = "loadNiiFile_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern int loadNiiFile_Volume(HandleRef handleNii, string pathFile);
        [DllImport("hbp_export", EntryPoint = "get_vertices_values_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern void get_vertices_values_Volume(HandleRef handleVolume, HandleRef surfaceHandle, float[] result);
        [DllImport("hbp_export", EntryPoint = "get_colors_from_values_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern void get_colors_from_values_Volume(HandleRef handleVolume, float[] values, int valuesLength, float negativeMin, float negativeMax, float positiveMin, float positiveMax, float alpha, float[] result);
        [DllImport("hbp_export", EntryPoint = "get_colors_from_values_texture_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern void get_colors_from_values_texture_Volume(HandleRef handleVolume, float[] values, int[] mask, int valuesLength, float min, float middle, float max, HandleRef textureHandle, float[] result);
        [DllImport("hbp_export", EntryPoint = "get_value_from_position_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern float get_value_from_position_Volume(HandleRef handleVolume, float x, float y, float z);
        [DllImport("hbp_export", EntryPoint = "get_average_value_around_position_with_mask_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern float get_average_value_around_position_with_mask_Volume(HandleRef handleVolume, float x, float y, float z, int precision, HandleRef maskVolume, float[] rawValues, int length, ref int actualLength);

        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_volume_create", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_create(out IntPtr volume);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_volume_destroy", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_destroy(IntPtr volume);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_volume_load_nifti", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_load_nifti(IntPtr volume, [MarshalAs(UnmanagedType.LPUTF8Str)] string path);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_volume_get_center", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_get_center(IntPtr volume, out Vec3 center);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_volume_get_spacing", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_get_spacing(IntPtr volume, out Vec3 spacing);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_volume_get_extrema", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_get_extrema(IntPtr volume, out VolumeExtrema extrema);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_volume_get_bounding_box", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_get_bounding_box(IntPtr volume, out IntPtr bbox);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_volume_get_orientation_vector", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_get_orientation_vector(IntPtr volume, int cutOrientation, int flip, out Vec3 normal);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_volume_sample_value", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_sample_value(IntPtr volume, ref Vec3 position, out float value);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_volume_size_offset_cut_plane", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_size_offset_cut_plane(IntPtr volume, IntPtr plane, int cutCount, out float offset);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_volume_copy_histogram_bins", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_copy_histogram_bins(IntPtr volume, int[] bins, int binCount, float minValue, float maxValue);
        #endregion
    }

    public class MultiVolume : CppDLLImportBase
    {
        #region Public Methods
        public void AddVolume(Volume volume)
        {
            add_volume_MultiVolume(_handle, volume.getHandle());
        }
        #endregion

        #region Memory Management
        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            _handle = new HandleRef(this, create_MultiVolume());
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            delete_MultiVolume(_handle);
        }
        #endregion

        #region DLLimport
        [DllImport("hbp_export", EntryPoint = "create_MultiVolume", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_MultiVolume();
        [DllImport("hbp_export", EntryPoint = "delete_MultiVolume", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_MultiVolume(HandleRef handleMultiVolume);
        [DllImport("hbp_export", EntryPoint = "add_volume_MultiVolume", CallingConvention = CallingConvention.Cdecl)]
        static private extern void add_volume_MultiVolume(HandleRef handleMultiVolume, HandleRef handleVolume);

        #endregion
    }
}
