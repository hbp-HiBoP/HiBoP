using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Data.Container;
using HBP.Core.Enums;
using HBP.Core.Errors;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;

namespace HBP.Tests.Serialization
{
    public class TargetedValidationTests
    {
        private TempDirectoryScope m_FixtureTemp;
        private PersistentDataTestScope m_PersistentData;

        [SetUp]
        public void SetUp()
        {
            m_FixtureTemp = new TempDirectoryScope();
            m_PersistentData = new PersistentDataTestScope(m_FixtureTemp.Path);
        }

        [TearDown]
        public void TearDown()
        {
            m_PersistentData?.Dispose();
            m_FixtureTemp?.Dispose();
        }

        [Test]
        public void MergedRequests_KeepIndependentAspectScopes()
        {
            var protocol = CreateProtocol("protocol", 10, 20);
            Patient patient = new();
            var first = CreateIEEG("first", protocol, patient);
            var second = CreateIEEG("second", protocol, patient);
            var request = new ValidationRequest(ValidationAspect.Epoching, new[] { first.ID }).Merge(new ValidationRequest(ValidationAspect.ChannelMapping, new[] { second.ID }));

            Assert.That(request.Matches(first, ValidationAspect.Epoching), Is.True);
            Assert.That(request.Matches(first, ValidationAspect.ChannelMapping), Is.False);
            Assert.That(request.Matches(second, ValidationAspect.Epoching), Is.False);
            Assert.That(request.Matches(second, ValidationAspect.ChannelMapping), Is.True);
        }

        [Test]
        public async Task ProtocolRequest_OpensOnlyDataUsingThatProtocol()
        {
            var firstProtocol = CreateProtocol("first-protocol", 10, 20);
            var secondProtocol = CreateProtocol("second-protocol", 30, 40);
            Patient patient = new();
            var first = CreateIEEG("first", firstProtocol, patient);
            var second = CreateIEEG("second", secondProtocol, patient);
            CountingMetadataReader reader = new(new[] { 10, 20, 30, 40 }, Array.Empty<string>());
            ValidationRequest request = new(ValidationAspect.Epoching | ValidationAspect.ChannelMapping, protocolIDs: new[] { firstProtocol.ID }, force: true);

            var result = await new DataInfoValidator(reader).ValidateAsync(new DataInfo[] { first, second }, request, 2, CancellationToken.None);
            Assert.That(result.TryApply(0), Is.True);

            Assert.That(reader.OpenCount, Is.EqualTo(1));
            Assert.That(reader.OpenedDataInfoIDs, Is.EqualTo(new[] { first.ID }));
        }

        [Test]
        public async Task ConcurrentAspectResults_MergeWithoutOverwritingEachOther()
        {
            var sourcePath = m_FixtureTemp.GetPath("concurrent-validation.edf");
            File.WriteAllBytes(sourcePath, new byte[] { 1, 2, 3 });
            var protocol = CreateProtocol("concurrent-protocol", 10, 20);
            Patient patient = new("patient", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), new[]
            {
                new Site("A1", Array.Empty<Coordinate>(), Array.Empty<BaseTagValue>(), "site")
            }, Array.Empty<BaseTagValue>(), string.Empty, "patient");
            IEEGDataInfo dataInfo = new("data", protocol, new EDF(sourcePath, Array.Empty<Error>(), Array.Empty<Warning>()), Array.Empty<Error>(), Array.Empty<Warning>(), patient, NormalizationType.Auto, string.Empty, "data");
            ValidationRequest availabilityRequest = new(ValidationAspect.SourceAvailability, new[] { dataInfo.ID }, force: true);
            ValidationRequest semanticRequest = new(ValidationAspect.Epoching | ValidationAspect.ChannelMapping, new[] { dataInfo.ID }, force: true);
            CountingMetadataReader metadataReader = new(new[] { 10, 20 }, new[] { "A1" });

            var initial = await new DataInfoValidator(metadataReader).ValidateAsync(new[] { dataInfo }, availabilityRequest.Merge(semanticRequest), 1, CancellationToken.None, generation: 3);
            Assert.That(initial.TryApply(3), Is.True);
            dataInfo.MarkValidationStale(ValidationAspect.Epoching | ValidationAspect.ChannelMapping);

