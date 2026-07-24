using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HBP.Core.Database;
using HBP.Core.Data;
using HBP.Core.Data.Container;
using HBP.Core.Enums;
using HBP.Core.Errors;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using PersistentDataManager = HBP.Core.Preferences.PersistentDataManager;

namespace HBP.Tests.Serialization
{
    public class ProtocolDatasetDataContainerTests
    {
        [Test]
        public void Protocol_BasicAndAdvancedBlocsWithAllTreatments_RoundTrip()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Protocol roughBasicProtocol = new(
                "protocol-dataset-basic-protocol",
                new[] { new Bloc("basic-bloc", 0, string.Empty, string.Empty, Array.Empty<SubBloc>(), "protocol-dataset-basic-bloc-001") },
                "protocol-dataset-basic-protocol-001");

            roughBasicProtocol.SetBasicProtocolFeatures();

            Assert.That(roughBasicProtocol.Blocs[0].MainSubBloc, Is.Not.Null);
            Assert.That(roughBasicProtocol.Blocs[0].MainSubBloc.MainEvent.Name, Is.EqualTo("basic-bloc"));
            Assert.That(roughBasicProtocol.Blocs[0].Sort, Is.EqualTo("Main_basic-bloc_CODE"));

            Protocol basicProtocol = new(
                "protocol-dataset-basic-protocol",
                new[]
                {
                    new Bloc(
                        "basic-bloc",
                        0,
                        string.Empty,
                        "main_stimulus_CODE",
                        new[]
                        {
                            new SubBloc(
                                "main",
                                0,
                                MainSecondaryEnum.Main,
                                new TimeWindow(-100, 250),
                                new TimeWindow(-100, 0),
                                new[] { new Event("stimulus", new[] { 1 }, MainSecondaryEnum.Main, "protocol-dataset-basic-event-001") },
                                Array.Empty<Icon>(),
                                Array.Empty<Treatment>(),
                                "protocol-dataset-basic-subbloc-001")
                        },
                        "protocol-dataset-basic-bloc-visualizable-001")
                },
                "protocol-dataset-basic-protocol-visualizable-001");
            basicProtocol.SetBasicProtocolFeatures();

            Assert.That(basicProtocol.IsAdvanced, Is.False);
            Assert.That(basicProtocol.IsVisualizable, Is.True);

            Protocol advancedProtocol = CreateAdvancedProtocolWithAllTreatments();
            Protocol loaded = RoundTrip(temp, advancedProtocol, "protocol-dataset-advanced-protocol.json");
            Bloc loadedAdvancedBloc = loaded.Blocs.Single(bloc => bloc.Name == "advanced-bloc");

            Assert.That(loaded.IsAdvanced, Is.True);
            Assert.That(loaded.Blocs, Has.Count.EqualTo(2));
            Assert.That(loadedAdvancedBloc.GetSortingMethodError(), Is.EqualTo(Bloc.SortingMethodError.NoError));
            Assert.That(loadedAdvancedBloc.SubBlocs.Select(subBloc => subBloc.Type), Is.EquivalentTo(new[] { MainSecondaryEnum.Main, MainSecondaryEnum.Secondary }));
            Assert.That(loadedAdvancedBloc.MainSubBloc.Events.Select(e => e.ID), Is.EquivalentTo(new[] { "protocol-dataset-event-main-001", "protocol-dataset-event-secondary-001", "protocol-dataset-event-secondary-002" }));
            Assert.That(loadedAdvancedBloc.MainSubBloc.Icons.Select(icon => icon.ID), Is.EquivalentTo(new[] { "protocol-dataset-icon-main-001", "protocol-dataset-icon-secondary-001" }));
            Assert.That(
                loadedAdvancedBloc.MainSubBloc.Treatments.Select(treatment => treatment.GetType()),
                Is.EquivalentTo(new[]
                {
                    typeof(AbsTreatment),
                    typeof(ClampTreatment),
                    typeof(FactorTreatment),
                    typeof(MeanTreatment),
                    typeof(MedianTreatment),
                    typeof(MinTreatment),
                    typeof(MaxTreatment),
                    typeof(OffsetTreatment),
                    typeof(RescaleTreatment),
                    typeof(ThresholdTreatment)
                }));
            Assert.That(loadedAdvancedBloc.MainSubBloc.Treatments.Select(treatment => treatment.ID), Is.EquivalentTo(AllTreatmentIds()));
            Assert.That(loadedAdvancedBloc.MainSubBloc.Treatments.Select(treatment => treatment.Order), Is.EquivalentTo(Enumerable.Range(0, 10)));
            Assert.That(((ClampTreatment)loadedAdvancedBloc.MainSubBloc.Treatments.Single(t => t is ClampTreatment)).Max, Is.EqualTo(2.5f).Within(0.0001f));
            Assert.That(((RescaleTreatment)loadedAdvancedBloc.MainSubBloc.Treatments.Single(t => t is RescaleTreatment)).AfterMax, Is.EqualTo(1.5f).Within(0.0001f));
        }

