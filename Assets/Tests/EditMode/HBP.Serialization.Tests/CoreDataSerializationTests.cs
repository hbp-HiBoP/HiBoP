using System;
using System.Collections.Generic;
using System.Linq;
using HBP.Core.Database;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEngine;
using DataProjectPreferences = HBP.Core.Data.ProjectPreferences;
using UserProjectPreferences = HBP.Core.Preferences.ProjectPreferences;

namespace HBP.Tests.Serialization
{
    public class CoreDataSerializationTests
    {
        [Test]
        public void TagsAndTagValues_RoundTrip()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            BoolTag boolTag = new("flag-alpha", "tag-bool-alpha");
            StringTag stringTag = new("text-alpha", "tag-string-alpha");
            IntTag intTag = new("int-alpha", false, 0, 0, "tag-int-alpha");
            FloatTag floatTag = new("float-alpha", false, 0, 0, "tag-float-alpha");
            EnumTag enumTag = new("enum-alpha", new[] { "one", "two" }, "tag-enum-alpha");
            PersistentDataManager.Tags.SetGeneralTags(new BaseTag[] { boolTag, stringTag, intTag, floatTag, enumTag }, false);

            Patient patient = new("patient-alpha", new BaseMesh[0], new MRI[0], new Site[0], new BaseTagValue[]
            {
                new BoolTagValue(boolTag, true, "value-bool-alpha"),
                new StringTagValue(stringTag, "value-alpha", "value-string-alpha"),
                new IntTagValue(intTag, 42, "value-int-alpha"),
                new FloatTagValue(floatTag, 4.2f, "value-float-alpha"),
                new EnumTagValue(enumTag, 1, "value-enum-alpha")
            }, "db-alpha", "patient-alpha");

            Patient loaded = RoundTrip(temp, patient, "patient.json");

            Assert.That(loaded.Tags, Has.Count.EqualTo(5));
            Assert.That(loaded.Tags[0], Is.TypeOf<BoolTagValue>());
            Assert.That(((BoolTagValue)loaded.Tags[0]).Value, Is.True);
            Assert.That(((StringTagValue)loaded.Tags[1]).Value, Is.EqualTo("value-alpha"));
            Assert.That(((IntTagValue)loaded.Tags[2]).Value, Is.EqualTo(42));
            Assert.That(((FloatTagValue)loaded.Tags[3]).Value, Is.EqualTo(4.2f).Within(0.001f));
            Assert.That(((EnumTagValue)loaded.Tags[4]).Value, Is.EqualTo(1));
        }

        [Test]
        public void Filters_RoundTrip()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            BoolTag boolTag = new("filter-flag-alpha", "filter-tag-bool-alpha");
            StringTag stringTag = new("filter-text-alpha", "filter-tag-string-alpha");
            PersistentDataManager.Tags.SetGeneralTags(new BaseTag[] { boolTag, stringTag }, false);

            AllFilterCondition source = new(new BaseFilterCondition[]
            {
                new NameFilterCondition("patient-alpha", true, false, false, "filter-name-alpha"),
                new DataStateFilterCondition(DataInfo.DataState.Ok, false, "filter-state-alpha"),
                new DataTypeFilterCondition(typeof(IEEGDataInfo), false, "filter-type-alpha"),
                new PatientTagFilterCondition(PatientTagFilterCondition.TargetType.Patient, boolTag, new BoolTagFilterValue { Value = true }, false, "filter-patient-tag-alpha"),
                new SiteTagFilterCondition(SiteTagFilterCondition.TargetType.Site, stringTag, new StringTagFilterValue { Value = "plot-value-alpha" }, false, "filter-site-tag-alpha"),
                new AnyFilterCondition(new BaseFilterCondition[] { new PatientNameFilterCondition("patient", false, false, false, "filter-patient-name-alpha") }, false, "filter-any-alpha")
            }, false, "filter-all-alpha");

            AllFilterCondition loaded = RoundTrip(temp, source, "filters.json");