            var availability = await new DataInfoValidator().ValidateAsync(new[] { dataInfo }, availabilityRequest, 1, CancellationToken.None, generation: 4);
            var semantics = await new DataInfoValidator(metadataReader).ValidateAsync(new[] { dataInfo }, semanticRequest, 1, CancellationToken.None, generation: 4);

            Assert.That(semantics.TryApply(4), Is.True);
            Assert.That(availability.TryApply(4), Is.True);
            Assert.That(dataInfo.ValidationStates.Any(state => state.Aspect == ValidationAspect.SourceAvailability && state.Status == ValidationStatus.Current), Is.True);
            Assert.That(dataInfo.ValidationStates.Any(state => state.Aspect == ValidationAspect.Epoching && state.Status == ValidationStatus.Current), Is.True);
            Assert.That(dataInfo.ValidationStates.Any(state => state.Aspect == ValidationAspect.ChannelMapping && state.Status == ValidationStatus.Current), Is.True);
        }

        [Test]
        public void PreloadingMetadataReader_ReusesTheRawRecordingCache()
        {
            Protocol protocol = new("preload-protocol", Array.Empty<Bloc>(), "preload-protocol");
            var dataInfo = CreateIEEG("preload-data", protocol, new Patient());
            var loadCount = 0;
            DataManager.Clear();
            DataManager.RawRecordingLoader = _ =>
            {
                loadCount++;
                return new DynamicData(new Dictionary<string, float[]>
                {
                    { "A1", new[] { 1f, 2f } }
                }, new Dictionary<string, string>
                {
                    { "A1", "uV" }
                }, new Frequency(1000));
            };

            try
            {
                var reader = DataManager.CreatePreloadingValidationMetadataReader();

                var first = reader.Read(dataInfo);
                var second = reader.Read(dataInfo);
                var loaded = DataManager.GetData(dataInfo);

                Assert.That(loadCount, Is.EqualTo(1));
                Assert.That(first.ChannelLabels, Is.EquivalentTo(new[] { "A1" }));
                Assert.That(second.ChannelLabels, Is.EquivalentTo(first.ChannelLabels));
                Assert.That(loaded, Is.TypeOf<IEEGData>());
            }
            finally
            {
                DataManager.Clear();
                DataManager.ResetRawRecordingLoader();
            }
        }

        [Test]
        public async Task Epoching_ValidatesEverySubBlocMainEvent_AndAcceptsAlternativeCodes()
        {
            var protocol = CreateProtocol("protocol", 10, 20);
            var dataInfo = CreateIEEG("data", protocol, new Patient());
            CountingMetadataReader reader = new(new[] { 11, 21 }, Array.Empty<string>());
            ValidationRequest request = new(ValidationAspect.Epoching, new[] { dataInfo.ID }, force: true);

            var result = await new DataInfoValidator(reader).ValidateAsync(new[] { dataInfo }, request, 1, CancellationToken.None);
            result.TryApply(0);

            Assert.That(reader.OpenCount, Is.EqualTo(1));
            Assert.That(dataInfo.ValidationStates.Where(state => state.Aspect == ValidationAspect.Epoching).Select(state => state.ScopeID), Is.EquivalentTo(new[]
            {
                "protocol-main-subbloc",
                "protocol-secondary-subbloc"
            }));
            Assert.That(dataInfo.Warnings.OfType<BlocsCantBeEpochedWarning>(), Is.Empty);
        }

        [Test]
        public async Task Epoching_IgnoresMissingSecondaryEventCodes()
        {
            var protocol = CreateProtocol("protocol", 10, 20);
            var dataInfo = CreateIEEG("data", protocol, new Patient());
            CountingMetadataReader reader = new(new[] { 10, 20 }, Array.Empty<string>());

            var result = await new DataInfoValidator(reader).ValidateAsync(new[] { dataInfo }, new ValidationRequest(ValidationAspect.Epoching, force: true), 1, CancellationToken.None);
            result.TryApply(0);

            Assert.That(dataInfo.Warnings.OfType<BlocsCantBeEpochedWarning>(), Is.Empty);
        }

