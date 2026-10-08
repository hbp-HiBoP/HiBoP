#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using HBP.Quest;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Site = HBP.Core.Object3D.Site;

namespace HBP.Tests.Quest
{
    public class QuestSiteProbeTests
    {
        [Test]
        public void CurrentContact_UsesTransformedSpheresWithoutAttractionOrSelectingCrossedSites()
        {
            using var fixture = new Fixture();
            var targets = Enumerable.Range(0, 15).Select(i => fixture.AddSite("A" + i.ToString("D2"), new Vector3(i * 10, 0, 0))).ToList();
            fixture.Visual.transform.SetPositionAndRotation(new Vector3(.2f, .3f, .7f), Quaternion.Euler(35, 70, 15));
            fixture.Visual.transform.localScale = Vector3.one * .002f;
            var policy = new QuestSiteContactPolicy();
            foreach (var target in targets)
                Assert.That(policy.FindContact(target.Site.transform.position, .0005f, targets), Is.SameAs(target));
            Assert.That(policy.FindContact(fixture.Visual.transform.TransformPoint(Vector3.left * 10), .0005f, targets), Is.Null);
            Assert.That(policy.FindContact(fixture.Visual.transform.TransformPoint(Vector3.right * 150), .0005f, targets), Is.Null, "No contact is retained or inferred along the intervening path.");
            Assert.That(policy.FindContact(fixture.Visual.transform.TransformPoint(new Vector3(50, 2, 0)), .0005f, targets), Is.Null, "No attraction outside the exact sum of radii.");
            Assert.That(fixture.Column.ColumnData.ID, Is.EqualTo("probe-column"));
            Assert.That(fixture.Column.SelectedSite, Is.Null, "Geometry alone never publishes or selects.");
        }

        [Test]
        public void Overlaps_ChooseNearestCenterAndStableIdsRegardlessOfRosterOrder()
        {
            using var fixture = new Fixture();
            var a = fixture.AddSite("A", Vector3.zero);
            var b = fixture.AddSite("B", new Vector3(1, 0, 0));
            var targets = new List<QuestSiteTarget> { b, a };
            var policy = new QuestSiteContactPolicy();
            Vector3 point = fixture.Visual.transform.TransformPoint(Vector3.right * .8f);
            Assert.That(policy.FindContact(point, .0005f, targets), Is.SameAs(b));
            b.Site.transform.localPosition = a.Site.transform.localPosition;
            point = a.Site.transform.position;
            Assert.That(policy.FindContact(point, .0005f, targets), Is.SameAs(a));
            targets.Reverse();
            Assert.That(policy.FindContact(point, .0005f, targets), Is.SameAs(a));
        }

        [Test]
        public void OverlappingColumns_CurrentContactSelectsItsColumnAndClearsThePreviousSite()
        {
            using var fixture = new Fixture();
            var second = fixture.Column.gameObject.AddComponent<Column3DAnatomy>();
            SetAuto(second, "ColumnData", new AnatomicColumn("Second", new BaseConfiguration(), new AnatomicConfiguration(), "second-column"));
            SetAuto(second, "Sites", new List<Site>());
            fixture.Scene.Columns.Add(second);
            foreach (var column in fixture.Scene.Columns)
                column.OnSelect.AddListener(() =>
                {
                    foreach (var other in fixture.Scene.Columns.Where(other => other != column))
                    {
                        other.IsSelected = false;
                        other.UnselectSite();
                    }
                });
            var a = fixture.AddSite("A", Vector3.zero);
            var b = fixture.AddSite("B", Vector3.right, second);
            var targets = new List<QuestSiteTarget> { a, b };
            var policy = new QuestSiteContactPolicy();
            Assert.That(QuestSiteSelection.TrySelect(fixture.Scene, a), Is.True);
            var contact = policy.FindContact(fixture.Visual.transform.TransformPoint(Vector3.right * .8f), .0005f, targets);
            Assert.That(contact, Is.SameAs(b), "The previously selected column has no priority.");
            Assert.That(QuestSiteSelection.TrySelect(fixture.Scene, contact), Is.True);
            Assert.That(fixture.Scene.SelectedColumn, Is.SameAs(second));
            Assert.That(fixture.Column.SelectedSite, Is.Null);
            targets.Reverse();
            for (int frame = 0; frame < 20; frame++)
            {
                Assert.That(policy.FindContact(b.Site.transform.position, .0005f, targets), Is.SameAs(b));
                Assert.That(QuestSiteSelection.TrySelect(fixture.Scene, b), Is.False);
                Assert.That(fixture.Scene.SelectedColumn, Is.SameAs(second));
            }

            Assert.That(QuestSiteSelection.TrySelect(fixture.Scene, a), Is.True);
            Assert.That(second.SelectedSite, Is.Null);
            Assert.That(fixture.Scene.SelectedColumn, Is.SameAs(fixture.Column));
        }

