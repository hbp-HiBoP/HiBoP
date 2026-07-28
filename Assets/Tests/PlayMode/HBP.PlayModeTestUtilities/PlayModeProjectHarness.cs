using System.Collections.Generic;
using HBP.Core.Database;
using HBP.Core.Data;
using HBP.Core.Data.Container;
using HBP.Core.Enums;
using HBP.Core.Errors;
using HBP.Core.Tools;
using UnityEngine;
using PersistentDataManager = HBP.Core.Preferences.PersistentDataManager;

namespace HBP.Tests.PlayMode.Utilities
{
    public static class PlayModeProjectHarness
    {
        public const string ProjectName = "playmode-synthetic-project";
        public const string ProjectId = "playmode-project-preferences-001";
        public const string ProtocolId = "playmode-protocol-001";
        public const string PatientId = "playmode-patient-001";
        public const string SiteId = "playmode-site-001";
        public const string DatasetId = "playmode-dataset-001";
        public const string VisualizationId = "playmode-visualization-001";
        public const string GroupId = "playmode-group-001";

        public static Project CreateAndLoadMinimalProject(string name = "playmode-project")
        {
            Project project = new(name, new ProjectPreferences("playmode-test", $"{name}-preferences"));
            ApplicationState.LoadedProject = project;
            return project;
        }

        public static Project CreateAndLoadCompleteProject()
        {
            Protocol protocol = CreateProtocol();
            DatabaseManager.Database.SetProtocols(new[] { protocol });

            StringTag siteTag = new("playmode-site-marker", "playmode-site-tag-001");
            BoolTag patientTag = new("playmode-patient-flag", "playmode-patient-tag-001");
            PersistentDataManager.Tags.SetGeneralTags(new BaseTag[] { siteTag }, false);
            PersistentDataManager.Tags.SetPatientTags(new BaseTag[] { patientTag }, false);
            PersistentDataManager.Tags.SetSiteTags(new BaseTag[] { siteTag }, false);

            Patient patient = CreatePatient(patientTag, siteTag);
            Dataset dataset = CreateDataset(protocol, patient);
            Visualization visualization = CreateVisualization(patient, dataset, protocol.Blocs[0]);
            Group group = new("playmode-group-alpha", new[] { patient }, GroupId);

            Project project = new(ProjectName, new ProjectPreferences("playmode-test", ProjectId), new[] { patient }, new[] { group }, new[] { dataset }, new[] { visualization });

            ApplicationState.LoadedProject = project;
            return project;
        }

        public static Protocol CreateProtocol()
        {
            HBP.Core.Data.Event mainEvent = new("playmode-event-alpha", new[] { 11 }, MainSecondaryEnum.Main, "playmode-event-001");
            TimeWindow window = new(-100, 250);
            SubBloc subBloc = new("playmode-subbloc-alpha", 0, MainSecondaryEnum.Main, window, new TimeWindow(-100, 0), new[] { mainEvent }, new[] { new Icon("playmode-icon-alpha", string.Empty, window, "playmode-icon-001") }, new Treatment[] { new MeanTreatment(true, window, false, new TimeWindow(), 0, "playmode-treatment-001") }, "playmode-subbloc-001");
            Bloc bloc = new("playmode-bloc-alpha", 0, string.Empty, "playmode-subbloc-alpha_event-alpha_CODE", new[] { subBloc }, "playmode-bloc-001");
            return new Protocol("playmode-protocol-alpha", new[] { bloc }, ProtocolId);
        }

        private static Patient CreatePatient(BoolTag patientTag, StringTag siteTag)
        {
            Site site = new("playmode-site-alpha", new[] { new Coordinate("playmode-space", new Vector3(1, 2, 3), "playmode-coordinate-001") }, new BaseTagValue[] { new StringTagValue(siteTag, "playmode-plot-value", "playmode-site-tag-value-001") }, SiteId);

            return new Patient("playmode-patient-alpha", new BaseMesh[0], new MRI[0], new[] { site }, new BaseTagValue[] { new BoolTagValue(patientTag, true, "playmode-patient-tag-value-001") }, "playmode-database-link-001", PatientId);
        }

