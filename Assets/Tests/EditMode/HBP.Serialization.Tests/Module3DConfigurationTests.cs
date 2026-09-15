using System;
using System.Collections.Generic;
using System.Linq;
using HBP.Core.Database;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using DataSphere = HBP.Core.Data.Sphere;
using SurfaceRepresentation = HBP.Core.Object3D.SurfaceRepresentation;

namespace HBP.Tests.Serialization
{
    public class Module3DConfigurationTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void LegacyCCEPConfiguration_LoadsWithOriginalIdentityAndCalibration(bool explicitType)
        {
            string type = explicitType ? "\"$type\":\"HBP.Core.Data.DynamicConfiguration\"," : string.Empty;
            string json = "{\"ID\":\"column\",\"DynamicConfiguration\":{" + type + "\"ID\":\"legacy-config\",\"Site Maximum Influence\":23,\"Span Min\":-7,\"Middle\":2,\"Span Max\":19}}";
            CCEPColumn column = ClassLoaderSaver.LoadFromJsonString<CCEPColumn>(json);
            Assert.That(column.CCEPConfiguration, Is.TypeOf<CCEPConfiguration>());
            Assert.That(column.CCEPConfiguration.ID, Is.EqualTo("legacy-config"));
            Assert.That(column.CCEPConfiguration.MaximumInfluence, Is.EqualTo(23));
            Assert.That(column.CCEPConfiguration.SpanMin, Is.EqualTo(-7));
            Assert.That(column.CCEPConfiguration.Middle, Is.EqualTo(2));
            Assert.That(column.CCEPConfiguration.SpanMax, Is.EqualTo(19));
            Assert.That(column.CCEPConfiguration.SiteID, Is.Null);
            Assert.That(column.CCEPConfiguration.MarsAtlasLabel, Is.EqualTo(-1));
            Assert.That(column.GetAllIdentifiable(), Does.Contain(column.CCEPConfiguration));
            using TempDirectoryScope temp = new();
            string path = temp.GetPath("migrated-ccep.json");
            Assert.That(ClassLoaderSaver.SaveToJSon(column, path, true), Is.True);
            string saved = System.IO.File.ReadAllText(path);
            Assert.That(saved, Does.Contain("\"CCEPConfiguration\""));
            Assert.That(saved, Does.Not.Contain("\"DynamicConfiguration\""));
            Assert.That(ClassLoaderSaver.LoadFromJson<CCEPColumn>(path).CCEPConfiguration.ID, Is.EqualTo("legacy-config"));
        }

        [Test]
        public void CCEPConfiguration_ConstructCloneCopyAndSerializationPreserveInheritedAndSourceState()
        {
            using TempDirectoryScope temp = new();
            var source = new CCEPConfiguration(23, -7, 2, 19, true, "patient_A1", 12, "ccep-configuration");
            var copied = new CCEPConfiguration();
            copied.Copy(source);
            DynamicConfiguration polymorphic = source;
            foreach (var configuration in new[] { (CCEPConfiguration)polymorphic.Clone(), copied, RoundTrip(temp, source, "ccep-config.json") })
            {
                Assert.That(configuration, Is.Not.SameAs(source));
                Assert.That(configuration.ID, Is.EqualTo(source.ID));
                Assert.That(configuration.MaximumInfluence, Is.EqualTo(23));
                Assert.That(configuration.SpanMin, Is.EqualTo(-7));
                Assert.That(configuration.Middle, Is.EqualTo(2));
                Assert.That(configuration.SpanMax, Is.EqualTo(19));
                Assert.That(configuration.UseMarsAtlas, Is.True);
                Assert.That(configuration.SiteID, Is.EqualTo("patient_A1"));
                Assert.That(configuration.MarsAtlasLabel, Is.EqualTo(12));
            }

            copied.GenerateID();
            Assert.That(copied.ID, Is.Not.EqualTo(source.ID));
        }

