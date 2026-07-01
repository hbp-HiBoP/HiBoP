using System;
using System.IO;
using System.Linq;
using HBP.Core.Data;
using HBP.Core.Data.Container;
using HBP.Core.Enums;
using HBP.Core.Errors;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEngine;
using Object3DSite = HBP.Core.Object3D.Site;
using Object3DSiteInformation = HBP.Core.Object3D.SiteInformation;
using Object3DSiteState = HBP.Core.Object3D.SiteState;
using PersistentDataManager = HBP.Core.Preferences.PersistentDataManager;

namespace HBP.Tests.Serialization
{
    public class PatientsGroupsTagsSitesTests
    {
        [Test]
        public void Patient_WithMeshesMrisSitesAndTagValues_RoundTrips()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            string meshPath = temp.GetPath("synthetic-mesh.gii");
            string atlasPath = temp.GetPath("synthetic-atlas.gii");
            string transformPath = temp.GetPath("synthetic-transform.trm");
            string mriPath = temp.GetPath("synthetic-mri.nii");
            File.WriteAllText(meshPath, "synthetic mesh");
            File.WriteAllText(atlasPath, "synthetic atlas");
            File.WriteAllText(transformPath, "synthetic transform");
            File.WriteAllText(mriPath, "synthetic mri");

            BoolTag patientTag = new("included", "phase3-patient-tag-001");
            StringTag siteTag = new("region", "phase3-site-tag-001");
            PersistentDataManager.Tags.SetPatientTags(new BaseTag[] { patientTag }, false);
            PersistentDataManager.Tags.SetSiteTags(new BaseTag[] { siteTag }, false);

            Site site = new(
                "A1",
                new[]
                {
                    new Coordinate("scanner", new Vector3(1.25f, -2.5f, 3.75f), "phase3-coordinate-scanner-001"),
                    new Coordinate("mni", new Vector3(-4, 5, 6), "phase3-coordinate-mni-001")
                },
                new BaseTagValue[] { new StringTagValue(siteTag, "temporal", "phase3-site-tag-value-001") },
                "phase3-site-001");
            Patient patient = new(
                "phase3-patient-alpha",
                new BaseMesh[] { new SingleMesh("phase3-mesh", transformPath, meshPath, atlasPath, "phase3-mesh-001") },
                new[] { new MRI("phase3-mri", mriPath, "phase3-mri-001") },
                new[] { site },
                new BaseTagValue[] { new BoolTagValue(patientTag, true, "phase3-patient-tag-value-001") },
                "phase3-database-link-001",
                "phase3-patient-001");

            Patient loaded = RoundTrip(temp, patient, "phase3-patient.json");

            Assert.That(loaded.ID, Is.EqualTo("phase3-patient-001"));
            Assert.That(loaded.Meshes, Has.Count.EqualTo(1));
            Assert.That(loaded.Meshes[0], Is.TypeOf<SingleMesh>());
            Assert.That(((SingleMesh)loaded.Meshes[0]).Path, Is.EqualTo(meshPath));
            Assert.That(loaded.MRIs.Select(mri => mri.File), Is.EquivalentTo(new[] { mriPath }));
            Assert.That(loaded.Sites, Has.Count.EqualTo(1));
            Assert.That(loaded.Sites[0].Coordinates.Select(c => c.ReferenceSystem), Is.EquivalentTo(new[] { "scanner", "mni" }));
            Assert.That(loaded.Sites[0].Coordinates[0].Position.ToVector3(), Is.EqualTo(new Vector3(1.25f, -2.5f, 3.75f)));
            Assert.That(((StringTagValue)loaded.Sites[0].Tags[0]).Value, Is.EqualTo("temporal"));
            Assert.That(((BoolTagValue)loaded.Tags[0]).Value, Is.True);
        }

        [Test]
        public void Group_RoundTripStoresPatientIdsAndResolvesProjectPatients()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Patient firstPatient = new("phase3-patient-alpha", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<Site>(), Array.Empty<BaseTagValue>(), "", "phase3-patient-alpha-id");
            Patient secondPatient = new("phase3-patient-beta", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<Site>(), Array.Empty<BaseTagValue>(), "", "phase3-patient-beta-id");
            ApplicationState.LoadedProject = new Project(
                "phase3-project",
                new ProjectPreferences("phase3-version", "phase3-project-id"),
                new[] { firstPatient, secondPatient },
                Array.Empty<Group>(),
                Array.Empty<Dataset>(),
                Array.Empty<Visualization>());
            Group group = new("phase3-group", new[] { firstPatient, secondPatient }, "phase3-group-001");

            Group loaded = RoundTrip(temp, group, "phase3-group.json");