        private static Dataset CreateDataset(Protocol protocol, Patient patient)
        {
            Error[] errors = new Error[0];
            Warning[] warnings = new Warning[0];
            DataInfo[] data =
            {
                new IEEGDataInfo("playmode-signal-alpha", protocol, new Elan("playmode.eeg", "playmode.pos", "", errors, warnings, "playmode-container-elan-001"), errors, warnings, patient, NormalizationType.Auto, "playmode-db-001", "playmode-data-ieeg-001"),
                new IEEGDataInfo("playmode-signal-micromed-alpha", protocol, new Micromed("playmode.trc", errors, warnings, "playmode-container-micromed-001"), errors, warnings, patient, NormalizationType.Auto, "playmode-db-001", "playmode-data-ieeg-micromed-001"),
                new CCEPDataInfo("playmode-response-alpha", protocol, new EDF("playmode.edf", errors, warnings, "playmode-container-edf-001"), errors, warnings, patient, "channel-alpha", "playmode-db-001", "playmode-data-ccep-001"),
                new FMRIDataInfo("playmode-fmri-alpha", protocol, new Nifti("playmode-fmri.nii", errors, warnings, "playmode-container-fmri-001"), new Nifti("playmode-mask.nii", errors, warnings, "playmode-container-mask-001"), errors, warnings, patient, "playmode-db-001", "playmode-data-fmri-001"),
                new MEGcDataInfo("playmode-megc-alpha", protocol, new BrainVision("playmode.vhdr", errors, warnings, "playmode-container-brainvision-001"), errors, warnings, patient, "playmode-db-001", "playmode-data-megc-001"),
                new MEGvDataInfo("playmode-megv-alpha", protocol, new FIF("playmode.fif", errors, warnings, "playmode-container-fif-001"), new Nifti("playmode-megv-mask.nii", errors, warnings, "playmode-container-megv-mask-001"), errors, warnings, patient, "playmode-db-001", "playmode-data-megv-001"),
                new SharedFMRIDataInfo("playmode-shared-fmri-alpha", protocol, new Nifti("playmode-shared.nii", errors, warnings, "playmode-container-shared-001"), new Nifti("playmode-shared-mask.nii", errors, warnings, "playmode-container-shared-mask-001"), errors, warnings, "playmode-db-001", "playmode-data-shared-fmri-001"),
                new StaticDataInfo("playmode-static-alpha", protocol, new CSV("playmode.csv", errors, warnings, "playmode-container-csv-001"), errors, warnings, patient, "playmode-db-001", "playmode-data-static-001")
            };

            return new Dataset("playmode-dataset-alpha", protocol, data, DatasetId);
        }

        private static Visualization CreateVisualization(Patient patient, Dataset dataset, Bloc bloc)
        {
            Column[] columns =
            {
                new AnatomicColumn("playmode-anatomic-alpha", CreateBaseConfiguration("anatomic"), new AnatomicConfiguration("playmode-anatomic-config-001"), "playmode-column-anatomic-001"),
                new IEEGColumn("playmode-ieeg-alpha", CreateBaseConfiguration("ieeg"), dataset, "playmode-signal-alpha", bloc, new DynamicConfiguration(12, -1, 0, 1, "playmode-dynamic-config-001"), "playmode-column-ieeg-001"),
                new CCEPColumn("playmode-ccep-alpha", CreateBaseConfiguration("ccep"), dataset, "playmode-response-alpha", bloc, new DynamicConfiguration(10, -2, 0, 2, "playmode-ccep-config-001"), "playmode-column-ccep-001"),
                new StaticColumn("playmode-static-alpha", CreateBaseConfiguration("static"), dataset, "playmode-static-alpha", new StaticConfiguration(9, -1, 0, 1, "playmode-static-config-001"), "playmode-column-static-001")
            };

            return new Visualization("playmode-visualization-alpha", new[] { patient }, columns, new VisualizationConfiguration(), VisualizationId);
        }

        private static BaseConfiguration CreateBaseConfiguration(string suffix)
        {
            return new BaseConfiguration(0.75f, new Dictionary<string, SiteConfiguration>
                {
                    { SiteId, new SiteConfiguration(false, true, Color.cyan, new[] { "playmode-label-alpha" }, $"playmode-site-config-{suffix}-001") }
                }, $"playmode-base-config-{suffix}-001");
        }
    }
}