        [Test]
        public void Dataset_DataInfoAndContainerVariants_RoundTripPreservesProtocolAndPatients()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            BoolTag patientTag = new("protocol-dataset-include", "protocol-dataset-patient-tag-001");
            StringTag siteTag = new("protocol-dataset-region", "protocol-dataset-site-tag-001");
            PersistentDataManager.Tags.SetPatientTags(new BaseTag[] { patientTag }, false);
            PersistentDataManager.Tags.SetSiteTags(new BaseTag[] { siteTag }, false);

            Protocol protocol = SyntheticProjectFactory.CreateProtocol();
            Patient patient = SyntheticProjectFactory.CreatePatient(patientTag, siteTag);
            DatabaseManager.Database.SetProtocols(new[] { protocol });
            Project contextProject = new(
                "protocol-dataset-project",
                new ProjectPreferences("protocol-dataset-version", "protocol-dataset-project-preferences-001"),
                new[] { patient },
                Array.Empty<Group>(),
                Array.Empty<Dataset>(),
                Array.Empty<Visualization>());
            ApplicationState.LoadedProject = contextProject;

            Dataset source = SyntheticProjectFactory.CreateDataset(protocol, patient);
            Dataset loaded = RoundTrip(temp, source, "protocol-dataset-dataset.json");
            LoadingContext context = new(
                PersistentDataManager.Tags.AllTags,
                new[] { protocol },
                new[] { patient },
                new[] { loaded });
            context.ResolveProject(
                new[] { patient },
                Array.Empty<Group>(),
                new[] { loaded },
                Array.Empty<Visualization>());

            Assert.That(loaded.Protocol, Is.SameAs(protocol));
            Assert.That(loaded.Data.Select(data => data.Protocol), Is.All.SameAs(protocol));
            Assert.That(loaded.GetStaticDataInfos(), Has.Length.EqualTo(1));
            Assert.That(loaded.GetIEEGDataInfos(), Has.Length.EqualTo(2));
            Assert.That(loaded.GetCCEPDataInfos(), Has.Length.EqualTo(1));
            Assert.That(loaded.GetFMRIDataInfos(), Has.Length.EqualTo(1));
            Assert.That(loaded.GetSharedFMRIDataInfos(), Has.Length.EqualTo(1));
            Assert.That(loaded.GetMEGDataInfos(), Has.Length.EqualTo(2));
            Assert.That(loaded.GetPatientDataInfos().Select(data => data.Patient), Is.All.SameAs(patient));
            Assert.That(loaded.GetIEEGDataInfos().Single(data => data.Name == "signal-alpha").Normalization, Is.EqualTo(NormalizationType.Auto));
            Assert.That(loaded.GetCCEPDataInfos()[0].StimulatedChannel, Is.EqualTo("channel-alpha"));
            Assert.That(loaded.GetFMRIDataInfos()[0].MaskDataContainer.ID, Is.EqualTo("synthetic-container-mask-001"));
            Assert.That(loaded.GetSharedFMRIDataInfos()[0].MaskDataContainer.ID, Is.EqualTo("synthetic-container-shared-mask-001"));

