using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Exceptions;
using HBP.Core.DLL.HbpCore;
using HBP.Core.Object3D;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Tests.PlayMode.Utilities;
using NUnit.Framework;
using UnityEngine.Events;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HBP.Tests.PlayMode.Module3D
{
    public class Module3DScenePlayModeTests
    {
        private GenericEvent<Base3DScene> m_OnSelectScene;
        private GenericEvent<Base3DScene> m_OnDeselectScene;
        private GenericEvent<Base3DScene> m_OnMinimizeScene;
        private GenericEvent<Column3D> m_OnSelectColumn;
        private GenericEvent<View3D> m_OnSelectView;
        private UnityEvent m_OnRequestUpdateInToolbar;
        private Module3DMain m_Module3DMainInstance;
        private SharedMaterials m_Module3DMainSharedMaterials;

        [SetUp]
        public void SetUp()
        {
            m_Module3DMainInstance = GetModule3DMainInstance();
            m_OnSelectScene = Module3DMain.OnSelectScene;
            m_OnDeselectScene = Module3DMain.OnDeselectScene;
            m_OnMinimizeScene = Module3DMain.OnMinimizeScene;
            m_OnSelectColumn = Module3DMain.OnSelectColumn;
            m_OnSelectView = Module3DMain.OnSelectView;
            m_OnRequestUpdateInToolbar = Module3DMain.OnRequestUpdateInToolbar;
            if (m_Module3DMainInstance != null)
            {
                m_Module3DMainSharedMaterials = GetPrivateField<SharedMaterials>(m_Module3DMainInstance, "m_SharedMaterials");
                SetPrivateField(m_Module3DMainInstance, "m_SharedMaterials", CreateSharedMaterials());
            }

            Module3DMain.OnSelectScene = new GenericEvent<Base3DScene>();
            Module3DMain.OnDeselectScene = new GenericEvent<Base3DScene>();
            Module3DMain.OnMinimizeScene = new GenericEvent<Base3DScene>();
            Module3DMain.OnSelectColumn = new GenericEvent<Column3D>();
            Module3DMain.OnSelectView = new GenericEvent<View3D>();
            Module3DMain.OnRequestUpdateInToolbar = new UnityEvent();
        }

        [TearDown]
        public void TearDown()
        {
            SetModule3DMainInstance(m_Module3DMainInstance);
            if (m_Module3DMainInstance != null)
            {
                SetPrivateField(m_Module3DMainInstance, "m_SharedMaterials", m_Module3DMainSharedMaterials);
            }
            Module3DMain.OnSelectScene = m_OnSelectScene;
            Module3DMain.OnDeselectScene = m_OnDeselectScene;
            Module3DMain.OnMinimizeScene = m_OnMinimizeScene;
            Module3DMain.OnSelectColumn = m_OnSelectColumn;
            Module3DMain.OnSelectView = m_OnSelectView;
            Module3DMain.OnRequestUpdateInToolbar = m_OnRequestUpdateInToolbar;
        }

        [UnityTest]
        [Category("PlayMode.Module3DScene")]
        public IEnumerator Module3DMain_SelectedSceneColumnAndViewFollowControlledSceneState()
        {
            using PlayModeSceneScope scene = new("Module3DSceneModule3DMainSelection");
            GameObject moduleObject = new("Controlled Module3DMain");
            moduleObject.SetActive(false);
            SceneManager.MoveGameObjectToScene(moduleObject, scene.Scene);
            Module3DMain module = moduleObject.AddComponent<Module3DMain>();
            SetPrivateField(module, "m_SharedMaterials", CreateSharedMaterials());
            SetModule3DMainInstance(module);

            Base3DScene firstScene = CreateBaseScene(scene, "First Scene");
            Base3DScene secondScene = CreateBaseScene(scene, "Second Scene");
            Column3DStatic firstColumn = CreateColumn<Column3DStatic>(scene, "First Scene Column");
            View3D firstView = CreateView(scene, "First Scene View");
            firstColumn.Views.Add(firstView);
            firstScene.Columns.Add(firstColumn);
            SetPrivateField(module, "m_Scenes", new List<Base3DScene> { firstScene, secondScene });

            firstScene.IsSelected = true;
            firstColumn.IsSelected = true;
            firstView.IsSelected = true;

            yield return null;

            Assert.That(Module3DMain.Scenes, Is.EquivalentTo(new[] { firstScene, secondScene }));
            Assert.That(Module3DMain.SelectedScene, Is.SameAs(firstScene));
            Assert.That(Module3DMain.SelectedColumn, Is.SameAs(firstColumn));
            Assert.That(Module3DMain.SelectedView, Is.SameAs(firstView));

            firstScene.IsSelected = false;
            secondScene.IsSelected = true;

            yield return null;

            Assert.That(Module3DMain.SelectedScene, Is.SameAs(secondScene));
            Assert.That(Module3DMain.SelectedColumn, Is.Null);
            Assert.That(Module3DMain.SelectedView, Is.Null);
        }

        [UnityTest]
        [Category("PlayMode.Module3DScene")]
        public IEnumerator Base3DScene_ManualColumnsExposeVariantListsAndSelection()
        {
            using PlayModeSceneScope scene = new("Module3DSceneModule3DColumns");
            Base3DScene baseScene = scene.Root.AddComponent<Base3DScene>();

            Column3DAnatomy anatomy = CreateColumn<Column3DAnatomy>(scene, "Anatomy Column");
            Column3DIEEG ieeg = CreateColumn<Column3DIEEG>(scene, "IEEG Column");
            Column3DCCEP ccep = CreateColumn<Column3DCCEP>(scene, "CCEP Column");
            Column3DFMRI fmri = CreateColumn<Column3DFMRI>(scene, "FMRI Column");
            Column3DMEG meg = CreateColumn<Column3DMEG>(scene, "MEG Column");
            Column3DStatic stat = CreateColumn<Column3DStatic>(scene, "Static Column");

            baseScene.Columns.Add(anatomy);
            baseScene.Columns.Add(ieeg);
            baseScene.Columns.Add(ccep);
            baseScene.Columns.Add(fmri);
            baseScene.Columns.Add(meg);
            baseScene.Columns.Add(stat);

            ccep.IsSelected = true;

            yield return null;

            Assert.That(baseScene.Columns, Has.Count.EqualTo(6));
            Assert.That(baseScene.ColumnsAnatomy, Is.EquivalentTo(new[] { anatomy }));
            Assert.That(baseScene.ColumnsDynamic, Is.EquivalentTo(new Column3DDynamic[] { ieeg, ccep }));
            Assert.That(baseScene.ColumnsIEEG, Is.EquivalentTo(new[] { ieeg }));
            Assert.That(baseScene.ColumnsCCEP, Is.EquivalentTo(new[] { ccep }));
            Assert.That(baseScene.ColumnsFMRI, Is.EquivalentTo(new[] { fmri }));
            Assert.That(baseScene.ColumnsMEG, Is.EquivalentTo(new[] { meg }));
            Assert.That(baseScene.ColumnsStatic, Is.EquivalentTo(new[] { stat }));
            Assert.That(baseScene.SelectedColumn, Is.SameAs(ccep));
            Assert.That(baseScene.ViewLineNumber, Is.EqualTo(0));
        }

        [UnityTest]
        [Category("PlayMode.Module3DScene")]
        public IEnumerator Base3DScene_UpdateVisibleStatePublishesVisibilityAndSelectionEvents()
        {
            using PlayModeSceneScope scene = new("Module3DSceneModule3DVisibleState");
            Base3DScene baseScene = CreateBaseScene(scene, "Visible State Scene");
            Column3DStatic column = CreateColumn<Column3DStatic>(scene, "Visible State Column");
            View3D view = CreateControlledView(scene, "Visible State View", out _, out _);
            column.Views.Add(view);
            baseScene.Columns.Add(column);
            column.IsSelected = true;
            view.IsSelected = true;

            List<bool> visibleStates = new();
            Base3DScene minimizedScene = null;
            Base3DScene selectedScene = null;
            Column3D selectedColumn = null;
            View3D selectedView = null;
            baseScene.OnChangeVisibleState.AddListener(visibleStates.Add);
            Module3DMain.OnMinimizeScene.AddListener(sceneValue => minimizedScene = sceneValue);
            Module3DMain.OnSelectScene.AddListener(sceneValue => selectedScene = sceneValue);
            Module3DMain.OnSelectColumn.AddListener(columnValue => selectedColumn = columnValue);
            Module3DMain.OnSelectView.AddListener(viewValue => selectedView = viewValue);

            baseScene.UpdateVisibleState(false);
            yield return null;

            Assert.That(baseScene.gameObject.activeSelf, Is.False);
            Assert.That(baseScene.IsSelected, Is.False);
            Assert.That(minimizedScene, Is.SameAs(baseScene));

            baseScene.UpdateVisibleState(true);
            yield return null;

            Assert.That(baseScene.gameObject.activeSelf, Is.True);
            Assert.That(baseScene.IsSelected, Is.True);
            Assert.That(selectedScene, Is.SameAs(baseScene));
            Assert.That(selectedColumn, Is.SameAs(column));
            Assert.That(selectedView, Is.SameAs(view));
            Assert.That(visibleStates, Is.EqualTo(new[] { false, true }));
        }

        [UnityTest]
        [Category("PlayMode.Module3DScene")]
        public IEnumerator Base3DScene_SelectionRaisesModule3DSelectionEvents()
        {
            using PlayModeSceneScope scene = new("Module3DSceneModule3DSceneSelection");
            Base3DScene baseScene = scene.Root.AddComponent<Base3DScene>();
            Base3DScene selectedScene = null;
            Base3DScene deselectedScene = null;

            void OnSelect(Base3DScene selected) => selectedScene = selected;
            void OnDeselect(Base3DScene deselected) => deselectedScene = deselected;

            Module3DMain.OnSelectScene.AddListener(OnSelect);
            Module3DMain.OnDeselectScene.AddListener(OnDeselect);
            try
            {
                baseScene.IsSelected = true;
                baseScene.IsSelected = false;

                yield return null;

                Assert.That(selectedScene, Is.SameAs(baseScene));
                Assert.That(deselectedScene, Is.SameAs(baseScene));
            }
            finally
            {
                Module3DMain.OnSelectScene.RemoveListener(OnSelect);
                Module3DMain.OnDeselectScene.RemoveListener(OnDeselect);
            }
        }

        [UnityTest]
        [Category("PlayMode.Module3DScene")]
        public IEnumerator Base3DScene_RemoveViewLine_RemovesViewsFromAllColumnsAndSelectsFallback()
        {
            using PlayModeSceneScope scene = new("Module3DSceneModule3DRemoveViewLine");
            Base3DScene baseScene = scene.Root.AddComponent<Base3DScene>();
            Column3DAnatomy firstColumn = CreateColumn<Column3DAnatomy>(scene, "First Column");
            Column3DStatic secondColumn = CreateColumn<Column3DStatic>(scene, "Second Column");
            View3D firstColumnFirstView = CreateView(scene, "First Column View 0");
            View3D firstColumnSecondView = CreateView(scene, "First Column View 1");
            View3D secondColumnFirstView = CreateView(scene, "Second Column View 0");
            View3D secondColumnSecondView = CreateView(scene, "Second Column View 1");

            firstColumn.Views.Add(firstColumnFirstView);
            firstColumn.Views.Add(firstColumnSecondView);
            secondColumn.Views.Add(secondColumnFirstView);
            secondColumn.Views.Add(secondColumnSecondView);
            baseScene.Columns.Add(firstColumn);
            baseScene.Columns.Add(secondColumn);
            firstColumn.IsSelected = true;
            firstColumnSecondView.IsSelected = true;
            int removedLine = -1;
            baseScene.OnRemoveViewLine.AddListener(line => removedLine = line);

            baseScene.RemoveViewLine(1);

            yield return null;

            Assert.That(baseScene.ViewLineNumber, Is.EqualTo(1));
            Assert.That(firstColumn.Views, Is.EquivalentTo(new[] { firstColumnFirstView }));
            Assert.That(secondColumn.Views, Is.EquivalentTo(new[] { secondColumnFirstView }));
            Assert.That(firstColumn.SelectedView, Is.SameAs(firstColumnFirstView));
            Assert.That(removedLine, Is.EqualTo(1));
            Assert.That(firstColumnSecondView == null, Is.True);
            Assert.That(secondColumnSecondView == null, Is.True);
        }

        [UnityTest]
        [Category("PlayMode.Module3DScene")]
        public IEnumerator Column3D_SelectionMinimizeAndActivityAlphaRaiseObservableEvents()
        {
            using PlayModeSceneScope scene = new("Module3DSceneModule3DColumnState");
            Column3DStatic column = CreateColumn<Column3DStatic>(scene, "Observable Column");
            Column3D selectedColumn = null;
            int minimizedUpdates = 0;
            int alphaUpdates = 0;

            void OnSelect(Column3D selected) => selectedColumn = selected;

            Module3DMain.OnSelectColumn.AddListener(OnSelect);
            column.OnChangeMinimizedState.AddListener(() => minimizedUpdates++);
            column.OnUpdateActivityAlpha.AddListener(() => alphaUpdates++);
            try
            {
                column.IsSelected = true;
                column.IsMinimized = true;
                column.IsMinimized = true;
                column.ActivityAlpha = 0.45f;
                column.ActivityAlpha = 0.45f;

                yield return null;

                Assert.That(selectedColumn, Is.SameAs(column));
                Assert.That(column.IsMinimized, Is.True);
                Assert.That(column.ActivityAlpha, Is.EqualTo(0.45f).Within(0.0001f));
                Assert.That(minimizedUpdates, Is.EqualTo(1));
                Assert.That(alphaUpdates, Is.EqualTo(1));
            }
            finally
            {
                Module3DMain.OnSelectColumn.RemoveListener(OnSelect);
            }
        }

        [UnityTest]
        [Category("PlayMode.Module3DScene")]
        public IEnumerator Column3D_LoadAndSaveConfigurationSynchronizesActivityAlpha()
        {
            using PlayModeSceneScope scene = new("Module3DSceneModule3DColumnConfiguration");
            Column3DAnatomy column = CreateColumn<Column3DAnatomy>(scene, "Configured Column");
            HBP.Core.Data.AnatomicColumn columnData = new(
                "configured-column",
                new HBP.Core.Data.BaseConfiguration(0.33f, new System.Collections.Generic.Dictionary<string, HBP.Core.Data.SiteConfiguration>()),
                new HBP.Core.Data.AnatomicConfiguration("module3d-scene-anatomic-config-001"),
                "module3d-scene-anatomic-column-001");
            SetAutoProperty(column, "ColumnData", columnData);
            SetAutoProperty(column, "Sites", new System.Collections.Generic.List<HBP.Core.Object3D.Site>());

            column.LoadConfiguration();
            column.ActivityAlpha = 0.66f;
            column.SaveConfiguration();

            yield return null;

            Assert.That(column.ActivityAlpha, Is.EqualTo(0.66f).Within(0.0001f));
            Assert.That(columnData.BaseConfiguration.ActivityAlpha, Is.EqualTo(0.66f).Within(0.0001f));
        }

        [UnityTest]
        [Category("PlayMode.Module3DScene")]
        public IEnumerator View3D_MinimizeSelectionAndCameraCircleStateUseControlledCameraGraph()
        {
            using PlayModeSceneScope scene = new("Module3DSceneModule3DViewCamera");
            GameObject viewObject = new("Synthetic View3D");
            viewObject.SetActive(false);
            SceneManager.MoveGameObjectToScene(viewObject, scene.Scene);

            View3D view = viewObject.AddComponent<View3D>();
            Camera3D camera3D = viewObject.AddComponent<Camera3D>();
            Camera camera = viewObject.AddComponent<Camera>();
            LineRenderer circleX = CreateLineRenderer(scene, "Circle X");
            LineRenderer circleY = CreateLineRenderer(scene, "Circle Y");
            LineRenderer circleZ = CreateLineRenderer(scene, "Circle Z");
            SetPrivateField(view, "m_Camera3D", camera3D);
            SetPrivateField(view, "m_RegularCullingMask", 1 << 3);
            SetPrivateField(view, "m_MinimizedCullingMask", 0);
            SetPrivateField(camera3D, "m_Camera", camera);
            SetPrivateField(camera3D, "m_CircleX", circleX);
            SetPrivateField(camera3D, "m_CircleY", circleY);
            SetPrivateField(camera3D, "m_CircleZ", circleZ);

            View3D selectedView = null;
            int selectedUpdates = 0;
            void OnSelect(View3D selected) => selectedView = selected;

            Module3DMain.OnSelectView.AddListener(OnSelect);
            view.OnSelect.AddListener(() => selectedUpdates++);
            try
            {
                view.IsMinimized = true;
                view.IsMinimized = false;
                view.IsSelected = true;
                view.AutomaticRotation = true;
                view.AutomaticRotationSpeed = 3.5f;
                view.DisplayRotationCircles = true;

                yield return null;

                Assert.That(camera.enabled, Is.True);
                Assert.That(camera.cullingMask, Is.EqualTo(1 << 3));
                Assert.That(selectedView, Is.SameAs(view));
                Assert.That(selectedUpdates, Is.EqualTo(1));
                Assert.That(view.AutomaticRotation, Is.True);
                Assert.That(view.AutomaticRotationSpeed, Is.EqualTo(3.5f).Within(0.0001f));
                Assert.That(circleX.gameObject.activeSelf, Is.True);
                Assert.That(circleY.gameObject.activeSelf, Is.True);
                Assert.That(circleZ.gameObject.activeSelf, Is.True);
            }
            finally
            {
                Module3DMain.OnSelectView.RemoveListener(OnSelect);
            }
        }

        [UnityTest]
        [Category("PlayMode.Module3DScene")]
        public IEnumerator Camera3D_ZoomAndStrafeRespectConfiguredTargetAndDistanceLimits()
        {
            using PlayModeSceneScope scene = new("Module3DSceneModule3DCameraMovement");
            GameObject cameraObject = new("Synthetic Camera3D");
            cameraObject.SetActive(false);
            SceneManager.MoveGameObjectToScene(cameraObject, scene.Scene);

            Camera3D camera3D = cameraObject.AddComponent<Camera3D>();
            Camera camera = cameraObject.AddComponent<Camera>();
            SetPrivateField(camera3D, "m_Camera", camera);
            SetPrivateField(camera3D, "m_MinDistance", 50.0f);
            SetPrivateField(camera3D, "m_MaxDistance", 150.0f);
            SetPrivateField(camera3D, "m_ZoomSpeed", 1.0f);
            SetPrivateField(camera3D, "m_Speed", 1.0f);
            cameraObject.transform.SetPositionAndRotation(new Vector3(0, 0, -100), Quaternion.identity);
            camera3D.Target = Vector3.zero;

            camera3D.Zoom(1000);
            float minDistance = Vector3.Distance(cameraObject.transform.position, camera3D.Target);

            camera3D.Zoom(-1000);
            float maxDistance = Vector3.Distance(cameraObject.transform.position, camera3D.Target);

            camera3D.HorizontalStrafe(10);
            camera3D.VerticalStrafe(5);

            yield return null;

            Assert.That(minDistance, Is.EqualTo(50).Within(0.0001f));
            Assert.That(maxDistance, Is.EqualTo(150).Within(0.0001f));
            Assert.That(camera3D.LocalTarget, Is.EqualTo(new Vector3(-2, -5, 0)));
            Assert.That(cameraObject.transform.position, Is.EqualTo(new Vector3(-2, -5, -150)));
        }

        [UnityTest]
        [Category("PlayMode.Module3DScene")]
        public IEnumerator View3D_SetViewportAndTargetTextureUpdateTheBackingCamera()
        {
            using PlayModeSceneScope scene = new("Module3DSceneModule3DViewViewport");
            View3D view = CreateControlledView(scene, "Viewport View", out _, out Camera camera);
            RenderTexture firstTexture = new(64, 32, 24);
            RenderTexture secondTexture = new(32, 64, 24);

            view.SetViewport(10, 20, 100, 200);
            view.TargetTexture = firstTexture;
            view.TargetTexture = secondTexture;

            yield return null;

            Assert.That(camera.rect.x, Is.EqualTo(10.0f / Screen.width).Within(0.0001f));
            Assert.That(camera.rect.y, Is.EqualTo(20.0f / Screen.height).Within(0.0001f));
            Assert.That(camera.rect.width, Is.EqualTo(100.0f / Screen.width).Within(0.0001f));
            Assert.That(camera.rect.height, Is.EqualTo(200.0f / Screen.height).Within(0.0001f));
            Assert.That(view.TargetTexture, Is.SameAs(secondTexture));

            firstTexture.Release();
            secondTexture.Release();
        }

        [UnityTest]
        [Category("PlayMode.Module3DScene")]
        public IEnumerator View3D_DefaultStandardViewsAndCameraTypeUseControlledCameraState()
        {
            using PlayModeSceneScope scene = new("Module3DSceneModule3DStandardViews");
            View3D line0 = CreateControlledView(scene, "Default View 0", out _, out _);
            View3D line1 = CreateControlledView(scene, "Default View 1", out _, out _);
            View3D line2 = CreateControlledView(scene, "Default View 2", out _, out _);
            line0.LineID = 0;
            line1.LineID = 1;
            line2.LineID = 2;

            line0.Default();
            line1.Default();
            line2.Default();
            line0.CameraType = CameraControl.Orbital;

            yield return null;

            Assert.That(Vector3.Distance(line0.LocalCameraPosition, line0.LocalCameraTarget), Is.EqualTo(100).Within(0.0001f));
            Assert.That(Vector3.Distance(line1.LocalCameraPosition, line1.LocalCameraTarget), Is.EqualTo(100).Within(0.0001f));
            Assert.That(Vector3.Distance(line2.LocalCameraPosition, line2.LocalCameraTarget), Is.EqualTo(100).Within(0.0001f));
            Assert.That(Mathf.Abs(Quaternion.Dot(line0.LocalCameraRotation, line1.LocalCameraRotation)), Is.LessThan(0.999f));
            Assert.That(Mathf.Abs(Quaternion.Dot(line1.LocalCameraRotation, line2.LocalCameraRotation)), Is.LessThan(0.999f));
            Assert.That(line0.CameraType, Is.EqualTo(CameraControl.Orbital));
        }

        [UnityTest]
        [Category("PlayMode.Module3DScene")]
        public IEnumerator Base3DScene_CameraTypeAndAutomaticRotationPropagateToControlledViews()
        {
            using PlayModeSceneScope scene = new("Module3DSceneModule3DCameraPropagation");
            Base3DScene baseScene = CreateBaseScene(scene, "Camera Propagation Scene");
            Column3DStatic firstColumn = CreateColumn<Column3DStatic>(scene, "First Propagation Column");
            Column3DStatic secondColumn = CreateColumn<Column3DStatic>(scene, "Second Propagation Column");
            View3D firstView = CreateControlledView(scene, "First Propagation View", out _, out _);
            View3D secondView = CreateControlledView(scene, "Second Propagation View", out _, out _);
            firstColumn.Views.Add(firstView);
            secondColumn.Views.Add(secondView);
            baseScene.Columns.Add(firstColumn);
            baseScene.Columns.Add(secondColumn);

            baseScene.CameraType = CameraControl.Orbital;
            baseScene.AutomaticRotation = true;
            baseScene.AutomaticRotationSpeed = 12.5f;

            yield return null;

            Assert.That(firstView.CameraType, Is.EqualTo(CameraControl.Orbital));
            Assert.That(secondView.CameraType, Is.EqualTo(CameraControl.Orbital));
            Assert.That(firstView.AutomaticRotation, Is.True);
            Assert.That(secondView.AutomaticRotation, Is.True);
            Assert.That(firstView.AutomaticRotationSpeed, Is.EqualTo(12.5f).Within(0.0001f));
            Assert.That(secondView.AutomaticRotationSpeed, Is.EqualTo(12.5f).Within(0.0001f));
        }

        [UnityTest]
        [Category("PlayMode.Module3DScene")]
        public IEnumerator Base3DScene_GenerateExportDirectoryUsesProjectVisualizationAndConfiguredExportRoot()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("Module3DSceneModule3DExportDirectory");
            Project project = PlayModeProjectHarness.CreateAndLoadCompleteProject();
            string exportRoot = temp.GetPath("exports");
            PersistentDataManager.UserPreferences.General.Project.DefaultExportLocation = exportRoot;
            Base3DScene baseScene = CreateBaseScene(scene, "Export Directory Scene");
            Visualization visualization = project.Visualizations.Single();

            baseScene.Initialize(visualization);
            string directory = baseScene.GenerateExportDirectory();

            yield return null;

            Assert.That(directory, Is.EqualTo(Path.Combine(exportRoot, project.Name, visualization.Name)));
            Assert.That(Directory.Exists(directory), Is.True);
        }

        [UnityTest]
        [Category("PlayMode.Module3DScene")]
        public IEnumerator ROIManager_UpdateROIMasksWithoutSelectedROIMarksSitesOutOfROIAndInvalidatesGenerator()
        {
            using PlayModeSceneScope scene = new("Module3DSceneModule3DROIManager");
            using PlayModeModule3DTestHarness harness = new(scene.Scene);
            GameObject managerObject = new("ROI Manager");
            SceneManager.MoveGameObjectToScene(managerObject, scene.Scene);
            ROIManager roiManager = managerObject.AddComponent<ROIManager>();
            SetPrivateField(roiManager, "m_Scene", harness.Scene);
            foreach (Core.Object3D.Site site in harness.Scene.Columns.SelectMany(column => column.Sites))
            {
                site.State.IsOutOfROI = false;
            }

            roiManager.UpdateROIMasks();

            yield return null;

            Assert.That(harness.Scene.Columns.SelectMany(column => column.Sites).Select(site => site.State.IsOutOfROI), Is.All.True);
            Assert.That(harness.Scene.SceneInformation.GeneratorNeedsUpdate, Is.True);
            Assert.That(harness.Scene.SceneInformation.SitesNeedUpdate, Is.True);
        }

        [UnityTest]
        [Category("PlayMode.Module3DScene")]
        [Category("NativeMigration")]
        public IEnumerator ROI_ContainsUsesStrictSphereUnion()
        {
            using PlayModeSceneScope scene = new("Module3DSceneManagedROIGeometry");
            CreateRuntimeModule3DMain(scene, "Managed ROI Geometry Module");
            ROI roi = CreateRuntimeROI(scene, "Managed ROI Geometry");

            roi.AddSphere(Module3DMain.DEFAULT_MESHES_LAYER, "Sphere A", Vector3.zero, 1.0f);
            roi.AddSphere(Module3DMain.DEFAULT_MESHES_LAYER, "Sphere B", new Vector3(3, 0, 0), 0.5f);

            yield return null;

            Assert.That(roi.Contains(Vector3.zero), Is.True);
            Assert.That(roi.Contains(new Vector3(0.5f, 0, 0)), Is.True);
            Assert.That(roi.Contains(new Vector3(1.0f, 0, 0)), Is.False);
            Assert.That(roi.Contains(new Vector3(2.0f, 0, 0)), Is.False);
            Assert.That(roi.Contains(new Vector3(3.0f, 0, 0)), Is.True);
        }

        [UnityTest]
        [Category("PlayMode.Module3DScene")]
        [Category("NativeMigration")]
        public IEnumerator ROI_UpdateMaskTracksSphereAddResizeMoveAndRemove()
        {
            using PlayModeSceneScope scene = new("Module3DSceneManagedROIMask");
            CreateRuntimeModule3DMain(scene, "Managed ROI Mask Module");
            ROI roi = CreateRuntimeROI(scene, "Managed ROI Mask");
            List<HBP.Core.Object3D.Site> sites = new()
            {
                CreateRuntimeSite(scene, "Center", Vector3.zero),
                CreateRuntimeSite(scene, "ResizeTarget", new Vector3(1.05f, 0, 0)),
                CreateRuntimeSite(scene, "MoveTarget", new Vector3(3.0f, 0, 0))
            };
            bool[] mask = new bool[sites.Count];

            roi.UpdateMask(sites, mask);
            Assert.That(mask, Is.EqualTo(new[] { true, true, true }));

            roi.AddSphere(Module3DMain.DEFAULT_MESHES_LAYER, "Sphere", Vector3.zero, 1.0f);
            roi.UpdateMask(sites, mask);
            Assert.That(mask, Is.EqualTo(new[] { false, true, true }));

            roi.ChangeSelectedSphereSize(1.0f);
            roi.UpdateMask(sites, mask);
            Assert.That(mask, Is.EqualTo(new[] { false, false, true }));

            roi.MoveSelectedSphere(new Vector3(3.0f, 0, 0));
            roi.UpdateMask(sites, mask);
            Assert.That(mask, Is.EqualTo(new[] { true, true, false }));

            roi.RemoveSelectedSphere();
            yield return null;

            roi.UpdateMask(sites, mask);
            Assert.That(mask, Is.EqualTo(new[] { true, true, true }));
        }

        [UnityTest]
        [Category("PlayMode.Module3DScene")]
        [Category("NativeMigration")]
        public IEnumerator ROI_PublicEditingVisibilityRenderingAndEventsUpdateObservableState()
        {
            using PlayModeSceneScope scene = new("Module3DSceneManagedROIPublicOperations");
            CreateRuntimeModule3DMain(scene, "Managed ROI Public Operations Module");
            ROI roi = CreateRuntimeROI(scene, "Managed ROI Public Operations");
            int nameChanges = 0;
            int numberChanges = 0;
            int parameterChanges = 0;
            int selectionChanges = 0;
            roi.OnUpdateROIName.AddListener(() => ++nameChanges);
            roi.OnChangeNumberOfSpheres.AddListener(() => ++numberChanges);
            roi.OnChangeSphereParameters.AddListener(() => ++parameterChanges);
            roi.OnChangeSphereSelectionState.AddListener(() => ++selectionChanges);

            roi.Name = "Language ROI";
            roi.AddSphere(Module3DMain.DEFAULT_MESHES_LAYER, "Sphere A", Vector3.zero, 1.0f);
            HBP.Data.Module3D.Sphere sphere = roi.SelectedSphere;

            Assert.That(roi.Name, Is.EqualTo("Language ROI"));
            Assert.That(nameChanges, Is.EqualTo(1));
            Assert.That(numberChanges, Is.EqualTo(1));
            Assert.That(selectionChanges, Is.EqualTo(2), "AddSphere selects through the public selection path.");
            Assert.That(sphere, Is.Not.Null);
            Assert.That(sphere.Selected, Is.True);

            roi.MoveSelectedSphere(new Vector3(1, 2, 3));
            Assert.That(sphere.Position, Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(parameterChanges, Is.EqualTo(1));

            roi.ChangeSelectedSphereSize(0.2f);
            Assert.That(sphere.InfluenceRadius, Is.EqualTo(1.0f));
            Assert.That(parameterChanges, Is.EqualTo(1), "The public dead zone must not resize the sphere.");

            roi.ChangeSelectedSphereSize(-1.0f);
            Assert.That(sphere.InfluenceRadius, Is.EqualTo(0.9f).Within(0.0001f));
            Assert.That(parameterChanges, Is.EqualTo(3), "Radius synchronization and ROI notification each raise their documented event.");

            roi.SetVisibility(false);
            Assert.That(sphere.gameObject.activeSelf, Is.False);
            roi.SetVisibility(true);
            Assert.That(sphere.gameObject.activeSelf, Is.True);

            int activeLayer = sphere.gameObject.layer;
            int inactiveLayer = LayerMask.NameToLayer("Inactive");
            Assert.That(inactiveLayer, Is.GreaterThanOrEqualTo(0), "The project must define its historical Inactive layer.");
            roi.SetRenderingState(false);
            Assert.That(sphere.gameObject.layer, Is.EqualTo(inactiveLayer));
            roi.SetRenderingState(true);
            Assert.That(sphere.gameObject.layer, Is.EqualTo(activeLayer));

            yield return null;
        }

        [UnityTest]
        [Category("PlayMode.Module3DScene")]
        [Category("NativeMigration")]
        public IEnumerator ROI_PublicSelectionAndRemovalOperationsChooseClosestAndKeepValidSelection()
        {
            using PlayModeSceneScope scene = new("Module3DSceneManagedROISelectionOperations");
            CreateRuntimeModule3DMain(scene, "Managed ROI Selection Operations Module");
            ROI roi = CreateRuntimeROI(scene, "Managed ROI Selection Operations");
            roi.AddSphere(Module3DMain.DEFAULT_MESHES_LAYER, "Near", Vector3.zero, 1.0f);
            roi.AddSphere(Module3DMain.DEFAULT_MESHES_LAYER, "Far", new Vector3(3, 0, 0), 1.0f);
            yield return null;

            roi.SelectSphere(-1);
            Assert.That(roi.SelectedSphereID, Is.EqualTo(-1));
            Assert.That(roi.SelectedSphere, Is.Null);
            Assert.That(roi.Spheres, Has.All.Matches<HBP.Data.Module3D.Sphere>(sphere => !sphere.Selected));

            Physics.SyncTransforms();
            roi.SelectClosestSphere(new Ray(new Vector3(-5, 0, 0), Vector3.right));
            Assert.That(roi.SelectedSphereID, Is.EqualTo(0));
            Assert.That(roi.SelectedSphere, Is.SameAs(roi.Spheres[0]));

            roi.SelectSphere(1);
            Assert.That(roi.SelectedSphereID, Is.EqualTo(1));
            roi.SelectSphere(99);
            Assert.That(roi.SelectedSphereID, Is.EqualTo(1), "An out-of-range public selection is ignored.");

            roi.RemoveSphere(0);
            Assert.That(roi.Spheres, Has.Count.EqualTo(1));
            Assert.That(roi.SelectedSphereID, Is.EqualTo(0));
            Assert.That(roi.SelectedSphere, Is.SameAs(roi.Spheres[0]));

            roi.RemoveSelectedSphere();
            Assert.That(roi.Spheres, Is.Empty);
            Assert.That(roi.SelectedSphereID, Is.EqualTo(-1));

            roi.MoveSelectedSphere(Vector3.one);
            roi.ChangeSelectedSphereSize(1.0f);
            roi.RemoveSelectedSphere();
            Assert.That(roi.Spheres, Is.Empty);

            yield return null;
        }

        [UnityTest]
        [Category("PlayMode.Module3DScene")]
        public IEnumerator ImplantationManager_ComparingSitesTracksSelectedSiteUntilDisabled()
        {
            using PlayModeSceneScope scene = new("Module3DSceneModule3DImplantationManager");
            using PlayModeModule3DTestHarness harness = new(scene.Scene);

            harness.SourceColumn.SelectSite(harness.SourceSiteA);
            harness.ImplantationManager.ComparingSites = true;

            yield return null;

            Assert.That(harness.ImplantationManager.SiteToCompare, Is.SameAs(harness.SourceSiteA));

            harness.ImplantationManager.ComparingSites = false;

            yield return null;

            Assert.That(harness.ImplantationManager.SiteToCompare, Is.Null);
        }

        [Test]
        [Category("PlayMode.Module3DScene")]
        public async Task Base3DScene_InitializeAsync_WithSyntheticMNIAndAnatomyColumnCreatesLoadedSceneGraph()
        {
            using PlayModeTempDirectoryScope temp = new();
            using SyntheticMNIScope mni = new(temp);
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("Module3DSceneModule3DInitializeSyntheticMNI");
            var initialized = await InitializeSyntheticAnatomicSceneAsync(temp, scene);
            Base3DScene baseScene = initialized.BaseScene;

            Assert.That(baseScene.SceneInformation.Initialized, Is.True);
            Assert.That(baseScene.MeshManager.Meshes, Has.Count.EqualTo(3));
            Assert.That(baseScene.MRIManager.MRIs, Has.Count.EqualTo(1));
            Assert.That(baseScene.ImplantationManager.Implantations, Has.Count.EqualTo(1));
            Assert.That(baseScene.Columns, Has.Count.EqualTo(1));
            Assert.That(baseScene.ColumnsAnatomy, Has.Count.EqualTo(1));
            Assert.That(baseScene.ViewLineNumber, Is.EqualTo(1));
            Assert.That(baseScene.SelectedColumn, Is.SameAs(baseScene.Columns[0]));
            Assert.That(baseScene.SelectedColumn.SelectedView, Is.Not.Null);
            Assert.That(baseScene.SelectedColumn.Sites, Has.Count.EqualTo(1));
            Assert.That(baseScene.SelectedColumn.SelectedSite, Is.SameAs(baseScene.SelectedColumn.Sites[0]));
            Assert.That(baseScene.SelectedColumn.BrainMesh, Is.Not.Null);
            Assert.That(initialized.LoadingMessages, Does.Contain("Loading MNI objects"));
            Assert.That(initialized.LoadingMessages, Does.Contain("Loading columns"));
        }

        [Test]
        [Category("PlayMode.Module3DScene")]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        [Category("HbpCoreOnly")]
        public async Task Base3DScene_HbpCoreComputesRuntimeCutTexturesAndSurfaceActivity()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            using PlayModeTempDirectoryScope temp = new();
                using SyntheticMNIScope mni = new(temp);
                using PlayModeApplicationStateScope appState = new(temp.Path);
                using PlayModePersistentDataScope persistentData = new(temp.Path);
                PersistentDataManager.UserPreferences.Visualization._3D.AutomaticEEGUpdate = false;
                using PlayModeSceneScope scene = new("Module3DSceneHbpCoreRuntimeCutAndSurface");
                GameObject moduleObject = new("Controlled Module3DMain For HbpCore");
                moduleObject.SetActive(false);
                SceneManager.MoveGameObjectToScene(moduleObject, scene.Scene);
                Module3DMain module = moduleObject.AddComponent<Module3DMain>();
                SetPrivateField(module, "m_SharedMaterials", CreateSharedMaterials());
                SetPrivateField(module, "m_Scenes", new List<Base3DScene>());
                SetModule3DMainInstance(module);
                var initialized = await InitializeSyntheticAnatomicSceneAsync(temp, scene);
                Base3DScene baseScene = initialized.BaseScene;
                Column3D column = baseScene.Columns.Single();

                Assert.That(baseScene.MRIManager.SelectedMRI.Volume.IsLoaded, Is.True);

                await WaitForConditionAsync(() =>
                    baseScene.MeshManager.BrainSurface != null
                    && baseScene.MeshManager.BrainSurface.NumberOfVertices > 0
                    && column.BrainMesh != null
                    && column.BrainMesh.GetComponent<MeshFilter>().mesh.vertexCount > 0,
                    "hbp_core initial brain geometry update");

                baseScene.AddCutPlane();
                await WaitForConditionAsync(() =>
                    baseScene.Cuts.Count == 1
                    && column.CutTextures.CutGenerators.Count == 1
                    && !baseScene.SceneInformation.CutsNeedUpdate
                    && !baseScene.SceneInformation.BaseCutTexturesNeedUpdate,
                    "hbp_core cut geometry and base cut texture update",
                    () => FormatCutUpdateDiagnostics(baseScene, column));

                Assert.That(column.CutTextures.CutGenerators[0].CutGeometryGenerator, Is.Not.Null);
                Assert.That(column.CutTextures.BaseBrainCutTextures[0].width, Is.GreaterThan(1));
                Assert.That(column.CutTextures.BaseBrainCutTextures[0].height, Is.GreaterThan(1));
                Assert.That(column.CutTextures.BaseBrainCutTextures[0].GetPixels32().Any(pixel => pixel.r != 0 || pixel.g != 0 || pixel.b != 0), Is.True);

                baseScene.UpdateGenerator();
                await WaitForConditionAsync(() => baseScene.IsGeneratorUpToDate, "hbp_core activity generator update", () => FormatGeneratorDiagnostics(baseScene), 600);
                await WaitForConditionAsync(() =>
                    !baseScene.SceneInformation.FunctionalCutTexturesNeedUpdate
                    && !baseScene.SceneInformation.FunctionalSurfaceNeedsUpdate,
                    "hbp_core functional cut and surface update");

                Assert.That(column.ActivityGenerator.GeneratorSurface, Is.Not.Null);
                Assert.That(column.SurfaceGenerator.ActivityGenerator, Is.SameAs(column.ActivityGenerator));
                Assert.That(column.CutTextures.BrainCutTextures[0].width, Is.EqualTo(column.CutTextures.BaseBrainCutTextures[0].width));
                Assert.That(column.CutTextures.BrainCutTextures[0].height, Is.EqualTo(column.CutTextures.BaseBrainCutTextures[0].height));
                Assert.That(column.SurfaceGenerator.ActivityUV, Has.Length.EqualTo(baseScene.MeshManager.BrainSurface.NumberOfVertices));
                Assert.That(column.SurfaceGenerator.AlphaUV, Has.Length.EqualTo(baseScene.MeshManager.BrainSurface.NumberOfVertices));
                Mesh brainMesh = column.BrainMesh.GetComponent<MeshFilter>().mesh;
                Assert.That(brainMesh.uv2, Has.Length.EqualTo(brainMesh.vertexCount));
                Assert.That(brainMesh.uv3, Has.Length.EqualTo(brainMesh.vertexCount));
        }

        [Test]
        [Category("PlayMode.Module3DScene")]
        public async Task Base3DScene_InitializedMultiColumnViewAndCutLifecycleStaySynchronized()
        {
            using PlayModeTempDirectoryScope temp = new();
            using SyntheticMNIScope mni = new(temp);
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("Module3DSceneModule3DInitializedLifecycle");
            var initialized = await InitializeSyntheticAnatomicSceneAsync(temp, scene, 2);
            Base3DScene baseScene = initialized.BaseScene;
            DisplayedObjects displayedObjects = GetPrivateField<DisplayedObjects>(baseScene, "m_DisplayedObjects");
            int addViewLineEvents = 0;
            int removedViewLine = -1;
            int addCutEvents = 0;

            baseScene.OnAddViewLine.AddListener(() => addViewLineEvents++);
            baseScene.OnRemoveViewLine.AddListener(line => removedViewLine = line);
            baseScene.OnAddCut.AddListener(_ => addCutEvents++);

            Assert.That(baseScene.Columns, Has.Count.EqualTo(2));
            Assert.That(baseScene.ViewLineNumber, Is.EqualTo(1));
            Assert.That(baseScene.Columns.Select(column => column.Views.Count), Is.All.EqualTo(1));
            Assert.That(baseScene.Columns.Select(column => column.Sites.Count), Is.All.EqualTo(1));

            baseScene.AddViewLine();
            baseScene.Columns[1].Views[1].IsSelected = true;
            baseScene.RemoveViewLine(1);

            Assert.That(addViewLineEvents, Is.EqualTo(1));
            Assert.That(removedViewLine, Is.EqualTo(1));
            Assert.That(baseScene.ViewLineNumber, Is.EqualTo(1));
            Assert.That(baseScene.Columns.Select(column => column.Views.Count), Is.All.EqualTo(1));
            Assert.That(baseScene.SelectedColumn, Is.SameAs(baseScene.Columns[1]));
            Assert.That(baseScene.SelectedColumn.SelectedView, Is.SameAs(baseScene.Columns[1].Views[0]));

            Core.Object3D.Cut cut = baseScene.AddCutPlane();
            int removeCutEvents = 0;
            cut.OnRemoveCut.AddListener(() => removeCutEvents++);

            Assert.That(addCutEvents, Is.EqualTo(1));
            Assert.That(baseScene.Cuts, Has.Count.EqualTo(1));
            Assert.That(baseScene.Cuts[0].ID, Is.EqualTo(0));
            Assert.That(displayedObjects.BrainCutMeshes, Has.Count.EqualTo(1));
            Assert.That(baseScene.Columns.Select(column => column.BrainCutMeshes.Count), Is.All.EqualTo(1));
            Assert.That(baseScene.SceneInformation.CutsNeedUpdate, Is.True);

            baseScene.RemoveCutPlane(cut);

            Assert.That(removeCutEvents, Is.EqualTo(1));
            Assert.That(baseScene.Cuts, Is.Empty);
            Assert.That(displayedObjects.BrainCutMeshes, Is.Empty);
            Assert.That(baseScene.Columns.Select(column => column.BrainCutMeshes.Count), Is.All.EqualTo(0));
            Assert.That(baseScene.SceneInformation.CutsNeedUpdate, Is.True);
        }

        [Test]
        [Category("PlayMode.Module3DScene")]
        public async Task Base3DScene_TriangleEraserToggleControlsGeneratedInvisibleMesh()
        {
            using PlayModeTempDirectoryScope temp = new();
            using SyntheticMNIScope mni = new(temp);
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("Module3DSceneModule3DCleanGeneratedObjects");
            var initialized = await InitializeSyntheticAnatomicSceneAsync(temp, scene);
            Base3DScene baseScene = initialized.BaseScene;
            DisplayedObjects displayedObjects = GetPrivateField<DisplayedObjects>(baseScene, "m_DisplayedObjects");
            GameObject brain = displayedObjects.Brain;
            GameObject simplifiedBrain = displayedObjects.SimplifiedBrain;
            Core.Object3D.Cut cut = baseScene.AddCutPlane();
            GameObject cutObject = displayedObjects.BrainCutMeshes.Single();

            Assert.That(brain, Is.Not.Null);
            Assert.That(simplifiedBrain, Is.Not.Null);
            displayedObjects.InstantiateInvisibleMesh(false);
            GameObject invisibleBrain = displayedObjects.InvisibleBrain;
            Assert.That(invisibleBrain, Is.Not.Null);
            Assert.That(invisibleBrain.activeSelf, Is.False);

            baseScene.SceneInformation.CollidersNeedUpdate = false;
            baseScene.TriangleEraser.IsEnabled = true;

            Assert.That(invisibleBrain.activeSelf, Is.True);
            Assert.That(baseScene.SceneInformation.CollidersNeedUpdate, Is.True);

            baseScene.TriangleEraser.IsEnabled = false;

            Assert.That(invisibleBrain.activeSelf, Is.False);
            Assert.That(cut, Is.Not.Null);
            Assert.That(cutObject, Is.Not.Null);
        }

        [Test]
        [Category("PlayMode.Module3DScene")]
        public async Task Base3DScene_CleanAsyncDestroysSceneAndGeneratedObjects()
        {
            using PlayModeTempDirectoryScope temp = new();
            using SyntheticMNIScope mni = new(temp);
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("Module3DSceneModule3DCleanAsync");
            GameObject moduleObject = new("Controlled Module3DMain For CleanAsync");
            moduleObject.SetActive(false);
            SceneManager.MoveGameObjectToScene(moduleObject, scene.Scene);
            Module3DMain module = moduleObject.AddComponent<Module3DMain>();
            SetPrivateField(module, "m_SharedMaterials", CreateSharedMaterials());
            SetPrivateField(module, "m_Scenes", new List<Base3DScene>());
            SetModule3DMainInstance(module);
            var initialized = await InitializeSyntheticAnatomicSceneAsync(temp, scene);
            Base3DScene baseScene = initialized.BaseScene;
            DisplayedObjects displayedObjects = GetPrivateField<DisplayedObjects>(baseScene, "m_DisplayedObjects");
            GameObject sceneObject = baseScene.gameObject;
            GameObject columnObject = baseScene.Columns[0].gameObject;
            GameObject brain = displayedObjects.Brain;
            GameObject simplifiedBrain = displayedObjects.SimplifiedBrain;
            baseScene.AddCutPlane();
            GameObject cutObject = displayedObjects.BrainCutMeshes.Single();
            displayedObjects.InstantiateInvisibleMesh(false);
            GameObject invisibleBrain = displayedObjects.InvisibleBrain;

            Assert.That(sceneObject, Is.Not.Null);
            Assert.That(columnObject, Is.Not.Null);
            Assert.That(brain, Is.Not.Null);
            Assert.That(simplifiedBrain, Is.Not.Null);
            Assert.That(cutObject, Is.Not.Null);
            Assert.That(invisibleBrain, Is.Not.Null);

            await baseScene.CleanAsync();
            await UniTask.Yield();

            Assert.That(sceneObject == null, Is.True);
            Assert.That(columnObject == null, Is.True);
            Assert.That(brain == null, Is.True);
            Assert.That(simplifiedBrain == null, Is.True);
            Assert.That(cutObject == null, Is.True);
            Assert.That(invisibleBrain == null, Is.True);
        }

        [Test]
        [Category("PlayMode.Module3DScene")]
        public async Task Base3DScene_LoadAndSaveConfigurationRoundTripsInitializedSceneState()
        {
            using PlayModeTempDirectoryScope temp = new();
            using SyntheticMNIScope mni = new(temp);
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("Module3DSceneModule3DConfigurationRoundTrip");
            var initialized = await InitializeSyntheticAnatomicSceneAsync(temp, scene);
            Base3DScene baseScene = initialized.BaseScene;
            Visualization visualization = initialized.Visualization;
            Vector3 firstViewPosition = new(12, 34, 56);
            Quaternion firstViewRotation = Quaternion.Euler(10, 20, 30);
            Vector3 firstViewTarget = new(1, 2, 3);
            Vector3 secondViewPosition = new(-12, 24, 72);
            Quaternion secondViewRotation = Quaternion.Euler(40, 50, 60);
            Vector3 secondViewTarget = new(4, 5, 6);

            visualization.Configuration = new VisualizationConfiguration(
                ColorType.Grayscale,
                ColorType.Hot,
                ColorType.Winter,
                MeshPart.Right,
                "MNI White matter",
                "MNI",
                string.Empty,
                true,
                true,
                0.42f,
                true,
                true,
                true,
                true,
                1.75f,
                0.2f,
                0.8f,
                CameraControl.Orbital,
                new[] { new HBP.Core.Data.Cut(new Vector3(0, 1, 0), CutOrientation.Coronal, true, 0.25f) },
                new[]
                {
                    new View(firstViewPosition, firstViewRotation, firstViewTarget),
                    new View(secondViewPosition, secondViewRotation, secondViewTarget)
                },
                Enumerable.Empty<RegionOfInterest>());

            baseScene.LoadConfiguration(false);

            Assert.That(baseScene.CutColor, Is.EqualTo(ColorType.Hot));
            Assert.That(baseScene.Colormap, Is.EqualTo(ColorType.Winter));
            Assert.That(baseScene.MeshManager.MeshPartToDisplay, Is.EqualTo(MeshPart.Right));
            Assert.That(baseScene.EdgeMode, Is.True);
            Assert.That(baseScene.IsBrainTransparent, Is.True);
            Assert.That(baseScene.StrongCuts, Is.True);
            Assert.That(baseScene.HideBlacklistedSites, Is.True);
            Assert.That(baseScene.ShowAllSites, Is.True);
            Assert.That(baseScene.AutomaticCutAroundSelectedSite, Is.True);
            Assert.That(baseScene.SiteGain, Is.EqualTo(1.75f));
            Assert.That(baseScene.MRIManager.MRICalMinFactor, Is.EqualTo(0.2f));
            Assert.That(baseScene.MRIManager.MRICalMaxFactor, Is.EqualTo(0.8f));
            Assert.That(baseScene.CameraType, Is.EqualTo(CameraControl.Orbital));
            Assert.That(baseScene.Cuts, Has.Count.EqualTo(1));
            Assert.That(baseScene.Cuts[0].Orientation, Is.EqualTo(CutOrientation.Coronal));
            Assert.That(baseScene.Cuts[0].Flip, Is.True);
            Assert.That(baseScene.Cuts[0].Position, Is.EqualTo(0.25f));
            Assert.That(baseScene.ViewLineNumber, Is.EqualTo(2));
            AssertVectorApproximately(baseScene.Columns[0].Views[0].LocalCameraPosition, firstViewPosition);
            AssertVectorApproximately(baseScene.Columns[0].Views[0].LocalCameraRotation.eulerAngles, firstViewRotation.eulerAngles);
            AssertVectorApproximately(baseScene.Columns[0].Views[0].LocalCameraTarget, firstViewTarget);
            AssertVectorApproximately(baseScene.Columns[0].Views[1].LocalCameraPosition, secondViewPosition);
            AssertVectorApproximately(baseScene.Columns[0].Views[1].LocalCameraRotation.eulerAngles, secondViewRotation.eulerAngles);
            AssertVectorApproximately(baseScene.Columns[0].Views[1].LocalCameraTarget, secondViewTarget);

            string selectedImplantationName = baseScene.ImplantationManager.SelectedImplantation.Name;
            baseScene.SaveConfiguration();

            Assert.That(visualization.Configuration.MeshName, Is.EqualTo("MNI White matter"));
            Assert.That(visualization.Configuration.MRIName, Is.EqualTo("MNI"));
            Assert.That(visualization.Configuration.ImplantationName, Is.EqualTo(selectedImplantationName));
            Assert.That(visualization.Configuration.Cuts, Has.Count.EqualTo(1));
            Assert.That(visualization.Configuration.Views, Has.Count.EqualTo(2));
            AssertVectorApproximately(visualization.Configuration.Views[1].Position.ToVector3(), secondViewPosition);
        }

        [Test]
        [Category("PlayMode.Module3DScene")]
        public async Task Base3DScene_InitializeAsync_WhenMNIIsUnavailableThrowsControlledException()
        {
            if (Object3DManager.MNI.IsLoaded)
            {
                Assert.Ignore("MNI objects are already loaded in this editor session; the positive InitializeAsync fixture is tracked separately.");
            }

            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("Module3DSceneModule3DInitializeMNIUnavailable");
            Project project = PlayModeProjectHarness.CreateAndLoadCompleteProject();
            Base3DScene baseScene = CreateBaseScene(scene, "Initialize MNI Unavailable Scene");
            Visualization visualization = project.Visualizations.Single();
            baseScene.Initialize(visualization);

            System.Exception exception = await AsyncPlayModeTestUtilities.CaptureExceptionAsync(async () =>
            {
                await baseScene.InitializeAsync(visualization, NoProgress, CancellationToken.None);
            });

            Assert.That(exception, Is.TypeOf<CanNotLoadMNI>());
            Assert.That(scene.Scene.isLoaded, Is.True);
        }

        private static T CreateColumn<T>(PlayModeSceneScope scene, string name) where T : Column3D
        {
            GameObject columnObject = new(name);
            columnObject.SetActive(false);
            SceneManager.MoveGameObjectToScene(columnObject, scene.Scene);
            return columnObject.AddComponent<T>();
        }

        private static Base3DScene CreateBaseScene(PlayModeSceneScope scene, string name)
        {
            GameObject sceneObject = new(name);
            sceneObject.SetActive(false);
            SceneManager.MoveGameObjectToScene(sceneObject, scene.Scene);
            return sceneObject.AddComponent<Base3DScene>();
        }

        private static LineRenderer CreateLineRenderer(PlayModeSceneScope scene, string name)
        {
            GameObject lineObject = new(name);
            lineObject.SetActive(false);
            SceneManager.MoveGameObjectToScene(lineObject, scene.Scene);
            return lineObject.AddComponent<LineRenderer>();
        }

        private static View3D CreateView(PlayModeSceneScope scene, string name)
        {
            GameObject viewObject = new(name);
            viewObject.SetActive(false);
            SceneManager.MoveGameObjectToScene(viewObject, scene.Scene);
            return viewObject.AddComponent<View3D>();
        }

        private static View3D CreateControlledView(PlayModeSceneScope scene, string name, out Camera3D camera3D, out Camera camera)
        {
            GameObject viewObject = new(name);
            viewObject.SetActive(false);
            SceneManager.MoveGameObjectToScene(viewObject, scene.Scene);
            GameObject cameraObject = new(name + " Camera");
            cameraObject.transform.SetParent(viewObject.transform, false);

            View3D view = viewObject.AddComponent<View3D>();
            camera3D = cameraObject.AddComponent<Camera3D>();
            camera = cameraObject.AddComponent<Camera>();
            SetPrivateField(view, "m_Camera3D", camera3D);
            SetPrivateField(camera3D, "m_Camera", camera);
            SetPrivateField(camera3D, "m_AssociatedView", view);
            SetPrivateField(camera3D, "m_MinDistance", 10.0f);
            SetPrivateField(camera3D, "m_MaxDistance", 200.0f);
            SetPrivateField(camera3D, "m_StartDistance", 100.0f);
            SetPrivateField(camera3D, "m_OriginalTarget", Vector3.zero);
            SetPrivateField(camera3D, "m_OriginalRotationEuler", new Vector3(0, 100, 90));
            return view;
        }

        private static Base3DScene CreateRuntimeBase3DScene(PlayModeSceneScope scene)
        {
            GameObject sceneObject = new("Runtime Scene 3D");
            sceneObject.SetActive(false);
            SceneManager.MoveGameObjectToScene(sceneObject, scene.Scene);

            Base3DScene baseScene = sceneObject.AddComponent<Base3DScene>();
            MeshManager meshManager = CreateManager<MeshManager>(sceneObject, "MeshManager", baseScene);
            MRIManager mriManager = CreateManager<MRIManager>(sceneObject, "MRIManager", baseScene);
            ImplantationManager implantationManager = CreateManager<ImplantationManager>(sceneObject, "ImplantationManager", baseScene);
            TriangleEraser triangleEraser = CreateManager<TriangleEraser>(sceneObject, "TriangleEraser", baseScene);
            AtlasManager atlasManager = CreateManager<AtlasManager>(sceneObject, "AtlasManager", baseScene);
            FMRIManager fmriManager = CreateManager<FMRIManager>(sceneObject, "FMRIManager", baseScene);
            ROIManager roiManager = CreateManager<ROIManager>(sceneObject, "ROIManager", baseScene);
            DisplayedObjects displayedObjects = CreateDisplayedObjects(sceneObject, baseScene);

            foreach (Component manager in new Component[] { meshManager, mriManager, implantationManager, triangleEraser, atlasManager, fmriManager, roiManager })
            {
                SetPrivateField(manager, "m_DisplayedObjects", displayedObjects);
            }

            Transform columnsContainer = new GameObject("Columns").transform;
            columnsContainer.SetParent(sceneObject.transform, false);
            SetPrivateField(baseScene, "m_MeshManager", meshManager);
            SetPrivateField(baseScene, "m_MRIManager", mriManager);
            SetPrivateField(baseScene, "m_ImplantationManager", implantationManager);
            SetPrivateField(baseScene, "m_TriangleEraser", triangleEraser);
            SetPrivateField(baseScene, "m_AtlasManager", atlasManager);
            SetPrivateField(baseScene, "m_FMRIManager", fmriManager);
            SetPrivateField(baseScene, "m_ROIManager", roiManager);
            SetPrivateField(baseScene, "m_DisplayedObjects", displayedObjects);
            SetPrivateField(baseScene, "m_ColumnsContainer", columnsContainer);
            SetPrivateField(baseScene, "m_Column3DAnatomyPrefab", CreateAnatomyColumnPrefab(sceneObject));

            sceneObject.SetActive(true);
            return baseScene;
        }

        private static async Task<(Project Project, Base3DScene BaseScene, Visualization Visualization, List<string> LoadingMessages)> InitializeSyntheticAnatomicSceneAsync(
            PlayModeTempDirectoryScope temp,
            PlayModeSceneScope scene,
            int anatomyColumnCount = 1)
        {
            Project project = CreateMinimalAnatomicProject(anatomyColumnCount);
            Base3DScene baseScene = CreateRuntimeBase3DScene(scene);
            Visualization visualization = project.Visualizations.Single();
            List<string> loadingMessages = new();

            baseScene.Initialize(visualization);
            await baseScene.InitializeAsync(
                visualization,
                (progress, duration, text) => loadingMessages.Add(text.ToString()),
                CancellationToken.None);
            baseScene.FinalizeInitialization();
            WireRuntimeCameraGraph(baseScene);
            EnsureRuntimeSiteConfigurations(baseScene);
            EnsureRuntimeCutColorSchemes(baseScene);
            baseScene.SceneInformation.GeometryNeedsUpdate = true;

            return (project, baseScene, visualization, loadingMessages);
        }

        private static void EnsureRuntimeCutColorSchemes(Base3DScene baseScene)
        {
            foreach (Column3D column in baseScene.Columns)
            {
                column.CutTextures.ResetColorSchemes(baseScene.Colormap, baseScene.CutColor);
            }
        }

        private static void EnsureRuntimeSiteConfigurations(Base3DScene baseScene)
        {
            foreach (Column3D column in baseScene.Columns)
            {
                foreach (HBP.Core.Object3D.Site site in column.Sites)
                {
                    site.State ??= new HBP.Core.Object3D.SiteState();
                    site.Configuration ??= new SiteConfiguration();
                    site.Configuration.Labels ??= Array.Empty<string>();
                }
            }
        }

        private static void WireRuntimeCameraGraph(Base3DScene baseScene)
        {
            foreach (Column3D column in baseScene.Columns)
            {
                foreach (View3D view in column.Views)
                {
                    Camera3D camera3D = GetPrivateField<Camera3D>(view, "m_Camera3D");
                    SetPrivateField(camera3D, "m_AssociatedScene", baseScene);
                    SetPrivateField(camera3D, "m_AssociatedColumn", column);
                    SetPrivateField(camera3D, "m_AssociatedView", view);
                    SetPrivateField(camera3D, "m_OriginalTarget", baseScene.MeshManager.SelectedMesh.Both.Center);
                    SetPrivateField(camera3D, "m_OriginalRotationEuler", camera3D.transform.localEulerAngles);
                }
            }
        }

        private static T CreateManager<T>(GameObject parent, string name, Base3DScene baseScene) where T : Component
        {
            GameObject managerObject = new(name);
            managerObject.transform.SetParent(parent.transform, false);
            T manager = managerObject.AddComponent<T>();
            SetPrivateField(manager, "m_Scene", baseScene);
            return manager;
        }

        private static DisplayedObjects CreateDisplayedObjects(GameObject parent, Base3DScene baseScene)
        {
            GameObject displayedObject = new("DisplayedObjects");
            displayedObject.transform.SetParent(parent.transform, false);
            DisplayedObjects displayedObjects = displayedObject.AddComponent<DisplayedObjects>();

            Transform brains = new GameObject("Brains").transform;
            Transform cuts = new GameObject("Cuts").transform;
            Transform sites = new GameObject("Sites").transform;
            Transform rois = new GameObject("ROIs").transform;
            brains.SetParent(displayedObject.transform, false);
            cuts.SetParent(displayedObject.transform, false);
            sites.SetParent(displayedObject.transform, false);
            rois.SetParent(displayedObject.transform, false);

            SetPrivateField(displayedObjects, "m_Scene", baseScene);
            SetPrivateField(displayedObjects, "m_BrainSurfaceMeshesParent", brains);
            SetPrivateField(displayedObjects, "m_BrainCutMeshesParent", cuts);
            SetPrivateField(displayedObjects, "m_SitesMeshesParent", sites);
            SetPrivateField(displayedObjects, "m_ROIParent", rois);
            SetPrivateField(displayedObjects, "m_BrainPrefab", CreateMeshPrefab("Brain Prefab"));
            SetPrivateField(displayedObjects, "m_SimplifiedBrainPrefab", CreateMeshPrefab("Simplified Brain Prefab"));
            SetPrivateField(displayedObjects, "m_InvisibleBrainPrefab", CreateMeshPrefab("Invisible Brain Prefab"));
            SetPrivateField(displayedObjects, "m_CutPrefab", CreateMeshPrefab("Cut Prefab"));
            SetPrivateField(displayedObjects, "m_SitePrefab", CreateSitePrefab());
            SetPrivateField(displayedObjects, "m_ROIPrefab", CreateROIPrefab());
            return displayedObjects;
        }

        private static GameObject CreateAnatomyColumnPrefab(GameObject parent)
        {
            GameObject columnObject = new("Anatomy Column Prefab");
            columnObject.SetActive(false);
            columnObject.transform.SetParent(parent.transform, false);
            Column3DAnatomy column = columnObject.AddComponent<Column3DAnatomy>();
            Transform brains = new GameObject("Brains").transform;
            Transform cuts = new GameObject("Cuts").transform;
            Transform sites = new GameObject("Sites").transform;
            Transform views = new GameObject("Views").transform;
            brains.SetParent(columnObject.transform, false);
            cuts.SetParent(columnObject.transform, false);
            sites.SetParent(columnObject.transform, false);
            views.SetParent(columnObject.transform, false);
            SetPrivateField(column, "m_BrainSurfaceMeshesParent", brains);
            SetPrivateField(column, "m_CutMeshesParent", cuts);
            SetPrivateField(column, "m_SitesMeshesParent", sites);
            SetPrivateField(column, "m_ViewPrefab", CreateRuntimeViewPrefab(columnObject));
            return columnObject;
        }

        private static GameObject CreateRuntimeViewPrefab(GameObject parent)
        {
            GameObject viewObject = new("View Prefab");
            viewObject.SetActive(false);
            viewObject.transform.SetParent(parent.transform, false);
            View3D view = viewObject.AddComponent<View3D>();
            GameObject cameraObject = new("Camera");
            cameraObject.transform.SetParent(viewObject.transform, false);
            Camera3D camera3D = cameraObject.AddComponent<Camera3D>();
            Camera camera = cameraObject.AddComponent<Camera>();
            System.Type postProcessLayerType = FindLoadedType("UnityEngine.Rendering.PostProcessing.PostProcessLayer");
            if (postProcessLayerType != null)
            {
                cameraObject.AddComponent(postProcessLayerType);
            }
            SetPrivateField(view, "m_Camera3D", camera3D);
            SetPrivateField(camera3D, "m_Camera", camera);
            SetPrivateField(camera3D, "m_CircleX", CreateLineRendererObject("Circle X", cameraObject));
            SetPrivateField(camera3D, "m_CircleY", CreateLineRendererObject("Circle Y", cameraObject));
            SetPrivateField(camera3D, "m_CircleZ", CreateLineRendererObject("Circle Z", cameraObject));
            SetPrivateField(camera3D, "m_CutCircle", CreateLineRendererObject("Cut Circle", cameraObject));
            SetPrivateField(camera3D, "m_CutCross1", CreateLineRendererObject("Cut Cross 1", cameraObject));
            SetPrivateField(camera3D, "m_CutCross2", CreateLineRendererObject("Cut Cross 2", cameraObject));
            return viewObject;
        }

        private static LineRenderer CreateLineRendererObject(string name, GameObject parent)
        {
            GameObject lineObject = new(name);
            lineObject.transform.SetParent(parent.transform, false);
            return lineObject.AddComponent<LineRenderer>();
        }

        private static GameObject CreateMeshPrefab(string name)
        {
            GameObject prefab = new(name);
            prefab.SetActive(false);
            prefab.AddComponent<MeshFilter>().sharedMesh = new Mesh();
            prefab.AddComponent<MeshRenderer>();
            prefab.AddComponent<MeshCollider>();
            return prefab;
        }

        private static GameObject CreateSitePrefab()
        {
            GameObject prefab = CreateMeshPrefab("Site Prefab");
            prefab.AddComponent<HBP.Core.Object3D.Site>();
            return prefab;
        }

        private static GameObject CreateROIPrefab()
        {
            GameObject prefab = new("ROI Prefab");
            prefab.SetActive(false);
            ROI roi = prefab.AddComponent<ROI>();
            GameObject spherePrefab = CreateROISpherePrefab();
            spherePrefab.transform.SetParent(prefab.transform, false);
            SetPrivateField(roi, "m_SpherePrefab", spherePrefab);
            return prefab;
        }

        private static GameObject CreateROISpherePrefab()
        {
            GameObject spherePrefab = CreateMeshPrefab("ROI Sphere Prefab");
            spherePrefab.AddComponent<SphereCollider>();
            spherePrefab.AddComponent<HBP.Data.Module3D.Sphere>();
            return spherePrefab;
        }

        private static void CreateRuntimeModule3DMain(PlayModeSceneScope scene, string name)
        {
            GameObject moduleObject = new(name);
            moduleObject.SetActive(false);
            SceneManager.MoveGameObjectToScene(moduleObject, scene.Scene);
            Module3DMain module = moduleObject.AddComponent<Module3DMain>();
            SetPrivateField(module, "m_SharedMaterials", CreateSharedMaterials());
            SetModule3DMainInstance(module);
        }

        private static ROI CreateRuntimeROI(PlayModeSceneScope scene, string name)
        {
            GameObject roiObject = new(name);
            SceneManager.MoveGameObjectToScene(roiObject, scene.Scene);
            ROI roi = roiObject.AddComponent<ROI>();
            SetPrivateField(roi, "m_SpherePrefab", CreateROISpherePrefab());
            return roi;
        }

        private static HBP.Core.Object3D.Site CreateRuntimeSite(PlayModeSceneScope scene, string name, Vector3 position)
        {
            GameObject siteObject = new(name);
            SceneManager.MoveGameObjectToScene(siteObject, scene.Scene);
            HBP.Core.Object3D.Site site = siteObject.AddComponent<HBP.Core.Object3D.Site>();
            Patient patient = new("roi-test-patient", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<HBP.Core.Data.Site>(), Array.Empty<BaseTagValue>(), string.Empty, "roi-test-patient");
            site.Information = new SiteInformation
            {
                Patient = patient,
                Name = name,
                Index = 0,
                DefaultPosition = position
            };
            site.State = new SiteState();
            site.Configuration = new SiteConfiguration();
            return site;
        }

        private static SharedMaterials CreateSharedMaterials()
        {
            SharedMaterials sharedMaterials = ScriptableObject.CreateInstance<SharedMaterials>();
            SetAutoProperty(sharedMaterials.ROI, "Normal", CreateTestMaterial(Color.gray));
            SetAutoProperty(sharedMaterials.ROI, "Selected", CreateTestMaterial(Color.yellow));
            SetAutoProperty(sharedMaterials.Site, "Basic", CreateTestMaterial(Color.white));
            SetAutoProperty(sharedMaterials.Site, "Positive", CreateSiteMaterial(Color.red, Color.magenta));
            SetAutoProperty(sharedMaterials.Site, "Negative", CreateSiteMaterial(Color.blue, Color.cyan));
            SetAutoProperty(sharedMaterials.Site, "Blacklisted", CreateSiteMaterial(Color.black, Color.gray));
            SetAutoProperty(sharedMaterials.Site, "Source", CreateSiteMaterial(Color.green, Color.yellow));
            SetAutoProperty(sharedMaterials.Site, "NotASource", CreateSiteMaterial(Color.white, Color.gray));
            return sharedMaterials;
        }

        private static SiteMaterial CreateSiteMaterial(Color normalColor, Color highlightedColor)
        {
            SiteMaterial siteMaterial = new();
            SetAutoProperty(siteMaterial, "Normal", CreateTestMaterial(normalColor));
            SetAutoProperty(siteMaterial, "Highlighted", CreateTestMaterial(highlightedColor));
            return siteMaterial;
        }

        private static Material CreateTestMaterial(Color color)
        {
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default") ?? Shader.Find("Standard");
            Assert.That(shader, Is.Not.Null, "A built-in shader is required to create runtime test materials.");
            Material material = new(shader);
            material.color = color;
            return material;
        }

        private static Project CreateMinimalAnatomicProject(int anatomyColumnCount = 1)
        {
            HBP.Core.Data.Site site = new(
                "module3d-scene-site-alpha",
                new[] { new Coordinate("MNI", new Vector3(1, 2, 3), "module3d-scene-coordinate-001") },
                Array.Empty<BaseTagValue>(),
                "module3d-scene-site-001");
            Patient patient = new(
                "module3d-scene-patient-alpha",
                Array.Empty<BaseMesh>(),
                Array.Empty<MRI>(),
                new[] { site },
                Array.Empty<BaseTagValue>(),
                string.Empty,
                "module3d-scene-patient-001");
            List<Column> columns = Enumerable.Range(0, anatomyColumnCount)
                .Select(index => (Column)new AnatomicColumn(
                    $"module3d-scene-anatomy-{index}",
                    new BaseConfiguration(),
                    new AnatomicConfiguration($"module3d-scene-anatomy-config-{index}"),
                    $"module3d-scene-column-anatomy-{index}"))
                .ToList();
            Visualization visualization = new(
                "module3d-scene-visualization-alpha",
                new[] { patient },
                columns,
                new VisualizationConfiguration(),
                "module3d-scene-visualization-001");
            Project project = new(
                "module3d-scene-project-alpha",
                new HBP.Core.Data.ProjectPreferences("module3d-scene-test", "module3d-scene-project-preferences-001"),
                new[] { patient },
                Array.Empty<Group>(),
                Array.Empty<Dataset>(),
                new[] { visualization });
            ApplicationState.LoadedProject = project;
            return project;
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = FindField(target.GetType(), fieldName);
            return (T)field.GetValue(target);
        }

        private static System.Type FindLoadedType(string typeName)
        {
            return System.AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(typeName))
                .FirstOrDefault(type => type != null);
        }

        private static void AssertVectorApproximately(Vector3 actual, Vector3 expected, float tolerance = 0.0001f)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(tolerance), $"Expected {expected} but was {actual}");
        }

        private static void SetPrivateField<T>(T target, string fieldName, object value)
        {
            FieldInfo field = FindField(target.GetType(), fieldName);
            field.SetValue(target, value);
        }

        private static void SetAutoProperty<T>(T target, string propertyName, object value)
        {
            FieldInfo field = FindField(target.GetType(), $"<{propertyName}>k__BackingField");
            field.SetValue(target, value);
        }

        private static FieldInfo FindField(System.Type type, string fieldName)
        {
            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null) return field;
                type = type.BaseType;
            }
            Assert.Fail($"Missing field {fieldName}");
            return null;
        }

        private static void NoProgress(float progress, float duration, LoadingText text)
        {
        }

        private static async Task WaitForConditionAsync(Func<bool> condition, string description, Func<string> diagnostics = null, int maxFrames = 120)
        {
            for (int frame = 0; frame < maxFrames; ++frame)
            {
                if (condition())
                {
                    return;
                }
                await UniTask.Delay(10);
            }
            string message = $"Timed out while waiting for {description}.";
            if (diagnostics != null)
            {
                message += $" {diagnostics()}";
            }
            Assert.Fail(message);
        }

        private static string FormatCutUpdateDiagnostics(Base3DScene baseScene, Column3D column)
        {
            return $"Cuts={baseScene.Cuts.Count}, CutGeometryGenerators={baseScene.CutGeometryGenerators.Count}, " +
                $"CutGenerators={column.CutTextures.CutGenerators.Count}, BaseTextures={column.CutTextures.BaseBrainCutTextures.Count}, " +
                $"BrainCutMeshes={column.BrainCutMeshes.Count}, GeometryNeedsUpdate={baseScene.SceneInformation.GeometryNeedsUpdate}, " +
                $"CutsNeedUpdate={baseScene.SceneInformation.CutsNeedUpdate}, BaseCutTexturesNeedUpdate={baseScene.SceneInformation.BaseCutTexturesNeedUpdate}, " +
                $"FunctionalCutTexturesNeedUpdate={baseScene.SceneInformation.FunctionalCutTexturesNeedUpdate}, GUICutTexturesNeedUpdate={baseScene.SceneInformation.GUICutTexturesNeedUpdate}, " +
                $"FunctionalSurfaceNeedsUpdate={baseScene.SceneInformation.FunctionalSurfaceNeedsUpdate}, GeneratorUpdateRequested={baseScene.SceneInformation.GeneratorUpdateRequested}.";
        }

        private static string FormatGeneratorDiagnostics(Base3DScene baseScene)
        {
            bool updatingGenerators = GetPrivateField<bool>(baseScene, "m_UpdatingGenerators");
            return $"IsGeneratorUpToDate={baseScene.IsGeneratorUpToDate}, UpdatingGenerators={updatingGenerators}, GeneratorNeedsUpdate={baseScene.SceneInformation.GeneratorNeedsUpdate}, " +
                $"GeneratorUpdateRequested={baseScene.SceneInformation.GeneratorUpdateRequested}, FunctionalCutTexturesNeedUpdate={baseScene.SceneInformation.FunctionalCutTexturesNeedUpdate}, " +
                $"FunctionalSurfaceNeedsUpdate={baseScene.SceneInformation.FunctionalSurfaceNeedsUpdate}, SitesNeedUpdate={baseScene.SceneInformation.SitesNeedUpdate}.";
        }

        private static string NativeFixturePath(params string[] parts)
        {
            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Assets", "Tests", "Fixtures", "Native");
            foreach (string part in parts)
            {
                path = Path.Combine(path, part);
            }
            return path;
        }

        private static Module3DMain GetModule3DMainInstance()
        {
            FieldInfo field = typeof(Singleton<Module3DMain>).GetField("m_Instance", BindingFlags.NonPublic | BindingFlags.Static);
            return (Module3DMain)field.GetValue(null);
        }

        private static void SetModule3DMainInstance(Module3DMain module)
        {
            FieldInfo field = typeof(Singleton<Module3DMain>).GetField("m_Instance", BindingFlags.NonPublic | BindingFlags.Static);
            field.SetValue(null, module);
        }

        private sealed class SyntheticMNIScope : IDisposable
        {
            private readonly MNIObjects m_PreviousMNI;

            public SyntheticMNIScope(PlayModeTempDirectoryScope temp)
            {
                m_PreviousMNI = Object3DManager.MNI;
                Object3DManager.MNI = CreateSyntheticMNI(temp.GetPath("synthetic-mni.obj"));
            }

            public void Dispose()
            {
                Object3DManager.MNI.Clean();
                Object3DManager.MNI = m_PreviousMNI;
            }

            private static MNIObjects CreateSyntheticMNI(string objPath)
            {
                HBP.Core.DLL.Volume volume = new();
                Vector3 center = Vector3.zero;
                Assert.That(volume.LoadNIFTIFile(NativeFixturePath("Nifti", "fmri_3d.nii")), Is.True);
                using HBP.Core.DLL.BBox bbox = volume.BoundingBox;
                center = bbox.Center;

                const float halfSize = 100.0f;
                File.WriteAllLines(objPath, new[]
                {
                    VertexLine(center + new Vector3(-halfSize, -halfSize, -halfSize)),
                    VertexLine(center + new Vector3(halfSize, -halfSize, -halfSize)),
                    VertexLine(center + new Vector3(halfSize, halfSize, -halfSize)),
                    VertexLine(center + new Vector3(-halfSize, halfSize, -halfSize)),
                    VertexLine(center + new Vector3(-halfSize, -halfSize, halfSize)),
                    VertexLine(center + new Vector3(halfSize, -halfSize, halfSize)),
                    VertexLine(center + new Vector3(halfSize, halfSize, halfSize)),
                    VertexLine(center + new Vector3(-halfSize, halfSize, halfSize)),
                    "f 1 2 3",
                    "f 1 3 4",
                    "f 5 8 7",
                    "f 5 7 6",
                    "f 1 5 6",
                    "f 1 6 2",
                    "f 2 6 7",
                    "f 2 7 3",
                    "f 3 7 8",
                    "f 3 8 4",
                    "f 4 8 5",
                    "f 4 5 1"
                });

                HBP.Core.DLL.Surface left = LoadSurface(objPath);
                HBP.Core.DLL.Surface right = LoadSurface(objPath);
                HBP.Core.DLL.Surface both = (HBP.Core.DLL.Surface)left.Clone();

                MNIObjects mni = new();
                SetAutoProperty(mni, "GreyMatter", new LeftRightMesh3D("MNI Grey matter", left, right, both, MeshType.MNI));
                SetAutoProperty(mni, "WhiteMatter", new LeftRightMesh3D("MNI White matter", (HBP.Core.DLL.Surface)left.Clone(), (HBP.Core.DLL.Surface)right.Clone(), (HBP.Core.DLL.Surface)both.Clone(), MeshType.MNI));
                SetAutoProperty(mni, "InflatedWhiteMatter", new LeftRightMesh3D("MNI Inflated", (HBP.Core.DLL.Surface)left.Clone(), (HBP.Core.DLL.Surface)right.Clone(), (HBP.Core.DLL.Surface)both.Clone(), MeshType.MNI));
                SetAutoProperty(mni, "MRI", new MRI3D("MNI", volume));
                SetAutoProperty(mni, "IsLoaded", true);
                return mni;
            }

            private static string VertexLine(Vector3 point)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "v {0} {1} {2}",
                    point.x,
                    point.y,
                    point.z);
            }

            private static HBP.Core.DLL.Surface LoadSurface(string objPath)
            {
                HBP.Core.DLL.Surface surface = new();
                Assert.That(surface.LoadOBJFile(objPath), Is.True, objPath);
                surface.ComputeNormals();
                return surface;
            }
        }
    }
}
