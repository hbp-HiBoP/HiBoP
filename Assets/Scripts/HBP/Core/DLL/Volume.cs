using System;
using System.Runtime.InteropServices;
using HBP.Core.DLL.HbpCore;
using HBP.Core.Enums;
using HBP.Core.Tools;
using UnityEngine;

namespace HBP.Core.DLL
{
    /// <summary>Volume loaded from a NIFTI file.</summary>
    public class Volume : CppDLLImportBase
    {
        public bool IsLoaded { get; private set; }

        public Vector3 Center
        {
            get
            {
                ThrowIfFailed(hbp_volume_get_center(_handle.Handle, out Vec3 value));
                return value.ToVector3();
            }
        }

        public Vector3 Spacing
        {
            get
            {
                ThrowIfFailed(hbp_volume_get_spacing(_handle.Handle, out Vec3 value));
                return value.ToVector3(convertReferenceSystem: false);
            }
        }

        public MRICalValues ExtremeValues
        {
            get
            {
                ThrowIfFailed(hbp_volume_get_extrema(_handle.Handle, out VolumeExtrema values));
                return new MRICalValues
                {
                    Min = values.min,
                    Max = values.max,
                    LoadedCalMin = values.loadedCalMin,
                    LoadedCalMax = values.loadedCalMax,
                    ComputedCalMin = values.recomputedCalMin,
                    ComputedCalMax = values.recomputedCalMax
                };
            }
        }

        public BBox BoundingBox
        {
            get
            {
                ThrowIfFailed(hbp_volume_get_bounding_box(_handle.Handle, out IntPtr bbox));
                return new BBox(bbox);
            }
        }

        public Volume()
        {
        }

        internal Volume(IntPtr volumePointer) : base(volumePointer)
        {
        }

        public bool LoadNIFTIFile(string path)
        {
            IsLoaded = hbp_volume_load_nifti(_handle.Handle, path) == HbpCoreStatus.Ok;
            return IsLoaded;
        }

        public float SizeOffsetCutPlane(Plane cutPlane, int nbCuts)
        {
            if (cutPlane == null) throw new ArgumentNullException(nameof(cutPlane));
            ThrowIfFailed(hbp_volume_size_offset_cut_plane(_handle.Handle, cutPlane.getHandle().Handle, nbCuts, out float offset));
            return offset;
        }

        public void SetPlaneWithOrientation(Plane plane, CutOrientation orientation, bool flip)
        {
            if (plane == null) throw new ArgumentNullException(nameof(plane));
            plane.Normal = GetOrientationVector(orientation, flip);
        }

        public Vector3 GetOrientationVector(CutOrientation orientation, bool flip)
        {
            ThrowIfFailed(hbp_volume_get_orientation_vector(_handle.Handle, (int)orientation, flip ? 1 : 0, out Vec3 value));
            return value.ToVector3();
        }

        public float[] GetVerticesValues(Surface surface)
        {
            if (surface == null) throw new ArgumentNullException(nameof(surface));
            float[] result = new float[surface.NumberOfVertices];
            GCHandle resultHandle = GCHandle.Alloc(result, GCHandleType.Pinned);
            try
            {
                ThrowIfFailed(hbp_volume_copy_surface_values_ptr(_handle.Handle, surface.getHandle().Handle, resultHandle.AddrOfPinnedObject(), result.Length));
            }
            finally
            {
                resultHandle.Free();
            }

            return result;
        }

        public Color[] ConvertValuesToColors(float[] values, float negativeMin, float negativeMax, float positiveMin, float positiveMax, float alpha)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            Color4[] nativeColors = new Color4[values.Length];
            ThrowIfFailed(hbp_volume_copy_fmri_colors_from_values(_handle.Handle, values, values.Length, negativeMin, negativeMax, positiveMin, positiveMax, alpha, nativeColors, nativeColors.Length));
            Color[] colors = new Color[nativeColors.Length];
            for (int i = 0; i < colors.Length; ++i) colors[i] = nativeColors[i].ToColor();
            return colors;
        }