        [Test]
        public async Task CcepChannelMapping_DoesNotOpenTheSource()
        {
            var protocol = CreateProtocol("protocol", 10, 20);
            Patient patient = new();
            CCEPDataInfo dataInfo = new("ccep", protocol, new EDF("missing.edf", Array.Empty<Error>(), Array.Empty<Warning>()), Array.Empty<Error>(), Array.Empty<Warning>(), patient, "A1", string.Empty, "ccep-data");
            CountingMetadataReader reader = new(Array.Empty<int>(), Array.Empty<string>());

            var result = await new DataInfoValidator(reader).ValidateAsync(new DataInfo[] { dataInfo }, new ValidationRequest(ValidationAspect.ChannelMapping, force: true), 1, CancellationToken.None);
            result.TryApply(0);

            Assert.That(reader.OpenCount, Is.Zero);
            Assert.That(dataInfo.Errors.OfType<ChannelNotFoundError>().Count(), Is.EqualTo(1));
            Assert.That(dataInfo.Errors.OfType<FileDoesNotExistError>(), Is.Empty);
        }

        [Test]
        public void StartupValidation_DoesNotParseCsvContent()
        {
            using TempDirectoryScope temp = new();
            var csvPath = temp.GetPath("malformed.csv");
            File.WriteAllText(csvPath, "name,value\nrow,not-a-number");
            StaticDataInfo dataInfo = new("static", CreateProtocol("protocol", 10, 20), new CSV(csvPath, Array.Empty<Error>(), Array.Empty<Warning>()), Array.Empty<Error>(), Array.Empty<Warning>(), new Patient(), string.Empty, "static-data");

            dataInfo.CheckErrorsAndWarnings(new ValidationRequest(ValidationAspect.SourceAvailability, force: true));

            Assert.That(dataInfo.ValidationStates.Any(state => state.Aspect == ValidationAspect.SourceAvailability), Is.True);
            Assert.That(dataInfo.ValidationStates.Any(state => state.Aspect == ValidationAspect.StaticContent), Is.False);
            Assert.That(dataInfo.Errors.OfType<InvalidDataFileError>(), Is.Empty);
        }

        [Test]
        public void BrainVisionAvailability_RequiresDataAndMarkerCompanions()
        {
            using TempDirectoryScope temp = new();
            var headerPath = temp.GetPath("recording.vhdr");
            var dataPath = temp.GetPath("recording.eeg");
            File.WriteAllText(headerPath, "DataFile=recording.eeg\nMarkerFile=recording.vmrk");
            File.WriteAllBytes(dataPath, new byte[] { 1 });
            BrainVision container = new(headerPath, Array.Empty<Error>(), Array.Empty<Warning>());

            var errors = container.GetErrors();

            Assert.That(errors, Has.Length.EqualTo(1));
            Assert.That(errors[0], Is.TypeOf<FileDoesNotExistError>());
            Assert.That(errors[0].Message, Does.Contain("recording.vmrk"));
        }

        [Test]
        public void ElanAvailability_IgnoresMissingOptionalNotes()
        {
            using TempDirectoryScope temp = new();
            var eegPath = temp.GetPath("recording.eeg");
            var posPath = temp.GetPath("recording.pos");
            File.WriteAllBytes(eegPath, new byte[] { 1 });
            File.WriteAllBytes(eegPath + Elan.HEADER_EXTENSION, new byte[] { 1 });
            File.WriteAllBytes(posPath, new byte[] { 1 });
            Elan container = new(eegPath, posPath, temp.GetPath("missing-notes.txt"), Array.Empty<Error>(), Array.Empty<Warning>());

            Assert.That(container.GetErrors(), Is.Empty);
        }

        [Test]
        public void ChangedFingerprint_MarksContentStale_WithoutReplayingIt()
        {
            using TempDirectoryScope temp = new();
            var csvPath = temp.GetPath("data.csv");
            File.WriteAllText(csvPath, "name,value\nrow,1");
            StaticDataInfo dataInfo = new("static", CreateProtocol("protocol", 10, 20), new CSV(csvPath, Array.Empty<Error>(), Array.Empty<Warning>()), Array.Empty<Error>(), Array.Empty<Warning>(), new Patient(), string.Empty, "static-data");
            dataInfo.CheckErrorsAndWarnings(new ValidationRequest(ValidationAspect.SourceAvailability | ValidationAspect.StaticContent, force: true));
            Assert.That(dataInfo.ValidationStates.Single(state => state.Aspect == ValidationAspect.StaticContent).Status, Is.EqualTo(ValidationStatus.Current));

            File.WriteAllText(csvPath, "name,value\nrow,not-a-number-and-a-different-length");
            dataInfo.CheckErrorsAndWarnings(new ValidationRequest(ValidationAspect.SourceAvailability, force: true));

            var staticState = dataInfo.ValidationStates.Single(state => state.Aspect == ValidationAspect.StaticContent);
            Assert.That(staticState.Status, Is.EqualTo(ValidationStatus.Stale));
            Assert.That(staticState.Errors, Is.Empty);
        }