        [Test]
        public void AtlasAndVisualizationConstructors_PreserveAllPersistentParameters()
        {
            using TempDirectoryScope temp = new();
            var atlas = new AtlasConfiguration(true, false, .4f, false, 7, true, "128", 11, false, "AUDI", "subject", "bloc", 3, .6f, .1f, .8f, .2f, .9f, 70, 90, 130, "atlas-config");
            var copied = new AtlasConfiguration();
            copied.Copy(atlas);
            foreach (var candidate in new[] { (AtlasConfiguration)atlas.Clone(), copied, RoundTrip(temp, atlas, "atlas-config.json") })
            {
                Assert.That(candidate, Is.Not.SameAs(atlas));
                Assert.That(candidate.ID, Is.EqualTo("atlas-config"));
                Assert.That(Newtonsoft.Json.Linq.JToken.DeepEquals(Newtonsoft.Json.Linq.JToken.FromObject(candidate), Newtonsoft.Json.Linq.JToken.FromObject(atlas)), Is.True);
            }

            var masks = new[] { 1, 0, 1 };
            var source = new VisualizationConfiguration(ColorType.Surface, ColorType.Default, ColorType.MatLab, MeshPart.Left, "mesh", "mri", "implantation", false, false, .2f, false, false, false, false, 1, 0, 1, CameraControl.Trackball, Array.Empty<Cut>(), Array.Empty<View>(), Array.Empty<RegionOfInterest>(), "visualization-config", SurfaceRepresentation.Inflated, atlas, "preview-mri", masks, new[] { 0, 1 });
            masks[1] = 1;
            Assert.That(source.ErasedTriangles[1], Is.Zero, "Constructor snapshots its mask input.");
            var clone = (VisualizationConfiguration)source.Clone();
            Assert.That(clone.AtlasConfiguration, Is.Not.SameAs(atlas));
            Assert.That(clone.AtlasConfiguration.ID, Is.EqualTo(atlas.ID));
            Assert.That(clone.PreviewMRIName, Is.EqualTo("preview-mri"));
            Assert.That(clone.ErasedTriangles, Is.EqualTo(new[] { 1, 0, 1 }));
            Assert.That(clone.ErasedSimplifiedTriangles, Is.EqualTo(new[] { 0, 1 }));
            Assert.That(clone.SurfaceRepresentation, Is.EqualTo(SurfaceRepresentation.Inflated));
            Assert.That(new FMRIConfiguration(.1f, .2f, .3f, .4f, true, false, true, "fmri-config", 2).SelectedResourceIndex, Is.EqualTo(2));
            Assert.That(new MEGConfiguration(.1f, .2f, .3f, .4f, true, false, true, "meg-config", 3).SelectedResourceIndex, Is.EqualTo(3));
            Assert.That(new StaticConfiguration(23, -7, 2, 19, "static-config", 4).SelectedResourceIndex, Is.EqualTo(4));
            Assert.That(new AnatomicConfiguration(27, "anatomy-config").MaximumInfluence, Is.EqualTo(27));
        }

        [Test]
        public void PersistentSnapshotChoices_RoundTripCloneAndCopyThroughExistingConfigurations()
        {
            using TempDirectoryScope temp = new();
            var source = new VisualizationConfiguration
            {
                PreviewMRIName = "MRI preview source",
                ErasedTriangles = new[] { 1, 0, 1 }, ErasedSimplifiedTriangles = new[] { 0, 1 },
                AtlasConfiguration = new AtlasConfiguration { IBC = false, IBCIndex = 7, DiFuMoAtlas = "128", DiFuMoArea = 11, AtlasAlpha = .4f, LocalizerProtocol = "AUDI", LocalizerData = "subject", LocalizerBloc = "bloc", LocalizerTime = 3 }
            };
            var copy = new VisualizationConfiguration();
            copy.Copy(source);
            foreach (var configuration in new[] { (VisualizationConfiguration)source.Clone(), copy, RoundTrip(temp, source, "snapshot-configuration.json") })
            {
                Assert.That(configuration.PreviewMRIName, Is.EqualTo(source.PreviewMRIName));
                Assert.That(configuration.ErasedTriangles, Is.EqualTo(source.ErasedTriangles));
                Assert.That(configuration.ErasedTriangles, Is.Not.SameAs(source.ErasedTriangles));
                Assert.That(configuration.AtlasConfiguration, Is.Not.SameAs(source.AtlasConfiguration));
                Assert.That(configuration.AtlasConfiguration.IBC, Is.False);
                Assert.That(configuration.AtlasConfiguration.IBCIndex, Is.EqualTo(7));
                Assert.That(configuration.AtlasConfiguration.DiFuMoAtlas, Is.EqualTo("128"));
                Assert.That(configuration.AtlasConfiguration.LocalizerTime, Is.EqualTo(3));
            }

            var fmri = new FMRIConfiguration { SelectedResourceIndex = 2 };
            var meg = new MEGConfiguration { SelectedResourceIndex = 3 };
            var stat = new StaticConfiguration { SelectedResourceIndex = 4 };
            var anatomy = new AnatomicConfiguration { MaximumInfluence = 27 };
            Assert.That(((FMRIConfiguration)fmri.Clone()).SelectedResourceIndex, Is.EqualTo(2));
            Assert.That(RoundTrip(temp, meg, "meg.json").SelectedResourceIndex, Is.EqualTo(3));
            Assert.That(RoundTrip(temp, stat, "static.json").SelectedResourceIndex, Is.EqualTo(4));
            Assert.That(RoundTrip(temp, anatomy, "anatomy.json").MaximumInfluence, Is.EqualTo(27));
            var ccep = new CCEPColumn { CCEPConfiguration = new CCEPConfiguration { SiteID = "patient_A1", UseMarsAtlas = true, MarsAtlasLabel = 5 } };
            var ccepClone = (CCEPColumn)ccep.Clone();
            Assert.That(ccepClone.CCEPConfiguration, Is.Not.SameAs(ccep.CCEPConfiguration));
            Assert.That(ccepClone.CCEPConfiguration.SiteID, Is.EqualTo("patient_A1"));
            Assert.That(ccepClone.CCEPConfiguration.MarsAtlasLabel, Is.EqualTo(5));
            Assert.That(new VisualizationConfiguration().AtlasConfiguration, Is.Null);
            Assert.That(new FMRIConfiguration().SelectedResourceIndex, Is.Zero);
            Assert.That(new CCEPColumn().CCEPConfiguration.SiteID, Is.Null);
        }

