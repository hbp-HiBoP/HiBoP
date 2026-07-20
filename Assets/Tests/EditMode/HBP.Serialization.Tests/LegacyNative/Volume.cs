using HBP.Core.DLL;
using HBP.Tests.Serialization;
using Plane = HBP.Core.DLL.Plane;
using Tools = HBP.Core.Tools;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HBP.Core.DLL.HbpCore;
using UnityEngine;
using HBP.Core.Enums;
using HBP.Core.Tools;

namespace HBP.Tests.Serialization.LegacyNative
{
    /// <summary>
    /// Class representing a volumr loaded from a NIFTI file
    /// </summary>
    public class Volume : CppDLLImportBase
    {
        private BenchmarkBackend m_Backend = OracleBackendContext.Current;

        #region Properties
        /// <summary>
        /// Is the volume completely loaded ?
        /// </summary>
        public bool IsLoaded { get; private set; }
        public bool UsesHbpCore => m_Backend == BenchmarkBackend.HbpCore;
        /// <summary>
        /// Center point of the volume
        /// </summary>
        public Vector3 Center
        {
            get
            {
                if (m_Backend == BenchmarkBackend.HbpCore)
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
                if (m_Backend == BenchmarkBackend.HbpCore)
                {
                    ThrowIfFailed(hbp_volume_get_spacing(_handle.Handle, out Vec3 value));
                    // Spacing is an unsigned magnitude, not an oriented spatial vector.
                    return value.ToVector3(convertReferenceSystem: false);
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
                if (m_Backend == BenchmarkBackend.HbpCore)
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
                if (m_Backend == BenchmarkBackend.HbpCore)
                {
                    ThrowIfFailed(hbp_volume_get_bounding_box(_handle.Handle, out IntPtr bbox));
                    return new BBox(bbox, BenchmarkBackend.HbpCore);
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
            if (m_Backend == BenchmarkBackend.HbpCore)
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
            if (m_Backend == BenchmarkBackend.HbpCore)
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
            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                ThrowIfFailed(hbp_volume_get_orientation_vector(_handle.Handle, (int)orientation, flip ? 1 : 0, out Vec3 value));
                return value.ToVector3();
            }

            float[] normal = new float[3];
            definePlaneWithOrientation_Volume(_handle, normal, (int)orientation, flip);
            return new Vector3(normal[0], normal[1], normal[2]);
        }
        /// <summary>
        /// Get values of the closest voxel of the Volume for each vertex of the input surface
        /// </summary>
        /// <param name="surface"></param>
        /// <returns></returns>
        public float[] GetVerticesValues(Surface surface)
        {
            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                if (surface.Backend != BenchmarkBackend.HbpCore)
                {
                    throw new InvalidOperationException("Volume.GetVerticesValues cannot mix hbp_core Volume with hbp_export Surface.");
                }

                float[] hbpCoreResult = new float[surface.NumberOfVertices];
                GCHandle resultHandle = GCHandle.Alloc(hbpCoreResult, GCHandleType.Pinned);
                try
                {
                    ThrowIfFailed(hbp_volume_copy_surface_values_ptr(_handle.Handle, surface.getHandle().Handle, resultHandle.AddrOfPinnedObject(), hbpCoreResult.Length));
                }
                finally
                {
                    resultHandle.Free();
                }
                return hbpCoreResult;
            }

            float[] result = new float[surface.NumberOfVertices];
            get_vertices_values_Volume(_handle, surface.getHandle(), result);
            return result;
        }
        public Color[] ConvertValuesToColors(float[] values, float negativeMin, float negativeMax, float positiveMin, float positiveMax, float alpha)
        {
            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                Color4[] nativeColors = new Color4[values.Length];
                ThrowIfFailed(hbp_volume_copy_fmri_colors_from_values(_handle.Handle, values, values.Length, negativeMin, negativeMax, positiveMin, positiveMax, alpha, nativeColors, nativeColors.Length));
                Color[] hbpCoreColors = new Color[nativeColors.Length];
                for (int i = 0; i < hbpCoreColors.Length; ++i)
                {
                    hbpCoreColors[i] = nativeColors[i].ToColor();
                }
                return hbpCoreColors;
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
        public Color[] ConvertValuesToColors(float[] values, int[] mask, float min, float middle, float max, Color32[] colorScheme)
        {
            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                Color4[] nativeColorScheme = colorScheme.ToNativeColor4Array();
                Color4[] nativeColors = new Color4[values.Length];
                ThrowIfFailed(hbp_volume_copy_localizer_colors_from_values(_handle.Handle, values, mask, values.Length, min, middle, max, nativeColorScheme, nativeColorScheme.Length, nativeColors, nativeColors.Length));
                Color[] hbpCoreColors = new Color[nativeColors.Length];
                for (int i = 0; i < hbpCoreColors.Length; ++i)
                {
                    hbpCoreColors[i] = nativeColors[i].ToColor();
                }
                return hbpCoreColors;
            }

            throw new NotSupportedException("Localizer color conversion with Color32[] is only available with hbp_core.");
        }
        public float GetValueFromPosition(Vector3 position)
        {
            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                Vec3 nativePosition = Vec3.FromVector3(position);
                ThrowIfFailed(hbp_volume_sample_value(_handle.Handle, ref nativePosition, out float value));
                return value;
            }

            return get_value_from_position_Volume(_handle, -position.x, position.y, position.z);
        }
        public float GetAverageValueAroundPositionWithMask(Vector3 position, int precision, Volume maskVolume, ref float[] rawValues, ref int actualLength)
        {
            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                if (maskVolume != null && maskVolume.Backend != BenchmarkBackend.HbpCore)
                {
                    throw new InvalidOperationException("Volume.GetAverageValueAroundPositionWithMask requires hbp_core mask volumes when the source volume uses hbp_core.");
                }

                Vec3 nativePosition = Vec3.FromVector3(position);
                IntPtr maskHandle = maskVolume == null ? IntPtr.Zero : maskVolume.getHandle().Handle;
                float[] targetRawValues = rawValues ?? Array.Empty<float>();
                ThrowIfFailed(hbp_volume_get_average_value_around_position_with_mask(
                    _handle.Handle,
                    ref nativePosition,
                    precision,
                    maskHandle,
                    out float average,
                    targetRawValues,
                    targetRawValues.Length,
                    out actualLength));
                return average;
            }