        [Test]
        public void Selection_UsesOwningColumnRejectsMaskedHiddenAndUnknownSitesAndDoesNotRepeat()
        {
            using var fixture = new Fixture();
            var a = fixture.AddSite("A", Vector3.zero);
            int events = 0;
            fixture.Column.OnSelectSite.AddListener(_ => events++);
            Assert.That(QuestSiteSelection.TrySelect(fixture.Scene, a), Is.True);
            Assert.That(QuestSiteSelection.TrySelect(fixture.Scene, a), Is.False);
            Assert.That(events, Is.EqualTo(1));
            var b = fixture.AddSite("B", Vector3.right * 10);
            b.Site.State.IsBlackListed = true;
            Assert.That(QuestSiteSelection.TrySelect(fixture.Scene, b), Is.True, "A visible blacklisted site is selectable, just like Desktop.");
            Assert.That(b.Site.State.IsEffectivelyMasked(false), Is.True, "Selection must not change its scientific exclusion.");
            fixture.Scene.HideBlacklistedSites = true;
            Assert.That(b.IsEligible(fixture.Scene), Is.False);
            Assert.That(QuestSiteSelection.TrySelect(fixture.Scene, b), Is.False);
            fixture.Scene.HideBlacklistedSites = false;
            b.Site.State.IsBlackListed = false;
            b.Site.State.IsFiltered = false;
            Assert.That(QuestSiteSelection.TrySelect(fixture.Scene, b), Is.False);
            b.Site.State.IsFiltered = true;
            b.Site.State.IsMasked = true;
            Assert.That(QuestSiteSelection.TrySelect(fixture.Scene, b), Is.False);
            b.Site.State.IsMasked = false;
            b.Site.State.IsOutOfROI = true;
            Assert.That(b.IsEligible(fixture.Scene), Is.False);
            fixture.Scene.ShowAllSites = true;
            Assert.That(b.IsEligible(fixture.Scene), Is.True);
            b.Site.State.IsOutOfROI = false;
            b.Site.gameObject.SetActive(false);
            Assert.That(QuestSiteSelection.TrySelect(fixture.Scene, b), Is.False);
            b.Site.gameObject.SetActive(true);
            fixture.Column.Sites.Remove(b.Site);
            Assert.That(QuestSiteSelection.TrySelect(fixture.Scene, b), Is.False);
            Assert.That(fixture.Column.SelectedSite, Is.SameAs(b.Site));
        }

        [Test]
        public async Task SelectionRing_TracksWorldRadiusVisibilityAndDesktopAnimation()
        {
            using var fixture = new Fixture();
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestColumn.prefab"));
            var cameraObject = new GameObject("Selection ring test camera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -.1f);
            camera.nearClipPlane = .001f;
            camera.fieldOfView = 15;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.05f, .05f, .05f, 1);
            var target = new RenderTexture(256, 256, 24);
            var pixels = new Texture2D(256, 256, TextureFormat.RGB24, false);
            try
            {
                var a = fixture.AddSite("A", Vector3.zero);
                var sitePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/3D/Objects/Site.prefab");
                a.Site.gameObject.AddComponent<MeshFilter>().sharedMesh = sitePrefab.GetComponent<MeshFilter>().sharedMesh;
                a.Site.gameObject.AddComponent<MeshRenderer>().sharedMaterial = sitePrefab.GetComponent<MeshRenderer>().sharedMaterial;
                var ring = root.GetComponent<QuestSiteSelectionRing>();
                var board = (RectTransform)typeof(QuestSiteSelectionRing).GetField("billboard", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ring);
                var image = (Image)typeof(QuestSiteSelectionRing).GetField("image", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ring);
                ring.Bind(fixture.Scene, fixture.Column);
                Assert.That(board.gameObject.activeSelf, Is.False);
                QuestSiteSelection.TrySelect(fixture.Scene, a);
                ring.Refresh(camera);
                Assert.That(board.gameObject.activeSelf, Is.True);
                Assert.That(board.TransformVector(Vector3.right).magnitude, Is.EqualTo(.0034f).Within(.000001f), "Desktop padding is 70% beyond the actual site diameter.");
                var animator = image.GetComponent<Animator>();
                animator.Update(0);
                Quaternion initial = image.transform.localRotation;
                animator.Update(.5f);
                Assert.That(Quaternion.Angle(initial, image.transform.localRotation), Is.GreaterThan(20));
                Assert.That(image.transform.localScale.x, Is.GreaterThan(1.01f), "The original animation pulses as well as rotating.");
                animator.speed = 0;
                await UniTask.NextFrame();
                ring.Refresh(camera);
                Canvas.ForceUpdateCanvases();
                camera.targetTexture = target;
                camera.Render();
                var previous = RenderTexture.active;
                try
                {
                    RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                    pixels.Apply();
                    Assert.That(pixels.GetPixels().Count(p => p.r > .8f && p.g > .8f && p.b > .8f), Is.GreaterThan(30), "The themed animated ring must actually render.");
                    var folder = System.IO.Path.GetFullPath("Logs/QuestUI");
                    System.IO.Directory.CreateDirectory(folder);
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, "Probe-Selection-Ring.png"), pixels.EncodeToPNG());
                }
                finally
                {
                    RenderTexture.active = previous;
                }

