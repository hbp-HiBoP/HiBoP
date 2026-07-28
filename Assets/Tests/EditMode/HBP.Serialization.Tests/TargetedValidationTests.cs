using HBP.Core.Data;
using HBP.Core.Data.Container;
using HBP.Core.Enums;
using HBP.Core.Errors;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            m_PersistentData =
                new PersistentDataTestScope(m_FixtureTemp.Path);
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
            Protocol protocol = CreateProtocol("protocol", 10, 20);
            Patient patient = new();
            IEEGDataInfo first = CreateIEEG(
                "first",
                protocol,
                patient);
            IEEGDataInfo second = CreateIEEG(
                "second",
                protocol,
                patient);
            ValidationRequest request = new ValidationRequest(
                    ValidationAspect.Epoching,
                    dataInfoIDs: new[] { first.ID })
                .Merge(new ValidationRequest(
                    ValidationAspect.ChannelMapping,
                    dataInfoIDs: new[] { second.ID }));

            Assert.That(
                request.Matches(first, ValidationAspect.Epoching),
                Is.True);
            Assert.That(
                request.Matches(first, ValidationAspect.ChannelMapping),
                Is.False);
            Assert.That(
                request.Matches(second, ValidationAspect.Epoching),
                Is.False);
            Assert.That(
                request.Matches(second, ValidationAspect.ChannelMapping),
                Is.True);
        }

        [Test]
        public async Task ProtocolRequest_OpensOnlyDataUsingThatProtocol()
        {
            Protocol firstProtocol =
                CreateProtocol("first-protocol", 10, 20);
            Protocol secondProtocol =
                CreateProtocol("second-protocol", 30, 40);
            Patient patient = new();
            IEEGDataInfo first =
                CreateIEEG("first", firstProtocol, patient);
            IEEGDataInfo second =
                CreateIEEG("second", secondProtocol, patient);
            CountingMetadataReader reader = new(
                new[] { 10, 20, 30, 40 },
                Array.Empty<string>());
            ValidationRequest request = new(
                ValidationAspect.Epoching |
                    ValidationAspect.ChannelMapping,
                protocolIDs: new[] { firstProtocol.ID },
                force: true);

            DataInfoValidationResult result =
                await new DataInfoValidator(reader).ValidateAsync(
                    new DataInfo[] { first, second },
                    request,
                    2,
                    CancellationToken.None);
            Assert.That(result.TryApply(0), Is.True);

            Assert.That(reader.OpenCount, Is.EqualTo(1));
            Assert.That(reader.OpenedDataInfoIDs, Is.EqualTo(new[] { first.ID }));
        }

        [Test]
        public async Task Epoching_ValidatesEverySubBlocMainEvent_AndAcceptsAlternativeCodes()
        {
            Protocol protocol = CreateProtocol("protocol", 10, 20);
            IEEGDataInfo dataInfo =
                CreateIEEG("data", protocol, new Patient());
            CountingMetadataReader reader = new(
                new[] { 11, 21 },
                Array.Empty<string>());
            ValidationRequest request = new(
                ValidationAspect.Epoching,
                dataInfoIDs: new[] { dataInfo.ID },
                force: true);

            DataInfoValidationResult result =
                await new DataInfoValidator(reader).ValidateAsync(
                    new[] { dataInfo },
                    request,
                    1,
                    CancellationToken.None);
            result.TryApply(0);

            Assert.That(reader.OpenCount, Is.EqualTo(1));
            Assert.That(
                dataInfo.ValidationStates
                    .Where(state =>
                        state.Aspect == ValidationAspect.Epoching)
                    .Select(state => state.ScopeID),
                Is.EquivalentTo(new[]
                {
                    "protocol-main-subbloc",
                    "protocol-secondary-subbloc"
                }));
            Assert.That(
                dataInfo.Warnings.OfType<BlocsCantBeEpochedWarning>(),
                Is.Empty);
        }

        [Test]
        public async Task Epoching_IgnoresMissingSecondaryEventCodes()
        {
            Protocol protocol = CreateProtocol("protocol", 10, 20);
            IEEGDataInfo dataInfo =
                CreateIEEG("data", protocol, new Patient());
            CountingMetadataReader reader = new(
                new[] { 10, 20 },
                Array.Empty<string>());

            DataInfoValidationResult result =
                await new DataInfoValidator(reader).ValidateAsync(
                    new[] { dataInfo },
                    new ValidationRequest(
                        ValidationAspect.Epoching,
                        force: true),
                    1,
                    CancellationToken.None);
            result.TryApply(0);

            Assert.That(
                dataInfo.Warnings.OfType<BlocsCantBeEpochedWarning>(),
                Is.Empty);
        }

        [Test]
        public async Task CcepChannelMapping_DoesNotOpenTheSource()
        {
            Protocol protocol = CreateProtocol("protocol", 10, 20);
            Patient patient = new();
            CCEPDataInfo dataInfo = new(
                "ccep",
                protocol,
                new EDF(
                    "missing.edf",
                    Array.Empty<Error>(),
                    Array.Empty<Warning>()),
                Array.Empty<Error>(),
                Array.Empty<Warning>(),
                patient,
                "A1",
                string.Empty,
                "ccep-data");
            CountingMetadataReader reader = new(
                Array.Empty<int>(),
                Array.Empty<string>());

            DataInfoValidationResult result =
                await new DataInfoValidator(reader).ValidateAsync(
                    new DataInfo[] { dataInfo },
                    new ValidationRequest(
                        ValidationAspect.ChannelMapping,
                        force: true),
                    1,
                    CancellationToken.None);
            result.TryApply(0);

            Assert.That(reader.OpenCount, Is.Zero);
            Assert.That(
                dataInfo.Errors.OfType<ChannelNotFoundError>().Count(),
                Is.EqualTo(1));
            Assert.That(
                dataInfo.Errors.OfType<FileDoesNotExistError>(),
                Is.Empty);
        }

        [Test]
        public void StartupValidation_DoesNotParseCsvContent()
        {
            using TempDirectoryScope temp = new();
            string csvPath = temp.GetPath("malformed.csv");
            File.WriteAllText(csvPath, "name,value\nrow,not-a-number");
            StaticDataInfo dataInfo = new(
                "static",
                CreateProtocol("protocol", 10, 20),
                new CSV(
                    csvPath,
                    Array.Empty<Error>(),
                    Array.Empty<Warning>()),
                Array.Empty<Error>(),
                Array.Empty<Warning>(),
                new Patient(),
                string.Empty,
                "static-data");

            dataInfo.CheckErrorsAndWarnings(
                new ValidationRequest(
                    ValidationAspect.SourceAvailability,
                    force: true));

            Assert.That(
                dataInfo.ValidationStates.Any(state =>
                    state.Aspect == ValidationAspect.SourceAvailability),
                Is.True);
            Assert.That(
                dataInfo.ValidationStates.Any(state =>
                    state.Aspect == ValidationAspect.StaticContent),
                Is.False);
            Assert.That(
                dataInfo.Errors.OfType<InvalidDataFileError>(),
                Is.Empty);
        }

        [Test]
        public void BrainVisionAvailability_RequiresDataAndMarkerCompanions()
        {
            using TempDirectoryScope temp = new();
            string headerPath = temp.GetPath("recording.vhdr");
            string dataPath = temp.GetPath("recording.eeg");
            File.WriteAllText(
                headerPath,
                "DataFile=recording.eeg\nMarkerFile=recording.vmrk");
            File.WriteAllBytes(dataPath, new byte[] { 1 });
            BrainVision container = new(
                headerPath,
                Array.Empty<Error>(),
                Array.Empty<Warning>());

            Error[] errors = container.GetErrors();

            Assert.That(errors, Has.Length.EqualTo(1));
            Assert.That(
                errors[0],
                Is.TypeOf<FileDoesNotExistError>());
            Assert.That(
                errors[0].Message,
                Does.Contain("recording.vmrk"));
        }

        [Test]
        public void ElanAvailability_IgnoresMissingOptionalNotes()
        {
            using TempDirectoryScope temp = new();
            string eegPath = temp.GetPath("recording.eeg");
            string posPath = temp.GetPath("recording.pos");
            File.WriteAllBytes(eegPath, new byte[] { 1 });
            File.WriteAllBytes(
                eegPath + Elan.HEADER_EXTENSION,
                new byte[] { 1 });
            File.WriteAllBytes(posPath, new byte[] { 1 });
            Elan container = new(
                eegPath,
                posPath,
                temp.GetPath("missing-notes.txt"),
                Array.Empty<Error>(),
                Array.Empty<Warning>());

            Assert.That(container.GetErrors(), Is.Empty);
        }

        [Test]
        public void ChangedFingerprint_MarksContentStale_WithoutReplayingIt()
        {
            using TempDirectoryScope temp = new();
            string csvPath = temp.GetPath("data.csv");
            File.WriteAllText(csvPath, "name,value\nrow,1");
            StaticDataInfo dataInfo = new(
                "static",
                CreateProtocol("protocol", 10, 20),
                new CSV(
                    csvPath,
                    Array.Empty<Error>(),
                    Array.Empty<Warning>()),
                Array.Empty<Error>(),
                Array.Empty<Warning>(),
                new Patient(),
                string.Empty,
                "static-data");
            dataInfo.CheckErrorsAndWarnings(
                new ValidationRequest(
                    ValidationAspect.SourceAvailability |
                        ValidationAspect.StaticContent,
                    force: true));
            Assert.That(
                dataInfo.ValidationStates.Single(state =>
                    state.Aspect == ValidationAspect.StaticContent).Status,
                Is.EqualTo(ValidationStatus.Current));

            File.WriteAllText(
                csvPath,
                "name,value\nrow,not-a-number-and-a-different-length");
            dataInfo.CheckErrorsAndWarnings(
                new ValidationRequest(
                    ValidationAspect.SourceAvailability,
                    force: true));

            ValidationState staticState =
                dataInfo.ValidationStates.Single(state =>
                    state.Aspect == ValidationAspect.StaticContent);
            Assert.That(
                staticState.Status,
                Is.EqualTo(ValidationStatus.Stale));
            Assert.That(
                staticState.Errors,
                Is.Empty);
        }

        [Test]
        public void ProtocolImpact_IgnoresSecondaryCodes_AndTargetsChangedMainSubBloc()
        {
            Protocol before = CreateProtocol("protocol", 10, 20);
            Protocol secondaryChanged = before.Clone() as Protocol;
            secondaryChanged.Blocs[0]
                .SubBlocs[0]
                .SecondaryEvents[0]
                .Codes = new List<int>();

            ValidationRequest secondaryRequest =
                ValidationImpactAnalyzer.ForProtocols(
                    new[] { before },
                    new[] { secondaryChanged });
            Assert.That(
                secondaryRequest.Aspects,
                Is.EqualTo(ValidationAspect.None));

            Protocol mainChanged = before.Clone() as Protocol;
            SubBloc changedSubBloc = mainChanged.Blocs[0].SubBlocs[1];
            changedSubBloc.MainEvent.Codes = new List<int> { 200 };
            ValidationRequest mainRequest =
                ValidationImpactAnalyzer.ForProtocols(
                    new[] { before },
                    new[] { mainChanged });

            Assert.That(
                mainRequest.Aspects,
                Is.EqualTo(ValidationAspect.Epoching));
            Assert.That(
                mainRequest.MatchesSubBloc(
                    CreateIEEG("data", mainChanged, new Patient()),
                    changedSubBloc),
                Is.True);
            Assert.That(
                mainRequest.GetTargetedSubBlocIDs(
                    CreateIEEG("data-2", mainChanged, new Patient())),
                Is.EquivalentTo(new[] { changedSubBloc.ID }));
        }

        [Test]
        public void AliasImpact_TargetsOnlyPathsWhoseResolutionChanged()
        {
            Protocol protocol = CreateProtocol("protocol", 10, 20);
            IEEGDataInfo affected = new(
                "affected",
                protocol,
                new EDF(
                    "[ROOT]/affected.edf",
                    Array.Empty<Error>(),
                    Array.Empty<Warning>()),
                Array.Empty<Error>(),
                Array.Empty<Warning>(),
                new Patient(),
                NormalizationType.Auto,
                string.Empty,
                "affected");
            IEEGDataInfo unaffected = CreateIEEG(
                "unaffected",
                protocol,
                new Patient());

            ValidationRequest request =
                ValidationImpactAnalyzer.ForAliases(
                    new[] { new Alias("[ROOT]", "C:/old") },
                    new[] { new Alias("[ROOT]", "C:/new") },
                    new DataInfo[] { affected, unaffected },
                    Array.Empty<Patient>());

            Assert.That(
                request.Matches(
                    affected,
                    ValidationAspect.SourceAvailability),
                Is.True);
            Assert.That(
                request.Matches(
                    unaffected,
                    ValidationAspect.SourceAvailability),
                Is.False);
        }

        [Test]
        public void ContainerTypeChange_InvalidatesAllDataInfoAspects()
        {
            Protocol protocol = CreateProtocol("protocol", 10, 20);
            Patient patient = new();
            IEEGDataInfo before = CreateIEEG(
                "data",
                protocol,
                patient);
            IEEGDataInfo after = new(
                before.Name,
                protocol,
                new BrainVision(
                    "data.edf",
                    Array.Empty<Error>(),
                    Array.Empty<Warning>()),
                Array.Empty<Error>(),
                Array.Empty<Warning>(),
                patient,
                NormalizationType.Auto,
                string.Empty,
                before.ID);

            ValidationRequest request =
                ValidationImpactAnalyzer.ForDataInfo(
                    before,
                    after);

            Assert.That(
                request.Aspects,
                Is.EqualTo(ValidationAspect.DataInfoAll));
            Assert.That(request.Matches(after), Is.True);
        }

        [Test]
        public void PatientImpact_IgnoresCoordinates_ButTargetsSiteRenames()
        {
            Patient before = new(
                "patient",
                Array.Empty<BaseMesh>(),
                Array.Empty<MRI>(),
                new[]
                {
                    new Site(
                        "A1",
                        Array.Empty<Coordinate>(),
                        Array.Empty<BaseTagValue>(),
                        "site")
                },
                Array.Empty<BaseTagValue>(),
                string.Empty,
                "patient");
            Patient coordinatesChanged = before.Clone() as Patient;
            coordinatesChanged.Sites[0].Coordinates.Add(new Coordinate());

            ValidationRequest coordinateRequest =
                ValidationImpactAnalyzer.ForPatients(
                    new[] { before },
                    new[] { coordinatesChanged });
            Assert.That(
                coordinateRequest.Aspects,
                Is.EqualTo(ValidationAspect.None));

            Patient renamed = before.Clone() as Patient;
            renamed.Sites[0].Name = "A2";
            ValidationRequest renameRequest =
                ValidationImpactAnalyzer.ForPatients(
                    new[] { before },
                    new[] { renamed });
            Assert.That(
                renameRequest.Aspects,
                Is.EqualTo(ValidationAspect.ChannelMapping));
            Assert.That(
                renameRequest.PatientIDs,
                Does.Contain(renamed.ID));
        }

        [Test]
        public async Task PatientAssetState_CapturesFingerprint_AndRoundTrips()
        {
            using TempDirectoryScope temp = new();
            string MRIPath = temp.GetPath("patient.nii");
            File.WriteAllBytes(MRIPath, new byte[] { 1, 2, 3, 4 });
            Patient patient = new(
                "patient",
                Array.Empty<BaseMesh>(),
                new[] { new MRI("MRI", MRIPath, "mri") },
                Array.Empty<Site>(),
                Array.Empty<BaseTagValue>(),
                string.Empty,
                "patient");

            PatientAssetValidationResult result =
                await new AssetReferenceValidator().ValidatePatientsAsync(
                    new[] { patient },
                    1,
                    CancellationToken.None,
                    generation: 7);
            Assert.That(result.TryApply(7), Is.True);

            Assert.That(patient.IsAssetValidationCurrent, Is.True);
            Assert.That(
                patient.AssetValidationState.Signature,
                Does.Contain(":4:"));

            string path = temp.GetPath("patient.json");
            Assert.That(
                ClassLoaderSaver.SaveToJSon(patient, path, true),
                Is.True);
            Patient loaded =
                ClassLoaderSaver.LoadFromJson<Patient>(path);
            Assert.That(loaded.IsAssetValidationCurrent, Is.True);
            Assert.That(
                loaded.AssetValidationState.Signature,
                Is.EqualTo(patient.AssetValidationState.Signature));
        }

        [Test]
        public void DataInfoValidationStates_RoundTripWithVisibleDiagnostics()
        {
            using TempDirectoryScope temp = new();
            string missingPath = temp.GetPath("missing.csv");
            StaticDataInfo dataInfo = new(
                "static",
                CreateProtocol("protocol", 10, 20),
                new CSV(
                    missingPath,
                    Array.Empty<Error>(),
                    Array.Empty<Warning>()),
                Array.Empty<Error>(),
                Array.Empty<Warning>(),
                new Patient(),
                string.Empty,
                "static-data");
            dataInfo.CheckErrorsAndWarnings(
                new ValidationRequest(
                    ValidationAspect.SourceAvailability,
                    force: true));
            string path = temp.GetPath("data-info.json");

            Assert.That(
                ClassLoaderSaver.SaveToJSon(dataInfo, path, true),
                Is.True);
            StaticDataInfo loaded =
                ClassLoaderSaver.LoadFromJson<StaticDataInfo>(path);

            Assert.That(
                loaded.ValidationStates.Any(state =>
                    state.Aspect ==
                        ValidationAspect.SourceAvailability &&
                    state.Status == ValidationStatus.Current),
                Is.True);
            Assert.That(
                loaded.Errors.OfType<FileDoesNotExistError>(),
                Is.Not.Empty);
        }

        private static IEEGDataInfo CreateIEEG(
            string id,
            Protocol protocol,
            Patient patient)
        {
            return new IEEGDataInfo(
                id,
                protocol,
                new EDF(
                    $"{id}.edf",
                    Array.Empty<Error>(),
                    Array.Empty<Warning>()),
                Array.Empty<Error>(),
                Array.Empty<Warning>(),
                patient,
                NormalizationType.Auto,
                string.Empty,
                id);
        }

        private static Protocol CreateProtocol(
            string id,
            int mainCode,
            int secondarySubBlocMainCode)
        {
            SubBloc main = CreateSubBloc(
                $"{id}-main",
                MainSecondaryEnum.Main,
                new[] { mainCode, mainCode + 1 });
            SubBloc secondary = CreateSubBloc(
                $"{id}-secondary",
                MainSecondaryEnum.Secondary,
                new[]
                {
                    secondarySubBlocMainCode,
                    secondarySubBlocMainCode + 1
                });
            Bloc bloc = new(
                "bloc",
                0,
                string.Empty,
                string.Empty,
                new[] { main, secondary },
                $"{id}-bloc");
            return new Protocol(id, new[] { bloc }, id);
        }

        private static SubBloc CreateSubBloc(
            string id,
            MainSecondaryEnum type,
            IEnumerable<int> mainCodes)
        {
            HBP.Core.Data.Event mainEvent = new(
                "main",
                mainCodes,
                MainSecondaryEnum.Main,
                $"{id}-main-event");
            HBP.Core.Data.Event secondaryEvent = new(
                "secondary",
                new[] { 900 },
                MainSecondaryEnum.Secondary,
                $"{id}-secondary-event");
            return new SubBloc(
                id,
                type == MainSecondaryEnum.Main ? 0 : 1,
                type,
                new TimeWindow(-100, 100),
                new TimeWindow(-100, 0),
                new[] { mainEvent, secondaryEvent },
                Array.Empty<Icon>(),
                Array.Empty<Treatment>(),
                $"{id}-subbloc");
        }

        private sealed class CountingMetadataReader :
            IEEGValidationMetadataReader
        {
            private readonly EEGValidationMetadata m_Metadata;
            private readonly List<string> m_OpenedDataInfoIDs = new();

            public int OpenCount => m_OpenedDataInfoIDs.Count;
            public IReadOnlyList<string> OpenedDataInfoIDs =>
                m_OpenedDataInfoIDs;

            public CountingMetadataReader(
                IEnumerable<int> triggerCodes,
                IEnumerable<string> channelLabels)
            {
                m_Metadata = new EEGValidationMetadata(
                    triggerCodes,
                    channelLabels);
            }

            public EEGValidationMetadata Read(DataInfo dataInfo)
            {
                m_OpenedDataInfoIDs.Add(dataInfo.ID);
                return m_Metadata;
            }
        }
    }
}
