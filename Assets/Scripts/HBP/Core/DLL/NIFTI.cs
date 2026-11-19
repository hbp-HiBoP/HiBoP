using System;
using System.Runtime.InteropServices;
using HBP.Core.DLL.HbpCore;

namespace HBP.Core.DLL
{
    public class NIFTI : CppDLLImportBase
    {
        private int[] m_CachedHistogramBins;
        private int m_CachedHistogramBinCount;
        private float m_CachedHistogramMin;
        private float m_CachedHistogramMax;
        private int m_CachedHistogramFilters;

        public Tools.MRICalValues ExtremeValues
        {
            get
            {
                ThrowIfFailed(hbp_nifti_get_extrema(_handle.Handle, out VolumeExtrema values));
                return new Tools.MRICalValues
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

        public int NumberOfVolumes
        {
            get
            {
                ThrowIfFailed(hbp_nifti_get_number_of_volumes(_handle.Handle, out int count));
                return count;
            }
        }

        public float StartTime
        {
            get
            {
                ThrowIfFailed(hbp_nifti_get_start_time(_handle.Handle, out float value));
                return value;
            }
        }

        public float TimeStep
        {
            get
            {
                ThrowIfFailed(hbp_nifti_get_time_step(_handle.Handle, out float value));
                return value;
            }
        }

        public string TimeUnit
        {
            get
            {
                ThrowIfFailed(hbp_nifti_get_time_unit(_handle.Handle, out IntPtr value));
                return Marshal.PtrToStringAnsi(value);
            }
        }

        public bool IsLoaded { get; private set; }

        public bool Load(string path)
        {
            m_CachedHistogramBins = null;
            IsLoaded = hbp_nifti_load(_handle.Handle, path) == HbpCoreStatus.Ok;
            return IsLoaded;
        }

        public Volume ExtractVolume(int t)
        {
            Volume volume = new();
            FillVolumeWithNifti(volume, t);
            return volume;
        }

        public void FillVolumeWithNifti(Volume volume, int t)
        {
            if (volume == null) throw new ArgumentNullException(nameof(volume));
            ThrowIfFailed(hbp_nifti_convert_to_volume(_handle.Handle, volume.getHandle().Handle, t));
            volume.MarkLoaded();
        }

        /// <param name="excludeMinimum">Exclude exact source minima for histogram display. This is not an anatomical background mask.</param>
        public int[] GetHistogramBins(int binCount, float min = 0.0f, float max = 0.0f, bool excludeMinimum = false, bool excludeZero = false)
        {
            if (binCount <= 0) throw new ArgumentOutOfRangeException(nameof(binCount));
            int filters = (excludeMinimum ? 1 : 0) | (excludeZero ? 2 : 0);
            if (m_CachedHistogramBins == null || m_CachedHistogramBinCount != binCount || m_CachedHistogramMin != min || m_CachedHistogramMax != max || m_CachedHistogramFilters != filters)
            {
                int[] bins = new int[binCount];
                ThrowIfFailed(filters != 0 ? hbp_nifti_copy_histogram_bins_filtered(_handle.Handle, bins, bins.Length, min, max, filters) : hbp_nifti_copy_histogram_bins(_handle.Handle, bins, bins.Length, min, max));
                m_CachedHistogramBins = bins;
                m_CachedHistogramBinCount = binCount;
                m_CachedHistogramMin = min;
                m_CachedHistogramMax = max;
                m_CachedHistogramFilters = filters;
            }

            return (int[])m_CachedHistogramBins.Clone();
        }

        protected override void create_DLL_class()
        {
            ThrowIfFailed(hbp_nifti_create(out IntPtr nifti));
            _handle = new HandleRef(this, nifti);
        }

        protected override void delete_DLL_class()
        {
            ThrowIfFailed(hbp_nifti_destroy(_handle.Handle));
        }

        private static void ThrowIfFailed(HbpCoreStatus status)
        {
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core NIFTI call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_nifti_create", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_nifti_create(out IntPtr nifti);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_nifti_destroy", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_nifti_destroy(IntPtr nifti);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_nifti_load", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_nifti_load(IntPtr nifti, [MarshalAs(UnmanagedType.LPUTF8Str)] string path);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_nifti_convert_to_volume", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_nifti_convert_to_volume(IntPtr nifti, IntPtr volume, int t);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_nifti_get_number_of_volumes", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_nifti_get_number_of_volumes(IntPtr nifti, out int count);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_nifti_get_extrema", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_nifti_get_extrema(IntPtr nifti, out VolumeExtrema extrema);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_nifti_get_start_time", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_nifti_get_start_time(IntPtr nifti, out float startTime);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_nifti_get_time_step", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_nifti_get_time_step(IntPtr nifti, out float timeStep);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_nifti_get_time_unit", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_nifti_get_time_unit(IntPtr nifti, out IntPtr timeUnit);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_nifti_copy_histogram_bins", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_nifti_copy_histogram_bins(IntPtr nifti, int[] bins, int binCount, float minValue, float maxValue);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_nifti_copy_histogram_bins_filtered", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_nifti_copy_histogram_bins_filtered(IntPtr nifti, int[] bins, int binCount, float minValue, float maxValue, int filters);
    }
}