            Assert.That(
                loaded.Data.Select(data => data.DataContainer.GetType()),
                Is.EquivalentTo(new[]
                {
                    typeof(Elan),
                    typeof(Micromed),
                    typeof(EDF),
                    typeof(Nifti),
                    typeof(BrainVision),
                    typeof(FIF),
                    typeof(CSV),
                    typeof(Nifti)
                }));
        }

        [Test]
        public void DataContainerPaths_AliasNormalizeAndConvertBackToFullPaths()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            string aliasRoot = temp.GetPath("protocol-dataset-data-root");
            Directory.CreateDirectory(aliasRoot);
            string csvPath = Path.Combine(aliasRoot, "signals.csv");
            File.WriteAllText(csvPath, "site,value");
            PersistentDataManager.Aliases.SetAliases(new[] { new Alias("[PROTOCOL_DATASET_DATA]", aliasRoot, "protocol-dataset-alias-001") }, false);

            CSV source = new(csvPath, Array.Empty<Error>(), Array.Empty<Warning>(), "protocol-dataset-csv-container-001");
            string jsonPath = temp.GetPath("protocol-dataset-csv.json");

            Assert.That(ClassLoaderSaver.SaveToJSon(source, jsonPath, true), Is.True);
            string json = File.ReadAllText(jsonPath);
            CSV loaded = ClassLoaderSaver.LoadFromJson<CSV>(jsonPath);

            Assert.That(json, Does.Contain("[PROTOCOL_DATASET_DATA]"));
            Assert.That(json, Does.Not.Contain(aliasRoot.Replace("\\", "\\\\")));
            Assert.That(loaded.File, Is.EqualTo(csvPath));

            loaded.ConvertAllPathsToFullPaths();

            Assert.That(loaded.SavedFile, Is.EqualTo(csvPath));
        }

        [Test]
        public void DataContainers_ReportMissingAndUnsupportedFilesPredictably()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            string wrongPath = temp.GetPath("wrong-format.txt");
            File.WriteAllText(wrongPath, "synthetic");
            string missingPath = temp.GetPath("missing-file.dat");

            DataContainer[] missingContainers =
            {
                new CSV(missingPath, Array.Empty<Error>(), Array.Empty<Warning>()),
                new BrainVision(missingPath, Array.Empty<Error>(), Array.Empty<Warning>()),
                new EDF(missingPath, Array.Empty<Error>(), Array.Empty<Warning>()),
                new Elan(missingPath, missingPath, string.Empty, Array.Empty<Error>(), Array.Empty<Warning>()),
                new FIF(missingPath, Array.Empty<Error>(), Array.Empty<Warning>()),
                new Micromed(missingPath, Array.Empty<Error>(), Array.Empty<Warning>()),
                new Nifti(missingPath, Array.Empty<Error>(), Array.Empty<Warning>())
            };
            DataContainer[] unsupportedContainers =
            {
                new CSV(wrongPath, Array.Empty<Error>(), Array.Empty<Warning>()),
                new BrainVision(wrongPath, Array.Empty<Error>(), Array.Empty<Warning>()),
                new EDF(wrongPath, Array.Empty<Error>(), Array.Empty<Warning>()),
                new Elan(wrongPath, wrongPath, string.Empty, Array.Empty<Error>(), Array.Empty<Warning>()),
                new FIF(wrongPath, Array.Empty<Error>(), Array.Empty<Warning>()),
                new Micromed(wrongPath, Array.Empty<Error>(), Array.Empty<Warning>()),
                new Nifti(wrongPath, Array.Empty<Error>(), Array.Empty<Warning>())
            };

            foreach (DataContainer container in missingContainers)
            {
                Assert.That(container.GetErrors().Any(error => error is FileDoesNotExistError), Is.True, container.GetType().Name);
            }

            foreach (DataContainer container in unsupportedContainers)
            {
                Assert.That(container.GetErrors().Any(error => error is WrongExtensionError), Is.True, container.GetType().Name);
            }

            Assert.That(new CSV(string.Empty, Array.Empty<Error>(), Array.Empty<Warning>()).GetErrors().Any(error => error is RequiredFieldEmptyError), Is.True);
        }

        private static Protocol CreateAdvancedProtocolWithAllTreatments()
        {
            TimeWindow mainWindow = new(-150, 350);
            TimeWindow baseline = new(-150, -25);
            Event mainEvent = new("stimulus", new[] { 10, 11 }, MainSecondaryEnum.Main, "protocol-dataset-event-main-001");
            Event responseEvent = new("response", new[] { 20 }, MainSecondaryEnum.Secondary, "protocol-dataset-event-secondary-001");
            Event markerEvent = new("marker", new[] { 30 }, MainSecondaryEnum.Secondary, "protocol-dataset-event-secondary-002");
            SubBloc mainSubBloc = new(
                "main",
                1,
                MainSecondaryEnum.Main,
                mainWindow,
                baseline,
                new[] { mainEvent, responseEvent, markerEvent },
                new[]
                {
                    new Icon("stimulus-icon", string.Empty, new TimeWindow(-20, 20), "protocol-dataset-icon-main-001"),
                    new Icon("response-icon", string.Empty, new TimeWindow(50, 120), "protocol-dataset-icon-secondary-001")
                },
                CreateAllTreatments(),
                "protocol-dataset-subbloc-main-001");
            SubBloc secondarySubBloc = new(
                "secondary",
                0,
                MainSecondaryEnum.Secondary,
                new TimeWindow(-50, 150),
                new TimeWindow(-50, 0),
                new[] { new Event("secondary-main", new[] { 40 }, MainSecondaryEnum.Main, "protocol-dataset-event-secondary-main-001") },
                Array.Empty<Icon>(),
                Array.Empty<Treatment>(),
                "protocol-dataset-subbloc-secondary-001");
            Bloc advancedBloc = new(
                "advanced-bloc",
                1,
                string.Empty,
                "main_response_LATENCY;main_stimulus_CODE",
                new[] { mainSubBloc, secondarySubBloc },
                "protocol-dataset-bloc-advanced-001");
            Bloc comparisonBloc = new(
                "comparison-bloc",
                0,
                string.Empty,
                "main_stimulus_CODE",
                new[]
                {
                    new SubBloc(
                        "main",
                        0,
                        MainSecondaryEnum.Main,
                        new TimeWindow(-100, 250),
                        new TimeWindow(-100, 0),
                        new[] { new Event("stimulus", new[] { 12 }, MainSecondaryEnum.Main, "protocol-dataset-event-comparison-main-001") },
                        Array.Empty<Icon>(),
                        Array.Empty<Treatment>(),
                        "protocol-dataset-subbloc-comparison-001")
                },
                "protocol-dataset-bloc-comparison-001");

            return new Protocol("protocol-dataset-advanced-protocol", new[] { comparisonBloc, advancedBloc }, "protocol-dataset-advanced-protocol-001");
        }

        private static Treatment[] CreateAllTreatments()
        {
            TimeWindow window = new(-10, 10);
            TimeWindow baseline = new(-20, -5);
            return new Treatment[]
            {
                new AbsTreatment(true, window, false, baseline, 0, "protocol-dataset-treatment-abs-001"),
                new ClampTreatment(true, window, true, baseline, true, -2.5f, true, 2.5f, 1, "protocol-dataset-treatment-clamp-001"),
                new FactorTreatment(true, window, false, baseline, 1.5f, 2, "protocol-dataset-treatment-factor-001"),
                new MeanTreatment(true, window, true, baseline, 3, "protocol-dataset-treatment-mean-001"),
                new MedianTreatment(true, window, false, baseline, 4, "protocol-dataset-treatment-median-001"),
                new MinTreatment(true, window, true, baseline, 5, "protocol-dataset-treatment-min-001"),
                new MaxTreatment(true, window, false, baseline, 6, "protocol-dataset-treatment-max-001"),
                new OffsetTreatment(true, window, true, baseline, -0.25f, 7, "protocol-dataset-treatment-offset-001"),
                new RescaleTreatment(true, window, false, baseline, -5f, 5f, -1.5f, 1.5f, 8, "protocol-dataset-treatment-rescale-001"),
                new ThresholdTreatment(true, window, true, baseline, true, -0.5f, true, 0.5f, 9, "protocol-dataset-treatment-threshold-001")
            };
        }

        private static string[] AllTreatmentIds()
        {
            return new[]
            {
                "protocol-dataset-treatment-abs-001",
                "protocol-dataset-treatment-clamp-001",
                "protocol-dataset-treatment-factor-001",
                "protocol-dataset-treatment-mean-001",
                "protocol-dataset-treatment-median-001",
                "protocol-dataset-treatment-min-001",
                "protocol-dataset-treatment-max-001",
                "protocol-dataset-treatment-offset-001",
                "protocol-dataset-treatment-rescale-001",
                "protocol-dataset-treatment-threshold-001"
            };
        }

        private static T RoundTrip<T>(TempDirectoryScope temp, T source, string fileName) where T : new()
        {
            string path = temp.GetPath(fileName);
            Assert.That(ClassLoaderSaver.SaveToJSon(source, path, true), Is.True);
            return ClassLoaderSaver.LoadFromJson<T>(path);
        }
    }
}