        [Test]
        public void VisualizationConfiguration_CloneAndCopy_PreserveSceneViewCameraAndColumnState()
        {
            VisualizationConfiguration source = new(ColorType.Surface, ColorType.Default, ColorType.MatLab, MeshPart.Left, "mesh-alpha", "mri-alpha", "implantation-alpha", true, true, 0.35f, true, true, true, true, 2.25f, 0.15f, 0.85f, CameraControl.Orbital, new[] { new Cut(Vector3.right, CutOrientation.Sagittal, true, 12.5f) }, new[] { new View(new Vector3(1, 2, 3), Quaternion.Euler(10, 20, 30), new Vector3(4, 5, 6)) }, new[] { new RegionOfInterest("roi-alpha", new List<DataSphere> { new(new Vector3(7, 8, 9), 3.5f) }) }, "module3d-configuration-visualization-config-001", SurfaceRepresentation.Inflated);

            VisualizationConfiguration clone = (VisualizationConfiguration)source.Clone();
            VisualizationConfiguration copy = new();
            copy.Copy(source);

            AssertConfigurationMatches(source, clone);
            AssertConfigurationMatches(source, copy);
            Assert.That(clone.RegionsOfInterest, Is.Not.SameAs(source.RegionsOfInterest));
            Assert.That(clone.RegionsOfInterest[0].Spheres, Is.Not.SameAs(source.RegionsOfInterest[0].Spheres));
        }

        [Test]
        [Category("NativeMigration")]
        [Category("MigrationFunctional")]
        public void RegionOfInterest_ManagedSerializationRoundTripPreservesEverySphere()
        {
            using TempDirectoryScope temp = new();
            VisualizationConfiguration source = new();
            source.RegionsOfInterest.Add(new RegionOfInterest("roi-round-trip", new List<DataSphere>
            {
                new(new Vector3(-7.5f, 2.25f, 0.125f), 3.5f),
                new(new Vector3(11, -13, 17), 0.25f)
            }));

            VisualizationConfiguration loaded = RoundTrip(temp, source, "module3d-roi-round-trip.json");

            Assert.That(loaded.RegionsOfInterest, Has.Count.EqualTo(1));
            Assert.That(loaded.RegionsOfInterest[0].Name, Is.EqualTo("roi-round-trip"));
            Assert.That(loaded.RegionsOfInterest[0].Spheres, Has.Count.EqualTo(2));
            Assert.That(loaded.RegionsOfInterest[0].Spheres[0].Position.ToVector3(), Is.EqualTo(new Vector3(-7.5f, 2.25f, 0.125f)));
            Assert.That(loaded.RegionsOfInterest[0].Spheres[0].Radius, Is.EqualTo(3.5f));
            Assert.That(loaded.RegionsOfInterest[0].Spheres[1].Position.ToVector3(), Is.EqualTo(new Vector3(11, -13, 17)));
            Assert.That(loaded.RegionsOfInterest[0].Spheres[1].Radius, Is.EqualTo(0.25f));
        }