            Assert.That(loaded.PatientsID, Is.EquivalentTo(new[] { firstPatient.ID, secondPatient.ID }));
            Assert.That(loaded.Patients.Select(patient => patient.ID), Is.EquivalentTo(new[] { firstPatient.ID, secondPatient.ID }));
            Assert.That(loaded.Patients[0], Is.SameAs(firstPatient));
        }

        [Test]
        public void SiteConfigurationAndSelectionState_PreserveLabelsFlagsAndGain()
        {
            GameObject gameObject = new("phase3-site-state");
            try
            {
                Object3DSite site = gameObject.AddComponent<Object3DSite>();
                site.Information = new Object3DSiteInformation();
                site.State = new Object3DSiteState();
                site.OnSelectSite = new GenericEvent<bool>();
                site.OnChangeConfiguration = new UnityEngine.Events.UnityEvent();
                bool selectionEventValue = false;
                site.OnSelectSite.AddListener(value => selectionEventValue = value);
                site.Configuration = new SiteConfiguration(true, false, Color.yellow, new[] { "old-label" }, "phase3-site-config-001");

                site.LoadConfiguration();
                site.IsSelected = true;
                site.State.ApplySpecificState(
                    importHighlighted: true,
                    isHighlighted: true,
                    importBlacklisted: true,
                    isBlacklisted: false,
                    importColor: true,
                    color: Color.cyan,
                    importLabels: true,
                    labels: new[] { "new-label" },
                    mergeLabels: true);
                site.SaveConfiguration();

                BaseConfiguration baseConfiguration = new(
                    0.42f,
                    new() { { "phase3-site-001", site.Configuration } },
                    "phase3-base-config-001");
                VisualizationConfiguration visualizationConfiguration = new()
                {
                    SiteGain = 2.5f,
                    HideBlacklistedSites = true,
                    ShowAllSites = false,
                    AutomaticCutAroundSelectedSite = true
                };

                Assert.That(selectionEventValue, Is.True);
                Assert.That(site.IsSelected, Is.True);
                Assert.That(site.Configuration.IsBlacklisted, Is.False);
                Assert.That(site.Configuration.IsHighlighted, Is.True);
                Assert.That(site.Configuration.Color, Is.EqualTo(Color.cyan));
                Assert.That(site.Configuration.Labels, Is.EquivalentTo(new[] { "old-label", "new-label" }));
                Assert.That(baseConfiguration.ConfigurationBySite["phase3-site-001"].Labels, Does.Contain("new-label"));
                Assert.That(visualizationConfiguration.SiteGain, Is.EqualTo(2.5f));
                Assert.That(visualizationConfiguration.HideBlacklistedSites, Is.True);
                Assert.That(visualizationConfiguration.AutomaticCutAroundSelectedSite, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SiteFilters_CheckNameTagsDataStateDataTypeAndSceneLocation()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            BoolTag patientTag = new("included", "phase3-filter-patient-tag-001");
            StringTag siteTag = new("region", "phase3-filter-site-tag-001");
            PersistentDataManager.Tags.SetPatientTags(new BaseTag[] { patientTag }, false);
            PersistentDataManager.Tags.SetSiteTags(new BaseTag[] { siteTag }, false);

            Site siteData = new(
                "A1",
                new[] { new Coordinate("scanner", new Vector3(-3, 4, 5), "phase3-filter-coordinate-001") },
                new BaseTagValue[] { new StringTagValue(siteTag, "temporal", "phase3-filter-site-tag-value-001") },
                "phase3-filter-site-001");
            Patient patient = new(
                "phase3-patient-alpha",
                Array.Empty<BaseMesh>(),
                Array.Empty<MRI>(),
                new[] { siteData },
                new BaseTagValue[] { new BoolTagValue(patientTag, true, "phase3-filter-patient-tag-value-001") },
                "",
                "phase3-filter-patient-001");
            Protocol protocol = SyntheticProjectFactory.CreateProtocol();
            IEEGDataInfo okData = new("phase3-ieeg", protocol, new Elan("synthetic.eeg", "synthetic.pos", "", Array.Empty<Error>(), Array.Empty<Warning>()), Array.Empty<Error>(), Array.Empty<Warning>(), patient, NormalizationType.Auto, "phase3-db", "phase3-filter-data-ok");
            StaticDataInfo errorData = new("phase3-static", protocol, new CSV("missing.csv", Array.Empty<Error>(), Array.Empty<Warning>()), new Error[] { new RequiredFieldEmptyError("phase3") }, Array.Empty<Warning>(), patient, "phase3-db", "phase3-filter-data-error");

            GameObject gameObject = new("phase3-filter-site");
            try
            {
                Object3DSite site = gameObject.AddComponent<Object3DSite>();
                site.Information = new Object3DSiteInformation();
                site.State = new Object3DSiteState();
                site.OnSelectSite = new GenericEvent<bool>();
                site.OnChangeConfiguration = new UnityEngine.Events.UnityEvent();
                site.Information.SiteData = siteData;
                site.Information.Patient = patient;
                site.Information.Name = siteData.Name;
                site.Information.DefaultPosition = siteData.Coordinates[0].Position.ToVector3();
                site.State.IsOutOfROI = false;

                SpecificSiteLocationFilterCondition.SceneLocationEvaluator = (condition, evaluatedSite) =>
                    ReferenceEquals(evaluatedSite, site) && condition.LocationType == SpecificSiteLocationFilterCondition.SpecificLocationType.BrainMesh;

                Assert.That(new NameFilterCondition("a1", true, false, false).Check(site), Is.True);
                Assert.That(new SiteTagFilterCondition(SiteTagFilterCondition.TargetType.Site, siteTag, new StringTagFilterValue { Value = "temp", ExactMatch = false }, false).Check(site), Is.True);
                Assert.That(new SiteTagFilterCondition(SiteTagFilterCondition.TargetType.Patient, patientTag, new BoolTagFilterValue { Value = true }, false).Check(site), Is.True);
                Assert.That(new RawSitePositionFilterCondition(RawSitePositionFilterCondition.AxisType.X, NumberComparisonType.Equal, 3, 0, 0, false).Check(site), Is.True);
                Assert.That(new DataStateFilterCondition(DataInfo.DataState.Ok, false).Check(okData), Is.True);
                Assert.That(new DataStateFilterCondition(DataInfo.DataState.Error, false).Check(errorData), Is.True);
                Assert.That(new DataTypeFilterCondition(typeof(IEEGDataInfo), false).Check(okData), Is.True);
                Assert.That(new SpecificSiteLocationFilterCondition(SpecificSiteLocationFilterCondition.SpecificLocationType.BrainMesh, MeshPart.Both, SpecificSiteLocationFilterCondition.Atlas.MarsAtlas, "", false).Check(site), Is.True);
                Assert.That(new SpecificSiteLocationFilterCondition(SpecificSiteLocationFilterCondition.SpecificLocationType.RegionOfInterest, MeshPart.Both, SpecificSiteLocationFilterCondition.Atlas.MarsAtlas, "", false).Check(site), Is.True);
            }
            finally
            {
                SpecificSiteLocationFilterCondition.SceneLocationEvaluator = null;
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SiteCsvImport_ReadsSyntheticAttributesAndGeneratesSiteTags()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);
            bool previousCorrection = Site.SiteNameCorrection;

            try
            {
                Site.SiteNameCorrection = false;
                string intranatCsv = temp.GetPath("phase3-sites.csv");
                File.WriteAllLines(intranatCsv, new[]
                {
                    "ignored header",
                    "contact\tregion\tquality",
                    "A01\ttemporal\tgood",
                    "B02\tfrontal\tn/a"
                });

                var sites = Site.LoadSitesFromCSVFile(intranatCsv);

                Assert.That(sites.Select(site => site.Name), Is.EquivalentTo(new[] { "A01", "B02" }));
                Assert.That(sites[0].Tags.Select(tag => tag.Tag.Name), Is.EquivalentTo(new[] { "region", "quality" }));
                Assert.That(sites[1].Tags.Select(tag => tag.Tag.Name), Is.EquivalentTo(new[] { "region" }));
                Assert.That(((StringTagValue)sites[0].Tags.First(tag => tag.Tag.Name == "quality")).Value, Is.EqualTo("good"));

                string tagCsv = temp.GetPath("phase3-site-tags.csv");
                File.WriteAllLines(tagCsv, new[]
                {
                    "Site,Laterality,Confidence",
                    "A01,left,7",
                    "B02,right,4"
                });

                var tagsBySite = PersistentDataManager.Tags.GenerateSiteTagsFromCSV(tagCsv);

                Assert.That(tagsBySite.Keys, Is.EquivalentTo(new[] { "A01", "B02" }));
                Assert.That(tagsBySite["A01"].Select(tag => tag.Tag.Name), Is.EquivalentTo(new[] { "Laterality", "Confidence" }));
                Assert.That(tagsBySite["A01"].First(tag => tag.Tag.Name == "Laterality").DisplayableValue, Is.EqualTo("left"));
                Assert.That(tagsBySite["A01"].First(tag => tag.Tag.Name == "Confidence").DisplayableValue, Is.EqualTo("7"));
            }
            finally
            {
                Site.SiteNameCorrection = previousCorrection;
            }
        }

        private static T RoundTrip<T>(TempDirectoryScope temp, T source, string fileName) where T : new()
        {
            string path = temp.GetPath(fileName);
            Assert.That(ClassLoaderSaver.SaveToJSon(source, path, true), Is.True);
            return ClassLoaderSaver.LoadFromJson<T>(path);
        }
    }
}
