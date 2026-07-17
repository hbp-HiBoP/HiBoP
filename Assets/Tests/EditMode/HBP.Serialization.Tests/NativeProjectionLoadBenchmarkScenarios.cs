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
        public static List<NativeProjectionLoadScenarioDefinition> Build(
            string profile,
            int timelineLength,
            bool includeExport,
            string filter)
        {
            List<NativeProjectionLoadScenarioDefinition> scenarios = new();
            HashSet<string> keys = new(StringComparer.OrdinalIgnoreCase);

            void Add(string prefix, int dimension, int sites, float radius, int columns = 1, bool export = false)
            {
                string name = $"{prefix}.d{dimension}.s{sites}.t{timelineLength}.r{radius:R}.c{columns}";
                if (!string.IsNullOrWhiteSpace(filter)
                    && name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return;
                }
                string key = $"{dimension}:{sites}:{timelineLength}:{radius:R}:{columns}:{export}";
                if (!keys.Add(key)) return;
                scenarios.Add(new NativeProjectionLoadScenarioDefinition(
                    name,
                    dimension,
                    sites,
                    timelineLength,
                    radius,
                    columns,
                    export));
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
            Add("projection.product-reference", 120, 25000, 15.0f);
            Add("projection.multicolumn", 96, 5000, 15.0f, columns: 3);
            if (includeExport) Add("projection.export", 96, 5000, 15.0f, export: true);

            if (profile.Equals("Extreme", StringComparison.OrdinalIgnoreCase))
            {
                Add("projection.extreme-radius", 120, 25000, 30.0f);
                Add("projection.extreme-radius", 120, 25000, 50.0f);
                Add("projection.extreme-multicolumn", 120, 25000, 15.0f, columns: 3);
            }
            return scenarios;
        }

        public static NativeProjectionLoadScenarioResult Run(
            NativeProjectionLoadScenarioDefinition definition,
            string surfacePath,
            string volumePath,
            string exportRoot,
            int repetitions)
        {
            NativeProjectionLoadScenarioResult result = new()
            {
                name = definition.Name,
                dimension = definition.Dimension,
                siteCount = definition.SiteCount,
                timelineLength = definition.TimelineLength,
                influenceDistance = definition.InfluenceDistance,
                columnCount = definition.ColumnCount,
                exportMeasured = definition.MeasureExport,
                workload = definition.Workload
            };

            for (int repetition = 0; repetition < repetitions; ++repetition)
            {
                result.samples.Add(RunSample(definition, surfacePath, volumePath, exportRoot, repetition));
            }

            NativeProjectionLoadSampleResult first = result.samples[0];
            Require(result.samples.All(sample => sample.checksum == first.checksum),
                $"Scenario {definition.Name} produced different checksums across repetitions.");
            result.surfaceVertexCount = LoadSurfaceVertexCount(surfacePath);
            result.generatedPointCount = first.generatedPointCount;
            result.activeSiteCount = first.activeSiteCount;
            result.neighborLinkCount = first.neighborLinkCount;
            result.estimatedCurrentValueAndWeightBytes = first.estimatedCurrentValueAndWeightBytes;
            result.medianTotalWallMilliseconds = Median(result.samples.Select(sample => sample.totalWallMilliseconds));
            result.medianTotalCpuMilliseconds = Median(result.samples.Select(sample => sample.totalCpuMilliseconds));
            result.medianComputeWallMilliseconds = Median(result.samples.Select(sample => sample.computeWallMilliseconds));
            result.medianComputeCpuMilliseconds = Median(result.samples.Select(sample => sample.computeCpuMilliseconds));
            result.maxPeakPrivateBytesDelta = result.samples.Max(sample => sample.peakPrivateBytesDelta);
            result.maxPeakWorkingSetBytesDelta = result.samples.Max(sample => sample.peakWorkingSetBytesDelta);
            result.maxRetainedPrivateBytesDelta = result.samples.Max(sample => sample.retainedPrivateBytesDelta);
            result.maxRetainedWorkingSetBytesDelta = result.samples.Max(sample => sample.retainedWorkingSetBytesDelta);
            return result;
        }

        private static NativeProjectionLoadSampleResult RunSample(
            NativeProjectionLoadScenarioDefinition definition,
            string surfacePath,
            string volumePath,
            string exportRoot,
            int repetition)
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
            Require(baselinePrivate > 0 && baselineWorkingSet > 0,
                "Process memory counters must be available for the projection benchmark.");
            TimeSpan cpuStart = process.TotalProcessorTime;
            long totalStart = Stopwatch.GetTimestamp();
            long totalEnd = totalStart;
            TimeSpan cpuEnd = cpuStart;
            GeneratorSurface generatorSurface = null;
            List<IEEGGenerator> generators = new();
            List<SurfaceGenerator> outputs = new();
            long exportFileBytes = 0;
            string exportPath = null;

            double generatorSurfaceWall = 0.0;
            double generatorSurfaceCpu = 0.0;
            double computeWall = 0.0;
            double computeCpu = 0.0;
            double displayWall = 0.0;
            double displayCpu = 0.0;
            double exportWall = 0.0;
            double exportCpu = 0.0;
            double nativeTotal = 0.0;
            double allocation = 0.0;
            double spatialIndex = 0.0;
            double neighborQuery = 0.0;
            double accumulation = 0.0;
            double normalization = 0.0;
            long generatedPoints = 0;
            long activeSites = 0;
            long neighborLinks = 0;
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
                    generatorSurface = new GeneratorSurface();
                    Measure(process, () => generatorSurface.Initialize(surface, volume, definition.Dimension),
                        out generatorSurfaceWall, out generatorSurfaceCpu);

                    for (int column = 0; column < definition.ColumnCount; ++column)
                    {
                        IEEGGenerator generator = new();
                        generators.Add(generator);
                        generator.Initialize(generatorSurface);
                        generator.EnablePerformanceMetrics(true);

                        Measure(process, () => generator.ComputeActivity(
                                sites,
                                definition.InfluenceDistance,
                                activity,
                                definition.TimelineLength,
                                definition.SiteCount,
                                SiteInfluenceByDistanceType.Linear),
                            out double columnComputeWall,
                            out double columnComputeCpu);
                        computeWall += columnComputeWall;
                        computeCpu += columnComputeCpu;

                        IEEGComputeMetrics metrics = generator.GetLastComputeMetrics();
                        nativeTotal += metrics.totalMilliseconds;
                        allocation += metrics.allocationMilliseconds;
                        spatialIndex += metrics.spatialIndexMilliseconds;
                        neighborQuery += metrics.neighborQueryMilliseconds;
                        accumulation += metrics.accumulationMilliseconds;
                        normalization += metrics.normalizationMilliseconds;
                        generatedPoints = metrics.generatedPointCount;
                        activeSites += metrics.activeSiteCount;
                        neighborLinks += metrics.neighborLinkCount;

                        SurfaceGenerator output = new();
                        outputs.Add(output);
                        output.Initialize(generator);
                        Measure(process, () => output.ComputeActivityUV(definition.TimelineLength / 2, 0.25f),
                            out double columnDisplayWall,
                            out double columnDisplayCpu);
                        displayWall += columnDisplayWall;
                        displayCpu += columnDisplayCpu;
                        Vector2[] uvs = output.ActivityUV;
                        Require(uvs.Length == surface.NumberOfVertices, "Unexpected activity UV count.");
                        Require(uvs.All(value => float.IsFinite(value.x) && float.IsFinite(value.y)),
                            "Activity UV output contains a non-finite value.");
                        checksum = Mix(checksum, Hash(uvs));
                    }

                    if (definition.MeasureExport)
                    {
                        Directory.CreateDirectory(exportRoot);
                        exportPath = Path.Combine(exportRoot, $"{definition.Name}-r{repetition}.nii");
                        IEEGGenerator generator = generators[generators.Count - 1];
                        bool saved = false;
                        Measure(process, () => saved = generator.SaveActivityAsNifti(
                                exportPath,
                                definition.TimelineLength,
                                100.0f,
                                0.0f,
                                "hbp_core projection load benchmark"),
                            out exportWall,
                            out exportCpu);
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
                    for (int i = outputs.Count - 1; i >= 0; --i) outputs[i].Dispose();
                    for (int i = generators.Count - 1; i >= 0; --i) generators[i].Dispose();
                    generatorSurface?.Dispose();
                    outputs.Clear();
                    generators.Clear();
                    generatorSurface = null;
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
            long estimatedBytes = checked(2L * generatedPoints * definition.TimelineLength * sizeof(float) * definition.ColumnCount);
            return new NativeProjectionLoadSampleResult
            {
                repetition = repetition,
                totalWallMilliseconds = ElapsedMilliseconds(totalStart, totalEnd),
                totalCpuMilliseconds = Math.Max(0.0, (cpuEnd - cpuStart).TotalMilliseconds),
                generatorSurfaceWallMilliseconds = generatorSurfaceWall,
                generatorSurfaceCpuMilliseconds = generatorSurfaceCpu,
                computeWallMilliseconds = computeWall,
                computeCpuMilliseconds = computeCpu,
                displayUpdateWallMilliseconds = displayWall,
                displayUpdateCpuMilliseconds = displayCpu,
                exportWallMilliseconds = exportWall,
                exportCpuMilliseconds = exportCpu,
                nativeTotalMilliseconds = nativeTotal,
                allocationMilliseconds = allocation,
                spatialIndexMilliseconds = spatialIndex,
                neighborQueryMilliseconds = neighborQuery,
                accumulationMilliseconds = accumulation,
                normalizationMilliseconds = normalization,
                nativeUnattributedMilliseconds = unattributed,
                nativePhaseCoverage = nativeTotal > 0.0 ? Math.Min(1.0, phaseSum / nativeTotal) : 0.0,
                generatedPointCount = generatedPoints,
                activeSiteCount = activeSites,
                neighborLinkCount = neighborLinks,
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
                    Vector3 nativePosition = new(
                        Mathf.Lerp(min.x, max.x, Halton(sequenceIndex, 2)),
                        Mathf.Lerp(min.y, max.y, Halton(sequenceIndex, 3)),
                        Mathf.Lerp(min.z, max.z, Halton(sequenceIndex, 5)));
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
                    activity[timeline * siteCount + site] =
                        (float)Math.Sin(site * 0.017 + timeline * 0.13)
                        + 0.25f * (float)Math.Cos(site * 0.007 - timeline * 0.19);
                }
            }
            return activity;
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
            return sorted.Length % 2 == 0
                ? (sorted[middle - 1] + sorted[middle]) * 0.5
                : sorted[middle];
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