            return get_average_value_around_position_with_mask_Volume(_handle, -position.x, position.y, position.z, precision, maskVolume.getHandle(), rawValues, rawValues.Length, ref actualLength);
        }
        public int[] GetHistogramBins(int binCount, float min = 0.0f, float max = 0.0f)
        {
            if (binCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(binCount));
            }
            if (m_Backend != BenchmarkBackend.HbpCore)
            {
                return null;
            }

            int[] bins = new int[binCount];
            ThrowIfFailed(hbp_volume_copy_histogram_bins(_handle.Handle, bins, bins.Length, min, max));
            return bins;
        }
        #endregion

        #region Memory Management
        internal BenchmarkBackend Backend => m_Backend;

        public Volume()
        {
        }

        internal Volume(IntPtr volumePointer, BenchmarkBackend backend) : base(volumePointer)
        {
            m_Backend = backend;
        }

        internal static Volume CreateHbpCore()
        {
            ThrowIfFailed(hbp_volume_create(out IntPtr volume));
            return new Volume(volume, BenchmarkBackend.HbpCore);
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
            if (m_Backend == BenchmarkBackend.HbpCore)
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
            if (m_Backend == BenchmarkBackend.HbpCore)
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
        [DllImport("hbp_export", EntryPoint = "loadNiiFile_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern int loadNiiFile_Volume(HandleRef handleNii, string pathFile);
        [DllImport("hbp_export", EntryPoint = "get_vertices_values_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern void get_vertices_values_Volume(HandleRef handleVolume, HandleRef surfaceHandle, float[] result);
        [DllImport("hbp_export", EntryPoint = "get_colors_from_values_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern void get_colors_from_values_Volume(HandleRef handleVolume, float[] values, int valuesLength, float negativeMin, float negativeMax, float positiveMin, float positiveMax, float alpha, float[] result);
        [DllImport("hbp_export", EntryPoint = "get_value_from_position_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern float get_value_from_position_Volume(HandleRef handleVolume, float x, float y, float z);
        [DllImport("hbp_export", EntryPoint = "get_average_value_around_position_with_mask_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern float get_average_value_around_position_with_mask_Volume(HandleRef handleVolume, float x, float y, float z, int precision, HandleRef maskVolume, float[] rawValues, int length, ref int actualLength);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_volume_create", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_create(out IntPtr volume);
        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_volume_destroy", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_destroy(IntPtr volume);
        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_volume_load_nifti", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_load_nifti(IntPtr volume, [MarshalAs(UnmanagedType.LPUTF8Str)] string path);
        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_volume_get_center", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_get_center(IntPtr volume, out Vec3 center);
        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_volume_get_spacing", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_get_spacing(IntPtr volume, out Vec3 spacing);
        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_volume_get_extrema", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_get_extrema(IntPtr volume, out VolumeExtrema extrema);
        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_volume_get_bounding_box", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_get_bounding_box(IntPtr volume, out IntPtr bbox);
        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_volume_get_orientation_vector", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_get_orientation_vector(IntPtr volume, int cutOrientation, int flip, out Vec3 normal);
        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_volume_sample_value", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_sample_value(IntPtr volume, ref Vec3 position, out float value);
        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_volume_get_average_value_around_position_with_mask", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_get_average_value_around_position_with_mask(IntPtr volume, ref Vec3 position, int precision, IntPtr mask, out float average, [Out] float[] rawValues, int rawValueCapacity, out int actualCount);
        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_volume_size_offset_cut_plane", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_size_offset_cut_plane(IntPtr volume, IntPtr plane, int cutCount, out float offset);
        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_volume_copy_histogram_bins", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_copy_histogram_bins(IntPtr volume, int[] bins, int binCount, float minValue, float maxValue);
        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_volume_copy_surface_values", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_copy_surface_values(IntPtr volume, IntPtr surface, [Out] float[] values, int valueCapacity);
        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_volume_copy_surface_values", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_copy_surface_values_ptr(IntPtr volume, IntPtr surface, IntPtr values, int valueCapacity);
        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_volume_copy_fmri_colors_from_values", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_copy_fmri_colors_from_values(IntPtr volume, [In] float[] values, int valueCount, float negativeMin, float negativeMax, float positiveMin, float positiveMax, float alpha, [Out] Color4[] colors, int colorCapacity);
        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_volume_copy_localizer_colors_from_values", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_volume_copy_localizer_colors_from_values(IntPtr volume, [In] float[] values, [In] int[] mask, int valueCount, float minValue, float middleValue, float maxValue, [In] Color4[] colorScheme, int colorCount, [Out] Color4[] colors, int colorCapacity);
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
