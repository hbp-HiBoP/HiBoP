using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Data.Container;
using HBP.Core.Database;
using HBP.Core.Enums;
using HBP.Core.Errors;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using UnityEditor;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    internal static class NativeFixtureProjectGenerator
    {
        private const string NativeAlias = "[HIBOP_NATIVE_FIXTURES]";
        private const string NativeFixturesFolder = "Assets/Tests/Fixtures/Native";
        private const string ProjectsOutputFolder = "Assets/Tests/Fixtures/Projects/Generated";

        [MenuItem("Tools/HiBoP Tests/Generate Native Fixture Projects")]
        public static async void Generate()
        {
            try
            {
                string repositoryRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string nativeFixturesRoot = Path.GetFullPath(Path.Combine(repositoryRoot, NativeFixturesFolder));
                string outputFolder = Path.GetFullPath(Path.Combine(repositoryRoot, ProjectsOutputFolder));
                Directory.CreateDirectory(outputFolder);

                using TempDirectoryScope temp = new();
                using ApplicationStateTestScope applicationState = new(temp.Path);
                using PersistentDataTestScope persistentData = new(temp.Path);

                PersistentDataManager.Aliases.SetAliases(new[] { new Alias(NativeAlias, nativeFixturesRoot, "native-fixtures-alias-001") }, false);

                string[] generated =
                {
                    await SaveProjectAsync(CreateNamedMinimalProject(), outputFolder),
                    await SaveProjectAsync(CreateNamedCompleteProject(), outputFolder),
                    await SaveProjectAsync(CreateNativeReferenceProject(), outputFolder)
                };

                AssetDatabase.Refresh();
                Debug.Log($"Generated HiBoP fixture projects:\n{string.Join("\n", generated)}");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static Project CreateNamedMinimalProject()
        {
            Project project = SyntheticProjectFactory.CreateMinimalProject();
            project.Name = "native-fixture-minimal";
            return project;
        }

        private static Project CreateNamedCompleteProject()
        {
            Project project = SyntheticProjectFactory.CreateCompleteProject();
            project.Name = "native-fixture-complete";
            return project;
        }

        private static Project CreateNativeReferenceProject()
        {
            Protocol protocol = SyntheticProjectFactory.CreateProtocol();
            DatabaseManager.Database.SetProtocols(new[] { protocol });

            StringTag siteTag = new("native-site-label", "native-site-tag-001");
            BoolTag patientTag = new("native-patient-included", "native-patient-tag-001");
            PersistentDataManager.Tags.SetGeneralTags(new BaseTag[] { siteTag }, false);
            PersistentDataManager.Tags.SetPatientTags(new BaseTag[] { patientTag }, false);
            PersistentDataManager.Tags.SetSiteTags(new BaseTag[] { siteTag }, false);

            Patient patient = CreateNativePatient(patientTag, siteTag);
            Dataset dataset = CreateNativeDataset(protocol, patient);
            Visualization visualization = SyntheticProjectFactory.CreateVisualization(patient, dataset, protocol.Blocs[0]);
            Group group = new("native-fixture-group", new[] { patient }, "native-fixture-group-001");

            return new Project("native-fixture-reference", new HBP.Core.Data.ProjectPreferences("native-fixture-version", "native-fixture-project-001"), new[] { patient }, new[] { group }, new[] { dataset }, new[] { visualization });
        }

        private static Patient CreateNativePatient(BoolTag patientTag, StringTag siteTag)
        {
            Site site = new("native-site-alpha", new[] { new Coordinate("synthetic-space", new Vector3(1, 2, 3), "native-coordinate-001") }, new BaseTagValue[] { new StringTagValue(siteTag, "native-site", "native-site-tag-value-001") }, "native-site-001");

            MRI mri = new("native-t1", FixturePath("Nifti", "mri_t1.nii"), "native-mri-001");

            LeftRightMesh mesh = new("native-white-matter", FixturePath("Patients", "synthetic-patient", "t1mri", "T1pre_synthetic", "registration", "RawT1-synthetic-patient_T1pre_synthetic_TO_Scanner_Based.trm"), FixturePath("Patients", "synthetic-patient", "t1mri", "T1pre_synthetic", "default_analysis", "segmentation", "mesh", "synthetic-patient_Lwhite.gii"), FixturePath("Patients", "synthetic-patient", "t1mri", "T1pre_synthetic", "default_analysis", "segmentation", "mesh", "synthetic-patient_Rwhite.gii"), FixturePath("Patients", "synthetic-patient", "t1mri", "T1pre_synthetic", "default_analysis", "segmentation", "mesh", "surface_analysis", "synthetic-patient_Lwhite_parcels_marsAtlas.gii"), FixturePath("Patients", "synthetic-patient", "t1mri", "T1pre_synthetic", "default_analysis", "segmentation", "mesh", "surface_analysis", "synthetic-patient_Rwhite_parcels_marsAtlas.gii"), "native-mesh-001");

            return new Patient("native-patient-alpha", new BaseMesh[] { mesh }, new[] { mri }, new[] { site }, new BaseTagValue[] { new BoolTagValue(patientTag, true, "native-patient-tag-value-001") }, "native-database-link-001", "native-patient-001");
        }

        private static Dataset CreateNativeDataset(Protocol protocol, Patient patient)
        {
            Error[] errors = Array.Empty<Error>();
            Warning[] warnings = Array.Empty<Warning>();
            DataInfo[] data =
            {
                new IEEGDataInfo("signal-alpha", protocol, new BrainVision(FixturePath("EEG", "BrainVision", "native_brainvision_alpha.vhdr"), errors, warnings, "native-container-brainvision-001"), errors, warnings, patient, NormalizationType.Auto, "native-db-001", "native-data-brainvision-001"),
                new IEEGDataInfo("micromed-from-brainvision", protocol, new Micromed(FixturePath("EEG", "Micromed", "native_from_brainvision.trc"), errors, warnings, "native-container-micromed-001"), errors, warnings, patient, NormalizationType.Auto, "native-db-001", "native-data-micromed-001"),
                new IEEGDataInfo("elan-from-brainvision", protocol, new Elan(FixturePath("EEG", "Elan", "native_from_brainvision.eeg"), FixturePath("EEG", "Elan", "native_from_brainvision.pos"), string.Empty, errors, warnings, "native-container-elan-001"), errors, warnings, patient, NormalizationType.Auto, "native-db-001", "native-data-elan-001"),
                new CCEPDataInfo("response-alpha", protocol, new EDF(FixturePath("EEG", "EDF", "native_edf.edf"), errors, warnings, "native-container-edf-001"), errors, warnings, patient, "A1", "native-db-001", "native-data-edf-001"),
                new FMRIDataInfo("fmri-alpha", protocol, new Nifti(FixturePath("Nifti", "fmri_4d.nii.gz"), errors, warnings, "native-container-fmri-001"), new Nifti(FixturePath("Nifti", "mask_binary.nii"), errors, warnings, "native-container-fmri-mask-001"), errors, warnings, patient, "native-db-001", "native-data-fmri-001"),
                new MEGcDataInfo("megc-fif-alpha", protocol, new FIF(FixturePath("EEG", "FIF", "native_raw.fif"), errors, warnings, "native-container-fif-001"), errors, warnings, patient, "native-db-001", "native-data-fif-001"),
                new MEGvDataInfo("megv-nifti-alpha", protocol, new Nifti(FixturePath("Nifti", "fmri_3d.nii"), errors, warnings, "native-container-megv-001"), new Nifti(FixturePath("Nifti", "megv_mask.nii"), errors, warnings, "native-container-megv-mask-001"), errors, warnings, patient, "native-db-001", "native-data-megv-001"),
                new StaticDataInfo("static-alpha", protocol, new CSV(FixturePath("Static", "native_static.csv"), errors, warnings, "native-container-csv-001"), errors, warnings, patient, "native-db-001", "native-data-csv-001"),
                new SharedFMRIDataInfo("shared-fmri-alpha", protocol, new Nifti(FixturePath("Nifti", "fmri_3d.nii"), errors, warnings, "native-container-shared-fmri-001"), new Nifti(FixturePath("Nifti", "mask_binary.nii"), errors, warnings, "native-container-shared-mask-001"), errors, warnings, "native-db-001", "native-data-shared-fmri-001")
            };

            return new Dataset("native-fixture-dataset", protocol, data, "native-dataset-001");
        }

        private static async UniTask<string> SaveProjectAsync(Project project, string outputFolder)
        {
            ApplicationState.LoadedProject = project;
            ApplicationState.LoadedProjectLocation = outputFolder;
            await project.SaveAsync(outputFolder, NoProgress, CancellationToken.None);
            return Path.Combine(outputFolder, project.FileName);
        }

        private static string FixturePath(params string[] segments)
        {
            return Path.Combine(NativeAlias, Path.Combine(segments));
        }

        private static void NoProgress(float progress, float duration, LoadingText text)
        {
        }
    }
}
