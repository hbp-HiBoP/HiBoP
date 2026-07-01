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
                "phase4-basic-protocol",
                new[] { new Bloc("basic-bloc", 0, string.Empty, string.Empty, Array.Empty<SubBloc>(), "phase4-basic-bloc-001") },
                "phase4-basic-protocol-001");

            roughBasicProtocol.SetBasicProtocolFeatures();

            Assert.That(roughBasicProtocol.Blocs[0].MainSubBloc, Is.Not.Null);
            Assert.That(roughBasicProtocol.Blocs[0].MainSubBloc.MainEvent.Name, Is.EqualTo("basic-bloc"));
            Assert.That(roughBasicProtocol.Blocs[0].Sort, Is.EqualTo("Main_basic-bloc_CODE"));

            Protocol basicProtocol = new(
                "phase4-basic-protocol",
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
                                new[] { new Event("stimulus", new[] { 1 }, MainSecondaryEnum.Main, "phase4-basic-event-001") },
                                Array.Empty<Icon>(),
                                Array.Empty<Treatment>(),
                                "phase4-basic-subbloc-001")
                        },
                        "phase4-basic-bloc-visualizable-001")
                },
                "phase4-basic-protocol-visualizable-001");
            basicProtocol.SetBasicProtocolFeatures();

            Assert.That(basicProtocol.IsAdvanced, Is.False);
            Assert.That(basicProtocol.IsVisualizable, Is.True);

            Protocol advancedProtocol = CreateAdvancedProtocolWithAllTreatments();
            Protocol loaded = RoundTrip(temp, advancedProtocol, "phase4-advanced-protocol.json");
            Bloc loadedAdvancedBloc = loaded.Blocs.Single(bloc => bloc.Name == "advanced-bloc");

            Assert.That(loaded.IsAdvanced, Is.True);
            Assert.That(loaded.Blocs, Has.Count.EqualTo(2));
            Assert.That(loadedAdvancedBloc.GetSortingMethodError(), Is.EqualTo(Bloc.SortingMethodError.NoError));
            Assert.That(loadedAdvancedBloc.SubBlocs.Select(subBloc => subBloc.Type), Is.EquivalentTo(new[] { MainSecondaryEnum.Main, MainSecondaryEnum.Secondary }));
            Assert.That(loadedAdvancedBloc.MainSubBloc.Events.Select(e => e.ID), Is.EquivalentTo(new[] { "phase4-event-main-001", "phase4-event-secondary-001", "phase4-event-secondary-002" }));
            Assert.That(loadedAdvancedBloc.MainSubBloc.Icons.Select(icon => icon.ID), Is.EquivalentTo(new[] { "phase4-icon-main-001", "phase4-icon-secondary-001" }));
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

            BoolTag patientTag = new("phase4-include", "phase4-patient-tag-001");
            StringTag siteTag = new("phase4-region", "phase4-site-tag-001");
            PersistentDataManager.Tags.SetPatientTags(new BaseTag[] { patientTag }, false);
            PersistentDataManager.Tags.SetSiteTags(new BaseTag[] { siteTag }, false);

            Protocol protocol = SyntheticProjectFactory.CreateProtocol();
            Patient patient = SyntheticProjectFactory.CreatePatient(patientTag, siteTag);
            DatabaseManager.Database.SetProtocols(new[] { protocol });
            Project contextProject = new(
                "phase4-project",
                new ProjectPreferences("phase4-version", "phase4-project-preferences-001"),
                new[] { patient },
                Array.Empty<Group>(),
                Array.Empty<Dataset>(),
                Array.Empty<Visualization>());
            ApplicationState.LoadedProject = contextProject;

            Dataset source = SyntheticProjectFactory.CreateDataset(protocol, patient);
            Dataset loaded = RoundTrip(temp, source, "phase4-dataset.json");
            contextProject.SetDatasets(new[] { loaded });
            foreach (PatientDataInfo patientDataInfo in loaded.GetPatientDataInfos())
            {
                patientDataInfo.UpdatePatient();
            }

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

            string aliasRoot = temp.GetPath("phase4-data-root");
            Directory.CreateDirectory(aliasRoot);
            string csvPath = Path.Combine(aliasRoot, "signals.csv");
            File.WriteAllText(csvPath, "site,value");
            PersistentDataManager.Aliases.SetAliases(new[] { new Alias("[PHASE4_DATA]", aliasRoot, "phase4-alias-001") }, false);

            CSV source = new(csvPath, Array.Empty<Error>(), Array.Empty<Warning>(), "phase4-csv-container-001");
            string jsonPath = temp.GetPath("phase4-csv.json");

            Assert.That(ClassLoaderSaver.SaveToJSon(source, jsonPath, true), Is.True);
            string json = File.ReadAllText(jsonPath);
            CSV loaded = ClassLoaderSaver.LoadFromJson<CSV>(jsonPath);

            Assert.That(json, Does.Contain("[PHASE4_DATA]"));
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
            Event mainEvent = new("stimulus", new[] { 10, 11 }, MainSecondaryEnum.Main, "phase4-event-main-001");
            Event responseEvent = new("response", new[] { 20 }, MainSecondaryEnum.Secondary, "phase4-event-secondary-001");
            Event markerEvent = new("marker", new[] { 30 }, MainSecondaryEnum.Secondary, "phase4-event-secondary-002");
            SubBloc mainSubBloc = new(
                "main",
                1,
                MainSecondaryEnum.Main,
                mainWindow,
                baseline,
                new[] { mainEvent, responseEvent, markerEvent },
                new[]
                {
                    new Icon("stimulus-icon", string.Empty, new TimeWindow(-20, 20), "phase4-icon-main-001"),
                    new Icon("response-icon", string.Empty, new TimeWindow(50, 120), "phase4-icon-secondary-001")
                },
                CreateAllTreatments(),
                "phase4-subbloc-main-001");
            SubBloc secondarySubBloc = new(
                "secondary",
                0,
                MainSecondaryEnum.Secondary,
                new TimeWindow(-50, 150),
                new TimeWindow(-50, 0),
                new[] { new Event("secondary-main", new[] { 40 }, MainSecondaryEnum.Main, "phase4-event-secondary-main-001") },
                Array.Empty<Icon>(),
                Array.Empty<Treatment>(),
                "phase4-subbloc-secondary-001");
            Bloc advancedBloc = new(
                "advanced-bloc",
                1,
                string.Empty,
                "main_response_LATENCY;main_stimulus_CODE",
                new[] { mainSubBloc, secondarySubBloc },
                "phase4-bloc-advanced-001");
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
                        new[] { new Event("stimulus", new[] { 12 }, MainSecondaryEnum.Main, "phase4-event-comparison-main-001") },
                        Array.Empty<Icon>(),
                        Array.Empty<Treatment>(),
                        "phase4-subbloc-comparison-001")
                },
                "phase4-bloc-comparison-001");

            return new Protocol("phase4-advanced-protocol", new[] { comparisonBloc, advancedBloc }, "phase4-advanced-protocol-001");
        }

        private static Treatment[] CreateAllTreatments()
        {
            TimeWindow window = new(-10, 10);
            TimeWindow baseline = new(-20, -5);
            return new Treatment[]
            {
                new AbsTreatment(true, window, false, baseline, 0, "phase4-treatment-abs-001"),
                new ClampTreatment(true, window, true, baseline, true, -2.5f, true, 2.5f, 1, "phase4-treatment-clamp-001"),
                new FactorTreatment(true, window, false, baseline, 1.5f, 2, "phase4-treatment-factor-001"),
                new MeanTreatment(true, window, true, baseline, 3, "phase4-treatment-mean-001"),
                new MedianTreatment(true, window, false, baseline, 4, "phase4-treatment-median-001"),
                new MinTreatment(true, window, true, baseline, 5, "phase4-treatment-min-001"),
                new MaxTreatment(true, window, false, baseline, 6, "phase4-treatment-max-001"),
                new OffsetTreatment(true, window, true, baseline, -0.25f, 7, "phase4-treatment-offset-001"),
                new RescaleTreatment(true, window, false, baseline, -5f, 5f, -1.5f, 1.5f, 8, "phase4-treatment-rescale-001"),
                new ThresholdTreatment(true, window, true, baseline, true, -0.5f, true, 0.5f, 9, "phase4-treatment-threshold-001")
            };
        }

        private static string[] AllTreatmentIds()
        {
            return new[]
            {
                "phase4-treatment-abs-001",
                "phase4-treatment-clamp-001",
                "phase4-treatment-factor-001",
                "phase4-treatment-mean-001",
                "phase4-treatment-median-001",
                "phase4-treatment-min-001",
                "phase4-treatment-max-001",
                "phase4-treatment-offset-001",
                "phase4-treatment-rescale-001",
                "phase4-treatment-threshold-001"
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
