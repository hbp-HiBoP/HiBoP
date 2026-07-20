using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HBP.Core.DLL;
using UnityEditor;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    public static class NativePerformanceBenchmarkCli
    {
        private const string BackendArgument = "-hbpBenchmarkBackend";
        private const string OutputArgument = "-hbpBenchmarkOutput";
        private const string FixtureRootArgument = "-hbpBenchmarkFixtureRoot";
        private const string WarmupArgument = "-hbpBenchmarkWarmup";
        private const string IterationsArgument = "-hbpBenchmarkIterations";
        private const string FilterArgument = "-hbpBenchmarkFilter";
        private const string IncludeVideoArgument = "-hbpBenchmarkIncludeVideo";

        public static void Run()
        {
            string outputPath = null;
            NativePerformanceWorkerReport report = new();
            int exitCode = 0;
            try
            {
                string[] arguments = Environment.GetCommandLineArgs();
                outputPath = RequireArgument(arguments, OutputArgument);
                string fixtureRoot = RequireArgument(arguments, FixtureRootArgument);
                string backendValue = RequireArgument(arguments, BackendArgument);
                int warmup = ParsePositive(arguments, WarmupArgument, minimum: 1);
                int iterations = ParsePositive(arguments, IterationsArgument, minimum: 20);
                HashSet<string> filters = ParseFilters(OptionalArgument(arguments, FilterArgument));
                BenchmarkBackend backend = ParseBackend(backendValue);

                report.backend = backend == BenchmarkBackend.HbpCore ? "hbp_core" : "hbp_export";
                report.startedUtc = DateTime.UtcNow.ToString("O");
                report.unityVersion = Application.unityVersion;
                report.operatingSystem = SystemInfo.operatingSystem;
                report.machineName = Environment.MachineName;
                report.processorCount = Environment.ProcessorCount;
                report.warmupIterations = warmup;
                report.measuredIterations = iterations;
                report.fixtureRoot = Path.GetFullPath(fixtureRoot);

                OracleBackendContext.Current = backend;
                NativePerformanceBenchmarkFixtures fixtures = new(fixtureRoot);
                fixtures.Ensure();
                bool includeVideo = HasArgument(arguments, IncludeVideoArgument);
                List<Func<NativePerformanceScenario>> factories = NativePerformanceBenchmarkScenarios.Build(
                    backend,
                    fixtures,
                    includeVideo,
                    (name, domain) => Matches(name, domain, filters));
                foreach (Func<NativePerformanceScenario> factory in factories)
                {
                    using NativePerformanceScenario scenario = factory();
                    Debug.Log($"[HBP Performance] {report.backend}: {scenario.Name}");
                    try
                    {
                        report.scenarios.Add(NativePerformanceMeasurement.Run(scenario, warmup, iterations));
                        WriteReport(outputPath, report);
                    }
                    catch (Exception exception)
                    {
                        report.scenarios.Add(new NativePerformanceScenarioResult
                        {
                            name = scenario.Name,
                            domain = scenario.Domain,
                            phase = scenario.Phase,
                            workload = scenario.Workload,
                            operationsPerIteration = scenario.OperationsPerIteration,
                            iterations = 0,
                            validationPassed = false,
                            validationMessage = exception.ToString(),
                            checksum = string.Empty,
                            millisecondsSamples = Array.Empty<double>(),
                            managedBytesSamples = Array.Empty<long>()
                        });
                        throw;
                    }
                }

                if (report.scenarios.Count == 0)
                {
                    throw new InvalidOperationException("The benchmark filter did not select any scenario.");
                }
                report.succeeded = true;
            }
            catch (Exception exception)
            {
                exitCode = 1;
                report.succeeded = false;
                report.error = exception.ToString();
                Debug.LogException(exception);
            }
            finally
            {
                report.finishedUtc = DateTime.UtcNow.ToString("O");
                OracleBackendContext.Reset();
                if (!string.IsNullOrWhiteSpace(outputPath))
                {
                    WriteReport(outputPath, report);
                }
                EditorApplication.Exit(exitCode);
            }
        }

        private static bool Matches(string name, string domain, HashSet<string> filters)
        {
            if (filters.Count == 0)
            {
                return true;
            }
            return filters.Any(filter =>
                name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                || domain.Equals(filter, StringComparison.OrdinalIgnoreCase));
        }

        private static HashSet<string> ParseFilters(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
            return new HashSet<string>(
                value.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(filter => filter.Trim()),
                StringComparer.OrdinalIgnoreCase);
        }

        private static BenchmarkBackend ParseBackend(string value)
        {
            if (value.Equals("hbp_core", StringComparison.OrdinalIgnoreCase)
                || value.Equals(nameof(BenchmarkBackend.HbpCore), StringComparison.OrdinalIgnoreCase))
            {
                return BenchmarkBackend.HbpCore;
            }
            if (value.Equals("hbp_export", StringComparison.OrdinalIgnoreCase)
                || value.Equals(nameof(BenchmarkBackend.HbpExport), StringComparison.OrdinalIgnoreCase))
            {
                return BenchmarkBackend.HbpExport;
            }
            throw new ArgumentException($"Unsupported benchmark backend: {value}.", BackendArgument);
        }

        private static int ParsePositive(string[] arguments, string name, int minimum)
        {
            string value = RequireArgument(arguments, name);
            if (!int.TryParse(value, out int parsed) || parsed < minimum)
            {
                throw new ArgumentException($"{name} must be an integer greater than or equal to {minimum}.");
            }
            return parsed;
        }

        private static string RequireArgument(string[] arguments, string name)
        {
            string value = OptionalArgument(arguments, name);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"Missing required command-line argument {name}.");
            }
            return value;
        }

        private static string OptionalArgument(string[] arguments, string name)
        {
            for (int i = 0; i + 1 < arguments.Length; ++i)
            {
                if (arguments[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[i + 1];
                }
            }
            return null;
        }

        private static bool HasArgument(string[] arguments, string name)
        {
            return arguments.Any(argument => argument.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private static void WriteReport(string path, NativePerformanceWorkerReport report)
        {
            string fullPath = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            string temporaryPath = fullPath + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(report, prettyPrint: true));
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            File.Move(temporaryPath, fullPath);
        }
    }
}