        [Test]
        public void VisualizationColumns_AllCurrentVariants_CloneRoundTripAndCompatibilityAreStable()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project sourceProject = SyntheticProjectFactory.CreateCompleteProject();
            Visualization source = sourceProject.Visualizations.Single();

            Assert.That(source.Columns.Select(column => column.GetType()), Is.EquivalentTo(new[]
            {
                typeof(AnatomicColumn),
                typeof(IEEGColumn),
                typeof(CCEPColumn),
                typeof(FMRIColumn),
                typeof(MEGColumn),
                typeof(StaticColumn)
            }));
            Assert.That(source.Columns.Select(column => column.IsCompatible(source.Patients)), Is.All.True);

            Column[] clones = source.Columns.Select(column => (Column)column.Clone()).ToArray();

            Assert.That(clones.Select(column => column.ID), Is.EquivalentTo(source.Columns.Select(column => column.ID)));
            Assert.That(clones.Select(column => column.BaseConfiguration.ID), Is.EquivalentTo(source.Columns.Select(column => column.BaseConfiguration.ID)));
            Assert.That(clones.Select(column => column.BaseConfiguration), Is.All.Not.Null);
            Assert.That(clones.Zip(source.Columns, (clone, original) => ReferenceEquals(clone.BaseConfiguration, original.BaseConfiguration)), Is.All.False);

            Visualization loaded = RoundTrip(temp, source, "module3d-configuration-visualization.json");
            LoadingContext context = new(PersistentDataManager.Tags.AllTags, new[] { sourceProject.Datasets[0].Protocol }, sourceProject.Patients, sourceProject.Datasets);
            context.ResolveProject(sourceProject.Patients, Array.Empty<Group>(), sourceProject.Datasets, new[] { loaded });

