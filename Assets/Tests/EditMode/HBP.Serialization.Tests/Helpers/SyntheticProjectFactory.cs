using System.Collections.Generic;
using HBP.Core.Database;
using HBP.Core.Data;
using HBP.Core.Data.Container;
using HBP.Core.Enums;
using HBP.Core.Errors;
using HBP.Core.Tools;
using UnityEngine;
using PersistentDataManager = HBP.Core.Preferences.PersistentDataManager;

namespace HBP.Tests.Serialization.Helpers
{
    internal static class SyntheticProjectFactory
    {
        public const string ProjectName = "synthetic-project-alpha";
        public const string ProjectId = "synthetic-project-preferences-001";
        public const string ProtocolId = "synthetic-protocol-001";
        public const string PatientId = "synthetic-patient-001";
        public const string SiteId = "synthetic-site-001";
        public const string DatasetId = "synthetic-dataset-001";
        public const string VisualizationId = "synthetic-visualization-001";
        public const string PlotTagId = "synthetic-plot-tag-001";

        public static Project CreateMinimalProject()
        {
            return new Project(ProjectName, new HBP.Core.Data.ProjectPreferences("test-version", ProjectId));
        }

        public static Project CreateCompleteProject()
        {
            Protocol protocol = CreateProtocol();
            DatabaseManager.Database.SetProtocols(new[] { protocol });

            StringTag plotTag = new("plot-marker-alpha", PlotTagId);
            BoolTag inclusionTag = new("flag-alpha", "synthetic-bool-tag-001");
            PersistentDataManager.Tags.SetGeneralTags(new BaseTag[] { plotTag }, false);
            PersistentDataManager.Tags.SetPatientTags(new BaseTag[] { inclusionTag }, false);
            PersistentDataManager.Tags.SetSiteTags(new BaseTag[] { plotTag }, false);

            Patient patient = CreatePatient(inclusionTag, plotTag);
            Dataset dataset = CreateDataset(protocol, patient);
            Visualization visualization = CreateVisualization(patient, dataset, protocol.Blocs[0]);
            Group group = new("synthetic-group-alpha", new[] { patient }, "synthetic-group-001");

            Project project = new(
                ProjectName,
                new HBP.Core.Data.ProjectPreferences("test-version", ProjectId),
                new[] { patient },
                new[] { group },
                new[] { dataset },
                new[] { visualization });

            ApplicationState.LoadedProject = project;
            return project;
        }

        public static Protocol CreateProtocol()
        {
            HBP.Core.Data.Event mainEvent = new("event-alpha", new[] { 11 }, MainSecondaryEnum.Main, "synthetic-event-001");
            TimeWindow window = new(-100, 250);
            SubBloc subBloc = new(
                "subbloc-alpha",
                0,
                MainSecondaryEnum.Main,
                window,
                new TimeWindow(-100, 0),
                new[] { mainEvent },
                new[] { new Icon("icon-alpha", string.Empty, window, "synthetic-icon-001") },
                new Treatment[] { new MeanTreatment(true, window, false, new TimeWindow(), 0, "synthetic-treatment-001") },
                "synthetic-subbloc-001");
            Bloc bloc = new("bloc-alpha", 0, string.Empty, "subbloc-alpha_event-alpha_CODE", new[] { subBloc }, "synthetic-bloc-001");
            return new Protocol("protocol-alpha", new[] { bloc }, ProtocolId);
        }

        public static Patient CreatePatient(BoolTag patientTag, StringTag siteTag)
        {
            Site site = new(
                "site-alpha",
                new[] { new Coordinate("synthetic-space", new Vector3(1, 2, 3), "synthetic-coordinate-001") },
                new BaseTagValue[] { new StringTagValue(siteTag, "plot-value-alpha", "synthetic-site-tag-value-001") },
                SiteId);

            return new Patient(
                "patient-alpha",
                new BaseMesh[0],
                new MRI[0],
                new[] { site },
                new BaseTagValue[] { new BoolTagValue(patientTag, true, "synthetic-patient-tag-value-001") },
                "synthetic-database-link-001",
                PatientId);
        }

