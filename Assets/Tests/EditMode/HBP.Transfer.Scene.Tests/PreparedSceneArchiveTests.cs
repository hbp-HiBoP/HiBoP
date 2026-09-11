using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Transfer.Scene;
using NUnit.Framework;

namespace HBP.Tests.Transfer
{
    /// <summary>Prepared for Lot C: these package tests do not invoke native readers or the Editor scene.</summary>
    public sealed class PreparedSceneArchiveTests
    {
        private string directory;

        [TestCase("Atlases/atlas.nii.gz", "Atlases/atlas.nii.gz.bytes")]
        [TestCase("Atlases/atlas.NII.GZ", "Atlases/atlas.NII.GZ.bytes")]
        [TestCase("IRM/MNI.nii", "IRM/MNI.nii")]
        public void ReferencePackagingPreservesCompressedScientificBytes(string scientificPath, string packagedPath)
        {
            Assert.That(StandardData.PackagedPath(scientificPath), Is.EqualTo(packagedPath));
        }

        [SetUp]
        public void SetUp() => directory = Path.Combine(Path.GetTempPath(), "hibop-scene-test-" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test]
        public void AllModalitiesRoundTripKeepsPatientsProtocolIdentityTrialsAndTimelines()
        {
            using var source = new SceneArchive(Path.Combine(directory, "source"));
            ScenePayload payload = Fixture(source);
            string file = Path.Combine(directory, "scene.hbscene");
            source.Write(payload, file);
            using var target = new SceneArchive(Path.Combine(directory, "target"), true, source.Globals);
            ScenePayload restored = target.Read(file);
            Assert.That(restored.Visualization.Columns.Select(c => c.GetType()), Is.EqualTo(payload.Visualization.Columns.Select(c => c.GetType())));
            Assert.That(restored.Visualization.Patients.Single().ID, Is.EqualTo("patient"));
            Assert.That(restored.Visualization.Patients.Single().Tags.Single().Tag.Name, Is.EqualTo("group"));
            Assert.That(restored.Visualization.Patients.Single().Tags.Single().DisplayableValue, Is.EqualTo("test"));
            var ieeg = (IEEGColumn)restored.Visualization.Columns[1];
            var ccep = (CCEPColumn)restored.Visualization.Columns[2];
            Assert.That(ieeg.Bloc, Is.SameAs(ieeg.Dataset.Protocol.Blocs.Single()));
            Assert.That(ccep.Bloc, Is.SameAs(ieeg.Bloc));
            Assert.That(ieeg.Data.Timeline.Length, Is.EqualTo(7));
            Assert.That(ieeg.Data.ProjectionTimeline.Length, Is.EqualTo(4));
            Assert.That(ieeg.Data.Timeline.SubTimelinesBySubBloc.Keys.Single(), Is.SameAs(ieeg.Bloc.SubBlocs.Single()));
            Assert.That(ieeg.Data.Timeline.SubTimelinesBySubBloc.Values.Single().StatisticsByEvent.Keys.Single(), Is.SameAs(ieeg.Bloc.SubBlocs.Single().MainEvent));
            var trials = ieeg.Data.DataByChannelID["patient_A1"].Trials;
            Assert.That(trials.Length, Is.EqualTo(2));
            Assert.That(trials[1].IsValid, Is.False);
            Assert.That(trials[0].ChannelSubTrialBySubBloc.Keys.Single(), Is.SameAs(ieeg.Bloc.SubBlocs.Single()));
            Assert.That(trials[0].ChannelSubTrialBySubBloc.Values.Single().Values, Is.EqualTo(new[] { 1f, 2f, 3f, 4f }));
            Assert.That(ieeg.Data.Timeline.OnUpdateCurrentIndex, Is.Not.Null);
            Assert.That(restored.Columns[5].Functional.Single().Values["patient_A1"], Is.EqualTo(new[] { 4f, 5f, 6f }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingResourceAndUnknownFormatFailBeforeRestoration(bool missingResource)
        {
            using var source = new SceneArchive(Path.Combine(directory, "source"));
            var payload = Fixture(source);
            if (missingResource) payload.Meshes[0].SimplifiedBoth = new string('b', 64) + ".bin";
            else payload.Version++;
            string file = Path.Combine(directory, "invalid.hbscene");
            source.Write(payload, file);
            using var target = new SceneArchive(Path.Combine(directory, "target"), true, source.Globals);
            Assert.Throws<InvalidDataException>(() => target.Read(file));
        }

        [Test]
        public void ChangedContentAndPathTraversalAreRejected()
        {
            using var source = new SceneArchive(Path.Combine(directory, "source"));
            var payload = Fixture(source);
            string file = Path.Combine(directory, "corrupt.hbscene");
            source.Write(payload, file);
            using (var zip = ZipFile.Open(file, ZipArchiveMode.Update))
            {
                var entry = zip.Entries.First(e => e.Name.EndsWith(".bin", StringComparison.Ordinal));
                string name = entry.Name;
                entry.Delete();
                using var writer = new BinaryWriter(zip.CreateEntry(name).Open());
                writer.Write(123);
            }

            using var target = new SceneArchive(Path.Combine(directory, "target"), true, source.Globals);
            Assert.Throws<InvalidDataException>(() => target.Read(file));
            Assert.Throws<InvalidDataException>(() => source.Resolve("../outside.bin"));
        }

        [Test]
        public void EqualVoxelFilesWithDifferentHeadersKeepDistinctNativePairs()
        {
            Directory.CreateDirectory(directory);
            string first = Path.Combine(directory, "first.img"), second = Path.Combine(directory, "second.img");
            File.WriteAllBytes(first, new byte[] { 1, 2, 3 });
            File.Copy(first, second);
            File.WriteAllText(Path.ChangeExtension(first, ".hdr"), "first header");
            File.WriteAllText(Path.ChangeExtension(second, ".hdr"), "second header");
            using var archive = new SceneArchive(Path.Combine(directory, "archive"));
            string Add(string path) => archive.AddFile(path, StandardData.HashFile(path), StandardData.HashFile(Path.ChangeExtension(path, ".hdr")));
            string firstId = Add(first), secondId = Add(second);
            Assert.That(firstId, Is.Not.EqualTo(secondId));
            Assert.That(archive.ReadNativePair(firstId)[0], Is.EqualTo(archive.ReadNativePair(secondId)[0]));
            Assert.That(File.ReadAllText(Path.ChangeExtension(archive.ResolveNativeFile(firstId), ".hdr")), Is.EqualTo("first header"));
            Assert.That(File.ReadAllText(Path.ChangeExtension(archive.ResolveNativeFile(secondId), ".hdr")), Is.EqualTo("second header"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PairingGlobalsRestoreCanonicalProtocolsTagsFiltersAndImages(bool deleteSourceImage)
        {
            using var source = new SceneArchive(Path.Combine(directory, "source"));
            var payload = Fixture(source);
            var data = source.Globals.Data;
            var protocol = data.Protocols.Single();
            var tag = data.Tags.AllTags.Single();
            string illustration = Path.Combine(directory, "illustration.png");
            File.WriteAllBytes(illustration, new byte[] { 1, 2, 3 });
            protocol.Blocs[0].IllustrationPath = illustration;
            source.Globals = new PairingContext(data);
            var presets = new FilterConditionsPresetCollection();
            presets.AddPreset(new FilterConditionsPreset(new BaseFilterCondition[]
            {
                new ProtocolFilterCondition { Protocols = new List<Protocol> { protocol } },
                new PatientTagFilterCondition { Tag = tag }
            }), typeof(Patient), false);
            using var globalsSource = new SceneArchive(Path.Combine(directory, "globals-source"));
            source.Globals.CaptureFilterPresets(presets, globalsSource);
            string globalFile = Path.Combine(directory, "globals.hbglobal");
            globalsSource.WriteGlobalData(data, globalFile);
            using var globalsTarget = new SceneArchive(Path.Combine(directory, "globals-target"));
            var paired = new PairingContext(globalsTarget.ReadGlobalData(globalFile));
            paired.RestoreFilterPresets(globalsTarget);
            Assert.That(paired.Data.Preferences, Is.Not.SameAs(data.Preferences));
            Assert.That(File.ReadAllBytes(paired.Data.Protocols[0].Blocs[0].IllustrationPath), Is.EqualTo(new byte[] { 1, 2, 3 }));
            var conditions = paired.FilterPresets.GetPresets(typeof(Patient)).Single().Conditions;
            Assert.That(((ProtocolFilterCondition)conditions[0]).Protocols.Single(), Is.SameAs(paired.Data.Protocols.Single()));
            Assert.That(((PatientTagFilterCondition)conditions[1]).Tag, Is.SameAs(paired.Data.Tags.AllTags.Single()));

            if (deleteSourceImage) File.Delete(illustration);
            else File.WriteAllBytes(illustration, new byte[] { 4, 5, 6 });
            string sceneFile = Path.Combine(directory, "scene.hbscene");
            source.Write(payload, sceneFile);
            using var target = new SceneArchive(Path.Combine(directory, "target"), true, paired);
            var restored = target.Read(sceneFile);
            var ieeg = (IEEGColumn)restored.Visualization.Columns[1];
            Assert.That(ieeg.Dataset.Protocol, Is.SameAs(paired.Data.Protocols.Single()));
            Assert.That(ieeg.Bloc, Is.SameAs(paired.Data.Protocols.Single().Blocs.Single()));
            Assert.That(ieeg.Data.Timeline.SubTimelinesBySubBloc.Keys.Single(), Is.SameAs(ieeg.Bloc.SubBlocs.Single()));
            Assert.That(restored.Visualization.Patients[0].Tags.Single().Tag, Is.SameAs(paired.Data.Tags.AllTags.Single()));
            using var zip = ZipFile.OpenRead(sceneFile);
            Assert.That(zip.Entries.Any(e => e.Name.EndsWith(".png", StringComparison.Ordinal)), Is.False);
            using var json = new StreamReader(zip.GetEntry("visualization.json").Open());
            string metadata = json.ReadToEnd();
            Assert.That(metadata, Does.Not.Contain("\"Preferences\""));
            Assert.That(metadata, Does.Contain("\"global\""));
        }

        [TestCase("missing")]
        [TestCase("unresolved-alias")]
        [TestCase("locked")]
        [TestCase("unsupported-extension")]
        public void UnavailableIllustrationsDoNotBlockProtocolOrSceneTransfer(string kind)
        {
            using var source = new SceneArchive(Path.Combine(directory, "source"));
            var payload = Fixture(source);
            var data = source.Globals.Data;
            var protocol = data.Protocols.Single();
            string path = kind == "unresolved-alias" ? "[UNRESOLVED]/illustration.png" : Path.Combine(directory, kind == "unsupported-extension" ? "illustration.png1" : "illustration.png");
            if (kind == "locked" || kind == "unsupported-extension") File.WriteAllBytes(path, new byte[] { 1, 2, 3 });
            using var locked = kind == "locked" ? File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None) : null;
            protocol.Blocs[0].IllustrationPath = path;
            protocol.Blocs[0].SubBlocs[0].Icons.Add(new Icon("unavailable icon", path, new TimeWindow(0, 100), "optional-icon"));
            source.Globals = new PairingContext(data);
            using var globalsSource = new SceneArchive(Path.Combine(directory, "globals-source"));
            source.Globals.CaptureFilterPresets(new FilterConditionsPresetCollection(), globalsSource);
            string globalFile = Path.Combine(directory, "globals.hbglobal");
            globalsSource.WriteGlobalData(data, globalFile);
            using var globalsTarget = new SceneArchive(Path.Combine(directory, "globals-target"));
            var paired = new PairingContext(globalsTarget.ReadGlobalData(globalFile));
            paired.RestoreFilterPresets(globalsTarget);
            Assert.That(paired.Data.Protocols.Single().Blocs.Single().IllustrationPath, Is.Empty);
            var icon = paired.Data.Protocols.Single().Blocs.Single().SubBlocs[0].Icons.Single();
            Assert.That(icon.ImagePath, Is.Empty);
            Assert.That(icon.Name, Is.EqualTo("unavailable icon"));
            Assert.That(icon.ID, Is.EqualTo("optional-icon"));
            Assert.That(paired.Data.Protocols.Single().Name, Is.EqualTo(protocol.Name));
            Assert.That(protocol.Blocs[0].IllustrationPath, Is.EqualTo(path.StandardizeToEnvironement()));
            Assert.That(Directory.GetFiles(globalsSource.DirectoryPath).Select(Path.GetFileName), Is.EquivalentTo(new[] { "globals.json" }));
            // A file appearing after pairing must not change the definition fingerprint either.
            if (kind == "missing") File.WriteAllBytes(path, new byte[] { 7, 8, 9 });
            string sceneFile = Path.Combine(directory, "scene.hbscene");
            source.Write(payload, sceneFile);
            using var target = new SceneArchive(Path.Combine(directory, "target"), true, paired);
            var restored = target.Read(sceneFile);
            Assert.That(((IEEGColumn)restored.Visualization.Columns[1]).Dataset.Protocol, Is.SameAs(paired.Data.Protocols.Single()));
            protocol.Name += " changed";
            Assert.Throws<InvalidOperationException>(() => source.Write(payload, Path.Combine(directory, "changed.hbscene")));
        }

        [Test]
        public void OptionalIllustrationsDoNotHideArchiveWriteFailures()
        {
            using var archive = new SceneArchive(Path.Combine(directory, "archive"));
            string image = Path.Combine(directory, "illustration.png");
            File.WriteAllBytes(image, new byte[] { 1, 2, 3 });
            Directory.Delete(archive.DirectoryPath);
            Assert.Throws<DirectoryNotFoundException>(() => archive.AddIllustration(image));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingOrChangedDefinitionRequiresPairingAgain(bool changed)
        {
            using var source = new SceneArchive(Path.Combine(directory, "source"));
            var payload = Fixture(source);
            if (changed) ((IEEGColumn)payload.Visualization.Columns[1]).Bloc.SubBlocs[0].Window = new TimeWindow(0, 60);
            else payload.Visualization.Patients[0].Tags.Add(new StringTagValue(new StringTag { Name = "new global tag" }, "local value"));
            Assert.Throws<InvalidOperationException>(() => source.Write(payload, Path.Combine(directory, "invalid.hbscene")));
        }

        [Test]
        public void PreviousPairingDeliveryIsRejectedEvenWithIdenticalDefinitions()
        {
            using var source = new SceneArchive(Path.Combine(directory, "source"));
            var payload = Fixture(source);
            string file = Path.Combine(directory, "scene.hbscene");
            source.Write(payload, file);
            var next = source.Globals.Data;
            next.Id = Guid.NewGuid().ToString("N");
            using var target = new SceneArchive(Path.Combine(directory, "target"), true, new PairingContext(next));
            Assert.Throws<InvalidDataException>(() => target.Read(file));
        }

        private static ScenePayload Fixture(SceneArchive archive)
        {
            var ev = new Event("event", new[] { 1 }, MainSecondaryEnum.Main, "event");
            var sub = new SubBloc("sub", 0, MainSecondaryEnum.Main, new TimeWindow(0, 30), new TimeWindow(0, 0), new[] { ev }, Array.Empty<Icon>(), Array.Empty<Treatment>(), "sub");
            var bloc = new Bloc("bloc", 0, "", "sub_event_CODE", new[] { sub }, "bloc");
            var protocol = new Protocol("protocol", new[] { bloc }, "protocol");
            var dataset = new Dataset("dataset", protocol, Array.Empty<DataInfo>(), "dataset");
            var ieeg = new IEEGColumn("iEEG", new BaseConfiguration(), dataset, "signal", bloc, new DynamicConfiguration(), "ieeg");
            var ccep = new CCEPColumn("CCEP", new BaseConfiguration(), dataset, "signal", bloc, new DynamicConfiguration(), "ccep");
            var stat = new StaticColumn { ID = "static" };
            stat.Data.ValueByChannelIDByLabel.Add("label", new Dictionary<string, float> { ["patient_A1"] = 7 });
            var statistics = new Dictionary<SubBloc, List<SubBlocEventsStatistics>> { [sub] = new() { new SubBlocEventsStatistics { StatisticsByEvent = new() { [ev] = new EventStatistics() } } } };
            var indices = new Dictionary<SubBloc, int> { [sub] = 0 };
            ieeg.Data.Timeline = new Timeline(bloc, statistics, indices, new Frequency(200));
            ieeg.Data.ProjectionTimeline = new Timeline(bloc, statistics, indices, new Frequency(100));
            ccep.Data.Timeline = ieeg.Data.Timeline;
            ccep.Data.ProjectionTimeline = ieeg.Data.ProjectionTimeline;
            ieeg.Data.ProcessedValuesByChannel.Add("patient_A1", new[] { 1f, 2f, 3f, 4f });
            ieeg.Data.DataByChannelID.Add("patient_A1", new BlocChannelData(new[]
            {
                new ChannelTrial(new Dictionary<SubBloc, ChannelSubTrial> { [sub] = new ChannelSubTrial(new[] { 1f, 2f, 3f, 4f }, "uV", true, new() { [ev] = new EventInformation(Array.Empty<EventInformation.EventOccurence>()) }) }, true),
                new ChannelTrial(new Dictionary<SubBloc, ChannelSubTrial> { [sub] = new ChannelSubTrial(new[] { 5f, 6f }, "uV", false, new()) }, false)
            }));
            var columns = new Column[] { new AnatomicColumn { ID = "anatomy" }, ieeg, ccep, stat, new FMRIColumn { ID = "fmri" }, new MEGColumn { ID = "meg" } };
            var payload = new ScenePayload { TransferId = "delivery", SessionId = "session", Revision = 3, Visualization = new Visualization("fixture", new[] { new Patient { ID = "patient" } }, columns) };
            payload.Visualization.Patients[0].Tags.Add(new StringTagValue(new StringTag { Name = "group" }, "test"));
            payload.Visualization.Configuration.MeshName = "MNI";
            payload.Visualization.Configuration.MRIName = "MNI";
            foreach (string name in new[] { "IRM/MNI.nii", "Meshes/MNI.trm", "Meshes/MNI_Lhemi.gii", "Meshes/MNI_Rhemi.gii", "Meshes/MNI_Lwhite.gii", "Meshes/MNI_Rwhite.gii" }) payload.StandardFiles[name] = new string('a', 64);
            // Opaque resource bytes exercise archive packaging; native geometry/image loading belongs to integration fixtures.
            string buffer = archive.AddBuffer(writer => writer.Write(42));
            payload.Meshes.Add(new MeshResource { Name = "MNI", Standard = "grey", Type = MeshType.MNI, SimplifiedBoth = buffer, SimplifiedLeft = buffer, SimplifiedRight = buffer, StandardBothMask = new[] { 1 }, StandardLeftMask = new[] { 1 }, StandardRightMask = new[] { 1 } });
            payload.MRIs.Add(new VolumeResource { Name = "MNI", Standard = "MNI" });
            archive.Globals = new PairingContext(new GlobalDataPayload
            {
                Preferences = new UserPreferences(), Tags = new TagCollection(Array.Empty<BaseTag>(), payload.Visualization.Patients[0].Tags.Select(v => v.Tag), Array.Empty<BaseTag>()),
                Protocols = new List<Protocol> { protocol }, Aliases = new AliasCollection(), Grid = 32
            });
            payload.GlobalContextId = archive.Globals.Id;
            payload.State.ErasedTriangles = new[] { 1 };
            payload.State.ErasedSimplifiedTriangles = new[] { 1 };
            foreach (var column in columns) payload.Columns.Add(new ColumnState { Id = column.ID, TimeStep = 1, Correlations = new(), CorrelationMeans = new() });
            payload.Columns[4].Functional.Add(new FunctionalResource { Name = "fmri", File = buffer });
            payload.Columns[5].Functional.Add(new FunctionalResource { Name = "meg", Frequency = 100, Values = new() { ["patient_A1"] = new[] { 4f, 5f, 6f } }, Units = new() { ["patient_A1"] = "fT" } });
            return payload;
        }
    }
}
