using System;
using System.IO;
using System.Linq;
using System.Threading;
using HBP.Core.Data;
using HBP.Core.Database;
using HBP.Core.Exceptions;
using HBP.Core.Tools;
using HBP.Data.BIDS;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEngine;
using LocalizersObjects = HBP.Core.Object3D.LocalizersObjects;

namespace HBP.Tests.Serialization
{
    public class BidsLocalizerDatabaseWorkflowTests
    {
        [Test]
        public void BidsDatabase_LoadsSyntheticParticipantsSessionsModalitiesAndIeegData()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Protocol protocol = SyntheticProjectFactory.CreateProtocol();
            DatabaseManager.Database.SetProtocols(new[] { protocol });
            string bidsRoot = CreateSyntheticBidsDatabase(temp, protocol.Name);
            DatabaseReference reference = new(
                "bids-localizer-database-bids",
                DatabaseType.BIDS,
                bidsRoot,
                new BIDSDatabaseParameters(),
                DateTime.MinValue,
                "bids-localizer-database-bids-reference-001");

            var discovered = BIDSParser.FindFiles(
                bidsRoot,
                new[] { "T1w", "ieeg", "electrodes" },
                new[] { ".nii", ".vhdr", ".json", ".tsv" }).ToList();

            Patient.LoadFromBIDSDatabase(reference, out Patient[] patients, null, CancellationToken.None);
            DataInfo.LoadFromBIDSDatabase(reference, patients.ToList(), out DataInfo[] dataInfos, null, CancellationToken.None);

            Assert.That(discovered.Select(file => file.Suffix), Is.SupersetOf(new[] { "T1w", "ieeg", "electrodes" }));
            Assert.That(discovered.Any(file => file.Extension == ".json" && file.Suffix == "ieeg"), Is.True);
            Assert.That(discovered.Single(file => file.Suffix == "T1w").Entities["ses"], Is.EqualTo("pre"));
            Assert.That(patients, Has.Length.EqualTo(1));
            Assert.That(patients[0].Name, Is.EqualTo("sub-01"));
            Assert.That(patients[0].MRIs.Single().Name, Is.EqualTo("T1w (pre)"));
            Assert.That(patients[0].Tags.Select(tag => tag.Tag.Name), Does.Contain("sex"));
            Assert.That(dataInfos, Has.Length.EqualTo(1));
            Assert.That(dataInfos[0].Name, Is.EqualTo("raw-01-clean"));
            Assert.That(dataInfos[0].Protocol, Is.SameAs(protocol));
            Assert.That(((PatientDataInfo)dataInfos[0]).Patient, Is.SameAs(patients[0]));
        }

        [Test]
        public void BidsExportConfigurationAndGeneratedPaths_RoundTripAndCreateExpectedFiles()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            BIDSExportConfiguration configuration = new()
            {
                Version = "bids-localizer-database",
                AnatomicalRules =
                {
                    new AnatomicalDataRule
                    {
                        DataType = "MRI",
                        SourceName = "preimplantation",
                        BIDSSuffix = "T1w",
                        BIDSSession = "pre"
                    }
                },
                CoordinateSystemRules =
                {
                    new CoordinateSystemRule { CoordinateSystemName = "scanner", BIDSSpace = "" },
                    new CoordinateSystemRule { CoordinateSystemName = "mni", BIDSSpace = "MNI152Lin" }
                }
            };
            BIDSExportConfiguration loadedConfiguration = RoundTrip(temp, configuration, "bids-localizer-database-bids-export-config.json");

            string sourceMri = temp.GetPath("bids-localizer-database-source-t1w.nii");
            File.WriteAllText(sourceMri, "synthetic mri");
            Patient patient = new(
                "bids-localizer-database patient",
                Array.Empty<BaseMesh>(),
                new[] { new MRI("preimplantation", sourceMri, "bids-localizer-database-mri-001") },
                new[]
                {
                    new Site(
                        "A1",
                        new[]
                        {
                            new Coordinate("scanner", new Vector3(1, 2, 3), "bids-localizer-database-coordinate-scanner-001"),
                            new Coordinate("mni", new Vector3(4, 5, 6), "bids-localizer-database-coordinate-mni-001")
                        },
                        Array.Empty<BaseTagValue>(),
                        "bids-localizer-database-site-001")
                },
                Array.Empty<BaseTagValue>(),
                "",
                "bids-localizer-database-patient-001");
            BIDSPatient bidsPatient = new(patient, Array.Empty<Protocol>(), Array.Empty<string>(), "01");
            string exportRoot = temp.GetPath("bids-localizer-database-export");
            Directory.CreateDirectory(exportRoot);