                fixture.Visual.transform.SetPositionAndRotation(new Vector3(.02f, .01f, 0), Quaternion.Euler(25, 40, 15));
                fixture.Visual.transform.localScale *= 3;
                root.transform.SetPositionAndRotation(new Vector3(-.1f, 0, 0), Quaternion.Euler(10, -30, 20));
                root.transform.localScale = Vector3.one * 2;
                ring.Refresh(camera);
                Assert.That(Vector3.Distance(board.position, a.Site.transform.position), Is.LessThan(.000001f));
                Assert.That(board.TransformVector(Vector3.right).magnitude, Is.EqualTo(.0102f).Within(.000001f));
                Assert.That(Quaternion.Angle(board.rotation, camera.transform.rotation), Is.LessThan(.01f));
                a.Site.State.IsBlackListed = true;
                ring.Refresh(camera);
                Assert.That(board.gameObject.activeSelf, Is.True);
                fixture.Scene.HideBlacklistedSites = true;
                ring.Refresh(camera);
                Assert.That(board.gameObject.activeSelf, Is.False);
                fixture.Scene.HideBlacklistedSites = false;
                a.Site.gameObject.SetActive(false);
                ring.Refresh(camera);
                Assert.That(board.gameObject.activeSelf, Is.False);
                a.Site.gameObject.SetActive(true);
                root.GetComponent<QuestColumnPresentation>().Hide();
                ring.Refresh(camera);
                Assert.That(board.gameObject.activeSelf, Is.False);
                root.GetComponent<QuestColumnPresentation>().Show();
                ring.Refresh(camera);
                Assert.That(board.gameObject.activeSelf, Is.True);
                fixture.Column.UnselectSite();
                ring.Refresh(camera);
                Assert.That(board.gameObject.activeSelf, Is.False);
                ring.Bind(null, fixture.Column);
                Assert.That(board.gameObject.activeSelf, Is.False);
            }
            finally
            {
                camera.targetTexture = null;
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(pixels);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task ATool_ArbitratesUIAndTriggerAndRequiresFreshPressAfterTrackingFocusOrSceneLoss(bool useAimPose)
        {
            using var fixture = new Fixture();
            var background = InputSystem.settings.backgroundBehavior;
            var editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.RegisterLayout("{\"name\":\"QuestSiteTestController\",\"extend\":\"XRController\",\"controls\":[{\"name\":\"primaryButton\",\"layout\":\"Button\",\"format\":\"FLT\"},{\"name\":\"triggerPressed\",\"layout\":\"Button\",\"format\":\"FLT\"},{\"name\":\"pointerPosition\",\"layout\":\"Vector3\"},{\"name\":\"pointerRotation\",\"layout\":\"Quaternion\"}]}");
            var device = InputSystem.AddDevice("QuestSiteTestController");
            InputSystem.SetDeviceUsage(device, CommonUsages.RightHand);
            var root = new GameObject("Probe input fixture");
            var policy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<QuestInteractionPolicy>("Assets/Resources/Themes/Quest/Quest Interaction Policy.asset"));
            try
            {
                var a = fixture.AddSite("A", Vector3.zero);
                var b = fixture.AddSite("B", Vector3.right * 10);
                var c = fixture.AddSite("C", Vector3.right * 20);
                var head = root.AddComponent<QuestDevicePoseTracker>();
                head.enabled = false;
                SetAuto(head, "IsTracked", true);
                var handObject = new GameObject("Right hand");
                handObject.transform.SetParent(root.transform);
                var right = handObject.AddComponent<QuestDevicePoseTracker>();
                right.enabled = false;
                SetAuto(right, "IsTracked", true);
                var camera = root.AddComponent<Camera>();
                camera.nearClipPlane = .01f;
                var window = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Window.prefab"), root.transform);
                window.transform.position = Vector3.forward * 1.15f;
                window.GetComponent<Canvas>().worldCamera = camera;
                var button = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Button.prefab"), window.transform).GetComponent<Button>();
                ((RectTransform)button.transform).anchoredPosition = Vector2.zero;
                int clicks = 0;
                button.onClick.AddListener(() => clicks++);
                var ui = new GameObject("Probe cursor", typeof(EventSystem));
                ui.transform.SetParent(root.transform);
                ui.SetActive(false);
                var pointer = ui.AddComponent<QuestPointerInput>();
                var probe = ui.AddComponent<QuestSiteProbe>();
                var marker = new GameObject("Probe sphere");
                marker.transform.SetParent(root.transform);
                var line = ui.AddComponent<LineRenderer>();
                QuestWindowInteractionTests.Set(pointer, "policy", policy);
                QuestWindowInteractionTests.Set(pointer, "head", head);
                QuestWindowInteractionTests.Set(pointer, "right", right);
                QuestWindowInteractionTests.Set(pointer, "rightRay", line);
                QuestWindowInteractionTests.Set(pointer, "siteProbe", probe);
                var origin = new GameObject("Rotated tracking origin");
                origin.transform.SetParent(root.transform);
                origin.transform.SetPositionAndRotation(new Vector3(.1f, .2f, .3f), Quaternion.Euler(0, 35, 0));
                if (useAimPose) QuestWindowInteractionTests.Set(pointer, "trackingOrigin", origin.transform);
                QuestWindowInteractionTests.Set(probe, "anatomy", fixture.View);
                QuestWindowInteractionTests.Set(probe, "policy", policy);
                QuestWindowInteractionTests.Set(probe, "right", right);
                QuestWindowInteractionTests.Set(probe, "marker", marker.transform);
                int selections = 0;
                probe.Selected += _ => selections++;
                ui.SetActive(true);
                await UniTask.NextFrame();
                SetAuto(head, "IsTracked", true);
                SetAuto(right, "IsTracked", true);
                Canvas.ForceUpdateCanvases();

                void Tick()
                {
                    InputSystem.Update();
                    pointer.Process();
                }

                void Press(string control, bool value) => InputSystem.QueueDeltaStateEvent(device.GetChildControl<ButtonControl>(control), value ? 1f : 0f);

                void Position(Vector3 point)
                {
                    right.transform.position = useAimPose ? point + new Vector3(.2f, -.1f, 0) : point;
                    if (!useAimPose) return;
                    InputSystem.QueueDeltaStateEvent(device.GetChildControl<Vector3Control>("pointerPosition"), origin.transform.InverseTransformPoint(point));
                    InputSystem.QueueDeltaStateEvent(device.GetChildControl<QuaternionControl>("pointerRotation"), Quaternion.Inverse(origin.transform.rotation));
                }

                Position(a.Site.transform.position);
                Tick();
                Press("triggerPressed", true);
                Tick();
                Press("primaryButton", true);
                Tick();
                Assert.That(probe.IsActive, Is.False, "A cannot replace an existing UI capture.");
                Press("triggerPressed", false);
                Tick();
                Assert.That(clicks, Is.EqualTo(1));
                Assert.That(probe.IsActive, Is.False, "Keeping A held after a capture ends does not start the tool.");
                Press("primaryButton", false);
                Tick();
                Press("primaryButton", true);
                Tick();
                Assert.That(probe.IsActive, Is.True);
                Assert.That(Vector3.Distance(marker.transform.position, a.Site.transform.position), Is.LessThan(.000001f), "The probe must use the exact ray origin, including the tracking-origin transform.");
                Assert.That(line.enabled, Is.False);
                Assert.That(fixture.Column.SelectedSite, Is.SameAs(a.Site));
                Tick();
                Tick();
                Assert.That(selections, Is.EqualTo(1));
                Press("triggerPressed", true);
                Tick();
                Assert.That(pointer.AllowsAnatomy(false), Is.False);
                Position(c.Site.transform.position);
                Tick();
                Assert.That(selections, Is.EqualTo(2), "Only C is touched at the current pose; crossing B between frames selects nothing.");
                Assert.That(fixture.Column.SelectedSite, Is.SameAs(c.Site));
                Press("primaryButton", false);
                Tick();
                Assert.That(marker.activeSelf, Is.False);
                Assert.That(fixture.Column.SelectedSite, Is.SameAs(c.Site));
                Press("triggerPressed", false);
                Tick();
                Assert.That(clicks, Is.EqualTo(1), "A held trigger must not acquire UI when the probe stops.");
                Press("primaryButton", true);
                Tick();
                SetAuto(right, "IsTracked", false);
                Tick();
                Assert.That(probe.IsActive, Is.False);
                Position(a.Site.transform.position);
                SetAuto(right, "IsTracked", true);
                Tick();
                Assert.That(probe.IsActive, Is.False);
                Assert.That(selections, Is.EqualTo(2));
                Press("primaryButton", false);
                Tick();
                Press("primaryButton", true);
                Tick();
                Assert.That(selections, Is.EqualTo(3));
                pointer.SendMessage("OnApplicationFocus", false);
                Assert.That(probe.IsActive, Is.False);
                pointer.SendMessage("OnApplicationFocus", true);
                Tick();
                Assert.That(probe.IsActive, Is.False);
                Press("primaryButton", false);
                Tick();
                Press("primaryButton", true);
                Tick();
                QuestWindowInteractionTests.Set(fixture.View, "current", null);
                Tick();
                Assert.That(probe.IsActive, Is.False);
                fixture.AttachScene();
                Tick();
                Assert.That(probe.IsActive, Is.False);
                Press("primaryButton", false);
                Tick();
                Press("primaryButton", true);
                Tick();
                Assert.That(probe.IsActive, Is.True);
                probe.enabled = false;
                Assert.That(marker.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(policy);
                InputSystem.RemoveDevice(device);
                InputSystem.RemoveLayout("QuestSiteTestController");
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            }
        }

        private static void SetAuto(object target, string property, object value)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField("<" + property + ">k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field == null) continue;
                field.SetValue(target, value);
                return;
            }

            throw new MissingFieldException(target.GetType().Name, property);
        }