            Assert.That(loaded.Columns.Select(column => column.GetType()), Is.EquivalentTo(source.Columns.Select(column => column.GetType())));
            Assert.That(loaded.Columns.Select(column => column.ID), Is.EquivalentTo(source.Columns.Select(column => column.ID)));
            Assert.That(loaded.Columns.Select(column => column.BaseConfiguration.ID), Is.EquivalentTo(source.Columns.Select(column => column.BaseConfiguration.ID)));
            Assert.That(loaded.Columns.Select(column => column.IsCompatible(sourceProject.Patients)), Is.All.True);
        }

        [Test]
        public void Visualization_IsVisualizableWithoutPatients_OnlyAllowsAnatomicAndSharedFMRIColumns()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project project = SyntheticProjectFactory.CreateCompleteProject();
            Visualization source = project.Visualizations.Single();
            AnatomicColumn anatomicColumn = source.AnatomicColumns.Single();
            FMRIColumn sharedFMRIColumn = source.FMRIColumns.Single();
            Dataset patientOnlyDataset = new("patient-only-dataset", project.Datasets[0].Protocol, project.Datasets[0].Data.Where(dataInfo => dataInfo is not SharedFMRIDataInfo));
            FMRIColumn patientOnlyFMRIColumn = new("patient-only-fmri", new BaseConfiguration(), patientOnlyDataset, new FMRIConfiguration());

            Assert.That(new Visualization("anatomic", Array.Empty<Patient>(), new Column[] { anatomicColumn }).IsVisualizable, Is.True);
            Assert.That(new Visualization("shared-fmri", Array.Empty<Patient>(), new Column[] { sharedFMRIColumn }).IsVisualizable, Is.True);
            Assert.That(new Visualization("anatomic-and-shared-fmri", Array.Empty<Patient>(), new Column[] { anatomicColumn, sharedFMRIColumn }).IsVisualizable, Is.True);
            Assert.That(new Visualization("patient-only-fmri", Array.Empty<Patient>(), new Column[] { patientOnlyFMRIColumn }).IsVisualizable, Is.False);

            foreach (Column patientDependentColumn in source.Columns.Where(column => column is IEEGColumn || column is CCEPColumn || column is MEGColumn || column is StaticColumn))
            {
                Assert.That(new Visualization(patientDependentColumn.Name, Array.Empty<Patient>(), new[] { patientDependentColumn }).IsVisualizable, Is.False, patientDependentColumn.GetType().Name);
            }
        }

        [Test]
        public void AnatomyAndDynamicDataParameters_ClampValuesAndRaiseChangeEvents()
        {
            AnatomyDataParameters anatomy = new();
            int anatomyUpdates = 0;
            anatomy.OnUpdateInfluenceDistance.AddListener(() => anatomyUpdates++);

            anatomy.InfluenceDistance = 75;
            anatomy.InfluenceDistance = 75;
            anatomy.InfluenceDistance = -10;

            Assert.That(anatomy.InfluenceDistance, Is.EqualTo(0).Within(0.0001f));
            Assert.That(anatomyUpdates, Is.EqualTo(2));

            DynamicDataParameters dynamic = new();
            int influenceUpdates = 0;
            int spanUpdates = 0;
            dynamic.OnUpdateInfluenceDistance.AddListener(() => influenceUpdates++);
            dynamic.OnUpdateSpanValues.AddListener(() => spanUpdates++);

            dynamic.InfluenceDistance = 80;
            dynamic.SetSpanValues(10, 50, 5);
            dynamic.SetSpanValues(0, 0, 0);

            Assert.That(dynamic.InfluenceDistance, Is.EqualTo(50).Within(0.0001f));
            Assert.That(dynamic.SpanMin, Is.EqualTo(5).Within(0.0001f));
            Assert.That(dynamic.Middle, Is.EqualTo(5).Within(0.0001f));
            Assert.That(dynamic.SpanMax, Is.EqualTo(5).Within(0.0001f));
            Assert.That(influenceUpdates, Is.EqualTo(1));
            Assert.That(spanUpdates, Is.EqualTo(1));
        }

        [Test]
        public void FMRIAndMEGDataParameters_SetResetAndHideValuesAreCharacterized()
        {
            FMRIDataParameters fmri = new();
            int fmriCalUpdates = 0;
            int fmriHideUpdates = 0;
            fmri.OnUpdateCalValues.AddListener(() => fmriCalUpdates++);
            fmri.OnUpdateHideValues.AddListener(() => fmriHideUpdates++);

            fmri.SetSpanValues(0.8f, 0.2f, 0.9f, 0.3f);
            fmri.SetHideValues(true, false, true);

            Assert.That(fmri.FMRINegativeCalMinFactor, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(fmri.FMRINegativeCalMaxFactor, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(fmri.FMRIPositiveCalMinFactor, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(fmri.FMRIPositiveCalMaxFactor, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(fmri.HideLowerValues, Is.True);
            Assert.That(fmri.HideMiddleValues, Is.False);
            Assert.That(fmri.HideHigherValues, Is.True);

            fmri.ResetSpanValues();
            fmri.ResetHideValues();

            Assert.That(fmri.FMRINegativeCalMinFactor, Is.EqualTo(0.05f).Within(0.0001f));
            Assert.That(fmri.FMRINegativeCalMaxFactor, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(fmri.HideLowerValues, Is.False);
            Assert.That(fmriCalUpdates, Is.EqualTo(2));
            Assert.That(fmriHideUpdates, Is.EqualTo(2));

            MEGDataParameters meg = new();
            int megCalUpdates = 0;
            int megHideUpdates = 0;
            meg.OnUpdateCalValues.AddListener(() => megCalUpdates++);
            meg.OnUpdateHideValues.AddListener(() => megHideUpdates++);

            meg.SetSpanValues(0.7f, 0.1f, 0.6f, 0.4f);
            meg.SetHideValues(false, true, true);
            meg.ResetSpanValues();
            meg.ResetHideValues();

            Assert.That(meg.FMRINegativeCalMinFactor, Is.EqualTo(0.05f).Within(0.0001f));
            Assert.That(meg.FMRINegativeCalMaxFactor, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(meg.FMRIPositiveCalMinFactor, Is.EqualTo(0.05f).Within(0.0001f));
            Assert.That(meg.FMRIPositiveCalMaxFactor, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(meg.HideMiddleValues, Is.False);
            Assert.That(megCalUpdates, Is.EqualTo(2));
            Assert.That(megHideUpdates, Is.EqualTo(2));
        }

        [Test]
        public void AtlasInfo_StoresHoverMetadataWithoutSceneDependencies()
        {
            AtlasInfo info = new(true, new Vector3(1, 2, 3), AtlasInfo.AtlasType.JuBrainAtlas, "area-alpha", "location-alpha", "label-alpha", "status-alpha", "doi-alpha");

            Assert.That(info.Enabled, Is.True);
            Assert.That(info.Position, Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(info.Type, Is.EqualTo(AtlasInfo.AtlasType.JuBrainAtlas));
            Assert.That(info.Information1, Is.EqualTo("area-alpha"));
            Assert.That(info.Information2, Is.EqualTo("location-alpha"));
            Assert.That(info.Information3, Is.EqualTo("label-alpha"));
            Assert.That(info.Information4, Is.EqualTo("status-alpha"));
            Assert.That(info.Information5, Is.EqualTo("doi-alpha"));
        }

        [Test]
        public void SceneInformation_UpdateFlagsCascadeToDependentRenderingWork()
        {
            SceneInformation information = new();

            information.GeometryNeedsUpdate = true;

            Assert.That(information.GeometryNeedsUpdate, Is.True);
            Assert.That(information.CutsNeedUpdate, Is.True);
            Assert.That(information.BaseCutTexturesNeedUpdate, Is.True);
            Assert.That(information.FunctionalCutTexturesNeedUpdate, Is.True);
            Assert.That(information.GUICutTexturesNeedUpdate, Is.True);

            information = new SceneInformation();

            information.BaseCutTexturesNeedUpdate = true;

            Assert.That(information.GeometryNeedsUpdate, Is.False);
            Assert.That(information.CutsNeedUpdate, Is.False);
            Assert.That(information.BaseCutTexturesNeedUpdate, Is.True);
            Assert.That(information.FunctionalCutTexturesNeedUpdate, Is.True);
            Assert.That(information.GUICutTexturesNeedUpdate, Is.True);
        }

        [Test]
        public void Scene3DPrefab_HasManagersContainersAndColumnVariantPrefabs()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/3D/Scenes/Scene 3D.prefab");
            Assert.That(prefab, Is.Not.Null);

            Base3DScene scene = prefab.GetComponent<Base3DScene>();
            Assert.That(scene, Is.Not.Null);

            SerializedObject serializedScene = new(scene);
            AssertReference<MeshManager>(serializedScene, "m_MeshManager");
            AssertReference<MRIManager>(serializedScene, "m_MRIManager");
            AssertReference<ImplantationManager>(serializedScene, "m_ImplantationManager");
            AssertReference<TriangleEraser>(serializedScene, "m_TriangleEraser");
            AssertReference<AtlasManager>(serializedScene, "m_AtlasManager");
            AssertReference<FMRIManager>(serializedScene, "m_FMRIManager");
            AssertReference<ROIManager>(serializedScene, "m_ROIManager");
            AssertReference<DisplayedObjects>(serializedScene, "m_DisplayedObjects");
            AssertReference<Transform>(serializedScene, "m_ColumnsContainer");

            AssertColumnPrefab<Column3DAnatomy>(serializedScene, "m_Column3DAnatomyPrefab");
            AssertColumnPrefab<Column3DIEEG>(serializedScene, "m_Column3DIEEGPrefab");
            AssertColumnPrefab<Column3DCCEP>(serializedScene, "m_Column3DCCEPPrefab");
            AssertColumnPrefab<Column3DFMRI>(serializedScene, "m_Column3DFMRIPrefab");
            AssertColumnPrefab<Column3DMEG>(serializedScene, "m_Column3DMEGPrefab");
            AssertColumnPrefab<Column3DStatic>(serializedScene, "m_Column3DStaticPrefab");
        }

        [Test]
        public void Scene3DPrefab_WiresDisplayedObjectsAndManagersToSceneGraph()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/3D/Scenes/Scene 3D.prefab");
            Assert.That(prefab, Is.Not.Null);

            Base3DScene scene = prefab.GetComponent<Base3DScene>();
            Assert.That(scene, Is.Not.Null);

            SerializedObject serializedScene = new(scene);
            DisplayedObjects displayedObjects = AssertReference<DisplayedObjects>(serializedScene, "m_DisplayedObjects");
            SerializedObject serializedDisplayedObjects = new(displayedObjects);

            AssertReference<Base3DScene>(serializedDisplayedObjects, "m_Scene");
            AssertReference<Transform>(serializedDisplayedObjects, "m_BrainSurfaceMeshesParent");
            AssertReference<Transform>(serializedDisplayedObjects, "m_BrainCutMeshesParent");
            AssertReference<Transform>(serializedDisplayedObjects, "m_SitesMeshesParent");
            AssertReference<Transform>(serializedDisplayedObjects, "m_ROIParent");
            AssertReference<GameObject>(serializedDisplayedObjects, "m_BrainPrefab");
            AssertReference<GameObject>(serializedDisplayedObjects, "m_SimplifiedBrainPrefab");
            AssertReference<GameObject>(serializedDisplayedObjects, "m_InvisibleBrainPrefab");
            AssertReference<GameObject>(serializedDisplayedObjects, "m_CutPrefab");
            AssertReference<GameObject>(serializedDisplayedObjects, "m_SitePrefab");
            AssertReference<GameObject>(serializedDisplayedObjects, "m_ROIPrefab");

            AssertManagerReferences<MeshManager>(serializedScene, "m_MeshManager", scene, displayedObjects);
            AssertManagerReferences<MRIManager>(serializedScene, "m_MRIManager", scene, displayedObjects);
            AssertManagerReferences<ImplantationManager>(serializedScene, "m_ImplantationManager", scene, displayedObjects);
            AssertManagerReferences<TriangleEraser>(serializedScene, "m_TriangleEraser", scene, displayedObjects);
            AssertManagerReferences<AtlasManager>(serializedScene, "m_AtlasManager", scene, displayedObjects);
            AssertManagerReferences<FMRIManager>(serializedScene, "m_FMRIManager", scene, displayedObjects);
            AssertManagerReferences<ROIManager>(serializedScene, "m_ROIManager", scene, displayedObjects);
        }

        [Test]
        public void View3DPrefab_HasCameraAndRenderingHelpers()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/3D/Scenes/View 3D.prefab");
            Assert.That(prefab, Is.Not.Null);

            View3D view = prefab.GetComponent<View3D>();
            Assert.That(view, Is.Not.Null);

            SerializedObject serializedView = new(view);
            Camera3D camera3D = AssertReference<Camera3D>(serializedView, "m_Camera3D");
            SerializedObject serializedCamera = new(camera3D);

            AssertReference<Camera>(serializedCamera, "m_Camera");
            AssertReference<LineRenderer>(serializedCamera, "m_CircleX");
            AssertReference<LineRenderer>(serializedCamera, "m_CircleY");
            AssertReference<LineRenderer>(serializedCamera, "m_CircleZ");
            AssertReference<LineRenderer>(serializedCamera, "m_CutCircle");
            AssertReference<LineRenderer>(serializedCamera, "m_CutCross1");
            AssertReference<LineRenderer>(serializedCamera, "m_CutCross2");
        }

        [Test]
        public void Column3DPrefabs_HaveViewPrefabAndRenderingContainers()
        {
            AssertColumnPrefabContract<Column3DAnatomy>("Assets/Prefabs/3D/Scenes/Column 3D Anatomy.prefab");
            AssertColumnPrefabContract<Column3DIEEG>("Assets/Prefabs/3D/Scenes/Column 3D IEEG.prefab");
            AssertColumnPrefabContract<Column3DCCEP>("Assets/Prefabs/3D/Scenes/Column 3D CCEP.prefab");
            AssertColumnPrefabContract<Column3DFMRI>("Assets/Prefabs/3D/Scenes/Column 3D FMRI.prefab");
            AssertColumnPrefabContract<Column3DMEG>("Assets/Prefabs/3D/Scenes/Column 3D MEG.prefab");
            AssertColumnPrefabContract<Column3DStatic>("Assets/Prefabs/3D/Scenes/Column 3D Static.prefab");
        }

        private static T RoundTrip<T>(TempDirectoryScope temp, T source, string fileName) where T : new()
        {
            string path = temp.GetPath(fileName);
            Assert.That(ClassLoaderSaver.SaveToJSon(source, path, true), Is.True);
            return ClassLoaderSaver.LoadFromJson<T>(path);
        }

        private static T AssertReference<T>(SerializedObject serializedObject, string propertyName) where T : UnityEngine.Object
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Missing serialized property {propertyName}");
            Assert.That(property.objectReferenceValue, Is.Not.Null, $"Missing serialized reference {propertyName}");
            Assert.That(property.objectReferenceValue, Is.TypeOf<T>().Or.AssignableTo<T>(), $"Unexpected reference type for {propertyName}");
            return (T)property.objectReferenceValue;
        }

        private static void AssertColumnPrefab<T>(SerializedObject serializedScene, string propertyName) where T : Column3D
        {
            GameObject prefab = AssertReference<GameObject>(serializedScene, propertyName);
            Assert.That(prefab.GetComponent<T>(), Is.Not.Null, $"{propertyName} must reference a prefab with {typeof(T).Name}");
        }

        private static void AssertColumnPrefabContract<T>(string path) where T : Column3D
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);

            T column = prefab.GetComponent<T>();
            Assert.That(column, Is.Not.Null, path);

            SerializedObject serializedColumn = new(column);
            AssertReference<Transform>(serializedColumn, "m_BrainSurfaceMeshesParent");
            AssertReference<Transform>(serializedColumn, "m_CutMeshesParent");
            AssertReference<Transform>(serializedColumn, "m_SitesMeshesParent");
            var desktop = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/3D/Scenes/Scene 3D.prefab").GetComponent<DesktopScenePresentation>();
            GameObject viewPrefab = AssertReference<GameObject>(new SerializedObject(desktop), "m_ViewPrefab");
            Assert.That(viewPrefab.GetComponent<View3D>(), Is.Not.Null, "The Desktop presentation must reference a View3D prefab");
            Assert.That(serializedColumn.FindProperty("m_ViewPrefab"), Is.Null, "Common content must not depend on Desktop camera prefabs.");
            Assert.That(prefab.transform.Find("Views"), Is.Not.Null, $"{path} must contain a Views child for Column3D.AddView");
        }

        private static void AssertManagerReferences<T>(SerializedObject serializedScene, string propertyName, Base3DScene expectedScene, DisplayedObjects expectedDisplayedObjects) where T : Component
        {
            T manager = AssertReference<T>(serializedScene, propertyName);
            SerializedObject serializedManager = new(manager);
            Assert.That(AssertReference<Base3DScene>(serializedManager, "m_Scene"), Is.SameAs(expectedScene), $"{propertyName}.m_Scene");
            Assert.That(AssertReference<DisplayedObjects>(serializedManager, "m_DisplayedObjects"), Is.SameAs(expectedDisplayedObjects), $"{propertyName}.m_DisplayedObjects");
        }

        private static void AssertConfigurationMatches(VisualizationConfiguration expected, VisualizationConfiguration actual)
        {
            Assert.That(actual.ID, Is.EqualTo(expected.ID));
            Assert.That(actual.BrainColor, Is.EqualTo(expected.BrainColor));
            Assert.That(actual.BrainCutColor, Is.EqualTo(expected.BrainCutColor));
            Assert.That(actual.Colormap, Is.EqualTo(expected.Colormap));
            Assert.That(actual.MeshPart, Is.EqualTo(expected.MeshPart));
            Assert.That(actual.MeshName, Is.EqualTo(expected.MeshName));
            Assert.That(actual.SurfaceRepresentation, Is.EqualTo(expected.SurfaceRepresentation));
            Assert.That(actual.MRIName, Is.EqualTo(expected.MRIName));
            Assert.That(actual.ImplantationName, Is.EqualTo(expected.ImplantationName));
            Assert.That(actual.ShowEdges, Is.EqualTo(expected.ShowEdges));
            Assert.That(actual.TransparentBrain, Is.EqualTo(expected.TransparentBrain));
            Assert.That(actual.BrainAlpha, Is.EqualTo(expected.BrainAlpha).Within(0.0001f));
            Assert.That(actual.StrongCuts, Is.EqualTo(expected.StrongCuts));
            Assert.That(actual.HideBlacklistedSites, Is.EqualTo(expected.HideBlacklistedSites));
            Assert.That(actual.ShowAllSites, Is.EqualTo(expected.ShowAllSites));
            Assert.That(actual.AutomaticCutAroundSelectedSite, Is.EqualTo(expected.AutomaticCutAroundSelectedSite));
            Assert.That(actual.SiteGain, Is.EqualTo(expected.SiteGain).Within(0.0001f));
            Assert.That(actual.MRICalMinFactor, Is.EqualTo(expected.MRICalMinFactor).Within(0.0001f));
            Assert.That(actual.MRICalMaxFactor, Is.EqualTo(expected.MRICalMaxFactor).Within(0.0001f));
            Assert.That(actual.CameraType, Is.EqualTo(expected.CameraType));
            Assert.That(actual.Cuts, Has.Count.EqualTo(1));
            Assert.That(actual.Cuts[0].Orientation, Is.EqualTo(CutOrientation.Sagittal));
            Assert.That(actual.Cuts[0].Flip, Is.True);
            Assert.That(actual.Cuts[0].Position, Is.EqualTo(12.5f).Within(0.0001f));
            Assert.That(actual.Views, Has.Count.EqualTo(1));
            Assert.That(actual.Views[0].Position.ToVector3(), Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(actual.Views[0].Target.ToVector3(), Is.EqualTo(new Vector3(4, 5, 6)));
            Assert.That(actual.RegionsOfInterest, Has.Count.EqualTo(1));
            Assert.That(actual.RegionsOfInterest[0].Name, Is.EqualTo("roi-alpha"));
            Assert.That(actual.RegionsOfInterest[0].Spheres.Single().Radius, Is.EqualTo(3.5f).Within(0.0001f));
        }
    }
}
