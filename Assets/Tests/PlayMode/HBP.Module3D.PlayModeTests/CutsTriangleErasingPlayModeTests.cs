using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Exceptions;
using HBP.Core.Object3D;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Rendering;
using HBP.Tests.PlayMode.Utilities;
using HBP.UI.Module3D;
using HBP.UI.Toolbar;
using HBP.UI.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HBP.Tests.PlayMode.Module3D
{
    public class CutsTriangleErasingPlayModeTests
    {
        private GenericEvent<Base3DScene> m_OnSelectScene;
        private GenericEvent<Base3DScene> m_OnDeselectScene;
        private GenericEvent<Base3DScene> m_OnMinimizeScene;
        private GenericEvent<Column3D> m_OnSelectColumn;
        private GenericEvent<View3D> m_OnSelectView;
        private UnityEvent m_OnRequestUpdateInToolbar;
        private Module3DMain m_Module3DMainInstance;
        private GameObject m_ControlledModule3DMainObject;

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

            Module3DMain.OnSelectScene = new GenericEvent<Base3DScene>();
            Module3DMain.OnDeselectScene = new GenericEvent<Base3DScene>();
            Module3DMain.OnMinimizeScene = new GenericEvent<Base3DScene>();
            Module3DMain.OnSelectColumn = new GenericEvent<Column3D>();
            Module3DMain.OnSelectView = new GenericEvent<View3D>();
            Module3DMain.OnRequestUpdateInToolbar = new UnityEvent();

            SetModule3DMainInstance(null);
            m_ControlledModule3DMainObject = new GameObject("Controlled Module3DMain CutsTriangleErasing");
            m_ControlledModule3DMainObject.SetActive(false);
            Module3DMain module = m_ControlledModule3DMainObject.AddComponent<Module3DMain>();
            SetPrivateField(module, "m_Scenes", new List<Base3DScene>());
            SetPrivateField(module, "m_SharedMaterials", CreateSharedMaterials());
            SetModule3DMainInstance(module);
        }

        [TearDown]
        public void TearDown()
        {
            SetModule3DMainInstance(m_Module3DMainInstance);
            Module3DMain.OnSelectScene = m_OnSelectScene;
            Module3DMain.OnDeselectScene = m_OnDeselectScene;
            Module3DMain.OnMinimizeScene = m_OnMinimizeScene;
            Module3DMain.OnSelectColumn = m_OnSelectColumn;
            Module3DMain.OnSelectView = m_OnSelectView;
            Module3DMain.OnRequestUpdateInToolbar = m_OnRequestUpdateInToolbar;

            if (m_ControlledModule3DMainObject != null)
            {
                MNIObjects currentMNI = Object3DManager.MNI;
                HBP.Core.DLL.MarsAtlas currentMarsAtlas = Object3DManager.MarsAtlas;
                HBP.Core.DLL.JuBrainAtlas currentJuBrainAtlas = Object3DManager.JuBrain;
                DiFuMoObjects currentDiFuMo = Object3DManager.DiFuMo;
                IBCObjects currentIBC = Object3DManager.IBC;
                LocalizersObjects currentLocalizers = Object3DManager.Localizers;

                Object3DManager.MNI = new MNIObjects();
                Object3DManager.MarsAtlas = new HBP.Core.DLL.MarsAtlas();
                Object3DManager.JuBrain = new HBP.Core.DLL.JuBrainAtlas();
                Object3DManager.DiFuMo = new DiFuMoObjects();
                Object3DManager.IBC = new IBCObjects();
                Object3DManager.Localizers = new LocalizersObjects();

                Object.DestroyImmediate(m_ControlledModule3DMainObject);

                Object3DManager.MNI = currentMNI;
                Object3DManager.MarsAtlas = currentMarsAtlas;
                Object3DManager.JuBrain = currentJuBrainAtlas;
                Object3DManager.DiFuMo = currentDiFuMo;
                Object3DManager.IBC = currentIBC;
                Object3DManager.Localizers = currentLocalizers;
                m_ControlledModule3DMainObject = null;
            }
        }

        [Test]
        [Category("PlayMode.CutsTriangleErasing")]
        public void Base3DScene_AddUpdateAndRemoveCutPlaneUpdatesSceneState()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("CutsTriangleErasingCutPlaneLifecycle");
            Base3DScene baseScene = CreateIsolatedCutsTriangleErasingScene(scene, temp, includeSurface: false);
            DisplayedObjects displayedObjects = GetPrivateField<DisplayedObjects>(baseScene, "m_DisplayedObjects");
            int modifyEvents = 0;
            int addEvents = 0;
            int removeEvents = 0;
            baseScene.OnModifyPlanesCuts.AddListener(() => modifyEvents++);
            baseScene.OnAddCut.AddListener(_ => addEvents++);

            HBP.Core.Object3D.Cut cut = baseScene.AddCutPlane();
            cut.OnRemoveCut.AddListener(() => removeEvents++);
            baseScene.SceneInformation.CutsNeedUpdate = false;
            cut.Orientation = CutOrientation.Custom;
            cut.Normal = Vector3.zero;
            cut.Position = 0.8f;

            baseScene.UpdateCutPlane(cut, true);

            Assert.That(addEvents, Is.EqualTo(1));
            Assert.That(baseScene.Cuts, Has.Count.EqualTo(1));
            Assert.That(displayedObjects.BrainCutMeshes, Has.Count.EqualTo(1));
            Assert.That(baseScene.CutGeometryGenerators, Has.Count.EqualTo(1));
            Assert.That(cut.ID, Is.EqualTo(0));
            Assert.That(cut.Normal, Is.EqualTo(Vector3.right));
            Assert.That(baseScene.LastPlaneModifiedIndex, Is.EqualTo(0));
            Assert.That(baseScene.SceneInformation.CutsNeedUpdate, Is.True);
            Assert.That(modifyEvents, Is.GreaterThanOrEqualTo(2));

            baseScene.RemoveCutPlane(cut);

            Assert.That(removeEvents, Is.EqualTo(1));
            Assert.That(baseScene.Cuts, Is.Empty);
            Assert.That(displayedObjects.BrainCutMeshes, Is.Empty);
            Assert.That(baseScene.CutGeometryGenerators, Is.Empty);
        }

        [Test]
        [Category("PlayMode.CutsTriangleErasing")]
        public void CutToolbarTools_WriteModeColorAndSiteCutStateBackToScene()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("CutsTriangleErasingCutToolbar");
            Base3DScene baseScene = CreateIsolatedCutsTriangleErasingScene(scene, temp, includeSurface: true);
            Column3D selectedColumn = null;
            View3D selectedView = null;

            Toggle cutModeToggle = CreateToggle("Cut Mode Toggle");
            CutMode cutMode = CreateTool<CutMode>("Cut Mode", tool => SetPrivateField(tool, "m_Toggle", cutModeToggle), baseScene, selectedColumn, selectedView);
            cutMode.Initialize();
            baseScene.SceneInformation.CutsNeedUpdate = false;

            cutModeToggle.SetIsOnWithoutNotify(true);
            cutModeToggle.onValueChanged.Invoke(true);

            Assert.That(baseScene.StrongCuts, Is.True);
            Assert.That(baseScene.SceneInformation.CutsNeedUpdate, Is.True);
            Assert.That(baseScene.BrainMaterials.BrainMaterial.GetInt("_StrongCuts"), Is.EqualTo(1));

            Dropdown cutColorDropdown = CreateDropdown("Cut Color Dropdown", "Default", "Grayscale");
            CutColor cutColor = CreateTool<CutColor>("Cut Color", tool => SetPrivateField(tool, "m_Dropdown", cutColorDropdown), baseScene, selectedColumn, selectedView);
            cutColor.Initialize();
            baseScene.SceneInformation.CutsNeedUpdate = false;

            cutColorDropdown.SetValueWithoutNotify(1);
            AssertNoException("Cut color dropdown value change", () => cutColorDropdown.onValueChanged.Invoke(1));

            Assert.That(baseScene.CutColor, Is.EqualTo(ColorType.Grayscale));
            Assert.That(baseScene.SceneInformation.CutsNeedUpdate, Is.True);

            Toggle cutAroundSiteToggle = CreateToggle("Cut Around Site Toggle");
            CutAroundSite cutAroundSite = CreateTool<CutAroundSite>("Cut Around Site", tool => SetPrivateField(tool, "m_Toggle", cutAroundSiteToggle), baseScene, selectedColumn, selectedView);
            cutAroundSite.Initialize();
            baseScene.SceneInformation.CutsNeedUpdate = false;

            cutAroundSiteToggle.SetIsOnWithoutNotify(true);
            AssertNoException("Cut around site toggle value change", () => cutAroundSiteToggle.onValueChanged.Invoke(true));

            Assert.That(baseScene.AutomaticCutAroundSelectedSite, Is.True);
            Assert.That(baseScene.SceneInformation.CutsNeedUpdate, Is.True);
        }

        [Test]
        [Category("PlayMode.CutsTriangleErasing")]
        public void CutParametersController_WritesPositionOrientationFlipCustomNormalAndRemoveToScene()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("CutsTriangleErasingCutParametersUI");
            Base3DScene baseScene = CreateIsolatedCutsTriangleErasingScene(scene, temp, includeSurface: false);
            HBP.Core.Object3D.Cut cut = baseScene.AddCutPlane();
            CutParameterUiHarness ui = CreateCutParameterUi(scene);
            ui.Controller.Initialize(baseScene, cut);
            ui.Controller.OpenControls();

            ui.Position.SetValueWithoutNotify(0.25f);
            ui.Position.onValueChanged.Invoke(0.25f);

            Assert.That(cut.Position, Is.EqualTo(0.25f));
            Assert.That(baseScene.LastPlaneModifiedIndex, Is.EqualTo(cut.ID));

            ui.Orientation.SetValueWithoutNotify((int)CutOrientation.Coronal);
            ui.Orientation.onValueChanged.Invoke((int)CutOrientation.Coronal);

            Assert.That(cut.Orientation, Is.EqualTo(CutOrientation.Coronal));
            Assert.That(baseScene.SceneInformation.CutsNeedUpdate, Is.True);

            ui.Flip.SetIsOnWithoutNotify(true);
            ui.Flip.onValueChanged.Invoke(true);

            Assert.That(cut.Flip, Is.True);
            Assert.That(cut.Position, Is.EqualTo(0.75f));

            ui.Orientation.SetValueWithoutNotify((int)CutOrientation.Custom);
            ui.CustomX.text = "2";
            ui.CustomY.text = "3";
            ui.CustomZ.text = "4";
            ui.Orientation.onValueChanged.Invoke((int)CutOrientation.Custom);

            Assert.That(cut.Orientation, Is.EqualTo(CutOrientation.Custom));
            Assert.That(cut.Normal, Is.EqualTo(new Vector3(2, 3, 4)));

            ui.CustomX.text = "5";
            ui.CustomX.onEndEdit.Invoke("5");

            Assert.That(cut.Normal, Is.EqualTo(new Vector3(5, 3, 4)));

            ui.Remove.onClick.Invoke();

            Assert.That(baseScene.Cuts, Is.Empty);
        }

        [Test]
        [Category("PlayMode.CutsTriangleErasing")]
        public void TriangleEraser_MaskActionsAffectOnlySelectedSceneAndCanBeResetCanceledAndLoaded()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("CutsTriangleErasingTriangleEraserMasks");
            Base3DScene firstScene = CreateIsolatedCutsTriangleErasingScene(scene, temp, "first", includeSurface: true);
            Base3DScene secondScene = CreateIsolatedCutsTriangleErasingScene(scene, temp, "second", includeSurface: true);
            DisplayedObjects firstDisplayedObjects = GetPrivateField<DisplayedObjects>(firstScene, "m_DisplayedObjects");
            firstDisplayedObjects.InstantiateInvisibleMesh(false);

            int[] firstBrainMask = CreateMask(firstScene.MeshManager.BrainSurface.NumberOfTriangles, 0);
            int[] firstSimplifiedMask = CreateMask(firstScene.MeshManager.SimplifiedMeshToUse.NumberOfTriangles, 0);
            int[] secondBrainBefore = secondScene.MeshManager.BrainSurface.VisibilityMask.ToArray();
            int[] secondSimplifiedBefore = secondScene.MeshManager.SimplifiedMeshToUse.VisibilityMask.ToArray();
            firstScene.SceneInformation.GeneratorNeedsUpdate = false;
            firstScene.SceneInformation.SurfaceProjectionNeedsUpdate = false;
            firstScene.SceneInformation.FunctionalSurfaceNeedsUpdate = false;

            AssertNoException("Apply first scene triangle masks", () => { firstScene.TriangleEraser.CurrentMasks = new List<int[]> { firstBrainMask, firstSimplifiedMask }; });

            Assert.That(firstScene.TriangleEraser.MeshHasInvisibleTriangles, Is.True);
            Assert.That(firstScene.TriangleEraser.CanCancelLastAction, Is.False);
            Assert.That(firstScene.SceneInformation.GeneratorNeedsUpdate, Is.False);
            Assert.That(firstScene.SceneInformation.SurfaceProjectionNeedsUpdate, Is.False);
            Assert.That(firstScene.SceneInformation.FunctionalSurfaceNeedsUpdate, Is.True);
            Assert.That(secondScene.MeshManager.BrainSurface.VisibilityMask, Is.EqualTo(secondBrainBefore));
            Assert.That(secondScene.MeshManager.SimplifiedMeshToUse.VisibilityMask, Is.EqualTo(secondSimplifiedBefore));

            List<int[]> savedMasks = firstScene.TriangleEraser.CurrentMasks.Select(mask => mask.ToArray()).ToList();
            PushTriangleEraserUndoMasks(firstScene, savedMasks);

            AssertNoException("Cancel first scene triangle erasing action", firstScene.TriangleEraser.CancelLastAction);

            Assert.That(firstScene.TriangleEraser.CurrentMasks[0], Is.EqualTo(savedMasks[0]));
            Assert.That(firstScene.TriangleEraser.CurrentMasks[1], Is.EqualTo(savedMasks[1]));
            Assert.That(firstScene.SceneInformation.GeneratorNeedsUpdate, Is.False);
            Assert.That(secondScene.MeshManager.BrainSurface.VisibilityMask, Is.EqualTo(secondBrainBefore));

            AssertNoException("Reset first scene triangle eraser", firstScene.TriangleEraser.ResetEraser);

            Assert.That(firstScene.TriangleEraser.MeshHasInvisibleTriangles, Is.False);
            Assert.That(firstScene.TriangleEraser.CanCancelLastAction, Is.False);
            Assert.That(firstScene.SceneInformation.GeneratorNeedsUpdate, Is.False);
            Assert.That(firstScene.TriangleEraser.CurrentMasks.SelectMany(mask => mask), Is.All.EqualTo(1));

            AssertNoException("Reload first scene triangle masks", () => { firstScene.TriangleEraser.CurrentMasks = savedMasks.Select(mask => mask.ToArray()).ToList(); });

            Assert.That(firstScene.TriangleEraser.CurrentMasks[0], Is.EqualTo(savedMasks[0]));
            Assert.That(firstScene.TriangleEraser.CurrentMasks[1], Is.EqualTo(savedMasks[1]));
            Assert.That(firstScene.TriangleEraser.MeshHasInvisibleTriangles, Is.True);
        }

        [Test]
        [Category("PlayMode.CutsTriangleErasing")]
        public void TriangleErasingToolbarTools_WriteModeDegreesAndCommandsToTriangleEraser()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("CutsTriangleErasingTriangleToolbar");
            Base3DScene baseScene = CreateIsolatedCutsTriangleErasingScene(scene, temp, includeSurface: true);
            (Column3D selectedColumn, View3D selectedView) = CreateDetachedToolSelection(scene);
            DisplayedObjects displayedObjects = GetPrivateField<DisplayedObjects>(baseScene, "m_DisplayedObjects");
            displayedObjects.InstantiateInvisibleMesh(false);
            AssertNoException("Apply toolbar triangle masks", () =>
            {
                baseScene.TriangleEraser.CurrentMasks = new List<int[]>
                {
                    CreateMask(baseScene.MeshManager.BrainSurface.NumberOfTriangles, 0),
                    CreateMask(baseScene.MeshManager.SimplifiedMeshToUse.NumberOfTriangles, 0)
                };
            });

            Dropdown dropdown = CreateDropdown("Triangle Mode Dropdown", "OneTri", "Cylinder", "Zone", "Invert", "Expand");
            InputField degrees = CreateInputField("Degrees Input", "30");
            RectTransform degreesParent = CreateRectTransform("Degrees Parent");
            TriangleErasingMode modeTool = CreateTool<TriangleErasingMode>("Triangle Erasing Mode", tool =>
            {
                SetPrivateField(tool, "m_Dropdown", dropdown);
                SetPrivateField(tool, "m_InputField", degrees);
                SetPrivateField(tool, "m_InputFieldParent", degreesParent);
            }, baseScene, selectedColumn, selectedView);
            modeTool.Initialize();

            dropdown.SetValueWithoutNotify((int)TriEraserMode.Zone);
            AssertNoException("Triangle mode dropdown value change", () => dropdown.onValueChanged.Invoke((int)TriEraserMode.Zone));
            degrees.text = "45";
            AssertNoException("Triangle erasing degrees edit", () => degrees.onEndEdit.Invoke("45"));

            Assert.That(baseScene.TriangleEraser.CurrentMode, Is.EqualTo(TriEraserMode.Zone));
            Assert.That(baseScene.TriangleEraser.Degrees, Is.EqualTo(45));
            Assert.That(degreesParent.gameObject.activeSelf, Is.True);

            Button invertButton = CreateButton("Invert Button");
            InvertErasing invertTool = CreateTool<InvertErasing>("Invert Erasing", tool => SetPrivateField(tool, "m_Button", invertButton), baseScene, selectedColumn, selectedView);
            invertTool.Initialize();
            AssertNoException("Invert erasing toolbar update", invertTool.UpdateTool);

            Assert.That(invertButton.interactable, Is.True);

            Button cancelButton = CreateButton("Cancel Button");
            CancelErasing cancelTool = CreateTool<CancelErasing>("Cancel Erasing", tool => SetPrivateField(tool, "m_Button", cancelButton), baseScene, selectedColumn, selectedView);
            cancelTool.Initialize();
            PushTriangleEraserUndoMasks(baseScene, baseScene.TriangleEraser.CurrentMasks.Select(mask => mask.ToArray()).ToList());
            cancelTool.UpdateTool();
            Assert.That(cancelButton.interactable, Is.True);
            AssertNoException("Cancel erasing toolbar click", cancelButton.onClick.Invoke);

            Assert.That(baseScene.TriangleEraser.CanCancelLastAction, Is.False);

            Button resetButton = CreateButton("Reset Button");
            ResetErasing resetTool = CreateTool<ResetErasing>("Reset Erasing", tool => SetPrivateField(tool, "m_Button", resetButton), baseScene, selectedColumn, selectedView);
            resetTool.Initialize();
            AssertNoException("Reset erasing toolbar click", resetButton.onClick.Invoke);

            Assert.That(baseScene.TriangleEraser.MeshHasInvisibleTriangles, Is.False);

            Button expandButton = CreateButton("Expand Button");
            ExpandErasing expandTool = CreateTool<ExpandErasing>("Expand Erasing", tool => SetPrivateField(tool, "m_Button", expandButton), baseScene, selectedColumn, selectedView);
            expandTool.Initialize();
            AssertNoException("Expand erasing toolbar update", expandTool.UpdateTool);

            Assert.That(expandButton.interactable, Is.False);
        }

        [Test]
        [Category("PlayMode.CutsTriangleErasing")]
        public void Base3DScene_AddCutPlaneWithNativeSurfaceCreatesGeometry()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("CutsTriangleErasingNativeCutPlane");
            Base3DScene baseScene = CreateIsolatedCutsTriangleErasingScene(scene, temp, "native-cut-plane", includeSurface: true);

            HBP.Core.Object3D.Cut cut = null;
            AssertNoException("Add cut plane on native surface", () => cut = baseScene.AddCutPlane());

            Assert.That(cut, Is.Not.Null);
            Assert.That(baseScene.Cuts, Has.Count.EqualTo(1));
            Assert.That(baseScene.CutGeometryGenerators, Has.Count.EqualTo(1));
            Assert.That(baseScene.Cuts[0].Position, Is.InRange(0f, 1f));
        }

        [Test]
        [Category("PlayMode.CutsTriangleErasing")]
        public void TriangleEraser_NativeMaskModesInvertAndExpandSurfaceMasks()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("CutsTriangleErasingNativeTriangleMaskModes");
            Base3DScene baseScene = CreateIsolatedCutsTriangleErasingScene(scene, temp, "native-mask-modes", includeSurface: true);
            DisplayedObjects displayedObjects = GetPrivateField<DisplayedObjects>(baseScene, "m_DisplayedObjects");
            displayedObjects.InstantiateInvisibleMesh(false);

            AssertNoException("Erase native triangle from ray hit", () =>
            {
                baseScene.TriangleEraser.CurrentMode = TriEraserMode.OneTri;
                baseScene.TriangleEraser.EraseTriangles(Vector3.forward, new Vector3(0.1f, 0.1f, 0f));
            });
            Assert.That(baseScene.TriangleEraser.CanCancelLastAction, Is.True);
            Assert.That(baseScene.TriangleEraser.CurrentMasks[0], Has.Length.EqualTo(baseScene.MeshManager.BrainSurface.NumberOfTriangles));
            AssertNoException("Reset native triangle eraser after ray hit", baseScene.TriangleEraser.ResetEraser);

            int[] initialBrainMask = CreateMask(baseScene.MeshManager.BrainSurface.NumberOfTriangles, 1);
            int[] initialSimplifiedMask = CreateMask(baseScene.MeshManager.SimplifiedMeshToUse.NumberOfTriangles, 1);
            initialBrainMask[0] = 0;
            initialSimplifiedMask[0] = 0;

            AssertNoException("Apply initial native triangle masks", () => { baseScene.TriangleEraser.CurrentMasks = new List<int[]> { initialBrainMask, initialSimplifiedMask }; });

            int hiddenBeforeExpand = baseScene.TriangleEraser.CurrentMasks[0].Count(value => value == 0);
            AssertNoException("Expand native triangle mask", () => { baseScene.TriangleEraser.CurrentMode = TriEraserMode.Expand; });
            int hiddenAfterExpand = baseScene.TriangleEraser.CurrentMasks[0].Count(value => value == 0);

            Assert.That(hiddenAfterExpand, Is.GreaterThanOrEqualTo(hiddenBeforeExpand));

            int[] beforeInvert = baseScene.TriangleEraser.CurrentMasks[0].ToArray();
            AssertNoException("Invert native triangle mask", () => { baseScene.TriangleEraser.CurrentMode = TriEraserMode.Invert; });
            int[] afterInvert = baseScene.TriangleEraser.CurrentMasks[0];

            Assert.That(afterInvert, Is.EqualTo(beforeInvert.Select(value => value == 0 ? 1 : 0).ToArray()));
            Assert.That(baseScene.TriangleEraser.CanCancelLastAction, Is.True);
        }

        [Test]
        [Category("PlayMode.CutsTriangleErasing")]
        public void Base3DScene_CutPlaneLifecycleWithoutNativeSurfaceUsesFallbackState()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("CutsTriangleErasingNativeCutGeneratorUnavailable");
            Base3DScene baseScene = CreateIsolatedCutsTriangleErasingScene(scene, temp, "unavailable", includeSurface: false);

            HBP.Core.Object3D.Cut cut = baseScene.AddCutPlane();
            cut.Orientation = CutOrientation.Custom;
            cut.Normal = Vector3.zero;
            cut.Position = 0.4f;

            AssertNoException("Update cut without native brain surface", () => baseScene.UpdateCutPlane(cut, true));

            Assert.That(baseScene.MeshManager.BrainSurface, Is.Null);
            Assert.That(cut.Normal, Is.EqualTo(Vector3.right));
            Assert.That(cut.Point.x, Is.Not.NaN);
            Assert.That(baseScene.SceneInformation.CutsNeedUpdate, Is.True);
        }

        private static async Task<(Project Project, Base3DScene BaseScene, Visualization Visualization)> InitializeSyntheticAnatomicSceneAsync(PlayModeSceneScope scene, string suffix = "alpha", int anatomyColumnCount = 1)
        {
            Project project = CreateMinimalAnatomicProject(suffix, anatomyColumnCount);
            Base3DScene baseScene = CreateRuntimeBase3DScene(scene);
            Visualization visualization = project.Visualizations.Single();

            baseScene.Initialize(visualization);
            await baseScene.InitializeAsync(visualization, NoProgress, CancellationToken.None);
            baseScene.FinalizeInitialization();
            WireRuntimeMeshSelection(baseScene);
            WireRuntimeCameraGraph(baseScene);
            EnsureRuntimeSiteConfigurations(baseScene);
            baseScene.enabled = false;

            return (project, baseScene, visualization);
        }

        private static Base3DScene CreateIsolatedCutsTriangleErasingScene(PlayModeSceneScope scene, PlayModeTempDirectoryScope temp, string suffix = "isolated", bool includeSurface = true, bool includeColumn = false)
        {
            Base3DScene baseScene = CreateRuntimeBase3DScene(scene);
            SetAutoProperty(baseScene, "BrainMaterials", new BrainMaterials());

            DisplayedObjects displayedObjects = GetPrivateField<DisplayedObjects>(baseScene, "m_DisplayedObjects");
            displayedObjects.InstantiateBrain();
            displayedObjects.InstantiateSimplifiedBrain();

            HBP.Core.DLL.Volume volume = new();
            SetAutoProperty(volume, "IsLoaded", true);
            baseScene.MRIManager.MRIs.Add(new MRI3D($"cuts-triangle-erasing-mri-{suffix}", volume));

            if (includeSurface)
            {
                HBP.Core.DLL.Surface surface = CreateTetraSurface(temp.GetPath($"cuts-triangle-erasing-surface-{suffix}.obj"));
                SetAutoProperty(baseScene.MeshManager, "BrainSurface", surface);
                SetAutoProperty(baseScene.MeshManager, "SimplifiedMeshToUse", (HBP.Core.DLL.Surface)surface.Clone());
            }

            if (includeColumn)
            {
                CreateSelectedColumn(scene, baseScene, suffix, displayedObjects.Brain);
            }

            baseScene.SceneInformation.Initialized = true;
            baseScene.SceneInformation.CompletelyLoaded = true;
            baseScene.enabled = false;
            return baseScene;
        }

        private static void CreateSelectedColumn(PlayModeSceneScope scene, Base3DScene baseScene, string suffix, GameObject brainMesh)
        {
            GameObject columnObject = new($"CutsTriangleErasing Column {suffix}");
            SceneManager.MoveGameObjectToScene(columnObject, scene.Scene);
            columnObject.transform.SetParent(baseScene.transform, false);
            Column3DAnatomy column = columnObject.AddComponent<Column3DAnatomy>();

            Transform brains = new GameObject("Brains").transform;
            Transform cuts = new GameObject("Cuts").transform;
            Transform sites = new GameObject("Sites").transform;
            Transform views = new GameObject("Views").transform;
            brains.SetParent(columnObject.transform, false);
            cuts.SetParent(columnObject.transform, false);
            sites.SetParent(columnObject.transform, false);
            views.SetParent(columnObject.transform, false);

            SetAutoProperty(column, "ColumnData", new AnatomicColumn($"cuts-triangle-erasing-column-{suffix}", new BaseConfiguration(), new AnatomicConfiguration($"cuts-triangle-erasing-column-config-{suffix}"), $"cuts-triangle-erasing-column-id-{suffix}"));
            SetAutoProperty(column, "Layer", "Default");
            SetPrivateField(column, "m_BrainSurfaceMeshesParent", brains);
            SetPrivateField(column, "m_CutMeshesParent", cuts);
            SetPrivateField(column, "m_SitesMeshesParent", sites);

            GameObject columnBrain = Object.Instantiate(brainMesh, brains);
            columnBrain.name = $"CutsTriangleErasing Column Brain {suffix}";
            columnBrain.SetActive(true);
            SetAutoProperty(column, "BrainMesh", columnBrain);

            View3D view = CreateSelectedRuntimeView(views);
            SetAutoProperty(column, "Views", new List<View3D> { view });
            view.IsSelected = true;

            HBP.Core.Object3D.Site site = CreateSelectedRuntimeSite(sites, suffix);
            SetAutoProperty(column, "Sites", new List<HBP.Core.Object3D.Site> { site });
            SetAutoProperty(column, "SelectedSite", site);
            column.IsSelected = true;
            baseScene.Columns.Add(column);
        }

        private static View3D CreateSelectedRuntimeView(Transform parent)
        {
            GameObject viewObject = new("CutsTriangleErasing View");
            viewObject.transform.SetParent(parent, false);
            View3D view = viewObject.AddComponent<View3D>();
            GameObject cameraObject = new("Camera");
            cameraObject.transform.SetParent(viewObject.transform, false);
            Camera3D camera3D = cameraObject.AddComponent<Camera3D>();
            Camera camera = cameraObject.AddComponent<Camera>();
            SetPrivateField(view, "m_Camera3D", camera3D);
            SetPrivateField(camera3D, "m_Camera", camera);
            return view;
        }

        private static (Column3D Column, View3D View) CreateDetachedToolSelection(PlayModeSceneScope scene)
        {
            GameObject columnObject = new("CutsTriangleErasing Detached Tool Column");
            SceneManager.MoveGameObjectToScene(columnObject, scene.Scene);
            Column3DAnatomy column = columnObject.AddComponent<Column3DAnatomy>();
            GameObject viewObject = new("CutsTriangleErasing Detached Tool View");
            SceneManager.MoveGameObjectToScene(viewObject, scene.Scene);
            View3D view = viewObject.AddComponent<View3D>();
            return (column, view);
        }

        private static HBP.Core.Object3D.Site CreateSelectedRuntimeSite(Transform parent, string suffix)
        {
            GameObject siteObject = new($"CutsTriangleErasing Site {suffix}");
            siteObject.transform.SetParent(parent, false);
            siteObject.transform.localPosition = new Vector3(0.2f, 0.3f, 0.4f);
            HBP.Core.Object3D.Site site = siteObject.AddComponent<HBP.Core.Object3D.Site>();
            Patient patient = new($"cuts-triangle-erasing-patient-{suffix}", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<HBP.Core.Data.Site>(), Array.Empty<BaseTagValue>(), string.Empty, $"cuts-triangle-erasing-patient-id-{suffix}");
            site.Information = new SiteInformation
            {
                Patient = patient,
                Name = $"cuts-triangle-erasing-site-{suffix}",
                Index = 0,
                DefaultPosition = siteObject.transform.localPosition
            };
            site.State = new SiteState();
            site.Configuration = new SiteConfiguration();
            site.IsSelected = true;
            return site;
        }

        private static HBP.Core.DLL.Surface CreateTetraSurface(string objPath)
        {
            File.WriteAllLines(objPath, new[]
            {
                "v 0 0 0",
                "v 1 0 0",
                "v 0 1 0",
                "v 0 0 1",
                "f 1 2 3",
                "f 1 4 2",
                "f 2 4 3",
                "f 3 4 1"
            });

            HBP.Core.DLL.Surface surface = new();
            Assert.That(surface.LoadOBJFile(objPath), Is.True, objPath);
            surface.ComputeNormals();
            return surface;
        }

        private static void PushTriangleEraserUndoMasks(Base3DScene baseScene, List<int[]> masks)
        {
            object masksStack = GetPrivateField<object>(baseScene.TriangleEraser, "m_MasksStack");
            object simplifiedMasksStack = GetPrivateField<object>(baseScene.TriangleEraser, "m_SimplifiedMasksStack");
            masksStack.GetType().GetMethod("Push").Invoke(masksStack, new object[] { masks[0].ToArray() });
            simplifiedMasksStack.GetType().GetMethod("Push").Invoke(simplifiedMasksStack, new object[] { masks[1].ToArray() });
        }

        private static Project CreateMinimalAnatomicProject(string suffix, int anatomyColumnCount = 1)
        {
            HBP.Core.Data.Site site = new($"cuts-triangle-erasing-site-{suffix}", new[] { new Coordinate("MNI", new Vector3(1, 2, 3), $"cuts-triangle-erasing-coordinate-{suffix}") }, Array.Empty<BaseTagValue>(), $"cuts-triangle-erasing-site-id-{suffix}");
            Patient patient = new($"cuts-triangle-erasing-patient-{suffix}", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), new[] { site }, Array.Empty<BaseTagValue>(), string.Empty, $"cuts-triangle-erasing-patient-id-{suffix}");
            List<Column> columns = Enumerable.Range(0, anatomyColumnCount).Select(index => (Column)new AnatomicColumn($"cuts-triangle-erasing-anatomy-{suffix}-{index}", new BaseConfiguration(), new AnatomicConfiguration($"cuts-triangle-erasing-anatomy-config-{suffix}-{index}"), $"cuts-triangle-erasing-column-anatomy-{suffix}-{index}")).ToList();
            Visualization visualization = new($"cuts-triangle-erasing-visualization-{suffix}", new[] { patient }, columns, new VisualizationConfiguration(), $"cuts-triangle-erasing-visualization-id-{suffix}");
            Project project = new($"cuts-triangle-erasing-project-{suffix}", new HBP.Core.Data.ProjectPreferences($"cuts-triangle-erasing-test-{suffix}", $"cuts-triangle-erasing-project-preferences-{suffix}"), new[] { patient }, Array.Empty<Group>(), Array.Empty<Dataset>(), new[] { visualization });
            ApplicationState.LoadedProject = project;
            return project;
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
            HBP.Data.Module3D.ROIManager roiManager = CreateManager<HBP.Data.Module3D.ROIManager>(sceneObject, "ROIManager", baseScene);
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
            HBPEdgeCameraSettings edgeSettings = cameraObject.AddComponent<HBPEdgeCameraSettings>();

            SetPrivateField(view, "m_Camera3D", camera3D);
            SetPrivateField(camera3D, "m_Camera", camera);
            SetPrivateField(camera3D, "m_EdgeSettings", edgeSettings);
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
            GameObject spherePrefab = new("ROI Sphere Prefab");
            spherePrefab.SetActive(false);
            spherePrefab.transform.SetParent(prefab.transform, false);
            spherePrefab.AddComponent<HBP.Data.Module3D.Sphere>();
            SetPrivateField(roi, "m_SpherePrefab", spherePrefab);
            return prefab;
        }

        private static CutParameterUiHarness CreateCutParameterUi(PlayModeSceneScope scene)
        {
            GameObject root = new("Cut Parameters Controller");
            SceneManager.MoveGameObjectToScene(root, scene.Scene);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            CutParametersController controller = root.AddComponent<CutParametersController>();

            Image image = CreateImage("Cut Image", root.transform);
            image.gameObject.AddComponent<Button>();
            image.sprite = Sprite.Create(new Texture2D(10, 10), new Rect(0, 0, 10, 10), Vector2.zero);
            Dropdown orientation = CreateDropdown("Orientation", Enum.GetNames(typeof(CutOrientation)));
            Slider position = CreateSlider("Position");
            GameObject positionParent = new("Position Parent");
            position.transform.SetParent(positionParent.transform, false);
            PressableButton plus = CreatePressableButton("Plus Position");
            PressableButton minus = CreatePressableButton("Minus Position");
            Toggle flip = CreateToggle("Flip");
            Button remove = CreateButton("Remove");
            RectTransform customValues = CreateRectTransform("Custom Values");
            InputField customX = CreateInputField("Custom X", "1");
            InputField customY = CreateInputField("Custom Y", "0");
            InputField customZ = CreateInputField("Custom Z", "0");
            Text positionTitle = CreateText("Position Title");
            Text positionValue = CreateText("Position Value");
            GameObject positionInformation = new("Position Information");
            RectTransform sites = CreateRectTransform("Sites");
            RectTransform cutLines = CreateRectTransform("Cut Lines");
            GameObject sitePrefab = new("Cut Site Prefab");
            sitePrefab.AddComponent<CutSite>();
            GameObject cutLinePrefab = new("Cut Line Prefab");

            foreach (Transform transform in new[]
                     {
                         image.transform, orientation.transform, positionParent.transform, plus.transform, minus.transform,
                         flip.transform, remove.transform, customValues.transform, customX.transform, customY.transform,
                         customZ.transform, positionTitle.transform, positionValue.transform, positionInformation.transform,
                         sites.transform, cutLines.transform
                     })
            {
                transform.SetParent(root.transform, false);
            }

            SetPrivateField(controller, "m_Image", image);
            SetPrivateField(controller, "m_Orientation", orientation);
            SetPrivateField(controller, "m_Position", position);
            SetPrivateField(controller, "m_PlusPosition", plus);
            SetPrivateField(controller, "m_MinusPosition", minus);
            SetPrivateField(controller, "m_Flip", flip);
            SetPrivateField(controller, "m_Remove", remove);
            SetPrivateField(controller, "m_CustomValues", customValues);
            SetPrivateField(controller, "m_CustomX", customX);
            SetPrivateField(controller, "m_CustomY", customY);
            SetPrivateField(controller, "m_CustomZ", customZ);
            SetPrivateField(controller, "m_PositionTitle", positionTitle);
            SetPrivateField(controller, "m_PositionValue", positionValue);
            SetPrivateField(controller, "m_PositionInformation", positionInformation);
            SetPrivateField(controller, "m_SitesRectTransform", sites);
            SetPrivateField(controller, "m_SitePrefab", sitePrefab);
            SetPrivateField(controller, "m_CutLinesRectTransform", cutLines);
            SetPrivateField(controller, "m_CutLinePrefab", cutLinePrefab);

            rootRect.sizeDelta = new Vector2(200, 200);
            return new CutParameterUiHarness(controller, position, orientation, flip, remove, customX, customY, customZ);
        }

        private static T CreateTool<T>(string name, Action<T> configure, Base3DScene scene, Column3D column, View3D view) where T : Tool
        {
            GameObject gameObject = new(name);
            T tool = gameObject.AddComponent<T>();
            configure(tool);
            tool.SelectedScene = scene;
            tool.SelectedColumn = column;
            tool.SelectedView = view;
            return tool;
        }

        private static Button CreateButton(string name)
        {
            GameObject gameObject = new(name);
            gameObject.AddComponent<RectTransform>();
            gameObject.AddComponent<Image>();
            return gameObject.AddComponent<Button>();
        }

        private static PressableButton CreatePressableButton(string name)
        {
            GameObject gameObject = new(name);
            gameObject.AddComponent<RectTransform>();
            gameObject.AddComponent<Image>();
            return gameObject.AddComponent<PressableButton>();
        }

        private static Toggle CreateToggle(string name)
        {
            GameObject gameObject = new(name);
            gameObject.AddComponent<RectTransform>();
            gameObject.AddComponent<Image>();
            return gameObject.AddComponent<Toggle>();
        }

        private static Dropdown CreateDropdown(string name, params string[] options)
        {
            GameObject gameObject = new(name);
            gameObject.AddComponent<RectTransform>();
            Dropdown dropdown = gameObject.AddComponent<Dropdown>();
            dropdown.options = options.Select(option => new Dropdown.OptionData(option)).ToList();
            return dropdown;
        }

        private static Slider CreateSlider(string name)
        {
            GameObject gameObject = new(name);
            gameObject.AddComponent<RectTransform>();
            return gameObject.AddComponent<Slider>();
        }

        private static Image CreateImage(string name, Transform parent)
        {
            GameObject gameObject = new(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.AddComponent<RectTransform>();
            return gameObject.AddComponent<Image>();
        }

        private static InputField CreateInputField(string name, string text)
        {
            GameObject gameObject = new(name);
            gameObject.AddComponent<RectTransform>();
            InputField input = gameObject.AddComponent<InputField>();
            input.text = text;
            return input;
        }

        private static Text CreateText(string name)
        {
            GameObject gameObject = new(name);
            gameObject.AddComponent<RectTransform>();
            return gameObject.AddComponent<Text>();
        }

        private static RectTransform CreateRectTransform(string name)
        {
            GameObject gameObject = new(name);
            return gameObject.AddComponent<RectTransform>();
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

        private static int[] CreateMask(int size, int value)
        {
            return Enumerable.Repeat(value, size).ToArray();
        }

        private static void EnsureRuntimeSiteConfigurations(Base3DScene baseScene)
        {
            foreach (Column3D column in baseScene.Columns)
            {
                foreach (HBP.Core.Object3D.Site site in column.Sites)
                {
                    site.State ??= new SiteState();
                    site.Configuration ??= new SiteConfiguration();
                    site.Configuration.Labels ??= Array.Empty<string>();
                }
            }
        }

        private static void WireRuntimeMeshSelection(Base3DScene baseScene)
        {
            Mesh3D selectedMesh = baseScene.MeshManager.SelectedMesh;
            SetAutoProperty(baseScene.MeshManager, "BrainSurface", selectedMesh.Both);
            SetAutoProperty(baseScene.MeshManager, "SimplifiedMeshToUse", selectedMesh.SimplifiedBoth ?? selectedMesh.Both);
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

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = FindField(target.GetType(), fieldName);
            return (T)field.GetValue(target);
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

        private static FieldInfo FindField(Type type, string fieldName)
        {
            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (field != null) return field;
                type = type.BaseType;
            }

            Assert.Fail($"Missing field {fieldName}");
            return null;
        }

        private static void NoProgress(float progress, float duration, LoadingText text)
        {
        }

        private static void AssertNoException(string label, Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Assert.Fail($"{label} threw {exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}");
            }
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

        private sealed class CutParameterUiHarness
        {
            public CutParametersController Controller { get; }
            public Slider Position { get; }
            public Dropdown Orientation { get; }
            public Toggle Flip { get; }
            public Button Remove { get; }
            public InputField CustomX { get; }
            public InputField CustomY { get; }
            public InputField CustomZ { get; }

            public CutParameterUiHarness(CutParametersController controller, Slider position, Dropdown orientation, Toggle flip, Button remove, InputField customX, InputField customY, InputField customZ)
            {
                Controller = controller;
                Position = position;
                Orientation = orientation;
                Flip = flip;
                Remove = remove;
                CustomX = customX;
                CustomY = customY;
                CustomZ = customZ;
            }
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
                File.WriteAllLines(objPath, new[]
                {
                    "v 0 0 0",
                    "v 1 0 0",
                    "v 0 1 0",
                    "v 0 0 1",
                    "f 1 2 3",
                    "f 1 4 2",
                    "f 2 4 3",
                    "f 3 4 1"
                });

                HBP.Core.DLL.Surface left = LoadSurface(objPath);
                HBP.Core.DLL.Surface right = LoadSurface(objPath);
                HBP.Core.DLL.Surface both = (HBP.Core.DLL.Surface)left.Clone();
                both.Append(right);
                HBP.Core.DLL.Volume volume = new();
                SetAutoProperty(volume, "IsLoaded", true);

                MNIObjects mni = new();
                SetAutoProperty(mni, "GreyMatter", new LeftRightMesh3D("MNI Grey matter", left, right, both, MeshType.MNI));
                SetAutoProperty(mni, "WhiteMatter", new LeftRightMesh3D("MNI White matter", (HBP.Core.DLL.Surface)left.Clone(), (HBP.Core.DLL.Surface)right.Clone(), (HBP.Core.DLL.Surface)both.Clone(), MeshType.MNI));
                SetAutoProperty(mni, "InflatedWhiteMatter", new LeftRightMesh3D("MNI Inflated", (HBP.Core.DLL.Surface)left.Clone(), (HBP.Core.DLL.Surface)right.Clone(), (HBP.Core.DLL.Surface)both.Clone(), MeshType.MNI));
                SetAutoProperty(mni, "MRI", new MRI3D("MNI", volume));
                SetAutoProperty(mni, "IsLoaded", true);
                return mni;
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