        [Test]
        public async Task AvailabilityResult_WithChangedFingerprint_MarksExistingContentStale()
        {
            var csvPath = m_FixtureTemp.GetPath("scoped-fingerprint.csv");
            File.WriteAllText(csvPath, "name,value\nrow,1");
            StaticDataInfo dataInfo = new("static", CreateProtocol("protocol", 10, 20), new CSV(csvPath, Array.Empty<Error>(), Array.Empty<Warning>()), Array.Empty<Error>(), Array.Empty<Warning>(), new Patient(), string.Empty, "static-data");
            ValidationRequest initialRequest = new(ValidationAspect.SourceAvailability | ValidationAspect.StaticContent, force: true);
            var initial = await new DataInfoValidator().ValidateAsync(new[] { dataInfo }, initialRequest, 1, CancellationToken.None, generation: 7);
            Assert.That(initial.TryApply(7), Is.True);

            File.WriteAllText(csvPath, "name,value\nrow,not-a-number-and-a-different-length");
            ValidationRequest availabilityRequest = new(ValidationAspect.SourceAvailability, force: true);
            var availability = await new DataInfoValidator().ValidateAsync(new[] { dataInfo }, availabilityRequest, 1, CancellationToken.None, generation: 8);

            Assert.That(availability.TryApply(8), Is.True);
            Assert.That(dataInfo.ValidationStates.Single(state => state.Aspect == ValidationAspect.StaticContent).Status, Is.EqualTo(ValidationStatus.Stale));
        }

        [Test]
        public void ProtocolImpact_IgnoresSecondaryCodes_AndTargetsChangedMainSubBloc()
        {
            var before = CreateProtocol("protocol", 10, 20);
            var secondaryChanged = before.Clone() as Protocol;
            secondaryChanged.Blocs[0].SubBlocs[0].SecondaryEvents[0].Codes = new List<int>();

            var secondaryRequest = ValidationImpactAnalyzer.ForProtocols(new[] { before }, new[] { secondaryChanged });
            Assert.That(secondaryRequest.Aspects, Is.EqualTo(ValidationAspect.None));

            var mainChanged = before.Clone() as Protocol;
            var changedSubBloc = mainChanged.Blocs[0].SubBlocs[1];
            changedSubBloc.MainEvent.Codes = new List<int> { 200 };
            var mainRequest = ValidationImpactAnalyzer.ForProtocols(new[] { before }, new[] { mainChanged });

            Assert.That(mainRequest.Aspects, Is.EqualTo(ValidationAspect.Epoching));
            Assert.That(mainRequest.MatchesSubBloc(CreateIEEG("data", mainChanged, new Patient()), changedSubBloc), Is.True);
            Assert.That(mainRequest.GetTargetedSubBlocIDs(CreateIEEG("data-2", mainChanged, new Patient())), Is.EquivalentTo(new[] { changedSubBloc.ID }));
        }

        [Test]
        public void AliasImpact_TargetsOnlyPathsWhoseResolutionChanged()
        {
            var protocol = CreateProtocol("protocol", 10, 20);
            IEEGDataInfo affected = new("affected", protocol, new EDF("[ROOT]/affected.edf", Array.Empty<Error>(), Array.Empty<Warning>()), Array.Empty<Error>(), Array.Empty<Warning>(), new Patient(), NormalizationType.Auto, string.Empty, "affected");
            var unaffected = CreateIEEG("unaffected", protocol, new Patient());

            var request = ValidationImpactAnalyzer.ForAliases(new[] { new Alias("[ROOT]", "C:/old") }, new[] { new Alias("[ROOT]", "C:/new") }, new DataInfo[] { affected, unaffected }, Array.Empty<Patient>());

            Assert.That(request.Matches(affected, ValidationAspect.SourceAvailability), Is.True);
            Assert.That(request.Matches(unaffected, ValidationAspect.SourceAvailability), Is.False);
        }

