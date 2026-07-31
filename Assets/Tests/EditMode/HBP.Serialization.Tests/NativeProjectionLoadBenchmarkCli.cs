using System;
using System.Collections.Generic;
using System.IO;
using HBP.Core.DLL;
using HBP.Core.Enums;
using UnityEditor;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    public static class NativeProjectionLoadBenchmarkCli
    {
        private const string OutputArgument = "-hbpProjectionOutput";
        private const string ProfileArgument = "-hbpProjectionProfile";
        private const string TimelineArgument = "-hbpProjectionTimeline";
        private const string RepetitionsArgument = "-hbpProjectionRepetitions";
        private const string WorkerCountArgument = "-hbpProjectionWorkers";
        private const string BatchSizeArgument = "-hbpProjectionBatchSites";
        private const string VolumeInterpolationArgument = "-hbpProjectionVolumeInterpolation";
        private const string FilterArgument = "-hbpProjectionFilter";
        private const string SurfaceArgument = "-hbpProjectionSurface";
        private const string VolumeArgument = "-hbpProjectionVolume";
        private const string IncludeExportArgument = "-hbpProjectionIncludeExport";

        public static void Run()
        {
            string outputPath = null;
            NativeProjectionLoadWorkerReport report = new();
            try
            {
                string[] arguments = Environment.GetCommandLineArgs();
                outputPath = RequireArgument(arguments, OutputArgument);
                string profile = RequireArgument(arguments, ProfileArgument);
                if (!profile.Equals("Smoke", StringComparison.OrdinalIgnoreCase) && !profile.Equals("Typical", StringComparison.OrdinalIgnoreCase) && !profile.Equals("Product", StringComparison.OrdinalIgnoreCase) && !profile.Equals("Extreme", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException($"Unsupported projection profile: {profile}.", ProfileArgument);
                }

                int timelineLength = ParsePositive(arguments, TimelineArgument);
                int repetitions = ParsePositive(arguments, RepetitionsArgument);
                int workerCount = ParseNonNegative(arguments, WorkerCountArgument);
                int batchSize = ParseNonNegative(arguments, BatchSizeArgument);
                VolumeInterpolation volumeInterpolation = ParseVolumeInterpolation(arguments);
                string filter = OptionalArgument(arguments, FilterArgument);
                string surfacePath = OptionalArgument(arguments, SurfaceArgument) ?? Path.Combine(Application.dataPath, "Data", "Meshes", "MNI_single_hight_Bhemi.obj");
                string volumePath = OptionalArgument(arguments, VolumeArgument) ?? Path.Combine(Application.dataPath, "Data", "IRM", "MNI.nii");
                bool includeExport = HasArgument(arguments, IncludeExportArgument);

                surfacePath = Path.GetFullPath(surfacePath);
                volumePath = Path.GetFullPath(volumePath);
                RequireFile(surfacePath, "MNI surface");
                RequireFile(volumePath, "MNI volume");

                report.profile = profile;
                report.startedUtc = DateTime.UtcNow.ToString("O");
                report.unityVersion = Application.unityVersion;
                report.operatingSystem = SystemInfo.operatingSystem;
                report.machineName = Environment.MachineName;
                report.processorCount = Environment.ProcessorCount;
                report.memorySamplingIntervalMilliseconds = NativeProjectionProcessMemorySampler.SampleIntervalMilliseconds;
                report.timelineLength = timelineLength;
                report.repetitions = repetitions;
                report.requestedParallelWorkerCount = workerCount;
                report.requestedNeighborBatchSize = batchSize;
                report.requestedVolumeInterpolation = volumeInterpolation.ToString();
                report.surfacePath = surfacePath;
                report.volumePath = volumePath;
                report.includeExport = includeExport;
                List<NativeProjectionLoadScenarioDefinition> scenarios = NativeProjectionLoadBenchmarkScenarios.Build(profile, timelineLength, includeExport, volumeInterpolation, filter);
                if (scenarios.Count == 0)
                {
                    throw new InvalidOperationException("The projection benchmark filter did not select any scenario.");
                }

                string exportRoot = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(outputPath)), "temporary-exports");
                foreach (NativeProjectionLoadScenarioDefinition scenario in scenarios)
                {
                    Debug.Log($"[HBP Projection Load] {scenario.Name}: {scenario.Workload}");
                    NativeProjectionLoadScenarioResult result = NativeProjectionLoadBenchmarkScenarios.Run(scenario, surfacePath, volumePath, exportRoot, repetitions, workerCount, batchSize);
                    report.scenarios.Add(result);
                    WriteReport(outputPath, report);
                }

                if (Directory.Exists(exportRoot) && Directory.GetFileSystemEntries(exportRoot).Length == 0)
                {
                    Directory.Delete(exportRoot);
                }

                report.succeeded = true;
            }
            catch (Exception exception)
            {
                report.succeeded = false;
                report.error = exception.ToString();
                Debug.LogException(exception);
            }
            finally
            {
                report.finishedUtc = DateTime.UtcNow.ToString("O");
                if (!string.IsNullOrWhiteSpace(outputPath)) WriteReport(outputPath, report);
                EditorApplication.Exit(report.succeeded ? 0 : 1);
            }
        }

        private static int ParsePositive(string[] arguments, string name)
        {
            string value = RequireArgument(arguments, name);
            if (!int.TryParse(value, out int parsed) || parsed <= 0)
            {
                throw new ArgumentException($"{name} must be a positive integer.");
            }

            return parsed;
        }

        private static int ParseNonNegative(string[] arguments, string name)
        {
            string value = RequireArgument(arguments, name);
            if (!int.TryParse(value, out int parsed) || parsed < 0)
            {
                throw new ArgumentException($"{name} must be a non-negative integer.");
            }

            return parsed;
        }

        private static VolumeInterpolation ParseVolumeInterpolation(string[] arguments)
        {
            string value = OptionalArgument(arguments, VolumeInterpolationArgument) ?? VolumeInterpolation.Nearest.ToString();
            if (!Enum.TryParse(value, ignoreCase: true, out VolumeInterpolation interpolation) || !Enum.IsDefined(typeof(VolumeInterpolation), interpolation))
            {
                throw new ArgumentException($"{VolumeInterpolationArgument} must be Nearest or Trilinear.");
            }

            return interpolation;
        }

        private static string RequireArgument(string[] arguments, string name)
        {
            string value = OptionalArgument(arguments, name);
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"Missing argument {name}.");
            return value;
        }

        private static string OptionalArgument(string[] arguments, string name)
        {
            for (int i = 0; i + 1 < arguments.Length; ++i)
            {
                if (arguments[i].Equals(name, StringComparison.OrdinalIgnoreCase)) return arguments[i + 1];
            }

            return null;
        }

        private static bool HasArgument(string[] arguments, string name)
        {
            foreach (string argument in arguments)
            {
                if (argument.Equals(name, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        private static void RequireFile(string path, string description)
        {
            if (!File.Exists(path)) throw new FileNotFoundException($"The {description} is missing.", path);
        }

        private static void WriteReport(string path, NativeProjectionLoadWorkerReport report)
        {
            string fullPath = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            string temporaryPath = fullPath + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(report, prettyPrint: true));
            if (File.Exists(fullPath)) File.Delete(fullPath);
            File.Move(temporaryPath, fullPath);
        }
    }
}
