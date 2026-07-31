using System;
using System.Runtime.InteropServices;
using HBP.Core.DLL.HbpCore;
using HBP.Core.Enums;

namespace HBP.Core.DLL
{
    public class IEEGGenerator : ActivityGenerator
    {
        private const int MaximumParallelWorkerCount = 16;

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

            ThrowIfFailed(hbp_ieeg_generator_compute_activity_from_sites(_handle.Handle, rawElectrodes.getHandle().Handle, influenceDistance, activityValues, timelineLength, (int)siteInfluenceByDistance));
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

            ThrowIfFailed(hbp_ieeg_generator_compute_activity_atlas(_handle.Handle, activityValues, timelineLength, areaMask.Length, areaMask, marsAtlas.getHandle().Handle));
        }

        public void AdjustValues(float middle, float spanMin, float spanMax)
        {
            ThrowIfFailed(hbp_ieeg_generator_adjust_values(_handle.Handle, middle, spanMin, spanMax));
        }

        internal void EnablePerformanceMetrics(bool enabled)
        {
            ThrowIfFailed(hbp_ieeg_generator_enable_performance_metrics(_handle.Handle, enabled ? 1 : 0));
        }

        internal void SetParallelOptions(int workerCount, int neighborBatchSize)
        {
            if (workerCount < 0 || workerCount > MaximumParallelWorkerCount) throw new ArgumentOutOfRangeException(nameof(workerCount));
            if (neighborBatchSize < 0) throw new ArgumentOutOfRangeException(nameof(neighborBatchSize));
            ThrowIfFailed(hbp_ieeg_generator_set_parallel_options(_handle.Handle, workerCount, neighborBatchSize));
        }

        public IEEGComputeMetrics GetLastComputeMetrics()
        {
            ThrowIfFailed(hbp_ieeg_generator_get_last_compute_metrics(_handle.Handle, out IEEGComputeMetrics metrics));
            return metrics;
        }

        protected override void create_DLL_class()
        {
            ThrowIfFailed(hbp_ieeg_generator_create(out IntPtr generator));
            _handle = new HandleRef(this, generator);
        }

        protected override void delete_DLL_class()
        {
            ThrowIfFailed(hbp_ieeg_generator_destroy(_handle.Handle));
        }

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_ieeg_generator_create", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_ieeg_generator_create(out IntPtr generator);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_ieeg_generator_destroy", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_ieeg_generator_destroy(IntPtr generator);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_ieeg_generator_set_parallel_options", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_ieeg_generator_set_parallel_options(IntPtr generator, int workerCount, int neighborBatchSize);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_ieeg_generator_enable_performance_metrics", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_ieeg_generator_enable_performance_metrics(IntPtr generator, int enabled);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_ieeg_generator_get_last_compute_metrics", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_ieeg_generator_get_last_compute_metrics(IntPtr generator, out IEEGComputeMetrics metrics);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_ieeg_generator_compute_activity_from_sites", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_ieeg_generator_compute_activity_from_sites(IntPtr generator, IntPtr sites, float maxDistance, [In] float[] activity, int timelineLength, int ratioDistance);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_ieeg_generator_compute_activity_atlas", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_ieeg_generator_compute_activity_atlas(IntPtr generator, [In] float[] activity, int timelineLength, int areaCount, [In] int[] mask, IntPtr atlas);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_ieeg_generator_adjust_values", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_ieeg_generator_adjust_values(IntPtr generator, float middle, float min, float max);
    }
}