        private sealed class Fixture : IDisposable
        {
            private readonly GameObject data = new("Inactive probe scientific fixture");
            public readonly GameObject Visual = new("Probe site presentation");
            public readonly Base3DScene Scene;
            public readonly Column3D Column;
            public readonly QuestAnatomyView View;

            public Fixture()
            {
                data.SetActive(false);
                Scene = data.AddComponent<Base3DScene>();
                Column = data.AddComponent<Column3DAnatomy>();
                SetAuto(Column, "ColumnData", new AnatomicColumn("Probe", new BaseConfiguration(), new AnatomicConfiguration(), "probe-column"));
                SetAuto(Column, "Sites", new List<Site>());
                Scene.Columns.Add(Column);
                View = data.AddComponent<QuestAnatomyView>();
                var presentation = data.AddComponent<QuestColumnPresentation>();
                SetAuto(presentation, "Column", Column);
                ((List<QuestColumnPresentation>)typeof(QuestAnatomyView).GetField("columns", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(View)).Add(presentation);
                Visual.transform.localScale = Vector3.one * .001f;
                AttachScene();
            }

            public void AttachScene()
            {
                var field = typeof(QuestAnatomyView).GetField("current", BindingFlags.Instance | BindingFlags.NonPublic);
                var restored = field.FieldType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single().Invoke(new object[] { Scene, null, null });
                QuestWindowInteractionTests.Set(View, "current", restored);
            }

            public QuestSiteTarget AddSite(string name, Vector3 position, Column3D owner = null)
            {
                owner ??= Column;
                var obj = new GameObject(name, typeof(SphereCollider), typeof(Site));
                obj.transform.SetParent(Visual.transform, false);
                obj.transform.localPosition = position;
                obj.GetComponent<SphereCollider>().radius = 1;
                var site = obj.GetComponent<Site>();
                site.Information = new SiteInformation { Name = name, Patient = new Patient { ID = "probe-patient" } };
                site.State = new SiteState();
                owner.Sites.Add(site);
                site.OnSelectSite.AddListener(selected =>
                {
                    if (selected) owner.UnselectSite();
                    SetAuto(owner, "SelectedSite", selected ? site : null);
                    owner.OnSelectSite.Invoke(owner.SelectedSite);
                });
                return new QuestSiteTarget(owner, site);
            }

            public void Dispose()
            {
                QuestWindowInteractionTests.Set(View, "current", null);
                Object.DestroyImmediate(data);
                Object.DestroyImmediate(Visual);
            }
        }
    }
}
#endif
