#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_STANDALONE_WIN
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Data.Tools;
using HBP.Quest.Desktop;
using HBP.UI.Main;
using HBP.UI.Tools;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Profiling;

namespace HBP.Dev
{
    /// <summary>Opt-in evidence through the real Display window, including automatic publication.</summary>
    public static class SceneDisplayDiagnostic
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            string[] args = Environment.GetCommandLineArgs();
            int output = Array.IndexOf(args, "-displaySyncEvidence");
            int project = Array.IndexOf(args, "-displaySyncProject");
            if (output < 0 || project < 0 || output + 1 >= args.Length || project + 1 >= args.Length) return;
            int selection = Array.IndexOf(args, "-displaySyncVisualizations");
            int endpoint = Array.IndexOf(args, "-displaySyncEndpoint");
            int hold = Array.IndexOf(args, "-displaySyncHoldSeconds");
            int passes = Array.IndexOf(args, "-displaySyncPasses");
            int passCount = passes >= 0 && passes + 1 < args.Length && int.TryParse(args[passes + 1], out int count) ? count : 2;
            bool requirePatientMesh = args.Contains("-displaySyncPatientMesh");
            bool openPatientFromSite = args.Contains("-displaySyncOpenPatient");
            int holdSeconds = hold >= 0 && hold + 1 < args.Length && int.TryParse(args[hold + 1], out int seconds) ? seconds : 0;
            string[] names = selection >= 0 && selection + 1 < args.Length ? args[selection + 1].Split(',') : null;
            string address = endpoint >= 0 && endpoint + 1 < args.Length ? args[endpoint + 1] : null;
            StartupWork = RunFromCommandLineAsync(args[project + 1], args[output + 1], args.Contains("-displaySyncEvidenceOnce"), names, address, holdSeconds, requirePatientMesh, passCount, openPatientFromSite).AsTask();
        }

        // Retain startup work; the wrapper observes failure and closes only an explicitly requested run.
        public static System.Threading.Tasks.Task StartupWork { get; private set; }

        private static async UniTask RunFromCommandLineAsync(string project, string directory, bool quit, string[] names, string endpoint, int holdSeconds, bool requirePatientMesh, int passes, bool openPatientFromSite)
        {
            int code = 0;
            try
            {
                await RunAsync(project, directory, passes: passes, visualizationNames: names, preferredEndpoint: endpoint, holdSeconds: holdSeconds, requirePatientMesh: requirePatientMesh, openPatientFromSite: openPatientFromSite);
            }
            catch (Exception error)
            {
                code = 1;
                UnityEngine.Debug.LogException(error);
            }
            finally
            {
                if (quit) Application.Quit(code);
            }
        }

        public static async UniTask RunAsync(string projectPath, string directory, int passes = 2, bool requireQuest = true, string[] visualizationNames = null, string preferredEndpoint = null, int holdSeconds = 0, bool requirePatientMesh = false, bool openPatientFromSite = false)
        {
            if (passes < 1 || passes > 10) throw new ArgumentOutOfRangeException(nameof(passes));
            if (holdSeconds < 0 || holdSeconds > 60) throw new ArgumentOutOfRangeException(nameof(holdSeconds));
            await UniTask.SwitchToMainThread();
            if (Module3DMain.IsInitialized && Module3DMain.Scenes.Any())
                throw new InvalidOperationException("Close existing visualizations before starting the Display diagnostic.");
            Directory.CreateDirectory(directory);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Application.exitCancellationToken);
            timeout.CancelAfter(TimeSpan.FromMinutes(10));
            var timings = new ConcurrentQueue<string>();

            void Log(string message, string stack, LogType type)
            {
                if (message.StartsWith("QUEST-PAIRING ", StringComparison.Ordinal) && timings.Count < 4096) timings.Enqueue(message);
            }

            Application.logMessageReceivedThreaded += Log;
            var results = new JArray();
            PerformanceManager performance = null;
            bool previousSleepSuspension = false;
            try
            {
                await ApplicationManager.WaitForDatabaseInitializationAsync(timeout.Token);
                await UniTask.WaitUntil(() => PerformanceManager.IsInitialized, cancellationToken: timeout.Token);
                performance = UnityEngine.Object.FindAnyObjectByType<PerformanceManager>();
                previousSleepSuspension = performance.SleepModeSuspended;
                // A scripted Display produces no physical mouse/key activity. Use
                // the existing benchmark override rather than measure the 1 FPS idle mode.
                performance.SleepModeSuspended = true;
                // The shared project loader owns its cancellation presentation. Do not
                // detach that work with AttachExternalCancellation and start Display behind it.
                await ProjectLoaderSaver.LoadAsync(new ProjectInfo(projectPath));
                timeout.CancelAfter(TimeSpan.FromMinutes(10));
                await UniTask.SwitchToMainThread(timeout.Token);
                await UniTask.WaitUntil(() => Module3DMain.IsInitialized && QuestManager.IsInitialized, cancellationToken: timeout.Token);
                var manager = QuestManager.Instance;
                if (requireQuest)
                {
                    if (!string.IsNullOrWhiteSpace(preferredEndpoint))
                    {
                        await UniTask.WaitUntil(() => !manager.IsBusy, cancellationToken: timeout.Token);
                        manager.SelectManual(preferredEndpoint);
                        await manager.PairAsync(_ => UniTask.FromException<InputDialogResult>(new InvalidOperationException("The diagnostic requires a remembered association; pair manually before restarting it.")));
                        if (!manager.LastOperationSucceeded) throw new InvalidOperationException(manager.Status);
                    }

                    await UniTask.WaitUntil(() => manager.IsPaired && !manager.IsBusy, cancellationToken: timeout.Token);
                    if (!manager.AutomaticallySynchronizeScenes) throw new InvalidOperationException("Enable automatic scene synchronization before measuring Display.");
                }

                var visualizations = (visualizationNames ?? new[] { "Small" }).Select(name => ApplicationState.LoadedProject.Visualizations.Single(value => value.Name == name)).Distinct().ToArray();
                if (visualizations.Length == 0 || visualizations.Length > 16) throw new ArgumentException("Select between one and sixteen visualizations.");
                for (int pass = 1; pass <= passes; pass++)
                {
                    timeout.Token.ThrowIfCancellationRequested();
                    var window = WindowsManager.Open("Visualization gestion window", null) as VisualizationGestion;
                    if (window == null) throw new InvalidOperationException("The Visualization gestion window prefab is unavailable.");
                    window.ListGestion.List.ObjectsSelected = visualizations;
                    var clock = Stopwatch.StartNew();
                    double previous = 0, maximumGap = 0;
                    long peakMemory = Profiler.GetTotalAllocatedMemoryLong();
                    long managedBefore = GC.GetTotalMemory(false);
                    int frames = 0;
                    var frameGaps = new List<double>();
                    bool sampling = true;

                    async UniTask SampleAsync()
                    {
                        while (sampling)
                        {
                            await UniTask.NextFrame();
                            double now = clock.Elapsed.TotalMilliseconds;
                            maximumGap = Math.Max(maximumGap, now - previous);
                            if (frameGaps.Count < 65536) frameGaps.Add(now - previous);
                            previous = now;
                            if (++frames % 30 == 0) peakMemory = Math.Max(peakMemory, Profiler.GetTotalAllocatedMemoryLong());
                        }
                    }

                    var sampler = SampleAsync();
                    Base3DScene[] scenes = Array.Empty<Base3DScene>();
                    try
                    {
                        await window.DisplayAsync(timeout.Token, showResults: false);
                        double localReturnMs = clock.Elapsed.TotalMilliseconds;
                        scenes = visualizations.Select(visualization => Module3DMain.Scenes.Single(value => value.Visualization == visualization)).ToArray();
                        foreach (var scene in scenes) await scene.PrepareRenderingAsync(timeout.Token);
                        double desktopReadyMs = clock.Elapsed.TotalMilliseconds;
                        if (requireQuest)
                        {
                            await UniTask.WaitUntil(() => scenes.All(manager.IsSceneSynchronized) || scenes.Any(scene => manager.GetSceneSynchronizationError(scene) != null) || !manager.IsPaired, cancellationToken: timeout.Token);
                            if (!scenes.All(manager.IsSceneSynchronized)) throw new InvalidOperationException(scenes.Select(manager.GetSceneSynchronizationError).FirstOrDefault(value => value != null) ?? "Quest disconnected during Display.");
                        }

                        double bothReadyMs = clock.Elapsed.TotalMilliseconds;
                        JObject patientOpening = null;
                        if (openPatientFromSite)
                        {
                            var source = scenes.Single();
                            if (!requireQuest || source.Type != SceneType.MultiPatients)
                                throw new InvalidOperationException("The patient benchmark needs a synchronized multipatient source.");

                            var target = source.Columns.SelectMany(column => column.Sites.Select(site => (column, site))).FirstOrDefault(item => item.site.Information.Patient.ID == source.Visualization.Patients[0].ID && !item.site.State.IsMasked);
                            if (target.site == null) throw new InvalidOperationException("The first patient has no visible site in the benchmark source.");
                            Module3DMain.SelectSceneContext(source, target.column);
                            source.SelectSite(target.column, target.site);
                            var elapsed = Stopwatch.StartNew();
                            var child = await manager.OpenPatientFromDesktopAsync(source, target.site.Information.Patient, timeout.Token, showResults: false);
                            double first = elapsed.Elapsed.TotalMilliseconds;
                            scenes = new[] { child, source };
                            await UniTask.NextFrame(cancellationToken: timeout.Token);
                            Module3DMain.SelectSceneContext(source, target.column);
                            source.SelectSite(target.column, target.site);
                            elapsed.Restart();
                            var reopened = await manager.OpenPatientFromDesktopAsync(source, target.site.Information.Patient, timeout.Token, showResults: false);
                            double repeated = elapsed.Elapsed.TotalMilliseconds;
                            if (reopened != child || !manager.IsSceneSynchronized(child) || !manager.IsSceneSynchronized(source) || child.MeshManager.SelectedMesh.Type != MeshType.Patient || child.MeshManager.SelectedMesh is RuntimeSingleMesh3D)
                                throw new InvalidOperationException("The benchmark requires the same synchronized opening and a persistent patient mesh.");

                            patientOpening = new JObject
                            {
                                ["firstMs"] = first, ["reselectionMs"] = repeated,
                                ["vertices"] = child.MeshManager.BrainSurface.NumberOfVertices,
                                ["triangles"] = child.MeshManager.BrainSurface.NumberOfTriangles
                            };
                        }

                        if (requirePatientMesh && scenes.Any(scene => scene.MeshManager.SelectedMesh.Type != MeshType.Patient || scene.MeshManager.SelectedMesh is RuntimeSingleMesh3D))
                            throw new InvalidOperationException("The diagnostic requires a persistent patient mesh to be selected.");
                        var selectedAnatomy = new JArray(scenes.Select(scene =>
                        {
                            var mesh = scene.MeshManager.SelectedMesh;
                            var surface = scene.MeshManager.BrainSurface;
                            return new JObject
                            {
                                ["meshType"] = mesh.Type.ToString(), ["representation"] = mesh.Representation.ToString(),
                                ["hemispheres"] = mesh.SupportsHemispheres, ["inflated"] = mesh.HasInflatedRepresentation,
                                ["vertices"] = surface.NumberOfVertices, ["triangles"] = surface.NumberOfTriangles
                            };
                        }));
                        double[] intervals = frameGaps.OrderBy(value => value).ToArray();
                        double Percentile(double fraction) => intervals.Length == 0 ? 0 : intervals[(int)((intervals.Length - 1) * fraction)];
                        results.Add(new JObject
                        {
                            ["pass"] = pass, ["sameSessionReopen"] = pass > 1,
                            ["anatomy"] = selectedAnatomy, ["patientOpening"] = patientOpening,
                            ["localDisplayReturnMs"] = localReturnMs, ["desktopReadyMs"] = desktopReadyMs,
                            ["desktopAndPublicationMs"] = bothReadyMs, ["maximumFrameGapMs"] = maximumGap,
                            ["frameGapP50Ms"] = Percentile(.50), ["frameGapP95Ms"] = Percentile(.95), ["frameGapP99Ms"] = Percentile(.99),
                            ["frameGapsOver100Ms"] = frameGaps.Count(value => value > 100), ["frameSamplesDropped"] = Math.Max(0, frames - frameGaps.Count),
                            ["frames"] = frames, ["unityAllocatedPeakBytes"] = peakMemory,
                            ["managedLiveBytesBefore"] = managedBefore, ["managedLiveBytesAfter"] = GC.GetTotalMemory(false)
                        });
                    }
                    catch
                    {
                        // This diagnostic owns the opening, but QuestManager owns its
                        // automatic send. Ask that owner to cancel and drain before returning.
                        foreach (var scene in Module3DMain.Scenes.Where(scene => visualizations.Contains(scene.Visualization)).ToArray())
                            await manager.CancelSceneSynchronizationAsync(scene);
                        throw;
                    }
                    finally
                    {
                        sampling = false;
                        await sampler;
                    }

                    if (pass == passes && holdSeconds > 0)
                        await UniTask.Delay(TimeSpan.FromSeconds(holdSeconds), cancellationToken: timeout.Token);
                    foreach (var scene in scenes) await Module3DMain.RemoveSceneAsync(scene);
                    await UniTask.Delay(TimeSpan.FromSeconds(2), cancellationToken: timeout.Token);
                }

                File.WriteAllText(Path.Combine(directory, "display.json"), new JObject
                {
                    ["success"] = true, ["unityVersion"] = Application.unityVersion,
                    ["platform"] = Application.platform.ToString(), ["buildGuid"] = Application.buildGUID,
                    ["editorPreliminary"] = Application.isEditor, ["questRequired"] = requireQuest,
                    ["visualizations"] = new JArray(visualizations.Select(value => value.Name)), ["columns"] = visualizations.Sum(value => value.Columns.Count),
                    ["patients"] = visualizations.SelectMany(value => value.Patients).Distinct().Count(), ["runs"] = results,
                    ["preferredEndpoint"] = preferredEndpoint,
                    ["requestedFormat"] = Environment.GetCommandLineArgs().Contains("-sceneTransferJson") ? "json" : Environment.GetCommandLineArgs().Contains("-sceneTransferRecording") ? "recording" : Environment.GetCommandLineArgs().Contains("-sceneTransferColumn") ? "column" : "automatic",
                    ["overlapCapture"] = Environment.GetCommandLineArgs().Contains("-sceneTransferOverlap"),
                    ["memorySamplingFrames"] = 30,
                    ["desktopIdleThrottlingSuspended"] = true,
                    ["targetFrameRate"] = Application.targetFrameRate,
                    ["displayAwaitIncludesQuestSynchronization"] = true,
                    ["displayReturnMeaning"] = "The historical localDisplayReturnMs field now awaits the unified Display operation, including Quest when connected at admission.",
                    ["readinessBoundary"] = "Local rendering prepared, initial publication applied, scientific rendering acknowledged for the same projection policy, and Quest batch interactions released.",
                    ["memoryMeaning"] = "Unity allocated memory and managed live bytes; neither is a total allocation counter."
                }.ToString());
            }
            catch (Exception error)
            {
                File.WriteAllText(Path.Combine(directory, "failure.txt"), error.ToString());
                throw;
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                if (performance != null) performance.SleepModeSuspended = previousSleepSuspension;
                Application.logMessageReceivedThreaded -= Log;
                File.WriteAllLines(Path.Combine(directory, "pairing-timings.log"), timings.ToArray());
            }
        }
    }
}
#endif
