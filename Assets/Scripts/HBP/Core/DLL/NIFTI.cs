using System;
using System.Runtime.InteropServices;

using HBP.Core.DLL.HbpCore;

namespace HBP.Core.DLL
{
    public class NIFTI : CppDLLImportBase
    {
        private NativeBackend m_Backend = NativeBackendOptions.ExperimentalBackend;
        private int[] m_CachedHistogramBins;
        private int m_CachedHistogramBinCount;
        private float m_CachedHistogramMin;
        private float m_CachedHistogramMax;

        #region Properties
        /// <summary>
        /// Get the calibration values of the loaded MRI
        /// </summary>
        public Tools.MRICalValues ExtremeValues
        {
            get
            {
                if (m_Backend == NativeBackend.HbpCore)
                {
                    ThrowIfFailed(hbp_nifti_get_extrema(_handle.Handle, out VolumeExtrema nativeValues));
                    return ToMRICalValues(nativeValues);
                }

                Tools.MRICalValues values = new();

                float[] valuesF = new float[6];
                retrieveExtremeValues_NIFTI(_handle, valuesF);

                values.Min = valuesF[0];
                values.Max = valuesF[1];
                values.LoadedCalMin = valuesF[2];
                values.LoadedCalMax = valuesF[3];
                values.ComputedCalMin = valuesF[4];
                values.ComputedCalMax = valuesF[5];

                return values;
            }
        }
        public int NumberOfVolumes
        {
            get
            {
                if (m_Backend == NativeBackend.HbpCore)
                {
                    ThrowIfFailed(hbp_nifti_get_number_of_volumes(_handle.Handle, out int count));
                    return count;
                }

                return number_of_volumes_NIFTI(_handle);
            }
        }
        public float StartTime
        {
            get
            {
                if (m_Backend == NativeBackend.HbpCore)
                {
                    ThrowIfFailed(hbp_nifti_get_start_time(_handle.Handle, out float startTime));
                    return startTime;
                }

                return get_start_time_NIFTI(_handle);
            }
        }
        public float TimeStep
        {
            get
            {
                if (m_Backend == NativeBackend.HbpCore)
                {
                    ThrowIfFailed(hbp_nifti_get_time_step(_handle.Handle, out float timeStep));
                    return timeStep;
                }

                return get_time_step_NIFTI(_handle);
            }
        }
        public string TimeUnit
        {
            get
            {
                if (m_Backend == NativeBackend.HbpCore)
                {
                    ThrowIfFailed(hbp_nifti_get_time_unit(_handle.Handle, out IntPtr ptr));
                    return Marshal.PtrToStringAnsi(ptr);
                }

                lock (typeof(Marshal))
                {
                    IntPtr ptr = get_time_unit_NIFTI(_handle);
                    return Marshal.PtrToStringAnsi(ptr);
                }
            }
        }
        public bool IsLoaded { get; private set; }
        #endregion

        #region Public Methods
        public bool Load(string path)
        {
            m_CachedHistogramBins = null;
            if (m_Backend == NativeBackend.HbpCore)
            {
                IsLoaded = hbp_nifti_load(_handle.Handle, path) == HbpCoreStatus.Ok;
                return IsLoaded;
            }

            IsLoaded = (loadNiiFile_NIFTI(_handle, path) == 1);
            return IsLoaded;
        }
        public Volume ExtractVolume(int t)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                Volume coreVolume = Volume.CreateHbpCore();
                FillVolumeWithNifti(coreVolume, t);
                return coreVolume;
            }