        [Test]
        public void ContainerTypeChange_InvalidatesAllDataInfoAspects()
        {
            var protocol = CreateProtocol("protocol", 10, 20);
            Patient patient = new();
            var before = CreateIEEG("data", protocol, patient);
            IEEGDataInfo after = new(before.Name, protocol, new BrainVision("data.edf", Array.Empty<Error>(), Array.Empty<Warning>()), Array.Empty<Error>(), Array.Empty<Warning>(), patient, NormalizationType.Auto, string.Empty, before.ID);

            var request = ValidationImpactAnalyzer.ForDataInfo(before, after);

            Assert.That(request.Aspects, Is.EqualTo(ValidationAspect.DataInfoAll));
            Assert.That(request.Matches(after), Is.True);
        }

        [Test]
        public void DataInfoImpact_BulkComparisonPreservesPerDataInfoScopes()
        {
            Patient patient = new();
            var originalProtocol = CreateProtocol("original", 10, 20);
            var originalStructure = CreateIEEG("structure", originalProtocol, patient);
            var originalEpoching = CreateIEEG("epoching", originalProtocol, patient);
            var changedStructure = originalStructure.Clone() as IEEGDataInfo;
            var changedEpoching = originalEpoching.Clone() as IEEGDataInfo;
            changedStructure.Name = "renamed";
            changedEpoching.Protocol = CreateProtocol("replacement", 30, 40);

            var request = ValidationImpactAnalyzer.ForDataInfos(new DataInfo[] { originalStructure, originalEpoching }, new DataInfo[] { changedStructure, changedEpoching });

            Assert.That(request.Matches(changedStructure, ValidationAspect.Structure), Is.True);
            Assert.That(request.Matches(changedStructure, ValidationAspect.Epoching), Is.False);
            Assert.That(request.Matches(changedEpoching, ValidationAspect.Structure), Is.False);
            Assert.That(request.Matches(changedEpoching, ValidationAspect.Epoching), Is.True);
        }

        [Test]
        public void DatabaseSnapshot_UsesLightweightSignaturesForComparison()
        {
            Patient before = new("patient", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), new[] { new Site("A1", Array.Empty<Coordinate>(), Array.Empty<BaseTagValue>(), "site") }, Array.Empty<BaseTagValue>(), string.Empty, "patient");
            var snapshot = ValidationImpactAnalyzer.CaptureDatabase(new[] { before }, Array.Empty<DataInfo>());
            var after = before.Clone() as Patient;
            after.Sites[0].Name = "A2";

            var request = snapshot.Compare(new[] { after }, Array.Empty<DataInfo>());

