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

namespace HBP.Tests.Serialization
{
    public class Module3DConfigurationTests
    {
        [Test]
        public void VisualizationConfiguration_CloneAndCopy_PreserveSceneViewCameraAndColumnState()
        {
            VisualizationConfiguration source = new(ColorType.Surface, ColorType.Default, ColorType.MatLab, MeshPart.Left, "mesh-alpha", "mri-alpha", "implantation-alpha", true, true, 0.35f, true, true, true, true, 2.25f, 0.15f, 0.85f, CameraControl.Orbital, new[] { new Cut(Vector3.right, CutOrientation.Sagittal, true, 12.5f) }, new[] { new View(new Vector3(1, 2, 3), Quaternion.Euler(10, 20, 30), new Vector3(4, 5, 6)) }, new[] { new RegionOfInterest("roi-alpha", new List<DataSphere> { new(new Vector3(7, 8, 9), 3.5f) }) }, "module3d-configuration-visualization-config-001");

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
            GameObject viewPrefab = AssertReference<GameObject>(serializedColumn, "m_ViewPrefab");
            Assert.That(viewPrefab.GetComponent<View3D>(), Is.Not.Null, $"{path} must reference a View3D prefab");
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