            BIDSUtility.ExportPatient(bidsPatient, exportRoot, loadedConfiguration, Array.Empty<BaseTag>());

            string participantFolder = Path.Combine(exportRoot, "sub-01");
            string anatFolder = Path.Combine(participantFolder, "ses-pre", "anat");
            string ieegFolder = Path.Combine(participantFolder, "ses-post", "ieeg");

            Assert.That(loadedConfiguration.Version, Is.EqualTo("bids-localizer-database"));
            Assert.That(loadedConfiguration.AnatomicalRules.Single().BIDSSuffix, Is.EqualTo("T1w"));
            Assert.That(File.Exists(Path.Combine(anatFolder, "sub-01_ses-pre_T1w.nii")), Is.True);
            Assert.That(File.Exists(Path.Combine(ieegFolder, "sub-01_ses-post_electrodes.tsv")), Is.True);
            Assert.That(File.Exists(Path.Combine(ieegFolder, "sub-01_ses-post_coordsystem.json")), Is.True);
            Assert.That(File.Exists(Path.Combine(ieegFolder, "sub-01_ses-post_space-MNI152Lin_electrodes.tsv")), Is.True);
            Assert.That(File.Exists(Path.Combine(ieegFolder, "sub-01_ses-post_space-MNI152Lin_coordsystem.json")), Is.True);
            Assert.That(File.ReadAllText(Path.Combine(ieegFolder, "sub-01_ses-post_electrodes.tsv")), Does.Contain("A1\t1.000\t2.000\t3.000"));
        }

        [Test]
        public void BidsImport_ReportsMissingRequiredParticipantsMetadata()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            string bidsRoot = temp.GetPath("bids-localizer-database-missing-participants");
            Directory.CreateDirectory(Path.Combine(bidsRoot, "sub-01", "ses-pre", "anat"));
            DatabaseReference reference = new(
                "bids-localizer-database-bids-missing",
                DatabaseType.BIDS,
                bidsRoot,
                new BIDSDatabaseParameters(),
                DateTime.MinValue,
                "bids-localizer-database-bids-reference-missing-001");

            HBPException exception = Assert.Catch<HBPException>(() =>
                Patient.LoadFromBIDSDatabase(reference, out _, null, CancellationToken.None));

            Assert.That(exception.Title, Is.EqualTo("Missing file"));
            Assert.That(exception.Message, Does.Contain("participants.tsv"));
        }

        [Test]
        public void DatabaseReferences_ForSupportedWorkflowTypes_RoundTripParameters()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            DatabaseReference[] references =
            {
                new("bids-localizer-database-brainvisa", DatabaseType.Brainvisa, temp.GetPath("brainvisa"), new BrainvisaDatabaseParameters(), DateTime.UtcNow, "bids-localizer-database-db-brainvisa-001"),
                new(
                    "bids-localizer-database-localizer",
                    DatabaseType.Localizer,
                    temp.GetPath("localizer"),
                    new LocalizerDatabaseParameters
                    {
                        IncludeRaw = true,
                        Frequencies = new[] { "f8f24" },
                        TemporalSmoothings = new[] { "sm0", "sm250" }
                    },
                    DateTime.UtcNow,
                    "bids-localizer-database-db-localizer-001"),
                new("bids-localizer-database-bids", DatabaseType.BIDS, temp.GetPath("bids"), new BIDSDatabaseParameters(), DateTime.UtcNow, "bids-localizer-database-db-bids-001"),
                new("bids-localizer-database-tags", DatabaseType.Tags, temp.GetPath("tags"), new TagsDatabaseParameters(), DateTime.UtcNow, "bids-localizer-database-db-tags-001")
            };

            DatabaseReference[] loaded = references
                .Select(reference => RoundTrip(temp, reference, reference.ID + DatabaseReference.EXTENSION))
                .ToArray();

            Assert.That(loaded.Select(reference => reference.Type), Is.EquivalentTo(new[] { DatabaseType.Brainvisa, DatabaseType.Localizer, DatabaseType.BIDS, DatabaseType.Tags }));
            Assert.That(loaded.Single(reference => reference.Type == DatabaseType.Brainvisa).Parameters, Is.TypeOf<BrainvisaDatabaseParameters>());
            Assert.That(loaded.Single(reference => reference.Type == DatabaseType.BIDS).Parameters, Is.TypeOf<BIDSDatabaseParameters>());
            Assert.That(loaded.Single(reference => reference.Type == DatabaseType.Tags).Parameters, Is.TypeOf<TagsDatabaseParameters>());

            LocalizerDatabaseParameters localizerParameters = (LocalizerDatabaseParameters)loaded.Single(reference => reference.Type == DatabaseType.Localizer).Parameters;
            Assert.That(localizerParameters.IncludeRaw, Is.True);
            Assert.That(localizerParameters.Frequencies, Is.EquivalentTo(new[] { "f8f24" }));
            Assert.That(localizerParameters.TemporalSmoothings, Is.EquivalentTo(new[] { "sm0", "sm250" }));
        }

        [Test]
        public void LocalizersDiscovery_BuildsProtocolDataAndBlocSelectionWithoutScene()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            string dataFolder = Path.Combine(ApplicationState.DataPath, "Atlases", "Localizers", "protocol-alpha", "signal-alpha");
            Directory.CreateDirectory(dataFolder);
            File.WriteAllText(Path.Combine(dataFolder, "bloc-one.nii"), "synthetic localizer");
            File.WriteAllText(Path.Combine(dataFolder, "bloc-two.nii.gz"), "synthetic localizer");
            File.WriteAllText(Path.Combine(dataFolder, "bloc-two_MASK.nii.gz"), "synthetic mask");

            LocalizersObjects localizers = new();

            Assert.That(localizers.AvailableProtocolNames, Is.EquivalentTo(new[] { "protocol-alpha" }));
            Assert.That(localizers.AvailableDataNames, Is.EquivalentTo(new[] { "signal-alpha" }));
            Assert.That(localizers.IsAvailable("protocol-alpha"), Is.True);
            Assert.That(localizers.GetAvailableBlocNames("protocol-alpha"), Is.EquivalentTo(new[] { "bloc-one", "bloc-two" }));
            Assert.That(localizers.Protocols, Is.Empty);
        }

        private static string CreateSyntheticBidsDatabase(TempDirectoryScope temp, string protocolName)
        {
            string bidsRoot = temp.GetPath("bids-localizer-database-bids");
            string anatFolder = Path.Combine(bidsRoot, "sub-01", "ses-pre", "anat");
            string ieegFolder = Path.Combine(bidsRoot, "sub-01", "ses-post", "ieeg");
            Directory.CreateDirectory(anatFolder);
            Directory.CreateDirectory(ieegFolder);

            File.WriteAllLines(Path.Combine(bidsRoot, "participants.tsv"), new[]
            {
                "participant_id\tsex\tgroup",
                "sub-01\tf\tsynthetic"
            });
            File.WriteAllText(Path.Combine(anatFolder, "sub-01_ses-pre_T1w.nii"), "synthetic mri");
            File.WriteAllText(Path.Combine(ieegFolder, $"sub-01_ses-post_task-{protocolName}_acq-raw_run-01_desc-clean_ieeg.vhdr"), "Brain Vision Data Exchange Header File Version 1.0");
            File.WriteAllText(Path.Combine(ieegFolder, $"sub-01_ses-post_task-{protocolName}_acq-raw_run-01_desc-clean_ieeg.json"), "{}");
            File.WriteAllLines(Path.Combine(ieegFolder, "sub-01_ses-post_space-scanner_electrodes.tsv"), new[]
            {
                "name\tx\ty\tz",
                "A1\t1\t2\t3"
            });

            return bidsRoot;
        }

        private static T RoundTrip<T>(TempDirectoryScope temp, T source, string fileName) where T : new()
        {
            string path = temp.GetPath(fileName);
            Assert.That(ClassLoaderSaver.SaveToJSon(source, path, true), Is.True);
            return ClassLoaderSaver.LoadFromJson<T>(path);
        }
    }
}