            Assert.That(request.Aspects, Is.EqualTo(ValidationAspect.ChannelMapping));
            Assert.That(request.PatientIDs, Does.Contain(after.ID));
        }

        [Test]
        public void PatientImpact_IgnoresCoordinates_ButTargetsSiteRenames()
        {
            Patient before = new("patient", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), new[]
            {
                new Site("A1", Array.Empty<Coordinate>(), Array.Empty<BaseTagValue>(), "site")
            }, Array.Empty<BaseTagValue>(), string.Empty, "patient");
            var coordinatesChanged = before.Clone() as Patient;
            coordinatesChanged.Sites[0].Coordinates.Add(new Coordinate());

            var coordinateRequest = ValidationImpactAnalyzer.ForPatients(new[] { before }, new[] { coordinatesChanged });
            Assert.That(coordinateRequest.Aspects, Is.EqualTo(ValidationAspect.None));

            var renamed = before.Clone() as Patient;
            renamed.Sites[0].Name = "A2";
            var renameRequest = ValidationImpactAnalyzer.ForPatients(new[] { before }, new[] { renamed });
            Assert.That(renameRequest.Aspects, Is.EqualTo(ValidationAspect.ChannelMapping));
            Assert.That(renameRequest.PatientIDs, Does.Contain(renamed.ID));
        }

        [Test]
        public async Task PatientAssetState_CapturesFingerprint_AndRoundTrips()
        {
            using TempDirectoryScope temp = new();
            var MRIPath = temp.GetPath("patient.nii");
            File.WriteAllBytes(MRIPath, new byte[] { 1, 2, 3, 4 });
            Patient patient = new("patient", Array.Empty<BaseMesh>(), new[] { new MRI("MRI", MRIPath, "mri") }, Array.Empty<Site>(), Array.Empty<BaseTagValue>(), string.Empty, "patient");

            var result = await new AssetReferenceValidator().ValidatePatientsAsync(new[] { patient }, 1, CancellationToken.None, generation: 7);
            Assert.That(result.TryApply(7), Is.True);

            Assert.That(patient.IsAssetValidationCurrent, Is.True);
            Assert.That(patient.AssetValidationState.Signature, Does.Contain(":4:"));

            var path = temp.GetPath("patient.json");
            Assert.That(ClassLoaderSaver.SaveToJSon(patient, path, true), Is.True);
            var loaded = ClassLoaderSaver.LoadFromJson<Patient>(path);
            Assert.That(loaded.IsAssetValidationCurrent, Is.True);
            Assert.That(loaded.AssetValidationState.Signature, Is.EqualTo(patient.AssetValidationState.Signature));
        }

        [Test]
        public void DataInfoValidationStates_RoundTripWithVisibleDiagnostics()
        {
            using TempDirectoryScope temp = new();
            var missingPath = temp.GetPath("missing.csv");
            StaticDataInfo dataInfo = new("static", CreateProtocol("protocol", 10, 20), new CSV(missingPath, Array.Empty<Error>(), Array.Empty<Warning>()), Array.Empty<Error>(), Array.Empty<Warning>(), new Patient(), string.Empty, "static-data");
            dataInfo.CheckErrorsAndWarnings(new ValidationRequest(ValidationAspect.SourceAvailability, force: true));
            var path = temp.GetPath("data-info.json");

            Assert.That(ClassLoaderSaver.SaveToJSon(dataInfo, path, true), Is.True);
            var loaded = ClassLoaderSaver.LoadFromJson<StaticDataInfo>(path);

            Assert.That(loaded.ValidationStates.Any(state => state.Aspect == ValidationAspect.SourceAvailability && state.Status == ValidationStatus.Current), Is.True);
            Assert.That(loaded.Errors.OfType<FileDoesNotExistError>(), Is.Not.Empty);
        }

        private static IEEGDataInfo CreateIEEG(string id, Protocol protocol, Patient patient)
        {
            return new IEEGDataInfo(id, protocol, new EDF($"{id}.edf", Array.Empty<Error>(), Array.Empty<Warning>()), Array.Empty<Error>(), Array.Empty<Warning>(), patient, NormalizationType.Auto, string.Empty, id);
        }

        private static Protocol CreateProtocol(string id, int mainCode, int secondarySubBlocMainCode)
        {
            var main = CreateSubBloc($"{id}-main", MainSecondaryEnum.Main, new[] { mainCode, mainCode + 1 });
            var secondary = CreateSubBloc($"{id}-secondary", MainSecondaryEnum.Secondary, new[]
            {
                secondarySubBlocMainCode,
                secondarySubBlocMainCode + 1
            });
            Bloc bloc = new("bloc", 0, string.Empty, string.Empty, new[] { main, secondary }, $"{id}-bloc");
            return new Protocol(id, new[] { bloc }, id);
        }

        private static SubBloc CreateSubBloc(string id, MainSecondaryEnum type, IEnumerable<int> mainCodes)
        {
            Event mainEvent = new("main", mainCodes, MainSecondaryEnum.Main, $"{id}-main-event");
            Event secondaryEvent = new("secondary", new[] { 900 }, MainSecondaryEnum.Secondary, $"{id}-secondary-event");
            return new SubBloc(id, type == MainSecondaryEnum.Main ? 0 : 1, type, new TimeWindow(-100, 100), new TimeWindow(-100, 0), new[] { mainEvent, secondaryEvent }, Array.Empty<Icon>(), Array.Empty<Treatment>(), $"{id}-subbloc");
        }

        private sealed class CountingMetadataReader : IEEGValidationMetadataReader
        {
            private readonly EEGValidationMetadata m_Metadata;
            private readonly List<string> m_OpenedDataInfoIDs = new();

            public CountingMetadataReader(IEnumerable<int> triggerCodes, IEnumerable<string> channelLabels)
            {
                m_Metadata = new EEGValidationMetadata(triggerCodes, channelLabels);
            }

            public int OpenCount => m_OpenedDataInfoIDs.Count;
            public IReadOnlyList<string> OpenedDataInfoIDs => m_OpenedDataInfoIDs;

            public EEGValidationMetadata Read(DataInfo dataInfo)
            {
                m_OpenedDataInfoIDs.Add(dataInfo.ID);
                return m_Metadata;
            }
        }
    }
}