            Volume exportedVolume = new();
            convertToVolume_NIFTI(_handle, exportedVolume.getHandle(), t);
            return exportedVolume;
        }
        public void FillVolumeWithNifti(Volume volume, int t)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                if (volume.Backend != NativeBackend.HbpCore)
                {
                    throw new InvalidOperationException("Cannot fill a hbp_export Volume from a hbp_core NIFTI.");
                }

                ThrowIfFailed(hbp_nifti_convert_to_volume(_handle.Handle, volume.getHandle().Handle, t));
                volume.MarkLoaded();
                return;
            }

            fill_volume_NIFTI(_handle, volume.getHandle(), t);
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

            if (m_CachedHistogramBins == null || m_CachedHistogramBinCount != binCount || m_CachedHistogramMin != min || m_CachedHistogramMax != max)
            {
                m_CachedHistogramBins = new int[binCount];
                ThrowIfFailed(hbp_nifti_copy_histogram_bins(_handle.Handle, m_CachedHistogramBins, m_CachedHistogramBins.Length, min, max));
                m_CachedHistogramBinCount = binCount;
                m_CachedHistogramMin = min;
                m_CachedHistogramMax = max;
            }
            return (int[])m_CachedHistogramBins.Clone();
        }
        #endregion

        #region Memory Management
        public NIFTI()
        {
        }

        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_nifti_create(out IntPtr nifti));
                _handle = new HandleRef(this, nifti);
                return;
            }

            _handle = new HandleRef(this, create_NIFTI());
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_nifti_destroy(_handle.Handle));
                return;
            }

            delete_NIFTI(_handle);
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
                throw new InvalidOperationException($"hbp_core NIFTI call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }

        #region DLLimport
        [DllImport("hbp_export", EntryPoint = "create_NIFTI", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_NIFTI();
        [DllImport("hbp_export", EntryPoint = "delete_NIFTI", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_NIFTI(HandleRef handleVolume);
        [DllImport("hbp_export", EntryPoint = "loadNiiFile_NIFTI", CallingConvention = CallingConvention.Cdecl)]
        static private extern int loadNiiFile_NIFTI(HandleRef handleNii, string pathFile);
        [DllImport("hbp_export", EntryPoint = "number_of_volumes_NIFTI", CallingConvention = CallingConvention.Cdecl)]
        static private extern int number_of_volumes_NIFTI(HandleRef handleNii);
        [DllImport("hbp_export", EntryPoint = "fill_volume_NIFTI", CallingConvention = CallingConvention.Cdecl)]
        static private extern void fill_volume_NIFTI(HandleRef handleNii, HandleRef handleVolume, int t);
        [DllImport("hbp_export", EntryPoint = "convertToVolume_NIFTI", CallingConvention = CallingConvention.Cdecl)]
        static private extern void convertToVolume_NIFTI(HandleRef handleNii, HandleRef handleVolume, int t);
        [DllImport("hbp_export", EntryPoint = "retrieveExtremeValues_NIFTI", CallingConvention = CallingConvention.Cdecl)]
        static private extern void retrieveExtremeValues_NIFTI(HandleRef handleNii, float[] extremeValues);
        [DllImport("hbp_export", EntryPoint = "get_start_time_NIFTI", CallingConvention = CallingConvention.Cdecl)]
        static private extern float get_start_time_NIFTI(HandleRef handleNii);
        [DllImport("hbp_export", EntryPoint = "get_time_step_NIFTI", CallingConvention = CallingConvention.Cdecl)]
        static private extern float get_time_step_NIFTI(HandleRef handleNii);
        [DllImport("hbp_export", EntryPoint = "get_time_unit_NIFTI", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr get_time_unit_NIFTI(HandleRef handleNii);

        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_nifti_create", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_nifti_create(out IntPtr nifti);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_nifti_destroy", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_nifti_destroy(IntPtr nifti);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_nifti_load", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_nifti_load(IntPtr nifti, [MarshalAs(UnmanagedType.LPUTF8Str)] string path);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_nifti_convert_to_volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_nifti_convert_to_volume(IntPtr nifti, IntPtr volume, int t);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_nifti_get_number_of_volumes", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_nifti_get_number_of_volumes(IntPtr nifti, out int count);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_nifti_get_extrema", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_nifti_get_extrema(IntPtr nifti, out VolumeExtrema extrema);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_nifti_get_start_time", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_nifti_get_start_time(IntPtr nifti, out float startTime);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_nifti_get_time_step", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_nifti_get_time_step(IntPtr nifti, out float timeStep);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_nifti_get_time_unit", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_nifti_get_time_unit(IntPtr nifti, out IntPtr timeUnit);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_nifti_copy_histogram_bins", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_nifti_copy_histogram_bins(IntPtr nifti, int[] bins, int binCount, float minValue, float maxValue);
        #endregion
    }
}
