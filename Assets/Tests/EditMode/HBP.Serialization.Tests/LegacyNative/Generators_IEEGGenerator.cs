using HBP.Core.DLL;
using HBP.Tests.Serialization;
using System;
using System.Runtime.InteropServices;
using HBP.Core.DLL.HbpCore;
using HBP.Core.Enums;

namespace HBP.Tests.Serialization.LegacyNative
{
    public class IEEGGenerator : ActivityGenerator
    {
        private const int MaximumParallelWorkerCount = 16;

        #region Public Methods

        public void ComputeActivity(RawSiteList rawElectrodes, float influenceDistance, float[] activityValues, int timelineLength, int numberOfSites, SiteInfluenceByDistanceType siteInfluenceByDistance)
        {
            if (rawElectrodes == null) throw new ArgumentNullException(nameof(rawElectrodes));
            if (activityValues == null) throw new ArgumentNullException(nameof(activityValues));
            if (timelineLength <= 0) throw new ArgumentOutOfRangeException(nameof(timelineLength));
            if (numberOfSites != rawElectrodes.NumberOfSites)
            {
                throw new ArgumentException("numberOfSites must match the raw site list count.", nameof(numberOfSites));
            }

            int expectedValueCount = checked(timelineLength * numberOfSites);
            if (activityValues.Length != expectedValueCount)
            {
                throw new ArgumentException($"Expected {expectedValueCount} activity values, received {activityValues.Length}.", nameof(activityValues));
            }

            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                ThrowIfFailed(hbp_ieeg_generator_compute_activity_from_sites(_handle.Handle, rawElectrodes.getHandle().Handle, influenceDistance, activityValues, timelineLength, (int)siteInfluenceByDistance));
                return;
            }

            compute_activity_IEEGGenerator(_handle, rawElectrodes.getHandle(), influenceDistance, activityValues, timelineLength, numberOfSites, (int)siteInfluenceByDistance);
        }

        public void ComputeActivityAtlas(float[] activityValues, int timelineLength, int[] areaMask, MarsAtlas marsAtlas)
        {
            if (activityValues == null) throw new ArgumentNullException(nameof(activityValues));
            if (areaMask == null) throw new ArgumentNullException(nameof(areaMask));
            if (marsAtlas == null) throw new ArgumentNullException(nameof(marsAtlas));
            if (timelineLength <= 0) throw new ArgumentOutOfRangeException(nameof(timelineLength));
            int expectedValueCount = checked(timelineLength * areaMask.Length);
            if (activityValues.Length != expectedValueCount)
            {
                throw new ArgumentException($"Expected {expectedValueCount} atlas activity values, received {activityValues.Length}.", nameof(activityValues));
            }

            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                if (marsAtlas.Backend != BenchmarkBackend.HbpCore)
                {
                    throw new InvalidOperationException($"IEEGGenerator.ComputeActivityAtlas cannot use a {marsAtlas.Backend} atlas with hbp_core.");
                }

                ThrowIfFailed(hbp_ieeg_generator_compute_activity_atlas(_handle.Handle, activityValues, timelineLength, areaMask.Length, areaMask, marsAtlas.getHandle().Handle));
                return;
            }

            compute_activity_atlas_IEEGGenerator(_handle, activityValues, timelineLength, areaMask, marsAtlas.getHandle());
        }

        public void AdjustValues(float middle, float spanMin, float spanMax)
        {
            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                ThrowIfFailed(hbp_ieeg_generator_adjust_values(_handle.Handle, middle, spanMin, spanMax));
                return;
            }

            adjust_values_IEEGGenerator(_handle, middle, spanMin, spanMax);
        }

        internal void EnablePerformanceMetrics(bool enabled)
        {
            if (m_Backend != BenchmarkBackend.HbpCore)
            {
                throw new NotSupportedException("Detailed iEEG performance metrics are only available with hbp_core.");
            }

            ThrowIfFailed(hbp_ieeg_generator_enable_performance_metrics(_handle.Handle, enabled ? 1 : 0));
        }

        internal void SetParallelOptions(int workerCount, int neighborBatchSize)
        {
            if (m_Backend != BenchmarkBackend.HbpCore)
            {
                throw new NotSupportedException("Parallel iEEG options are only available with hbp_core.");
            }

            if (workerCount < 0 || workerCount > MaximumParallelWorkerCount) throw new ArgumentOutOfRangeException(nameof(workerCount));
            if (neighborBatchSize < 0) throw new ArgumentOutOfRangeException(nameof(neighborBatchSize));
            ThrowIfFailed(hbp_ieeg_generator_set_parallel_options(_handle.Handle, workerCount, neighborBatchSize));
        }

        internal IEEGComputeMetrics GetLastComputeMetrics()
        {
            if (m_Backend != BenchmarkBackend.HbpCore)
            {
                throw new NotSupportedException("Detailed iEEG performance metrics are only available with hbp_core.");
            }

            ThrowIfFailed(hbp_ieeg_generator_get_last_compute_metrics(_handle.Handle, out IEEGComputeMetrics metrics));
            return metrics;
        }

        #endregion

        #region Memory Management

        protected override void create_DLL_class()
        {
            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                ThrowIfFailed(hbp_ieeg_generator_create(out IntPtr generator));
                _handle = new HandleRef(this, generator);
                return;
            }

            _handle = new HandleRef(this, create_IEEGGenerator());
        }

        protected override void delete_DLL_class()
        {
            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                ThrowIfFailed(hbp_ieeg_generator_destroy(_handle.Handle));
                return;
            }

            delete_IEEGGenerator(_handle);
        }

        #endregion

        #region DLLImport

        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "create_IEEGGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_IEEGGenerator();

        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "delete_IEEGGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_IEEGGenerator(HandleRef generator);

        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "compute_activity_IEEGGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void compute_activity_IEEGGenerator(HandleRef generator, HandleRef rawSiteList, float maxDistance, float[] activity, int timelineLength, int sitesNumber, int ratioDistance);

        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "compute_activity_atlas_IEEGGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void compute_activity_atlas_IEEGGenerator(HandleRef generator, float[] activity, int timelineLength, int[] mask, HandleRef marsAtlas);

        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "adjust_values_IEEGGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void adjust_values_IEEGGenerator(HandleRef generator, float middle, float min, float max);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_ieeg_generator_create", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_ieeg_generator_create(out IntPtr generator);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_ieeg_generator_destroy", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_ieeg_generator_destroy(IntPtr generator);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_ieeg_generator_set_parallel_options", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_ieeg_generator_set_parallel_options(IntPtr generator, int workerCount, int neighborBatchSize);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_ieeg_generator_enable_performance_metrics", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_ieeg_generator_enable_performance_metrics(IntPtr generator, int enabled);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_ieeg_generator_get_last_compute_metrics", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_ieeg_generator_get_last_compute_metrics(IntPtr generator, out IEEGComputeMetrics metrics);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_ieeg_generator_compute_activity_from_sites", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_ieeg_generator_compute_activity_from_sites(IntPtr generator, IntPtr sites, float maxDistance, [In] float[] activity, int timelineLength, int ratioDistance);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_ieeg_generator_compute_activity_atlas", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_ieeg_generator_compute_activity_atlas(IntPtr generator, [In] float[] activity, int timelineLength, int areaCount, [In] int[] mask, IntPtr atlas);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_ieeg_generator_adjust_values", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_ieeg_generator_adjust_values(IntPtr generator, float middle, float min, float max);

        #endregion
    }
}
