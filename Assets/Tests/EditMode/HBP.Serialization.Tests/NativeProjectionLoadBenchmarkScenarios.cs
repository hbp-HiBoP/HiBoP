using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HBP.Core.DLL;
using HBP.Core.Enums;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    internal static class NativeProjectionLoadBenchmarkScenarios
    {
        public static List<NativeProjectionLoadScenarioDefinition> Build(string profile, int timelineLength, bool includeExport, VolumeInterpolation volumeInterpolation, string filter)
        {
            List<NativeProjectionLoadScenarioDefinition> scenarios = new();
            HashSet<string> keys = new(StringComparer.OrdinalIgnoreCase);

            void Add(string prefix, int dimension, int sites, float radius, int columns = 1, bool export = false, int timeline = 0, int samplingFrequencyHz = 0, SyntheticTimeSeriesDefinition syntheticTimeSeries = null)
            {
                int scenarioTimelineLength = timeline > 0 ? timeline : timelineLength;
                string name = $"{prefix}.d{dimension}.s{sites}.t{scenarioTimelineLength}.r{radius:R}.c{columns}";
                if (!string.IsNullOrWhiteSpace(filter) && name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return;
                }

                string key = $"{dimension}:{sites}:{scenarioTimelineLength}:{radius:R}:{columns}:{export}";
                if (!keys.Add(key)) return;
                scenarios.Add(new NativeProjectionLoadScenarioDefinition(name, dimension, sites, scenarioTimelineLength, radius, columns, export, volumeInterpolation, samplingFrequencyHz: samplingFrequencyHz, syntheticTimeSeries: syntheticTimeSeries ?? DefaultSyntheticLayout(sites, scenarioTimelineLength, samplingFrequencyHz)));
            }

            void AddRadii(string prefix, int dimension, int sites, params float[] radii)
            {
                string radiusSlug = string.Join("-", radii.Select(value => value.ToString("R")));
                string name = $"{prefix}.d{dimension}.s{sites}.t{timelineLength}.r{radiusSlug}.c{radii.Length}";
                if (!string.IsNullOrWhiteSpace(filter) && name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return;
                }

                string key = $"{dimension}:{sites}:{timelineLength}:{radiusSlug}:{radii.Length}:false";
                if (!keys.Add(key)) return;
                scenarios.Add(new NativeProjectionLoadScenarioDefinition(name, dimension, sites, timelineLength, radii[0], radii.Length, false, volumeInterpolation, radii, syntheticTimeSeries: DefaultSyntheticLayout(sites, timelineLength, 0)));
            }

            if (profile.Equals("Smoke", StringComparison.OrdinalIgnoreCase))
            {
                Add("projection.smoke", 24, 1000, 15.0f);
                if (includeExport) Add("projection.smoke-export", 24, 1000, 15.0f, export: true);
                return scenarios;
            }

            foreach (int sites in new[] { 1000, 5000, 10000, 25000 })
            {
                Add("projection.site-scale", 96, sites, 15.0f);
            }

            foreach (float radius in new[] { 5.0f, 15.0f, 30.0f, 50.0f })
            {
                Add("projection.radius-scale", 96, 5000, radius);
            }

            foreach (int dimension in new[] { 80, 96, 120 })
            {
                Add("projection.dimension-scale", dimension, 5000, 15.0f);
            }

            SyntheticTimeSeriesDefinition productTimeSeries = new(250, 120, 100, 100 * 2001, 2001, 501, 1000);
            Add("projection.product-reference", 80, 30000, 15.0f, timeline: 100, syntheticTimeSeries: productTimeSeries);
            Add("projection.product-reference-multicolumn", 80, 30000, 15.0f, columns: 3, timeline: 100, syntheticTimeSeries: productTimeSeries);
            Add("projection.multicolumn", 96, 5000, 15.0f, columns: 3);
            if (includeExport) Add("projection.export", 96, 5000, 15.0f, export: true);

            if (profile.Equals("Extreme", StringComparison.OrdinalIgnoreCase))
            {
                int lowFrequencyLength = SyntheticTimeSeriesFactory.InclusiveSampleCount(1500, 64);
                int highFrequencyLength = SyntheticTimeSeriesFactory.InclusiveSampleCount(1500, 2048);
                Add("projection.high-frequency", 24, 1000, 15.0f, timeline: lowFrequencyLength, samplingFrequencyHz: 64);
                Add("projection.high-frequency", 24, 1000, 15.0f, timeline: highFrequencyLength, samplingFrequencyHz: 2048);
                Add("projection.memory-reference", 120, 1000, 15.0f);
                Add("projection.cache-reference", 120, 1000, 15.0f, columns: 3);
                AddRadii("projection.cache-churn", 120, 1000, 5.0f, 15.0f, 30.0f, 15.0f, 5.0f);
                Add("projection.extreme-radius", 120, 25000, 30.0f);
                Add("projection.extreme-radius", 120, 25000, 50.0f);
                Add("projection.extreme-multicolumn", 120, 25000, 15.0f, columns: 3);
            }

            return scenarios;
        }

        public static NativeProjectionLoadScenarioResult Run(NativeProjectionLoadScenarioDefinition definition, string surfacePath, string volumePath, string exportRoot, int repetitions, int workerCount, int neighborBatchSize)
        {
            NativeProjectionLoadScenarioResult result = new()
            {
                name = definition.Name,
                dimension = definition.Dimension,
                siteCount = definition.SiteCount,
                timelineLength = definition.TimelineLength,
                samplingFrequencyHz = definition.SamplingFrequencyHz,
                influenceDistance = definition.InfluenceDistance,
                influenceDistances = definition.InfluenceDistances,
                columnCount = definition.ColumnCount,
                exportMeasured = definition.MeasureExport,
                workload = definition.Workload,
                volumeInterpolation = definition.VolumeInterpolation.ToString()
            };

            for (int repetition = 0; repetition < repetitions; ++repetition)
            {
                result.samples.Add(RunSample(definition, surfacePath, volumePath, exportRoot, repetition, workerCount, neighborBatchSize));
            }

            NativeProjectionLoadSampleResult first = result.samples[0];
            Require(result.samples.All(sample => sample.checksum == first.checksum), $"Scenario {definition.Name} produced different checksums across repetitions.");
            result.surfaceVertexCount = LoadSurfaceVertexCount(surfacePath);
            result.generatedPointCount = first.generatedPointCount;
            result.activeSiteCount = first.activeSiteCount;
            result.neighborLinkCount = first.neighborLinkCount;
            result.storedValueCount = first.storedValueCount;
            result.storedWeightCount = first.storedWeightCount;
            result.spatialIndexCacheHitCount = first.spatialIndexCacheHitCount;
            result.spatialIndexCacheMissCount = first.spatialIndexCacheMissCount;
            result.maxSpatialIndexCacheEntryCount = first.maxSpatialIndexCacheEntryCount;
            result.maxSpatialIndexCacheBytes = first.maxSpatialIndexCacheBytes;
            result.spatialIndexGeometryVersion = first.spatialIndexGeometryVersion;
            result.parallelWorkerCount = first.parallelWorkerCount;
            result.neighborBatchSize = first.neighborBatchSize;
            result.neighborBatchCount = first.neighborBatchCount;
            result.maxTemporaryNeighborPeakBytes = result.samples.Max(sample => sample.temporaryNeighborPeakBytes);
            result.temporaryNeighborBudgetBytes = first.temporaryNeighborBudgetBytes;
            result.estimatedCurrentValueAndWeightBytes = first.estimatedCurrentValueAndWeightBytes;
            result.cutTexturePixelCount = first.cutTexturePixelCount;
            result.estimatedCutStencilPayloadBytes = first.estimatedCutStencilPayloadBytes;
            result.medianTotalWallMilliseconds = Median(result.samples.Select(sample => sample.totalWallMilliseconds));
            result.p95TotalWallMilliseconds = Percentile95(result.samples.Select(sample => sample.totalWallMilliseconds));
            result.medianTotalCpuMilliseconds = Median(result.samples.Select(sample => sample.totalCpuMilliseconds));
            result.medianComputeWallMilliseconds = Median(result.samples.Select(sample => sample.computeWallMilliseconds));
            result.p95ComputeWallMilliseconds = Percentile95(result.samples.Select(sample => sample.computeWallMilliseconds));
            result.medianComputeCpuMilliseconds = Median(result.samples.Select(sample => sample.computeCpuMilliseconds));
            result.medianCutPreparationWallMilliseconds = Median(result.samples.Select(sample => sample.cutPreparationWallMilliseconds));
            result.medianCutTimelineUpdateWallMilliseconds = Median(result.samples.Select(sample => sample.meanCutTimelineUpdateWallMilliseconds));
            result.p95CutTimelineUpdateWallMilliseconds = Percentile95(result.samples.Select(sample => sample.meanCutTimelineUpdateWallMilliseconds));
            result.medianCutTimelineUpdateCpuMilliseconds = Median(result.samples.Select(sample => sample.meanCutTimelineUpdateCpuMilliseconds));
            result.medianCutTimelineFillWallMilliseconds = Median(result.samples.Select(sample => sample.meanCutTimelineFillWallMilliseconds));
            result.medianCutTimelineCopyWallMilliseconds = Median(result.samples.Select(sample => sample.meanCutTimelineCopyWallMilliseconds));
            result.maxPeakPrivateBytesDelta = result.samples.Max(sample => sample.peakPrivateBytesDelta);
            result.maxPeakWorkingSetBytesDelta = result.samples.Max(sample => sample.peakWorkingSetBytesDelta);
            result.maxRetainedPrivateBytesDelta = result.samples.Max(sample => sample.retainedPrivateBytesDelta);
            result.maxRetainedWorkingSetBytesDelta = result.samples.Max(sample => sample.retainedWorkingSetBytesDelta);
            SyntheticTimeSeriesDefinition timeSeries = definition.SyntheticTimeSeries;
            result.memoryLayers = new NativeProjectionMemoryLayerResult
            {
                syntheticManagedLayerBasis = timeSeries == null ? "not measured" : $"{timeSeries.PatientCount} patients; {timeSeries.ChannelsPerPatient} channels/patient; " + $"{timeSeries.TrialCount} trials; {timeSeries.RecordingSampleCount} raw samples/channel; " + $"{timeSeries.WindowSampleCount} window samples; {timeSeries.BaselineSampleCount} baseline samples",
                managedRawSignalBytes = timeSeries?.ManagedRawSignalBytes ?? 0L,
                managedEpochBytes = timeSeries?.ManagedEpochBytes ?? 0L,
                managedDerivedBytes = timeSeries?.ManagedDerivedBytes ?? 0L,
                managedColumnArrayBytes = first.managedActivityInputBytes,
                nativeProjectionBytes = first.estimatedCurrentValueAndWeightBytes,
                estimatedTextureBytes = first.estimatedCutStencilPayloadBytes,
                peakPrivateBytesDelta = result.maxPeakPrivateBytesDelta,
                retainedPrivateBytesDelta = result.maxRetainedPrivateBytesDelta
            };
            return result;
        }

        private static SyntheticTimeSeriesDefinition DefaultSyntheticLayout(int channels, int samples, int samplingFrequencyHz)
        {
            return new SyntheticTimeSeriesDefinition(1, channels, 1, samples, samples, 0, samplingFrequencyHz > 0 ? samplingFrequencyHz : 1000);
        }

        private static NativeProjectionLoadSampleResult RunSample(NativeProjectionLoadScenarioDefinition definition, string surfacePath, string volumePath, string exportRoot, int repetition, int workerCount, int neighborBatchSize)
        {
            using Surface surface = LoadSurface(surfacePath);
            using Volume volume = LoadVolume(volumePath);
            using RawSiteList sites = CreateSites(volume, definition.SiteCount);
            float[] activity = CreateActivity(definition.SiteCount, definition.TimelineLength);
            ForceCollection();

            using Process process = Process.GetCurrentProcess();
            NativeProjectionProcessMemorySnapshot baselineMemory = NativeProjectionProcessMemory.Read(process);
            long baselinePrivate = baselineMemory.PrivateBytes;
            long baselineWorkingSet = baselineMemory.WorkingSetBytes;
            Require(baselinePrivate > 0 && baselineWorkingSet > 0, "Process memory counters must be available for the projection benchmark.");
            TimeSpan cpuStart = process.TotalProcessorTime;
            long totalStart = Stopwatch.GetTimestamp();
            long totalEnd = totalStart;
            TimeSpan cpuEnd = cpuStart;
            ActivityProjectionGrid projectionGrid = null;
            HBP.Core.Object3D.Cut cut = null;
            CutGeometryGenerator cutGeometry = null;
            List<IEEGGenerator> generators = new();
            List<SurfaceGenerator> outputs = new();
            List<CutGenerator> cutOutputs = new();
            long exportFileBytes = 0;
            string exportPath = null;

            double projectionGridWall = 0.0;
            double projectionGridCpu = 0.0;
            double computeWall = 0.0;
            double computeCpu = 0.0;
            double displayWall = 0.0;
            double displayCpu = 0.0;
            double cutPreparationWall = 0.0;
            double cutPreparationCpu = 0.0;
            double cutTimelineUpdatesWall = 0.0;
            double cutTimelineUpdatesCpu = 0.0;
            double cutTimelineFillsWall = 0.0;
            double cutTimelineCopiesWall = 0.0;
            int cutTimelineUpdateCount = 0;
            int cutTexturePixelCount = 0;
            long estimatedCutStencilPayloadBytes = 0;
            double exportWall = 0.0;
            double exportCpu = 0.0;
            double nativeTotal = 0.0;
            double allocation = 0.0;
            double spatialIndex = 0.0;
            double spatialIndexBuild = 0.0;
            double spatialIndexLookup = 0.0;
            double neighborQuery = 0.0;
            double accumulation = 0.0;
            double normalization = 0.0;
            long generatedPoints = 0;
            long activeSites = 0;
            long neighborLinks = 0;
            long storedValues = 0;
            long storedWeights = 0;
            long spatialIndexCacheHits = 0;
            long spatialIndexCacheMisses = 0;
            long maxSpatialIndexCacheEntries = 0;
            long maxSpatialIndexCacheBytes = 0;
            long spatialIndexGeometryVersion = 0;
            long parallelWorkerCount = 0;
            long actualNeighborBatchSize = 0;
            long neighborBatchCount = 0;
            long temporaryNeighborPeakBytes = 0;
            long temporaryNeighborBudgetBytes = 0;
            ulong checksum = 1469598103934665603UL;
            long steadyPrivate = baselinePrivate;
            long steadyWorkingSet = baselineWorkingSet;
            long peakPrivate;
            long peakWorkingSet;
            long retainedPrivate;
            long retainedWorkingSet;

            using (NativeProjectionProcessMemorySampler sampler = new(process, baselinePrivate, baselineWorkingSet))
            {
                sampler.Start();
                try
                {
                    projectionGrid = new ActivityProjectionGrid();
                    Measure(process, () => projectionGrid.Initialize(volume, definition.Dimension, definition.VolumeInterpolation), out projectionGridWall, out projectionGridCpu);

                    cut = new HBP.Core.Object3D.Cut(volume.Center, Vector3.forward)
                    {
                        Orientation = CutOrientation.Axial,
                        Position = 0.5f
                    };
                    cutGeometry = new CutGeometryGenerator();
                    cutGeometry.Initialize(volume, cut, -1);
                    cutTexturePixelCount = cutGeometry.TextureSize.x * cutGeometry.TextureSize.y;
                    estimatedCutStencilPayloadBytes = (long)cutTexturePixelCount * (definition.VolumeInterpolation == VolumeInterpolation.Nearest ? sizeof(int) : 16L);
                    Color32[] grayscale = CreateColorScheme(grayscale: true);
                    Color32[] activityColors = CreateColorScheme(grayscale: false);

                    for (int column = 0; column < definition.ColumnCount; ++column)
                    {
                        IEEGGenerator generator = new();
                        generators.Add(generator);
                        generator.Initialize(projectionGrid);
                        generator.SetParallelOptions(workerCount, neighborBatchSize);
                        generator.EnablePerformanceMetrics(true);

                        Measure(process, () => generator.ComputeActivity(sites, definition.InfluenceDistances[column], activity, definition.TimelineLength, definition.SiteCount, SiteInfluenceByDistanceType.Linear), out double columnComputeWall, out double columnComputeCpu);
                        computeWall += columnComputeWall;
                        computeCpu += columnComputeCpu;

                        IEEGComputeMetrics metrics = generator.GetLastComputeMetrics();
                        nativeTotal += metrics.totalMilliseconds;
                        allocation += metrics.allocationMilliseconds;
                        spatialIndex += metrics.spatialIndexMilliseconds;
                        spatialIndexBuild += metrics.spatialIndexBuildMilliseconds;
                        spatialIndexLookup += metrics.spatialIndexLookupMilliseconds;
                        neighborQuery += metrics.neighborQueryMilliseconds;
                        accumulation += metrics.accumulationMilliseconds;
                        normalization += metrics.normalizationMilliseconds;
                        generatedPoints = metrics.generatedPointCount;
                        activeSites += metrics.activeSiteCount;
                        neighborLinks += metrics.neighborLinkCount;
                        storedValues += metrics.storedValueCount;
                        storedWeights += metrics.storedWeightCount;
                        spatialIndexCacheHits += metrics.spatialIndexCacheHitCount;
                        spatialIndexCacheMisses += metrics.spatialIndexCacheMissCount;
                        maxSpatialIndexCacheEntries = Math.Max(maxSpatialIndexCacheEntries, metrics.spatialIndexCacheEntryCount);
                        maxSpatialIndexCacheBytes = Math.Max(maxSpatialIndexCacheBytes, metrics.spatialIndexCacheBytes);
                        if (spatialIndexGeometryVersion == 0) spatialIndexGeometryVersion = metrics.spatialIndexGeometryVersion;
                        Require(metrics.spatialIndexGeometryVersion == spatialIndexGeometryVersion, "The geometry version changed during a scenario.");
                        if (parallelWorkerCount == 0) parallelWorkerCount = metrics.parallelWorkerCount;
                        Require(metrics.parallelWorkerCount == parallelWorkerCount, "The effective parallel worker count changed during a scenario.");
                        if (actualNeighborBatchSize == 0) actualNeighborBatchSize = metrics.neighborBatchSize;
                        Require(metrics.neighborBatchSize == actualNeighborBatchSize, "The effective neighbor batch size changed during a scenario.");
                        neighborBatchCount += metrics.neighborBatchCount;
                        temporaryNeighborPeakBytes = Math.Max(temporaryNeighborPeakBytes, metrics.temporaryNeighborPeakBytes);
                        if (temporaryNeighborBudgetBytes == 0)
                        {
                            temporaryNeighborBudgetBytes = metrics.temporaryNeighborBudgetBytes;
                        }

                        Require(metrics.temporaryNeighborBudgetBytes == temporaryNeighborBudgetBytes, "The temporary-neighbor budget changed during a scenario.");
                        Require(metrics.temporaryNeighborPeakBytes <= metrics.temporaryNeighborBudgetBytes * 2, "The temporary-neighbor allocation exceeded the bounded-capacity guard.");
                        Require(metrics.spatialIndexCacheEntryCount >= 1 && metrics.spatialIndexCacheEntryCount <= 2, "The spatial-index cache must contain one or two entries.");
                        Require(metrics.storedValueCount == metrics.generatedPointCount * definition.TimelineLength, "The contiguous value buffer has an unexpected size.");
                        Require(metrics.storedWeightCount == metrics.generatedPointCount, "Weights must be stored once per generated point.");

                        SurfaceGenerator output = new();
                        outputs.Add(output);
                        output.Initialize(generator, surface);
                        Measure(process, () => output.ComputeActivityUV(definition.TimelineLength / 2, 0.25f), out double columnDisplayWall, out double columnDisplayCpu);
                        displayWall += columnDisplayWall;
                        displayCpu += columnDisplayCpu;
                        Vector2[] uvs = output.ActivityUV;
                        Require(uvs.Length == surface.NumberOfVertices, "Unexpected activity UV count.");
                        Require(uvs.All(value => float.IsFinite(value.x) && float.IsFinite(value.y)), "Activity UV output contains a non-finite value.");
                        checksum = Mix(checksum, Hash(uvs));

                        CutGenerator cutOutput = new();
                        cutOutputs.Add(cutOutput);
                        cutOutput.Initialize(generator, cutGeometry, blurFactor: 0);
                        cutOutput.FillTextureWithVolume(grayscale, 0.0f, 1.0f);
                        Color32[] preparedPixels = null;
                        Measure(process, () =>
                        {
                            cutOutput.FillTextureWithActivity(activityColors, 0, 0.25f);
                            preparedPixels = cutOutput.CopyOverlayPixels();
                        }, out double columnCutPreparationWall, out double columnCutPreparationCpu);
                        cutPreparationWall += columnCutPreparationWall;
                        cutPreparationCpu += columnCutPreparationCpu;
                        Require(preparedPixels.Length == cutGeometry.TextureSize.x * cutGeometry.TextureSize.y, "Unexpected prepared cut pixel count.");

                        const int updateCount = 512;
                        Color32[] updatedPixels = null;
                        Measure(process, () =>
                        {
                            for (int update = 0; update < updateCount; ++update)
                            {
                                int timelineIndex = update % definition.TimelineLength;
                                long fillStart = Stopwatch.GetTimestamp();
                                cutOutput.FillTextureWithActivity(activityColors, timelineIndex, 0.25f);
                                cutTimelineFillsWall += ElapsedMilliseconds(fillStart, Stopwatch.GetTimestamp());
                                long copyStart = Stopwatch.GetTimestamp();
                                updatedPixels = cutOutput.CopyOverlayPixels();
                                cutTimelineCopiesWall += ElapsedMilliseconds(copyStart, Stopwatch.GetTimestamp());
                            }
                        }, out double columnCutUpdatesWall, out double columnCutUpdatesCpu);
                        cutTimelineUpdatesWall += columnCutUpdatesWall;
                        cutTimelineUpdatesCpu += columnCutUpdatesCpu;
                        cutTimelineUpdateCount += updateCount;
                        checksum = Mix(checksum, Hash(updatedPixels));
                    }

                    if (definition.MeasureExport)
                    {
                        Directory.CreateDirectory(exportRoot);
                        exportPath = Path.Combine(exportRoot, $"{definition.Name}-r{repetition}.nii");
                        IEEGGenerator generator = generators[generators.Count - 1];
                        bool saved = false;
                        Measure(process, () => saved = generator.SaveActivityAsNifti(exportPath, definition.TimelineLength, 100.0f, 0.0f, "hbp_core projection load benchmark"), out exportWall, out exportCpu);
                        Require(saved && File.Exists(exportPath), "Activity NIfTI export failed.");
                        exportFileBytes = new FileInfo(exportPath).Length;
                    }

                    sampler.SampleNow();
                    NativeProjectionProcessMemorySnapshot steadyMemory = NativeProjectionProcessMemory.Read(process);
                    steadyPrivate = steadyMemory.PrivateBytes;
                    steadyWorkingSet = steadyMemory.WorkingSetBytes;
                    totalEnd = Stopwatch.GetTimestamp();
                    cpuEnd = process.TotalProcessorTime;
                }
                finally
                {
                    sampler.Stop();
                    for (int i = cutOutputs.Count - 1; i >= 0; --i) cutOutputs[i].Dispose();
                    for (int i = outputs.Count - 1; i >= 0; --i) outputs[i].Dispose();
                    for (int i = generators.Count - 1; i >= 0; --i) generators[i].Dispose();
                    cutGeometry?.Dispose();
                    cut?.Dispose();
                    projectionGrid?.Dispose();
                    cutOutputs.Clear();
                    outputs.Clear();
                    generators.Clear();
                    projectionGrid = null;
                    cutGeometry = null;
                    cut = null;
                }

                peakPrivate = sampler.PeakPrivateBytes;
                peakWorkingSet = sampler.PeakWorkingSetBytes;
            }

            if (!string.IsNullOrEmpty(exportPath) && File.Exists(exportPath)) File.Delete(exportPath);
            ForceCollection();
            NativeProjectionProcessMemorySnapshot retainedMemory = NativeProjectionProcessMemory.Read(process);
            retainedPrivate = retainedMemory.PrivateBytes;
            retainedWorkingSet = retainedMemory.WorkingSetBytes;

            double phaseSum = allocation + spatialIndex + neighborQuery + accumulation + normalization;
            double unattributed = Math.Max(0.0, nativeTotal - phaseSum);
            long estimatedBytes = checked((storedValues + storedWeights) * sizeof(float));
            Require(spatialIndexCacheHits + spatialIndexCacheMisses == definition.ColumnCount, "Every spatial-index lookup must be reported as a hit or miss.");
            return new NativeProjectionLoadSampleResult
            {
                repetition = repetition,
                totalWallMilliseconds = ElapsedMilliseconds(totalStart, totalEnd),
                totalCpuMilliseconds = Math.Max(0.0, (cpuEnd - cpuStart).TotalMilliseconds),
                projectionGridWallMilliseconds = projectionGridWall,
                projectionGridCpuMilliseconds = projectionGridCpu,
                computeWallMilliseconds = computeWall,
                computeCpuMilliseconds = computeCpu,
                displayUpdateWallMilliseconds = displayWall,
                displayUpdateCpuMilliseconds = displayCpu,
                cutPreparationWallMilliseconds = cutPreparationWall,
                cutPreparationCpuMilliseconds = cutPreparationCpu,
                cutTimelineUpdatesWallMilliseconds = cutTimelineUpdatesWall,
                cutTimelineUpdatesCpuMilliseconds = cutTimelineUpdatesCpu,
                meanCutTimelineUpdateWallMilliseconds = cutTimelineUpdateCount > 0 ? cutTimelineUpdatesWall / cutTimelineUpdateCount : 0.0,
                meanCutTimelineUpdateCpuMilliseconds = cutTimelineUpdateCount > 0 ? cutTimelineUpdatesCpu / cutTimelineUpdateCount : 0.0,
                meanCutTimelineFillWallMilliseconds = cutTimelineUpdateCount > 0 ? cutTimelineFillsWall / cutTimelineUpdateCount : 0.0,
                meanCutTimelineCopyWallMilliseconds = cutTimelineUpdateCount > 0 ? cutTimelineCopiesWall / cutTimelineUpdateCount : 0.0,
                cutTimelineUpdateCount = cutTimelineUpdateCount,
                exportWallMilliseconds = exportWall,
                exportCpuMilliseconds = exportCpu,
                nativeTotalMilliseconds = nativeTotal,
                allocationMilliseconds = allocation,
                spatialIndexMilliseconds = spatialIndex,
                spatialIndexBuildMilliseconds = spatialIndexBuild,
                spatialIndexLookupMilliseconds = spatialIndexLookup,
                neighborQueryMilliseconds = neighborQuery,
                accumulationMilliseconds = accumulation,
                normalizationMilliseconds = normalization,
                nativeUnattributedMilliseconds = unattributed,
                nativePhaseCoverage = nativeTotal > 0.0 ? Math.Min(1.0, phaseSum / nativeTotal) : 0.0,
                generatedPointCount = generatedPoints,
                activeSiteCount = activeSites,
                neighborLinkCount = neighborLinks,
                storedValueCount = storedValues,
                storedWeightCount = storedWeights,
                spatialIndexCacheHitCount = spatialIndexCacheHits,
                spatialIndexCacheMissCount = spatialIndexCacheMisses,
                maxSpatialIndexCacheEntryCount = maxSpatialIndexCacheEntries,
                maxSpatialIndexCacheBytes = maxSpatialIndexCacheBytes,
                spatialIndexGeometryVersion = spatialIndexGeometryVersion,
                parallelWorkerCount = parallelWorkerCount,
                neighborBatchSize = actualNeighborBatchSize,
                neighborBatchCount = neighborBatchCount,
                temporaryNeighborPeakBytes = temporaryNeighborPeakBytes,
                temporaryNeighborBudgetBytes = temporaryNeighborBudgetBytes,
                baselinePrivateBytes = baselinePrivate,
                baselineWorkingSetBytes = baselineWorkingSet,
                peakPrivateBytesDelta = Math.Max(0L, peakPrivate - baselinePrivate),
                peakWorkingSetBytesDelta = Math.Max(0L, peakWorkingSet - baselineWorkingSet),
                steadyPrivateBytesDelta = Math.Max(0L, steadyPrivate - baselinePrivate),
                steadyWorkingSetBytesDelta = Math.Max(0L, steadyWorkingSet - baselineWorkingSet),
                retainedPrivateBytesDelta = Math.Max(0L, retainedPrivate - baselinePrivate),
                retainedWorkingSetBytesDelta = Math.Max(0L, retainedWorkingSet - baselineWorkingSet),
                managedActivityInputBytes = checked((long)activity.Length * sizeof(float)),
                estimatedCurrentValueAndWeightBytes = estimatedBytes,
                cutTexturePixelCount = cutTexturePixelCount,
                estimatedCutStencilPayloadBytes = estimatedCutStencilPayloadBytes,
                exportFileBytes = exportFileBytes,
                cacheFileBytes = 0,
                checksum = checksum.ToString("X16"),
                validationPassed = true,
                validationMessage = $"Finite surface UVs; native phase coverage {phaseSum / Math.Max(nativeTotal, double.Epsilon):P1}."
            };
        }

        private static RawSiteList CreateSites(Volume volume, int siteCount)
        {
            using BBox boundingBox = volume.BoundingBox;
            Vector3 nativeA = Vec3.FromVector3(boundingBox.Min).ToVector3(convertReferenceSystem: false);
            Vector3 nativeB = Vec3.FromVector3(boundingBox.Max).ToVector3(convertReferenceSystem: false);
            Vector3 min = Vector3.Min(nativeA, nativeB);
            Vector3 max = Vector3.Max(nativeA, nativeB);
            Vector3 margin = (max - min) * 0.05f;
            min += margin;
            max -= margin;

            RawSiteList sites = new();
            try
            {
                for (int i = 0; i < siteCount; ++i)
                {
                    int sequenceIndex = i + 1;
                    Vector3 nativePosition = new(Mathf.Lerp(min.x, max.x, Halton(sequenceIndex, 2)), Mathf.Lerp(min.y, max.y, Halton(sequenceIndex, 3)), Mathf.Lerp(min.z, max.z, Halton(sequenceIndex, 5)));
                    sites.AddSite($"S{i}", nativePosition, i / 100, i % 100);
                    sites.UpdateMask(i, false);
                }

                return sites;
            }
            catch
            {
                sites.Dispose();
                throw;
            }
        }

        private static float[] CreateActivity(int siteCount, int timelineLength)
        {
            float[] activity = new float[checked(siteCount * timelineLength)];
            for (int timeline = 0; timeline < timelineLength; ++timeline)
            {
                for (int site = 0; site < siteCount; ++site)
                {
                    activity[timeline * siteCount + site] = SyntheticTimeSeriesFactory.ValueAt(0, site, 0, timeline);
                }
            }

            return activity;
        }

        private static Color32[] CreateColorScheme(bool grayscale)
        {
            Color32[] colors = new Color32[256];
            for (int index = 0; index < colors.Length; ++index)
            {
                byte value = (byte)index;
                colors[index] = grayscale ? new Color32(value, value, value, 255) : new Color32(value, (byte)(255 - value), (byte)(value / 2), 255);
            }

            return colors;
        }

        private static float Halton(int index, int radix)
        {
            float result = 0.0f;
            float fraction = 1.0f / radix;
            while (index > 0)
            {
                result += fraction * (index % radix);
                index /= radix;
                fraction /= radix;
            }

            return result;
        }

        private static Surface LoadSurface(string path)
        {
            Surface surface = new();
            if (!surface.LoadOBJFile(path))
            {
                surface.Dispose();
                throw new InvalidOperationException($"Could not load MNI surface {path}.");
            }

            return surface;
        }

        private static int LoadSurfaceVertexCount(string path)
        {
            using Surface surface = LoadSurface(path);
            return surface.NumberOfVertices;
        }

        private static Volume LoadVolume(string path)
        {
            Volume volume = new();
            if (!volume.LoadNIFTIFile(path))
            {
                volume.Dispose();
                throw new InvalidOperationException($"Could not load MNI volume {path}.");
            }

            return volume;
        }

        private static void Measure(Process process, Action action, out double wallMilliseconds, out double cpuMilliseconds)
        {
            process.Refresh();
            TimeSpan cpuStart = process.TotalProcessorTime;
            long wallStart = Stopwatch.GetTimestamp();
            action();
            long wallEnd = Stopwatch.GetTimestamp();
            process.Refresh();
            TimeSpan cpuEnd = process.TotalProcessorTime;
            wallMilliseconds = ElapsedMilliseconds(wallStart, wallEnd);
            cpuMilliseconds = Math.Max(0.0, (cpuEnd - cpuStart).TotalMilliseconds);
        }

        private static double ElapsedMilliseconds(long start, long end)
        {
            return (end - start) * 1000.0 / Stopwatch.Frequency;
        }

        private static double Median(IEnumerable<double> values)
        {
            double[] sorted = values.OrderBy(value => value).ToArray();
            int middle = sorted.Length / 2;
            return sorted.Length % 2 == 0 ? (sorted[middle - 1] + sorted[middle]) * 0.5 : sorted[middle];
        }

        private static double Percentile95(IEnumerable<double> values)
        {
            double[] sorted = values.OrderBy(value => value).ToArray();
            int index = Math.Max(0, (int)Math.Ceiling(sorted.Length * 0.95) - 1);
            return sorted[index];
        }

        private static ulong Hash(Vector2[] values)
        {
            ulong checksum = (ulong)values.Length;
            int stride = Math.Max(1, values.Length / 64);
            for (int i = 0; i < values.Length; i += stride)
            {
                checksum = Mix(checksum, unchecked((ulong)(uint)Mathf.RoundToInt(values[i].x * 100000.0f)));
                checksum = Mix(checksum, unchecked((ulong)(uint)Mathf.RoundToInt(values[i].y * 100000.0f)));
            }

            return checksum;
        }

        private static ulong Hash(Color32[] values)
        {
            ulong checksum = (ulong)values.Length;
            int stride = Math.Max(1, values.Length / 64);
            for (int i = 0; i < values.Length; i += stride)
            {
                Color32 value = values[i];
                checksum = Mix(checksum, value.r);
                checksum = Mix(checksum, value.g);
                checksum = Mix(checksum, value.b);
                checksum = Mix(checksum, value.a);
            }

            return checksum;
        }

        private static ulong Mix(ulong seed, ulong value)
        {
            return (seed ^ value) * 1099511628211UL;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void ForceCollection()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
