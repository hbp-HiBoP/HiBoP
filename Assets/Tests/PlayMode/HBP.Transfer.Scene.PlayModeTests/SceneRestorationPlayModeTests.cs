#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Quest;
using HBP.Tests.PlayMode.Utilities;
using HBP.Transfer.Scene;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Event = HBP.Core.Data.Event;
using ProjectPreferences = HBP.Core.Data.ProjectPreferences;

namespace HBP.Tests.SceneTransfer
{
    public class SceneRestorationPlayModeTests
    {
        [TestCase(false)]
        [TestCase(true)]
        [Timeout(900000)]
        public async Task SnapshotConfiguration_OpensWithCommonPreferencesAndCalibrations(bool automatic)
        {
            using var temp = new PlayModeTempDirectoryScope();
            using var settings = new PlayModePersistentDataScope(temp.Path);
            using var scope = new PlayModeSceneScope("SnapshotConfigurationParity");
            PersistentDataManager.UserPreferences.Visualization._3D.AutomaticEEGUpdate = automatic;
            var preferences = PersistentDataManager.UserPreferences;
            await PrepareReferencesAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(6));
            var token = timeout.Token;
            using var source = new SceneArchive(Path.Combine(temp.Path, "source"));
            var payload = CreateFixture(source);
            var ccepModel = (CCEPColumn)payload.Visualization.Columns[2];
            ccepModel.CCEPConfiguration.SpanMin = -11;
            ccepModel.CCEPConfiguration.Middle = 3;
            ccepModel.CCEPConfiguration.SpanMax = 17;
            ((AnatomicColumn)payload.Visualization.Columns[0]).AnatomicConfiguration.MaximumInfluence = 23;
            ((StaticColumn)payload.Visualization.Columns[3]).StaticConfiguration.SelectedResourceIndex = 1;
            ((MEGColumn)payload.Visualization.Columns[5]).MEGConfiguration.SelectedResourceIndex = 1;
            payload.Visualization.Configuration.AtlasConfiguration = new AtlasConfiguration { MarsAtlas = true, AtlasAlpha = .37f, IBCIndex = 4, NegativeMin = .1f, NegativeMax = .8f, PositiveMin = .2f, PositiveMax = .9f, FMRIAlpha = .6f, LocalizerMin = 70, LocalizerMiddle = 90, LocalizerMax = 130 };
            payload.Visualization.Configuration.RegionsOfInterest.Add(new RegionOfInterest("Scientific ROI", new List<Core.Data.Sphere> { new Core.Data.Sphere(Vector3.zero, 12f) }));
            string file = Path.Combine(temp.Path, "snapshot.hbscene");
            source.Write(payload, file);
            RestoredScene desktop = null, quest = null;
            try
            {
                var desktopArchive = new SceneArchive(Path.Combine(temp.Path, "desktop"), true, source.Globals);
                desktop = await SceneRestoration.PrepareAsync(desktopArchive.Read(file), desktopArchive, AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/3D/Scenes/Scene 3D.prefab").GetComponent<Base3DScene>(), scope.Root.transform, token);
                var questArchive = new SceneArchive(Path.Combine(temp.Path, "quest"), true, source.Globals);
                quest = await SceneRestoration.PrepareAsync(questArchive.Read(file), questArchive, AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/3D/Scenes/Scene 3D Content.prefab").GetComponent<Base3DScene>(), scope.Root.transform, token);
                foreach (var restored in new[] { desktop, quest })
                {
                    var scene = restored.Scene;
                    Assert.That(PersistentDataManager.UserPreferences, Is.SameAs(preferences));
                    Assert.That(scene.IsGeneratorUpToDate, Is.EqualTo(automatic));
                    var ccep = (Column3DCCEP)scene.Columns[2];
                    Assert.That(ccep.SelectedSourceSite.Information.FullID, Is.EqualTo("patient_A1"));
                    Assert.That(ccep.DynamicParameters.SpanMin, Is.EqualTo(-11));
                    Assert.That(ccep.DynamicParameters.Middle, Is.EqualTo(3));
                    Assert.That(ccep.DynamicParameters.SpanMax, Is.EqualTo(17));
                    Assert.That(((Column3DAnatomy)scene.Columns[0]).AnatomyParameters.InfluenceDistance, Is.EqualTo(23));
                    Assert.That(((Column3DStatic)scene.Columns[3]).SelectedLabelIndex, Is.EqualTo(1));
                    Assert.That(((Column3DMEG)scene.Columns[5]).SelectedMEGIndex, Is.EqualTo(1));
                    Assert.That(scene.TriangleEraser.CurrentMasks[0][0], Is.Zero);
                    Assert.That(scene.FMRIManager.DisplayIBCContrasts, Is.False);
                    Assert.That(scene.FMRIManager.SelectedIBCContrastID, Is.EqualTo(4));
                    Assert.That(scene.AtlasManager.AtlasAlpha, Is.EqualTo(.37f));
                    Assert.That(scene.AtlasManager.DisplayMarsAtlas, Is.True);
                    var expectedAtlasColors = Object3DManager.MarsAtlas.ConvertIndicesToColors(Object3DManager.MarsAtlas.GetSurfaceAreaLabels(scene.MeshManager.ReferenceSurface), -1);
                    foreach (var column in scene.Columns)
                    {
                        var mesh = column.BrainMesh.GetComponent<MeshFilter>().sharedMesh;
                        Assert.That(mesh.colors, Has.Length.EqualTo(mesh.vertexCount));
                        Assert.That(mesh.colors, Is.EqualTo(expectedAtlasColors));
                    }

                    var configuration = scene.CaptureConfiguration();
                    Assert.That(configuration.AtlasConfiguration.ID, Is.EqualTo(scene.Visualization.Configuration.AtlasConfiguration.ID));
                    Assert.That(configuration.RegionsOfInterest[0].Name, Is.EqualTo("Scientific ROI"));
                    Assert.That(configuration.RegionsOfInterest[0].Spheres[0].Radius, Is.EqualTo(12f));
                    Assert.That(configuration.AtlasConfiguration.IBCIndex, Is.EqualTo(4));
                    Assert.That(scene.Columns.All(column => column.NavigationTimeline == null || !column.NavigationTimeline.IsPlaying), Is.True);
                }

                if (!automatic)
                {
                    quest.Scene.UpdateGenerator();
                    await quest.Scene.PrepareRenderingAsync(token);
                    Assert.That(quest.Scene.IsGeneratorUpToDate, Is.True, "Manual calculation remains available with automatic calculation disabled.");
                    Assert.That(desktop.Scene.IsGeneratorUpToDate, Is.False);
                }
            }
            finally
            {
                if (quest != null) await quest.CloseAsync();
                if (desktop != null) await desktop.CloseAsync();
                await UniTask.NextFrame();
            }
        }

        [TestCase("scene-008")]
        [TestCase("scene-008-patient")]
        [Timeout(900000)]
        public async Task DesktopProject_LoadsSavesAndTransfersAllSixModalities(string fixtureName)
        {
            await FailOnUnexpectedCancellation(() => DesktopProjectScenarioAsync(fixtureName));
        }

        private static async Task DesktopProjectScenarioAsync(string fixtureName)
        {
            string fixture = Path.GetFullPath(".artifacts/scene-008/fixture/" + fixtureName + ".hibop");
            Assert.That(File.Exists(fixture), Is.True, "Run Tools/Prepare-SceneQualificationFixture.py.");
            using var temp = new PlayModeTempDirectoryScope();
            using var app = new PlayModeApplicationStateScope(temp.Path);
            typeof(ApplicationState).GetProperty(nameof(ApplicationState.DataPath), BindingFlags.Public | BindingFlags.Static).SetValue(null, Path.GetFullPath("Assets/Data"));
            using var settings = new PlayModePersistentDataScope(temp.Path);
            using var scope = new PlayModeSceneScope("FullDesktopScene");
            PersistentDataManager.UserPreferences.Visualization._3D.AutomaticEEGUpdate = true;
            await PrepareReferencesAsync();
            // Two complete buffer exports plus installed-reference hashing exceed four
            // minutes on the Editor's Mono runtime; Players are measured separately.
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(6));
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var token = timeout.Token;
            var protocol = ClassLoaderSaver.LoadFromJson<Protocol>(Path.GetFullPath(".artifacts/scene-008/fixture/scene-008.prov"));
            Core.Database.DatabaseManager.Database.SetProtocols(new[] { protocol });
            var info = new ProjectInfo(fixture);
            var project = new Project(info.Name, new ProjectPreferences("placeholder"));
            ApplicationState.LoadedProject = project;
            ApplicationState.LoadedProjectLocation = temp.Path;
            await project.LoadAsync(info, (_, _, _) => { }, token);
            await project.CurrentLoadingOperation.EnsureValidatedAsync(token);
            Assert.That(project.StructuralRecoveryReport.HasIssues, Is.False);
            var model = project.Visualizations.Single();
            Assert.That(model.IsVisualizable, Is.True, "Incompatible columns: " + string.Join(", ", model.Columns.Where(c => !c.IsCompatible(model.Patients)).Select(c => c.Name)));
            await model.LoadAsync((_, _, _) => { }, token);
            Debug.Log($"Scene qualification {fixtureName}: project/model loaded in {clock.Elapsed.TotalSeconds:F1}s.");
            await UniTask.SwitchToMainThread();
            var desktop = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/3D/Scenes/Scene 3D.prefab"), scope.Root.transform).GetComponent<Base3DScene>();
            var quest = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestAnatomy.prefab"), scope.Root.transform).GetComponent<QuestAnatomyView>();
            try
            {
                desktop.Initialize(model);
                await desktop.InitializeAsync(model, (_, _, _) => { }, token);
                await desktop.CompleteInitializationAsync(null, null, token);
                await desktop.PrepareRenderingAsync(token);
                Debug.Log($"Scene qualification {fixtureName}: Desktop prepared at {clock.Elapsed.TotalSeconds:F1}s.");
                Assert.That(desktop.Columns.Count, Is.EqualTo(6));
                Assert.That(desktop.Columns.All(c => c.Views.Count == 1), Is.True);
                var ccep = desktop.Columns.OfType<Column3DCCEP>().Single();
                ccep.SelectedSourceSite = ccep.Sources.First();
                await desktop.PrepareRenderingAsync(token);
                Assert.That(ccep.ActivityValues.Any(v => v != 0), Is.True);
                await SceneQualification.RunAsync(desktop, Path.GetFullPath(".artifacts/scene-008/editor-desktop-" + fixtureName), token);
                Debug.Log($"Scene qualification {fixtureName}: Desktop diagnostic completed at {clock.Elapsed.TotalSeconds:F1}s.");
                var globals = new PairingContext(new GlobalDataPayload { Preferences = PersistentDataManager.UserPreferences, Tags = PersistentDataManager.Tags, Protocols = new() { protocol }, Aliases = PersistentDataManager.Aliases, Grid = Core.DLL.ActivityProjectionSettings.VolumeGridDimension, Interpolation = Core.DLL.ActivityProjectionSettings.VolumeInterpolation });
                using var delivery = await DesktopSceneCapture.CaptureDeliveryAsync(desktop, "desktop", "qualification", 1, globals, token);
                string file = (string)typeof(SceneDelivery).GetField("file", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(delivery);
                var archive = new SceneArchive(Path.Combine(temp.Path, "quest"), true, globals);
                await quest.ApplyAsync(archive.Read(file), archive, token);
                Debug.Log($"Scene qualification {fixtureName}: Quest scene restored at {clock.Elapsed.TotalSeconds:F1}s.");
                Assert.That(quest.Scene.Columns.Count, Is.EqualTo(6));
                await SceneQualification.RunAsync(quest.Scene, Path.GetFullPath(".artifacts/scene-008/editor-restored-" + fixtureName), token);
                Debug.Log($"Scene qualification {fixtureName}: restored diagnostic completed at {clock.Elapsed.TotalSeconds:F1}s.");
                // Save a test-owned project, then resolve it afresh through the normal project loader.
                await project.SaveAsync(temp.Path, (_, _, _) => { }, token);
                var savedInfo = new ProjectInfo(Path.Combine(temp.Path, project.FileName));
                var reloaded = new Project(savedInfo.Name, new ProjectPreferences("reload"));
                ApplicationState.LoadedProject = reloaded;
                await reloaded.LoadAsync(savedInfo, (_, _, _) => { }, token);
                await reloaded.CurrentLoadingOperation.EnsureValidatedAsync(token);
                Assert.That(reloaded.StructuralRecoveryReport.HasIssues, Is.False);
                Assert.That(reloaded.Visualizations.Single().Columns.Select(c => c.GetType()), Is.EqualTo(model.Columns.Select(c => c.GetType())));
                Debug.Log($"Scene qualification {fixtureName}: save/reload verified at {clock.Elapsed.TotalSeconds:F1}s.");
            }
            finally
            {
                await quest.ClearAsync();
                await desktop.CleanAsync();
                Object.Destroy(quest.gameObject);
                await UniTask.NextFrame();
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        [Timeout(900000)]
        public async Task CompleteScene_RestoresNativeModalitiesAndIndependentColumns_ThenRecaptures(bool blocks)
        {
            await FailOnUnexpectedCancellation(() => CompleteSceneScenarioAsync(blocks));
        }

        private static async Task CompleteSceneScenarioAsync(bool blocks)
        {
            string root = Path.Combine(Path.GetTempPath(), "hibop-scene-runtime-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            using var settings = new PlayModePersistentDataScope(root);
            using var scope = new PlayModeSceneScope("SceneTransfer");
            PersistentDataManager.UserPreferences.Visualization._3D.AutomaticEEGUpdate = true;
            await PrepareReferencesAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(4));
            var token = timeout.Token;
            var view = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestAnatomy.prefab"), scope.Root.transform).GetComponent<QuestAnatomyView>();
            try
            {
                await UniTask.SwitchToMainThread();
                using var source = new SceneArchive(Path.Combine(root, "source"), deferResourceWrites: blocks);
                ScenePayload payload = CreateFixture(source);
                string file = Path.Combine(root, "fixture.hbscene");
                source.Write(payload, file); // Also retained for the later invalid-replacement scenarios.
                var archive = new SceneArchive(Path.Combine(root, "received"), true, source.Globals);
                ScenePayload received;
                if (blocks)
                {
                    var packets = new List<byte[]>();
                    HBP.Transfer.Transport.BlockContainer.Produce(source.CaptureBlockResources(source.CaptureMetadata(payload)), packets.Add, token);
                    using var input = new MemoryStream(packets.SelectMany(p => p).ToArray());
                    input.Position = 4;
                    await HBP.Transfer.Transport.BlockContainer.ReceiveAsync(input, archive, token);
                    received = archive.ReadPrepared();
                }
                else
                {
                    received = archive.Read(file);
                }

                await view.ApplyAsync(received, archive, token);
                var scene = view.Scene;
                Assert.That(scene.DesktopPresentation, Is.Null);
                Assert.That(scene.Columns.Select(c => c.GetType()), Is.EqualTo(new[] { typeof(Column3DAnatomy), typeof(Column3DIEEG), typeof(Column3DCCEP), typeof(Column3DStatic), typeof(Column3DFMRI), typeof(Column3DMEG) }));
                Assert.That(scene.Columns.All(c => c.Views.Count == 0), Is.True);
                Assert.That(scene.GetComponentsInChildren<Camera>(true), Is.Empty);
                Assert.That(view.Columns.Count, Is.EqualTo(6));
                foreach (var column in scene.Columns)
                {
                    Assert.That(column.BrainMesh.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThan(0), column.Name);
                    Assert.That(column.Sites.Count, Is.EqualTo(2), column.Name);
                    if (column.NavigationTimeline != null) Assert.That(column.NavigationTimeline.IsPlaying, Is.False, column.Name);
                }

                var ieeg = scene.Columns.OfType<Column3DIEEG>().Single();
                var ccep = scene.Columns.OfType<Column3DCCEP>().Single();
                Assert.That(scene.TriangleEraser.CurrentMasks[0][0], Is.Zero, "Restored erasure must survive geometry preparation.");
                Assert.That(ccep.SelectedSourceSite.Information.FullID, Is.EqualTo("patient_A1"));
                Assert.That(ccep.ActivityValues, Does.Contain(8f));
                await SceneQualification.RunAsync(scene, Path.GetFullPath(".artifacts/scene-008/editor-six-modalities"), token);
                Assert.That(ieeg.Timeline, Is.Not.SameAs(ccep.Timeline));
                int otherIndex = ccep.Timeline.CurrentIndex;
                ieeg.Timeline.CurrentIndex = 2;
                await scene.PrepareRenderingAsync(token);
                Assert.That(ccep.Timeline.CurrentIndex, Is.EqualTo(otherIndex));
                Assert.That(ieeg.ColumnIEEGData.Data.DataByChannelID["patient_A1"].Trials[1].IsValid, Is.False);
                var originalSites = ieeg.Sites.Select(s => s.transform.localPosition).ToArray();
                Vector3 otherPose = view.Columns[0].transform.position;
                view.Columns[1].transform.SetPositionAndRotation(new Vector3(1, 2, 3), Quaternion.Euler(10, 20, 30));
                view.Columns[1].transform.localScale = Vector3.one * 2;
                Assert.That(ieeg.Sites.Select(s => s.transform.localPosition), Is.EqualTo(originalSites));
                Assert.That(view.Columns[0].transform.position, Is.EqualTo(otherPose));
                Assert.That(scene.Columns.Select(c => c.BrainMesh.GetComponent<MeshFilter>().sharedMesh).Distinct().Count(), Is.EqualTo(6));

                var stat = scene.Columns.OfType<Column3DStatic>().Single();
                stat.SelectedLabelIndex = 1;
                var fmri = scene.Columns.OfType<Column3DFMRI>().Single();
                fmri.Timeline.CurrentIndex = fmri.Timeline.Length - 1;
                var meg = scene.Columns.OfType<Column3DMEG>().Single();
                meg.SelectedMEGIndex = 1;
                await scene.PrepareRenderingAsync(token);
                Assert.That(meg.SelectedFMRI.Volumes.Count, Is.GreaterThan(0));
                Assert.That(stat.SelectedLabelIndex, Is.EqualTo(1));
                var cut = scene.AddCutPlane();
                scene.UpdateCutPlane(cut);
                await scene.PrepareRenderingAsync(token);
                Assert.That(scene.Columns.All(c => c.BrainCutMeshes.Count == 1), Is.True);

                using var delivery = await DesktopSceneCapture.CaptureDeliveryAsync(scene, "recaptured", "runtime", 2, source.Globals, token);
                string capturedFile = (string)typeof(SceneDelivery).GetField("file", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(delivery);
                using var capturedArchive = new SceneArchive(Path.Combine(root, "recaptured"), true, source.Globals);
                var captured = capturedArchive.Read(capturedFile);
                Assert.That(captured.Columns.Count, Is.EqualTo(6));
                Assert.That(((StaticColumn)captured.Visualization.Columns[3]).StaticConfiguration.SelectedResourceIndex, Is.EqualTo(1));
                Assert.That(((MEGColumn)captured.Visualization.Columns[5]).MEGConfiguration.SelectedResourceIndex, Is.EqualTo(1));
                Assert.That(((IEEGColumn)captured.Visualization.Columns[1]).Data.ProcessedValuesByChannel["patient_A1"], Is.EqualTo(new[] { 1f, 2f, 3f, 4f }));
                Assert.That(captured.Visualization.Configuration.Cuts.Count, Is.EqualTo(1));

                // A failed replacement must leave the current common scene and poses alive.
                var invalidArchive = new SceneArchive(Path.Combine(root, "invalid"), true, source.Globals);
                var invalid = invalidArchive.Read(file);
                invalid.StandardFiles["IRM/MNI.nii"] = new string('0', 64);
                Exception failure = null;
                try
                {
                    await view.ApplyAsync(invalid, invalidArchive, token);
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
                finally
                {
                    invalidArchive.Dispose();
                }

                Assert.That(failure, Is.TypeOf<InvalidDataException>());
                Assert.That(view.Scene, Is.SameAs(scene));
                Assert.That(view.Columns[1].transform.localScale, Is.EqualTo(Vector3.one * 2));
                using var incompatibleArchive = new SceneArchive(Path.Combine(root, "incompatible-topology"), true, source.Globals);
                var incompatible = incompatibleArchive.Read(file);
                incompatible.Visualization.Configuration.ErasedTriangles = new[] { 1 };
                UnityEngine.TestTools.LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("InvalidDataException: Configured erasure does not match"));
                Exception topologyFailure = null;
                try
                {
                    await view.ApplyAsync(incompatible, incompatibleArchive, token);
                }
                catch (Exception exception)
                {
                    topologyFailure = exception;
                }

                Assert.That(topologyFailure, Is.TypeOf<InvalidDataException>());
                Assert.That(view.Scene, Is.SameAs(scene), "An invalid configuration must not replace the published scene.");
                // Cancel a real preparation after it has started reading its native inputs.
                using var cancel = new CancellationTokenSource();
                var cancelledArchive = new SceneArchive(Path.Combine(root, "cancelled"), true, source.Globals);
                Task replacement = view.ApplyAsync(cancelledArchive.Read(file), cancelledArchive, cancel.Token);
                await UniTask.NextFrame();
                cancel.Cancel();
                Exception cancellation = null;
                try
                {
                    await replacement;
                }
                catch (Exception exception)
                {
                    cancellation = exception;
                }
                finally
                {
                    cancelledArchive.Dispose();
                }

                Assert.That(cancellation, Is.InstanceOf<OperationCanceledException>());
                Assert.That(view.Scene, Is.SameAs(scene));
                await scene.PrepareRenderingAsync(token);
                Assert.That(scene.Columns.All(c => c.BrainMesh.GetComponent<MeshFilter>().sharedMesh.vertexCount > 0), Is.True);
                Debug.Log($"Native common scene completed: six modalities, {scene.Columns.Sum(c => c.Sites.Count)} rendered sites, recapture {delivery.EncodedBytes} bytes.");
                await view.ClearAsync();
            }
            finally
            {
                await view.ClearAsync();
                Object.Destroy(view.gameObject);
                await UniTask.NextFrame();
                Directory.Delete(root, true);
            }
        }

        [Test]
        public async Task UnexpectedCancellation_IsReportedAsFailure()
        {
            Exception failure = null;
            try
            {
                await FailOnUnexpectedCancellation(() => Task.FromCanceled(new CancellationToken(true)));
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            Assert.That(failure, Is.TypeOf<AssertionException>());
        }

        private static async Task FailOnUnexpectedCancellation(Func<Task> scenario)
        {
            // Unity Test Framework's Task wrapper only checks IsFaulted, not IsCanceled.
            // Turn unexpected cancellation into a fault so an incomplete scenario cannot pass.
            try
            {
                await scenario();
            }
            catch (OperationCanceledException exception)
            {
                throw new AssertionException("Qualification cancelled before completion: " + exception);
            }
        }

        private static async UniTask PrepareReferencesAsync()
        {
            // Shared native preparation cannot be cancelled. Await it before starting the
            // scenario timeout; the NUnit guard leaves room for cold preparation and cleanup.
            var clock = System.Diagnostics.Stopwatch.StartNew();
            Debug.Log("Scene qualification: preparing standard references.");
            await Base3DScene.PrepareStandardResourcesAsync();
            await UniTask.SwitchToMainThread();
            Debug.Log($"Scene qualification: standard references prepared in {clock.Elapsed.TotalSeconds:F1}s.");
        }

        private static ScenePayload CreateFixture(SceneArchive archive)
        {
            var ev = new Event("event", new[] { 1 }, MainSecondaryEnum.Main, "event");
            var sub = new SubBloc("sub", 0, MainSecondaryEnum.Main, new TimeWindow(0, 30), new TimeWindow(0, 0), new[] { ev }, Array.Empty<Icon>(), Array.Empty<Treatment>(), "sub");
            var bloc = new Bloc("bloc", 0, "", "sub_event_CODE", new[] { sub }, "bloc");
            var protocol = new Protocol("SCENE-008 synthetic", new[] { bloc }, "scene-protocol");
            var dataset = new Dataset("synthetic", protocol, Array.Empty<DataInfo>(), "dataset");
            var ieeg = new IEEGColumn("iEEG", new BaseConfiguration(), dataset, "signal", bloc, new DynamicConfiguration(), "ieeg");
            var ccep = new CCEPColumn("CCEP", new BaseConfiguration(), dataset, "signal", bloc, new CCEPConfiguration(), "ccep");
            var stats = new Dictionary<SubBloc, List<SubBlocEventsStatistics>> { [sub] = new() { new SubBlocEventsStatistics { StatisticsByEvent = new() { [ev] = new EventStatistics() } } } };
            var indices = new Dictionary<SubBloc, int> { [sub] = 0 };
            ieeg.Data.Timeline = new Timeline(bloc, stats, indices, new Frequency(200));
            ieeg.Data.ProjectionTimeline = new Timeline(bloc, stats, indices, new Frequency(100));
            ccep.Data.Timeline = ieeg.Data.Timeline;
            ccep.Data.ProjectionTimeline = ieeg.Data.ProjectionTimeline;
            ieeg.Data.ProcessedValuesByChannel["patient_A1"] = new[] { 1f, 2f, 3f, 4f };
            ieeg.Data.UnitByChannelID["patient_A1"] = "uV";
            ieeg.Data.DataByChannelID["patient_A1"] = new BlocChannelData(new[]
            {
                new ChannelTrial(new Dictionary<SubBloc, ChannelSubTrial> { [sub] = new ChannelSubTrial(new[] { 1f, 2f, 3f, 4f }, "uV", true, new() { [ev] = new EventInformation(Array.Empty<EventInformation.EventOccurence>()) }) }, true),
                new ChannelTrial(new Dictionary<SubBloc, ChannelSubTrial> { [sub] = new ChannelSubTrial(new[] { 5f, 6f }, "uV", false, new()) }, false)
            });
            var stat = new StaticColumn { ID = "static", Name = "Static" };
            ccep.Data.ProcessedValuesByChannelIDByStimulatedChannelID["patient_A1"] = new() { ["patient_A1"] = new[] { 2f, 4f, 6f, 8f } };
            ccep.Data.UnityByChannelIDByStimulatedChannelID["patient_A1"] = new() { ["patient_A1"] = "uV" };
            ccep.Data.DataByChannelIDByStimulatedChannelID["patient_A1"] = new() { ["patient_A1"] = ieeg.Data.DataByChannelID["patient_A1"] };
            ccep.Data.StatisticsByChannelIDByStimulatedChannelID["patient_A1"] = new();
            stat.Data.ValueByChannelIDByLabel["first"] = new() { ["patient_A1"] = 7 };
            stat.Data.ValueByChannelIDByLabel["second"] = new() { ["patient_A1"] = -3 };
            var patient = new Patient { ID = "patient", Name = "Synthetic" };
            patient.Sites.Add(new Core.Data.Site("A1", new[] { new Coordinate("MNI", new Vector3(-31.25f, -18.5f, 26.75f)) }, Array.Empty<BaseTagValue>(), "site1"));
            patient.Sites.Add(new Core.Data.Site("A2", new[] { new Coordinate("MNI", new Vector3(-27.5f, -15.25f, 29)) }, Array.Empty<BaseTagValue>(), "site2"));
            var columns = new Column[] { new AnatomicColumn { ID = "anatomy", Name = "Density" }, ieeg, ccep, stat, new FMRIColumn { ID = "fmri", Name = "fMRI" }, new MEGColumn { ID = "meg", Name = "MEG" } };
            var model = new Visualization("SCENE-008 six modalities", new[] { patient }, columns);
            model.Configuration.MeshName = Object3DManager.MNI.GreyMatter.Name;
            model.Configuration.MRIName = Object3DManager.MNI.MRI.Name;
            model.Configuration.ImplantationName = "MNI";
            model.Configuration.FirstColumnToSelect = -1;
            archive.Globals = new PairingContext(new GlobalDataPayload { Preferences = PersistentDataManager.UserPreferences, Tags = PersistentDataManager.Tags, Protocols = new() { protocol }, Aliases = PersistentDataManager.Aliases, Grid = Core.DLL.ActivityProjectionSettings.VolumeGridDimension, Interpolation = Core.DLL.ActivityProjectionSettings.VolumeInterpolation });
            var payload = new ScenePayload { TransferId = "fixture", SessionId = "runtime", Revision = 1, GlobalContextId = archive.Globals.Id, Visualization = model, StandardFiles = new(Object3DManager.MNI.ResourceHashes) };
            var mesh = Object3DManager.MNI.GreyMatter;
            payload.Meshes.Add(new MeshResource { Name = mesh.Name, Standard = "grey", Type = MeshType.MNI, StandardBothMask = mesh.Both.VisibilityMask, StandardLeftMask = mesh.Left.VisibilityMask, StandardRightMask = mesh.Right.VisibilityMask, SimplifiedBoth = archive.AddSurface(mesh.SimplifiedBoth), SimplifiedLeft = archive.AddSurface(mesh.SimplifiedLeft), SimplifiedRight = archive.AddSurface(mesh.SimplifiedRight) });
            payload.MRIs.Add(new VolumeResource { Name = Object3DManager.MNI.MRI.Name, Standard = "MNI" });
            payload.Visualization.Configuration.ErasedTriangles = Enumerable.Repeat(1, mesh.Both.NumberOfTriangles).ToArray();
            payload.Visualization.Configuration.ErasedSimplifiedTriangles = Enumerable.Repeat(1, mesh.SimplifiedBoth.NumberOfTriangles).ToArray();
            payload.Visualization.Configuration.ErasedTriangles[0] = 0;
            foreach (var column in columns)
            {
                var state = new ColumnState { Id = column.ID };
                if (column == ccep) ccep.CCEPConfiguration = new CCEPConfiguration { SiteID = "patient_A1" };
                payload.Columns.Add(state);
            }

            string fmri = Path.GetFullPath("Assets/Tests/Fixtures/Native/Nifti/fmri_4d.nii.gz");
            string image = archive.AddFile(fmri, StandardData.HashFile(fmri));
            payload.Columns[4].Functional.Add(new FunctionalResource { Name = "fMRI", File = image });
            payload.Columns[5].Functional.Add(new FunctionalResource { Name = "channels", Values = new() { ["A1"] = new[] { 1f, 2f, 3f } }, Units = new() { ["A1"] = "fT" }, Frequency = 100 });
            payload.Columns[5].Functional.Add(new FunctionalResource { Name = "volume", File = image, Values = new(), Units = new() });
            return payload;
        }
    }
}
#endif