        public Color[] ConvertValuesToColors(float[] values, int[] mask, float min, float middle, float max, Color32[] colorScheme)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (mask == null) throw new ArgumentNullException(nameof(mask));
            if (colorScheme == null) throw new ArgumentNullException(nameof(colorScheme));
            Color4[] nativeColorScheme = colorScheme.ToNativeColor4Array();
            Color4[] nativeColors = new Color4[values.Length];
            ThrowIfFailed(hbp_volume_copy_localizer_colors_from_values(_handle.Handle, values, mask, values.Length, min, middle, max, nativeColorScheme, nativeColorScheme.Length, nativeColors, nativeColors.Length));
            Color[] colors = new Color[nativeColors.Length];
            for (int i = 0; i < colors.Length; ++i) colors[i] = nativeColors[i].ToColor();
            return colors;
        }

        public float GetValueFromPosition(Vector3 position)
        {
            Vec3 nativePosition = Vec3.FromVector3(position);
            ThrowIfFailed(hbp_volume_sample_value(_handle.Handle, ref nativePosition, out float value));
            return value;
        }

        public float GetAverageValueAroundPositionWithMask(Vector3 position, int precision, Volume maskVolume, ref float[] rawValues, ref int actualLength)
        {
            Vec3 nativePosition = Vec3.FromVector3(position);
            IntPtr maskHandle = maskVolume == null ? IntPtr.Zero : maskVolume.getHandle().Handle;
            float[] targetRawValues = rawValues ?? Array.Empty<float>();
            ThrowIfFailed(hbp_volume_get_average_value_around_position_with_mask(_handle.Handle, ref nativePosition, precision, maskHandle, out float average, targetRawValues, targetRawValues.Length, out actualLength));
            return average;
        }

        public int[] GetHistogramBins(int binCount, float min = 0.0f, float max = 0.0f)
        {
            if (binCount <= 0) throw new ArgumentOutOfRangeException(nameof(binCount));
            int[] bins = new int[binCount];
            ThrowIfFailed(hbp_volume_copy_histogram_bins(_handle.Handle, bins, bins.Length, min, max));
            return bins;
        }

        internal void MarkLoaded()
        {
            IsLoaded = true;
        }

        protected override void create_DLL_class()
        {
            ThrowIfFailed(hbp_volume_create(out IntPtr volume));
            _handle = new HandleRef(this, volume);
        }

        protected override void delete_DLL_class()
        {
            ThrowIfFailed(hbp_volume_destroy(_handle.Handle));
        }

        private static void ThrowIfFailed(HbpCoreStatus status)
        {
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core Volume call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_volume_create", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_volume_create(out IntPtr volume);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_volume_destroy", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_volume_destroy(IntPtr volume);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_volume_load_nifti", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_volume_load_nifti(IntPtr volume, [MarshalAs(UnmanagedType.LPUTF8Str)] string path);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_volume_get_center", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_volume_get_center(IntPtr volume, out Vec3 center);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_volume_get_spacing", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_volume_get_spacing(IntPtr volume, out Vec3 spacing);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_volume_get_extrema", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_volume_get_extrema(IntPtr volume, out VolumeExtrema extrema);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_volume_get_bounding_box", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_volume_get_bounding_box(IntPtr volume, out IntPtr bbox);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_volume_get_orientation_vector", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_volume_get_orientation_vector(IntPtr volume, int cutOrientation, int flip, out Vec3 normal);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_volume_sample_value", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_volume_sample_value(IntPtr volume, ref Vec3 position, out float value);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_volume_get_average_value_around_position_with_mask", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_volume_get_average_value_around_position_with_mask(IntPtr volume, ref Vec3 position, int precision, IntPtr mask, out float average, [Out] float[] rawValues, int rawValueCapacity, out int actualCount);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_volume_size_offset_cut_plane", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_volume_size_offset_cut_plane(IntPtr volume, IntPtr plane, int cutCount, out float offset);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_volume_copy_histogram_bins", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_volume_copy_histogram_bins(IntPtr volume, int[] bins, int binCount, float minValue, float maxValue);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_volume_copy_surface_values", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_volume_copy_surface_values_ptr(IntPtr volume, IntPtr surface, IntPtr values, int valueCapacity);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_volume_copy_fmri_colors_from_values", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_volume_copy_fmri_colors_from_values(IntPtr volume, [In] float[] values, int valueCount, float negativeMin, float negativeMax, float positiveMin, float positiveMax, float alpha, [Out] Color4[] colors, int colorCapacity);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_volume_copy_localizer_colors_from_values", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_volume_copy_localizer_colors_from_values(IntPtr volume, [In] float[] values, [In] int[] mask, int valueCount, float minValue, float middleValue, float maxValue, [In] Color4[] colorScheme, int colorCount, [Out] Color4[] colors, int colorCapacity);
    }
}