            Assert.That(loaded.Conditions, Has.Count.EqualTo(6));
            Assert.That(loaded.Conditions[0], Is.TypeOf<NameFilterCondition>());
            Assert.That(loaded.Conditions[3], Is.TypeOf<PatientTagFilterCondition>());
            Assert.That(loaded.Conditions[4], Is.TypeOf<SiteTagFilterCondition>());
            Assert.That(loaded.Conditions[5], Is.TypeOf<AnyFilterCondition>());
        }

        [Test]
        public void ProtocolDatasetAndVisualization_RoundTrip()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project sourceProject = SyntheticProjectFactory.CreateCompleteProject();
            Protocol loadedProtocol = RoundTrip(temp, sourceProject.Datasets[0].Protocol, "protocol.json");
            Patient loadedPatient = RoundTrip(temp, sourceProject.Patients[0], "patient.json");

            Dataset loadedDataset = RoundTrip(temp, sourceProject.Datasets[0], "dataset.json");
            Visualization loadedVisualization = RoundTrip(temp, sourceProject.Visualizations[0], "visualization.json");
            LoadingContext context = new(PersistentDataManager.Tags.AllTags, new[] { loadedProtocol }, new[] { loadedPatient }, new[] { loadedDataset });
            context.ResolveProject(new[] { loadedPatient }, Array.Empty<Group>(), new[] { loadedDataset }, new[] { loadedVisualization });

            Assert.That(loadedProtocol.ID, Is.EqualTo(SyntheticProjectFactory.ProtocolId));
            Assert.That(loadedProtocol.Blocs.Select(b => b.ID), Is.EquivalentTo(sourceProject.Datasets[0].Protocol.Blocs.Select(b => b.ID)));
            Assert.That(loadedPatient.ID, Is.EqualTo(SyntheticProjectFactory.PatientId));
            Assert.That(loadedPatient.Sites.Select(s => s.ID), Is.EquivalentTo(sourceProject.Patients[0].Sites.Select(s => s.ID)));
            Assert.That(loadedDataset.ID, Is.EqualTo(SyntheticProjectFactory.DatasetId));
            Assert.That(loadedDataset.Protocol.ID, Is.EqualTo(SyntheticProjectFactory.ProtocolId));
            Assert.That(loadedDataset.Data.Select(d => d.ID), Is.EquivalentTo(sourceProject.Datasets[0].Data.Select(d => d.ID)));
            Assert.That(loadedDataset.Data.Select(d => d.DataContainer.ID), Is.EquivalentTo(sourceProject.Datasets[0].Data.Select(d => d.DataContainer.ID)));
            Assert.That(loadedVisualization.ID, Is.EqualTo(SyntheticProjectFactory.VisualizationId));
            Assert.That(loadedVisualization.Patients.Select(p => p.ID), Is.EquivalentTo(sourceProject.Visualizations[0].Patients.Select(p => p.ID)));
            Assert.That(loadedVisualization.Columns.Select(c => c.ID), Is.EquivalentTo(sourceProject.Visualizations[0].Columns.Select(c => c.ID)));
            Assert.That(loadedVisualization.Columns.Select(c => c.BaseConfiguration.ID), Is.EquivalentTo(sourceProject.Visualizations[0].Columns.Select(c => c.BaseConfiguration.ID)));
        }

        [Test]
        public void Preferences_RoundTrip()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);
            AveragingType defaultAveraging = DataManager.DefaultAveraging;
            NormalizationType defaultNormalization = DataManager.DefaultNormalization;
            AveragingType defaultPositionAveraging = DataManager.DefaultPositionAveraging;

            try
            {
                DataProjectPreferences projectPreferences = new("format-alpha", "project-preferences-alpha");
                UserPreferences userPreferences = new(new GeneralPreferences(new UserProjectPreferences("default-project-alpha", temp.GetPath("projects"), temp.GetPath("exports")), new ThemePreferences(), new LocalizationPreferences(), new SystemPreferences(false, 512, 7, 24), new MiscPreferences(true)), new DataPreferences(new EEGPreferences(AveragingType.Mean, NormalizationType.Protocol, 0.01f, false, TemporalSamplingPolicy.Round), new ProtocolPreferences(AveragingType.Mean, -42, 43, 17), new AnatomicPreferences(false, true, true, false, true), new AtlasesPreferences(false, true, false, true, false, true, false, true, true, false, true, false, true, false, true, false)), new VisualizationPreferences(new _3DPreferences(false, true, LayoutDirection.Horizontal, SiteInfluenceByDistanceType.Linear, "MRI-alpha", "Mesh-alpha", "Implantation-alpha", "MNI-alpha", "MNI-mesh-alpha", "MNI-implantation-alpha"), new TrialMatrixPreferences(false, false, false, 5, false, BlocFormatType.TrialHeight, 12, 0.01f, 0.2f, 1.1f), new GraphPreferences(false, false, 3, 9, 2), new CutPreferences(false)), "user-preferences-alpha");

                DataProjectPreferences loadedProjectPreferences = RoundTrip(temp, projectPreferences, "project-preferences.json");
                UserPreferences loadedUserPreferences = RoundTrip(temp, userPreferences, "user-preferences.json");

                Assert.That(loadedProjectPreferences.ID, Is.EqualTo("project-preferences-alpha"));
                Assert.That(loadedProjectPreferences.Version, Is.EqualTo("format-alpha"));
                Assert.That(loadedUserPreferences.ID, Is.EqualTo("user-preferences-alpha"));
                Assert.That(loadedUserPreferences.General.Project.DefaultName, Is.EqualTo("default-project-alpha"));
                Assert.That(loadedUserPreferences.General.System.MultiThreading, Is.False);
                Assert.That(loadedUserPreferences.General.System.MemoryCacheLimit, Is.EqualTo(512));
                Assert.That(loadedUserPreferences.General.Misc.AdvancedFeatures, Is.True);
                Assert.That(loadedUserPreferences.Data.EEG.Averaging, Is.EqualTo(AveragingType.Mean));
                Assert.That(loadedUserPreferences.Data.EEG.Normalization, Is.EqualTo(NormalizationType.Protocol));
                Assert.That(loadedUserPreferences.Data.EEG.TemporalSampling, Is.EqualTo(TemporalSamplingPolicy.Round));
                Assert.That(loadedUserPreferences.Data.Protocol.Step, Is.EqualTo(17));
                Assert.That(loadedUserPreferences.Data.Anatomic.MeshPreloading, Is.True);
                Assert.That(loadedUserPreferences.Data.Atlases.PreloadDiFuMo64, Is.True);
                Assert.That(loadedUserPreferences.Visualization._3D.VisualizationsLayoutDirection, Is.EqualTo(LayoutDirection.Horizontal));
                Assert.That(loadedUserPreferences.Visualization._3D.SiteInfluenceByDistance, Is.EqualTo(SiteInfluenceByDistanceType.Linear));
                Assert.That(loadedUserPreferences.Visualization.TrialMatrix.SubBlocFormat, Is.EqualTo(BlocFormatType.TrialHeight));
                Assert.That(loadedUserPreferences.Visualization.Graph.ShowSEM, Is.False);
                Assert.That(loadedUserPreferences.Visualization.Graph.MaxSites, Is.EqualTo(3));
                Assert.That(loadedUserPreferences.Visualization.Cut.ShowCutLines, Is.False);
            }
            finally
            {
                DataManager.DefaultAveraging = defaultAveraging;
                DataManager.DefaultNormalization = defaultNormalization;
                DataManager.DefaultPositionAveraging = defaultPositionAveraging;
            }
        }

        [Test]
        public void UserPreferences_AutoNormalizationFallsBackToNone()
        {
            EEGPreferences preferences = new(AveragingType.Mean, NormalizationType.Auto, 0.05f, true);

            Assert.That(preferences.Normalization, Is.EqualTo(NormalizationType.None));
        }

        [Test]
        public void MissingOptionalFields_UseCurrentDefaults()
        {
            VisualizationConfiguration configuration = ClassLoaderSaver.LoadFromJsonString<VisualizationConfiguration>("{\"ID\":\"legacy-visualization-config-minimal\",\"Mesh\":\"legacy-mesh-alpha\"}");

            Assert.That(configuration.ID, Is.EqualTo("legacy-visualization-config-minimal"));
            Assert.That(configuration.MeshName, Is.EqualTo("legacy-mesh-alpha"));
            Assert.That(configuration.BrainAlpha, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(configuration.CameraType, Is.EqualTo(CameraControl.Trackball));
            Assert.That(configuration.Cuts, Is.Not.Null.And.Empty);
            Assert.That(configuration.Views, Is.Not.Null.And.Empty);
            Assert.That(configuration.RegionsOfInterest, Is.Not.Null.And.Empty);

            UserPreferences userPreferences = ClassLoaderSaver.LoadFromJsonString<UserPreferences>("{\"ID\":\"legacy-user-preferences-minimal\",\"General\":{\"System\":{\"MultiThreading\":false}}}");

            Assert.That(userPreferences.ID, Is.EqualTo("legacy-user-preferences-minimal"));
            Assert.That(userPreferences.General.System.MultiThreading, Is.False);
            Assert.That(userPreferences.General.Project, Is.Not.Null);
            Assert.That(userPreferences.Data, Is.Not.Null);
            Assert.That(userPreferences.Visualization, Is.Not.Null);
            Assert.That(userPreferences.Visualization.Graph.SiteColors, Is.Not.Null);
        }

        [Test]
        public void UnknownAndObsoleteFields_AreIgnored()
        {
            DataProjectPreferences projectPreferences = ClassLoaderSaver.LoadFromJsonString<DataProjectPreferences>("{\"ID\":\"legacy-project-preferences-unknown\",\"Version\":\"0.1.0\",\"CanLoadProject\":false,\"ObsoleteDatabasePath\":\"ignored\"}");
            VisualizationConfiguration configuration = ClassLoaderSaver.LoadFromJsonString<VisualizationConfiguration>("{\"ID\":\"legacy-visualization-config-unknown\",\"Brain Alpha\":0.45,\"OldCutMode\":\"ignored\",\"RemovedToolbarState\":{\"Value\":true}}");

            Assert.That(projectPreferences.ID, Is.EqualTo("legacy-project-preferences-unknown"));
            Assert.That(projectPreferences.Version, Is.EqualTo("0.1.0"));
            Assert.That(projectPreferences.CanLoadProject, Is.True);
            Assert.That(configuration.ID, Is.EqualTo("legacy-visualization-config-unknown"));
            Assert.That(configuration.BrainAlpha, Is.EqualTo(0.45f).Within(0.0001f));
            Assert.That(configuration.Cuts, Is.Not.Null);
        }

        [Test]
        public void SerializedIds_RemainStableThroughRepeatedJsonRoundTrips()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project sourceProject = SyntheticProjectFactory.CreateCompleteProject();
            Project firstLoad = RoundTripPersistedProjectParts(temp, sourceProject, "first");
            Project secondLoad = RoundTripPersistedProjectParts(temp, firstLoad, "second");

            Assert.That(CollectPersistedIds(secondLoad), Is.EquivalentTo(CollectPersistedIds(firstLoad)));
            Assert.That(CollectPersistedIds(firstLoad), Is.EquivalentTo(CollectPersistedIds(sourceProject)));
        }

        [Test]
        public void Clone_DoesNotShareMutableCollections()
        {
            TagCollection tags = new(new BaseTag[] { new StringTag("general-alpha", "tag-general-alpha") }, new BaseTag[] { new BoolTag("patient-alpha", "tag-patient-alpha") }, new BaseTag[] { new EnumTag("site-alpha", new[] { "one", "two" }, "tag-site-alpha") }, "tag-collection-alpha");
            TagCollection clonedTags = tags.Clone() as TagCollection;

            tags.AddGeneralTag(new FloatTag("general-beta", "tag-general-beta"), false);

            Assert.That(clonedTags.GeneralTags.Select(tag => tag.ID), Is.EquivalentTo(new[] { "tag-general-alpha" }));
            Assert.That(clonedTags.GeneralTags[0], Is.Not.SameAs(tags.GeneralTags[0]));

            AliasCollection aliases = new(new[] { new Alias("[ALPHA]", "value-alpha", "alias-alpha") }, "aliases-alpha");
            AliasCollection clonedAliases = aliases.Clone() as AliasCollection;

            aliases.AddAlias(new Alias("[BETA]", "value-beta", "alias-beta"), false);

            Assert.That(clonedAliases.Aliases.Select(alias => alias.ID), Is.EquivalentTo(new[] { "alias-alpha" }));
            Assert.That(clonedAliases.Aliases[0], Is.Not.SameAs(aliases.Aliases[0]));

            BaseConfiguration baseConfiguration = new(0.5f, new Dictionary<string, SiteConfiguration>
            {
                { "site-alpha", new SiteConfiguration(false, true, Color.cyan, new[] { "label-alpha" }, "site-config-alpha") }
            }, "base-config-alpha");
            BaseConfiguration clonedBaseConfiguration = baseConfiguration.Clone() as BaseConfiguration;

            baseConfiguration.ConfigurationBySite["site-alpha"].Labels[0] = "mutated-label";

            Assert.That(clonedBaseConfiguration.ConfigurationBySite["site-alpha"].Labels, Is.EquivalentTo(new[] { "label-alpha" }));
            Assert.That(clonedBaseConfiguration.ConfigurationBySite["site-alpha"], Is.Not.SameAs(baseConfiguration.ConfigurationBySite["site-alpha"]));

            VisualizationConfiguration visualizationConfiguration = new();
            visualizationConfiguration.Cuts.Add(new Cut(Vector3.up, CutOrientation.Axial, false, 0.25f));
            visualizationConfiguration.Views.Add(new View(Vector3.one, Quaternion.identity, Vector3.zero));
            visualizationConfiguration.RegionsOfInterest.Add(new RegionOfInterest("roi-alpha", new List<Sphere> { new(Vector3.one, 2f) }));
            VisualizationConfiguration clonedVisualizationConfiguration = visualizationConfiguration.Clone() as VisualizationConfiguration;

            visualizationConfiguration.Cuts.Add(new Cut(Vector3.right, CutOrientation.Sagittal, true, 0.75f));
            visualizationConfiguration.RegionsOfInterest[0].Spheres.Add(new Sphere(Vector3.zero, 4f));

            Assert.That(clonedVisualizationConfiguration.Cuts, Has.Count.EqualTo(1));
            Assert.That(clonedVisualizationConfiguration.RegionsOfInterest[0].Spheres, Has.Count.EqualTo(1));
        }

        [Test]
        public void Copy_AppliesFieldsWithoutReplacingGlobalObjectIdentity()
        {
            TagCollection sourceTags = new(new BaseTag[] { new StringTag("general-alpha", "source-tag-general-alpha") }, new BaseTag[] { new BoolTag("patient-alpha", "source-tag-patient-alpha") }, new BaseTag[] { new EnumTag("site-alpha", new[] { "one", "two" }, "source-tag-site-alpha") }, "source-tag-collection");
            TagCollection targetTags = new(new BaseTag[] { new StringTag("old-general", "target-tag-general-old") }, new BaseTag[0], new BaseTag[0], "target-tag-collection");

            targetTags.Copy(sourceTags);

            Assert.That(targetTags.ID, Is.EqualTo("target-tag-collection"));
            Assert.That(targetTags.GeneralTags.Select(tag => tag.ID), Is.EquivalentTo(new[] { "source-tag-general-alpha" }));
            Assert.That(targetTags.GeneralTags[0], Is.SameAs(sourceTags.GeneralTags[0]));

            AliasCollection sourceAliases = new(new[] { new Alias("[ALPHA]", "value-alpha", "source-alias-alpha") }, "source-aliases");
            AliasCollection targetAliases = new(new[] { new Alias("[OLD]", "value-old", "target-alias-old") }, "target-aliases");

            targetAliases.Copy(sourceAliases);

            Assert.That(targetAliases.ID, Is.EqualTo("target-aliases"));
            Assert.That(targetAliases.Aliases.Select(alias => alias.ID), Is.EquivalentTo(new[] { "source-alias-alpha" }));
            Assert.That(targetAliases.Aliases[0], Is.SameAs(sourceAliases.Aliases[0]));

            UserPreferences sourcePreferences = new(new GeneralPreferences(), new DataPreferences(), new VisualizationPreferences(), "source-preferences");
            UserPreferences targetPreferences = new(new GeneralPreferences(), new DataPreferences(), new VisualizationPreferences(), "target-preferences");

            targetPreferences.Copy(sourcePreferences);

            Assert.That(targetPreferences.ID, Is.EqualTo("target-preferences"));
            Assert.That(targetPreferences.General, Is.SameAs(sourcePreferences.General));
            Assert.That(targetPreferences.Data, Is.SameAs(sourcePreferences.Data));
            Assert.That(targetPreferences.Visualization, Is.SameAs(sourcePreferences.Visualization));
        }

        private static T RoundTrip<T>(TempDirectoryScope temp, T source, string fileName) where T : new()
        {
            string path = temp.GetPath(fileName);
            Assert.That(ClassLoaderSaver.SaveToJSon(source, path, true), Is.True);
            return ClassLoaderSaver.LoadFromJson<T>(path);
        }

        private static Project RoundTripPersistedProjectParts(TempDirectoryScope temp, Project sourceProject, string prefix)
        {
            Protocol loadedProtocol = RoundTrip(temp, sourceProject.Datasets[0].Protocol, $"{prefix}-protocol.json");
            Patient loadedPatient = RoundTrip(temp, sourceProject.Patients[0], $"{prefix}-patient.json");

            Dataset loadedDataset = RoundTrip(temp, sourceProject.Datasets[0], $"{prefix}-dataset.json");
            Visualization loadedVisualization = RoundTrip(temp, sourceProject.Visualizations[0], $"{prefix}-visualization.json");
            LoadingContext context = new(PersistentDataManager.Tags.AllTags, new[] { loadedProtocol }, new[] { loadedPatient }, new[] { loadedDataset });
            context.ResolveProject(new[] { loadedPatient }, Array.Empty<Group>(), new[] { loadedDataset }, new[] { loadedVisualization });

            return new Project(sourceProject.Name, sourceProject.Preferences.Clone() as DataProjectPreferences, new[] { loadedPatient }, Array.Empty<Group>(), new[] { loadedDataset }, new[] { loadedVisualization });
        }

        private static IEnumerable<string> CollectPersistedIds(Project project)
        {
            List<string> ids = new() { project.Preferences.ID };
            foreach (Patient patient in project.Patients) ids.AddRange(patient.GetAllIdentifiable().Select(data => data.ID));
            foreach (Dataset dataset in project.Datasets)
            {
                ids.Add(dataset.Protocol.ID);
                ids.AddRange(dataset.Protocol.GetAllIdentifiable().Select(data => data.ID));
                ids.AddRange(dataset.GetAllIdentifiable().Select(data => data.ID));
            }

            foreach (Visualization visualization in project.Visualizations) ids.AddRange(visualization.GetAllIdentifiable().Select(data => data.ID));
            return ids;
        }
    }
}