        public static Dataset CreateDataset(Protocol protocol, Patient patient)
        {
            Error[] errors = new Error[0];
            Warning[] warnings = new Warning[0];
            DataInfo[] data =
            {
                new IEEGDataInfo("signal-alpha", protocol, new Elan("synthetic.eeg", "synthetic.pos", "", errors, warnings, "synthetic-container-elan-001"), errors, warnings, patient, NormalizationType.Auto, "synthetic-db-001", "synthetic-data-ieeg-001"),
                new CCEPDataInfo("response-alpha", protocol, new EDF("synthetic.edf", errors, warnings, "synthetic-container-edf-001"), errors, warnings, patient, "channel-alpha", "synthetic-db-001", "synthetic-data-ccep-001"),
                new FMRIDataInfo("fmri-alpha", protocol, new Nifti("synthetic-fmri.nii", errors, warnings, "synthetic-container-fmri-001"), new Nifti("synthetic-mask.nii", errors, warnings, "synthetic-container-mask-001"), errors, warnings, patient, "synthetic-db-001", "synthetic-data-fmri-001"),
                new MEGcDataInfo("megc-alpha", protocol, new BrainVision("synthetic.vhdr", errors, warnings, "synthetic-container-brainvision-001"), errors, warnings, patient, "synthetic-db-001", "synthetic-data-megc-001"),
                new MEGvDataInfo("megv-alpha", protocol, new FIF("synthetic.fif", errors, warnings, "synthetic-container-fif-001"), new Nifti("synthetic-megv-mask.nii", errors, warnings, "synthetic-container-megv-mask-001"), errors, warnings, patient, "synthetic-db-001", "synthetic-data-megv-001"),
                new StaticDataInfo("static-alpha", protocol, new CSV("synthetic.csv", errors, warnings, "synthetic-container-csv-001"), errors, warnings, patient, "synthetic-db-001", "synthetic-data-static-001"),
                new SharedFMRIDataInfo("shared-fmri-alpha", protocol, new Nifti("synthetic-shared.nii", errors, warnings, "synthetic-container-shared-001"), new Nifti("synthetic-shared-mask.nii", errors, warnings, "synthetic-container-shared-mask-001"), errors, warnings, "synthetic-db-001", "synthetic-data-shared-fmri-001")
            };

            return new Dataset("dataset-alpha", protocol, data, DatasetId);
        }

        public static Visualization CreateVisualization(Patient patient, Dataset dataset, Bloc bloc)
        {
            BaseConfiguration baseConfiguration = new(
                0.75f,
                new Dictionary<string, SiteConfiguration>
                {
                    { SiteId, new SiteConfiguration(false, true, Color.cyan, new[] { "label-alpha" }, "synthetic-site-config-001") }
                },
                "synthetic-base-config-001");

            Column[] columns =
            {
                new AnatomicColumn("anatomic-alpha", baseConfiguration, new AnatomicConfiguration("synthetic-anatomic-config-001"), "synthetic-column-anatomic-001"),
                new IEEGColumn("ieeg-alpha", baseConfiguration.Clone() as BaseConfiguration, dataset, "signal-alpha", bloc, new DynamicConfiguration(12, -1, 0, 1, "synthetic-dynamic-config-001"), "synthetic-column-ieeg-001"),
                new CCEPColumn("ccep-alpha", baseConfiguration.Clone() as BaseConfiguration, dataset, "response-alpha", bloc, new DynamicConfiguration(10, -2, 0, 2, "synthetic-ccep-config-001"), "synthetic-column-ccep-001"),
                new FMRIColumn("fmri-alpha", baseConfiguration.Clone() as BaseConfiguration, dataset, new FMRIConfiguration(0.1f, 0.5f, 0.1f, 0.5f, false, true, false, "synthetic-fmri-config-001"), "synthetic-column-fmri-001"),
                new MEGColumn("meg-alpha", baseConfiguration.Clone() as BaseConfiguration, dataset, new MEGConfiguration(0.1f, 0.5f, 0.1f, 0.5f, true, false, false, "synthetic-meg-config-001"), "synthetic-column-meg-001"),
                new StaticColumn("static-alpha", baseConfiguration.Clone() as BaseConfiguration, dataset, "static-alpha", new StaticConfiguration(9, -1, 0, 1, "synthetic-static-config-001"), "synthetic-column-static-001")
            };

            return new Visualization("visualization-alpha", new[] { patient }, columns, new VisualizationConfiguration(), VisualizationId);
        }
    }
}
