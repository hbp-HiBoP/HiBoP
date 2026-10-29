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
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Site = HBP.Core.Object3D.Site;

namespace HBP.Tests.Quest
{
    public sealed class QuestSelectionLabTests
    {
        [Test]
        public void Assistance_ReferenceMatchesStrictContactIncludingTransformedTargets()
        {
            using var fixture = new Targets();
            var a = fixture.Add("A1", Vector3.zero);
            fixture.Root.transform.SetPositionAndRotation(new Vector3(.3f, .1f, .8f), Quaternion.Euler(20, 50, 35));
            fixture.Root.transform.localScale = new Vector3(2, 1, 3);
            var strict = new QuestSiteContactPolicy();
            var assisted = new QuestSiteAssistance();
            var settings = QuestSelectionSettings.Profile(0);
            foreach (Vector3 delta in new[] { Vector3.zero, Vector3.up * .001f, Vector3.right * .0036f, Vector3.forward * .01f })
            {
                Vector3 point = a.Site.transform.position + delta;
                Assert.That(assisted.Find(point, Vector3.forward, new Ray(point, Vector3.forward), settings, fixture.Items, null), Is.SameAs(strict.FindContact(point, settings.ContactRadius, fixture.Items)));
            }
        }

        [Test]
        public void DepthToleranceIsBoundedAndDirectTouchOverridesStabilization()
        {
            using var fixture = new Targets();
            var a = fixture.Add("A1", new Vector3(0, 0, .008f));
            var b = fixture.Add("B1", new Vector3(.004f, 0, 0));
            var settings = QuestSelectionSettings.Profile(3);
            var policy = new QuestSiteAssistance();
            var aim = new Ray(Vector3.zero, Vector3.forward);
            Assert.That(policy.Find(Vector3.zero, Vector3.forward, aim, settings, fixture.Items, null), Is.SameAs(a));
            Assert.That(policy.Find(b.Site.transform.position, Vector3.forward, aim, settings, fixture.Items, a), Is.SameAs(b));
            Assert.That(policy.Find(Vector3.one, Vector3.forward, aim, settings, fixture.Items, a), Is.Null, "No sticky target outside the volume, including when no other candidate exists.");
        }

        [Test]
        public void RayRejectsBehindAndBeyondRangeAndCanSelectRearTargetFromAnotherAngle()
        {
            using var fixture = new Targets();
            var a = fixture.Add("A1", Vector3.forward * .1f);
            var b = fixture.Add("B1", Vector3.forward * .2f);
            fixture.Add("Behind1", Vector3.back * .1f);
            fixture.Add("Beyond1", Vector3.forward * 2);
            var settings = QuestSelectionSettings.Profile(4);
            settings.RayDistance = .3f;
            var policy = new QuestSiteAssistance();
            var aim = new Ray(Vector3.zero, Vector3.forward);
            Assert.That(policy.Find(Vector3.zero, Vector3.forward, aim, settings, fixture.Items, null), Is.SameAs(a));
            Assert.That(policy.CandidateCount, Is.EqualTo(2));
            Vector3 origin = Vector3.right * .1f;
            aim = new Ray(origin, (b.Site.transform.position - origin).normalized);
            Assert.That(policy.Find(origin, aim.direction, aim, settings, fixture.Items, null), Is.SameAs(b));
        }

        [Test]
        public void SettingsRejectInvalidSavedValuesAndProfilesAreIndependent()
        {
            var settings = QuestSelectionSettings.Profile(2);
            var copy = settings.Copy();
            copy.DepthTolerance = float.NaN;
            copy.Mode = (QuestSelectionMode)99;
            copy.RayDistance = float.PositiveInfinity;
            copy.HapticAmplitude = -1;
            copy.Validate();
            Assert.That(copy.Mode, Is.EqualTo(QuestSelectionMode.Contact));
            Assert.That(float.IsFinite(copy.DepthTolerance) && float.IsFinite(copy.RayDistance), Is.True);
            Assert.That(copy.HapticAmplitude, Is.Zero);
            Assert.That(settings.Mode, Is.EqualTo(QuestSelectionMode.Depth));
            Assert.That(settings.Debug, Is.False);
            copy.LabelScale = 0;
            copy.Validate();
            Assert.That(copy.LabelScale, Is.EqualTo(.4f), "Old saved settings get the smaller default label.");
        }

        [Test]
        public void RandomCloudUsesFullBrainBoxAndStableUniformSamplesWithoutChangingGlobalRandom()
        {
            var bounds = new Bounds(new Vector3(5, -15, 10), new Vector3(100, 140, 120));
            var before = UnityEngine.Random.state;
            var patient = QuestSelectionLayouts.Patient(bounds.center, Vector3.forward, bounds);
            Assert.That(UnityEngine.Random.state.Equals(before), Is.True);
            var cloud = patient.Sites.Where(s => s.Name.StartsWith("U")).ToArray();
            Assert.That(cloud.Length, Is.EqualTo(2000));
            Assert.That(patient.Sites.Select(s => s.ID).Distinct().Count(), Is.EqualTo(2025));

            Vector3 Position(HBP.Core.Data.Site s)
            {
                Vector3 p = s.Coordinates.Single().Position.ToVector3();
                return new Vector3(HBP.Core.DLL.ReferenceSystemConversion.ConvertX(p.x), p.y, p.z);
            }

            var points = cloud.Select(Position).ToArray();
            Assert.That(points.All(bounds.Contains), Is.True);
            Assert.That(points.Select(p => p.x).Average(), Is.EqualTo(bounds.center.x).Within(bounds.size.x * .03f));
            Assert.That(points.Select(p => p.y).Average(), Is.EqualTo(bounds.center.y).Within(bounds.size.y * .03f));
            Assert.That(points.Select(p => p.z).Average(), Is.EqualTo(bounds.center.z).Within(bounds.size.z * .03f));
            Assert.That(points.Any(p => p.z < bounds.center.z) && points.Any(p => p.z > bounds.center.z), Is.True, "Keep both the retained and removed halves.");
            var repeated = QuestSelectionLayouts.Patient(bounds.center, Vector3.forward, bounds).Sites.Where(s => s.Name.StartsWith("U")).Select(Position);
            Assert.That(repeated, Is.EqualTo(points));
        }

        [Test]
        public void StabilizationRetainsOnlySmallScoreDifferencesAndReleasesForBetterNeighbour()
        {
            using var fixture = new Targets();
            var old = fixture.Add("A1", new Vector3(-.0005f, 0, .008f));
            var next = fixture.Add("B1", new Vector3(.0005f, 0, .008f));
            var settings = QuestSelectionSettings.Profile(3);
            var policy = new QuestSiteAssistance();
            Vector3 point = new Vector3(.0001f, 0, 0);
            Assert.That(policy.Find(point, Vector3.forward, new Ray(point, Vector3.forward), settings, fixture.Items, old), Is.SameAs(old));
            Assert.That(policy.Retained, Is.True);
            point.x = .0004f;
            Assert.That(policy.Find(point, Vector3.forward, new Ray(point, Vector3.forward), settings, fixture.Items, old), Is.SameAs(next));
            Assert.That(policy.Retained, Is.False);
        }

        [Test]
        public void LayoutsUseRemovedHalfAndDeterministicUniqueIdentities()
        {
            var patient = QuestSelectionLayouts.Patient(new Vector3(2, -15, 9), Vector3.forward);
            Assert.That(patient.Sites.Count, Is.EqualTo(25));
            Assert.That(patient.Sites.Select(s => s.ID).Distinct().Count(), Is.EqualTo(25));
            Assert.That(patient.Sites.All(s => s.Coordinates.Single().Position.ToVector3().z > 9), Is.True);
            Assert.That(patient.Sites.Count(s => s.Name.StartsWith("R")), Is.EqualTo(8));
        }

        [Test]
        public void CursorOffsetsUseRotatedRayFrameAndSignedPhysicalMetres()
        {
            var rotation = Quaternion.Euler(25, 65, 40);
            var origin = new Vector3(.2f, .4f, -.3f);
            var offset = new Vector3(-.012f, .03f, .02f);
            var pose = QuestSiteProbe.ResolveCursorPose(new Ray(origin, rotation * Vector3.forward), rotation * Vector3.up, offset);
            Assert.That(Vector3.Distance(pose.position, origin + rotation * offset), Is.LessThan(.000001f));
            Assert.That(Quaternion.Angle(pose.rotation, rotation), Is.LessThan(.01f));
            Assert.That(Vector3.Distance(pose.position, origin), Is.EqualTo(offset.magnitude).Within(.000001f));
            var vertical = QuestSiteProbe.ResolveCursorPose(new Ray(origin, Vector3.up), Vector3.up, offset);
            Assert.That(float.IsFinite(vertical.position.x) && float.IsFinite(vertical.position.y) && float.IsFinite(vertical.position.z), Is.True);
            Assert.That(Vector3.Distance(vertical.rotation * Vector3.forward, Vector3.up), Is.LessThan(.000001f));
        }

        [Test]
        public void CursorSettingsRoundTripClampAndDoNotChangeCaptureGeometry()
        {
            var settings = QuestSelectionSettings.Profile(5);
            settings.CursorStyle = QuestCursorStyle.Cage;
            settings.CursorColor = QuestCursorColor.Amber;
            settings.CursorOffset = new Vector3(-.012f, .03f, .02f);
            var saved = JsonUtility.FromJson<QuestSelectionSettings>(JsonUtility.ToJson(settings));
            Assert.That(saved.CursorOffset, Is.EqualTo(settings.CursorOffset));
            Assert.That(saved.CursorStyle, Is.EqualTo(QuestCursorStyle.Cage));
            Assert.That(saved.ContactRadius, Is.EqualTo(.003f));
            Assert.That(saved.MarkerRadius, Is.EqualTo(.001f));
            var reference = QuestSelectionSettings.Profile(0);
            reference.CopyCursorFrom(saved);
            Assert.That(reference.ContactRadius, Is.EqualTo(.0005f));
            Assert.That(reference.CursorOffset, Is.EqualTo(saved.CursorOffset));
            saved.CursorOffset = new Vector3(float.NaN, -1, float.PositiveInfinity);
            saved.CursorStyle = (QuestCursorStyle)99;
            saved.CursorGuideRadius = 0;
            saved.Validate();
            Assert.That(saved.CursorOffset, Is.EqualTo(new Vector3(0, -.3f, 0)));
            Assert.That(saved.CursorStyle, Is.EqualTo(QuestCursorStyle.Sphere));
            Assert.That(saved.CursorGuideRadius, Is.EqualTo(.006f));
        }

        [Test]
        public void AcceptedPresetAndProductionPrefabShareTheValidatedCursor()
        {
            var settings = QuestSelectionSettings.Default();
            Assert.That(settings.Mode, Is.EqualTo(QuestSelectionMode.Depth));
            Assert.That(settings.ContactRadius, Is.EqualTo(.003f));
            Assert.That(settings.MarkerRadius, Is.EqualTo(.001f));
            Assert.That(settings.DepthTolerance, Is.EqualTo(.01f));
            Assert.That(settings.Attraction && settings.Stabilize && settings.NameCue, Is.True);
            Assert.That(settings.AttractionRadius, Is.EqualTo(.003f));
            Assert.That(settings.SwitchMargin, Is.EqualTo(.15f));
            Assert.That(settings.CursorStyle, Is.EqualTo(QuestCursorStyle.Halo));
            Assert.That(settings.CursorColor, Is.EqualTo(QuestCursorColor.White));
            Assert.That(settings.CursorGuideRadius, Is.EqualTo(.004f));
            Assert.That(settings.CursorOffset, Is.EqualTo(new Vector3(0, 0, .03f)));
            var quest = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestBootstrap.prefab");
            Assert.That(quest.GetComponent<QuestSelectionLab>(), Is.Null);
            var probe = quest.GetComponentInChildren<QuestSiteProbe>(true);
            var probeFields = new SerializedObject(probe);
            Assert.That(probeFields.FindProperty("usePreferredSelection").boolValue, Is.True);
            foreach (var path in new[] { "Assets/Prefabs/Quest/QuestBootstrap.prefab", "Assets/Prefabs/Quest/QuestSelectionLab.prefab" })
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var feedback = root.GetComponentsInChildren<QuestSelectionFeedback>(true).Single();
                var fields = new SerializedObject(feedback);
                Assert.That(fields.FindProperty("probe").objectReferenceValue, Is.SameAs(root.GetComponentInChildren<QuestSiteProbe>(true)));
                Assert.That(fields.FindProperty("cursorCore").objectReferenceValue, Is.Not.Null);
                Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(feedback.gameObject), Is.EqualTo("Assets/Prefabs/Quest/3D/QuestSelectionFeedback.prefab"));
            }

            settings.CursorGuideRadius = .02f;
            Assert.That(QuestSelectionSettings.Default().CursorGuideRadius, Is.EqualTo(.004f), "Instances must not mutate the accepted preset.");
        }

        [Test, Timeout(120000)]
        public async Task StandaloneSceneLoadsRealMniAndAllLayoutsWithoutNetworkAndCloses()
        {
            await EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Scenes/QuestSelectionLab.unity", new LoadSceneParameters(LoadSceneMode.Additive));
            var scene = SceneManager.GetSceneByPath("Assets/_Scenes/QuestSelectionLab.unity");
            QuestAnatomyView view = null;
            try
            {
                var lab = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<QuestSelectionLab>(true)).Single();
                view = lab.GetComponentInChildren<QuestAnatomyView>();
                using var timeout = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(90));
                await UniTask.WaitUntil(() => lab.Ready, cancellationToken: timeout.Token);
                Assert.That(lab.GetComponentsInChildren<QuestAnatomySession>(true), Is.Empty);
                Assert.That(lab.GetComponentsInChildren<QuestConnectionPanel>(true), Is.Empty);
                Assert.That(view.Scene.MeshManager.SelectedMesh.Type, Is.EqualTo(HBP.Core.Enums.MeshType.MNI));
                Assert.That(view.Scene.Cuts.Single().Position, Is.EqualTo(.5f));
                Assert.That(view.Scene.Cuts.Single().Orientation, Is.EqualTo(HBP.Core.Enums.CutOrientation.Axial));
                var column = view.Scene.Columns.Single();
                Assert.That(view.Scene.ProjectionRequested || view.Scene.AutomaticRecomputeEnabled || view.Scene.IsActivityProjectionComputing, Is.False, "The lab must never request or automatically compute activity/density projection.");
                Assert.That(view.Scene.ProjectionState, Is.EqualTo(ActivityProjectionState.Absent));
                Assert.That(column.ActivityAlpha, Is.Zero);
                var cut = view.Scene.Cuts.Single();
                Assert.That(column.Sites.Where(s => !s.Information.Name.StartsWith("U")).All(s => Vector3.Dot(cut.Normal, s.transform.localPosition - cut.Point) > 0), Is.True, "Controlled layouts must be in the removed half, including the view-aligned depth pair.");
                Assert.That(column.Sites.Count, Is.EqualTo(2025));
                foreach (string prefix in new[] { "S", "R", "N", "D", "C", "U" })
                {
                    // A state-only assertion misses stale GameObjects: let the real rendering loop advance.
                    await UniTask.NextFrame();
                    await UniTask.NextFrame();
                    var expected = column.Sites.Where(s => s.Information.Name.StartsWith(prefix)).ToArray();
                    var visible = column.Sites.Where(s => s.gameObject.activeInHierarchy && s.GetComponent<Renderer>().enabled && !s.GetComponent<Renderer>().forceRenderingOff).ToArray();
                    Assert.That(visible, Is.EquivalentTo(expected), "The displayed layout must change without a later selection or settings event.");
                    Assert.That(column.Sites.Where(s => new QuestSiteTarget(column, s).IsSelectable(view.Scene)), Is.EquivalentTo(expected), "Only displayed sites can be selected.");
                    Assert.That(column.Sites.Where(s => !s.State.IsMasked).All(s => s.Information.Name.StartsWith(prefix)), Is.True);
                    Assert.That(view.Scene.ProjectionRequested || view.Scene.IsActivityProjectionComputing, Is.False, "Changing layouts must keep the anatomy free of activity.");
                    lab.Execute("trial");
                    var target = column.Sites.First(s => !s.State.IsMasked && (prefix != "U" || Vector3.Dot(cut.Normal, s.transform.localPosition - cut.Point) > 0));
                    Assert.That(QuestSiteSelection.TrySelect(view.Scene, new QuestSiteTarget(column, target)), Is.True);
                    if (prefix == "R")
                    {
                        var probe = lab.GetComponentInChildren<QuestSiteProbe>();
                        var status = lab.transform.Find("Campaign/Background/Content Viewport/Content/Information").GetComponent<UnityEngine.UI.Text>();
                        probe.OnSelected.Invoke(target);
                        await UniTask.NextFrame();
                        Assert.That(status.text, Does.Contain("Cible : R2"));
                        lab.Execute("trial");
                        await UniTask.NextFrame();
                        Assert.That(status.text, Does.Contain("Cible : R1"), "Redo must restart the entire row, rather than resume the previous target.");
                        Assert.That(lab.ControlEnabled("confirm") || lab.ControlEnabled("next"), Is.False, "The row advances automatically, without manual validation/skip controls.");
                        foreach (var site in expected.OrderBy(s => s.Information.Name))
                        {
                            Assert.That(QuestSiteSelection.TrySelect(view.Scene, new QuestSiteTarget(column, site)), Is.True);
                            probe.OnSelected.Invoke(site);
                        }

                        await UniTask.NextFrame();
                        Assert.That(status.text, Does.Contain("Parcours terminé"));
                        Assert.That(lab.ControlEnabled("skip"), Is.False);
                    }
                    else lab.Execute("confirm");

                    lab.Execute("layout");
                }

                await UniTask.NextFrame();
                await UniTask.NextFrame();
                Assert.That(column.Sites.Where(s => s.gameObject.activeInHierarchy).Select(s => s.Information.Name), Is.EquivalentTo(new[] { "S1" }), "Cycling back restores the isolated layout.");
                var positions = column.Sites.Select(s => s.transform.position).ToArray();

                lab.Execute("defaults");
                Assert.That(lab.Settings.CursorStyle, Is.EqualTo(QuestCursorStyle.Halo));
                Assert.That(lab.Settings.CursorGuideRadius, Is.EqualTo(.004f));
                Assert.That(lab.Settings.CursorOffset, Is.EqualTo(new Vector3(0, 0, .03f)));
                lab.Execute("profile"); // The strict reference remains available after the accepted default.
                lab.Execute("save-a");
                lab.Execute("profile");
                lab.Execute("save-b");
                lab.Execute("ab");
                Assert.That(lab.Settings.Mode, Is.EqualTo(QuestSelectionMode.Contact));
                Assert.That(lab.Settings.CandidateCue, Is.False, "After saving B, the first comparison recalls A.");
                await UniTask.NextFrame();
                Assert.That(column.Sites.Select(s => s.transform.position), Is.EqualTo(positions), "Selection profiles must not move the sites.");
                Assert.That(column.Sites.Where(s => s.gameObject.activeInHierarchy).Select(s => s.Information.Name), Is.EquivalentTo(new[] { "S1" }));
                Assert.That(lab.GetComponentsInChildren<QuestSelectionLabControl>(true).All(c => c.GetComponentInChildren<UnityEngine.UI.Text>(true).font != null), Is.True);
                lab.Settings.CursorStyle = QuestCursorStyle.Sphere;
                lab.Execute("offset-reset");
                lab.Execute("cursor-style");
                lab.Execute("offset-preset");
                Assert.That(lab.Settings.CursorStyle, Is.EqualTo(QuestCursorStyle.Halo));
                Assert.That(lab.Settings.CursorOffset, Is.EqualTo(new Vector3(0, .03f, .02f)));
                lab.Execute("save-a");
                lab.Execute("cursor-style");
                lab.Execute("offset-preset");
                lab.Execute("save-b");
                lab.Execute("ab");
                Assert.That(lab.Settings.CursorStyle, Is.EqualTo(QuestCursorStyle.Halo));
                Assert.That(lab.Settings.CursorOffset, Is.EqualTo(new Vector3(0, .03f, .02f)));
                lab.Execute("profile");
                Assert.That(lab.Settings.CursorStyle, Is.EqualTo(QuestCursorStyle.Halo), "Changing selection profiles must preserve cursor appearance and placement.");
                Assert.That(lab.Settings.CursorOffset, Is.EqualTo(new Vector3(0, .03f, .02f)));
                var cursorProbe = lab.GetComponentInChildren<QuestSiteProbe>();
                lab.Execute("offset-x");
                var keypad = lab.GetComponentInChildren<QuestNumericKeypad>(true);
                Assert.That(keypad.IsOpen, Is.True);
                keypad.Enter("1");
                keypad.Enter("2");
                keypad.Enter("+/-");
                keypad.Apply();
                Assert.That(keypad.IsOpen, Is.False);
                Assert.That(lab.Settings.CursorOffset.x, Is.EqualTo(-.012f), "The real keypad must accept signed offsets in millimetres.");
                var feedback = lab.GetComponentInChildren<QuestSelectionFeedback>();
                var feedbackFields = new SerializedObject(feedback);
                var backs = Enumerable.Range(0, 4).Select(i => (LineRenderer)feedbackFields.FindProperty("cursorBack").GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
                var fronts = Enumerable.Range(0, 4).Select(i => (LineRenderer)feedbackFields.FindProperty("cursorFront").GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
                Assert.That(feedbackFields.FindProperty("cursorCore").objectReferenceValue, Is.Not.Null);
                var point = new Vector3(.2f, .4f, .6f);
                typeof(QuestSiteProbe).GetField("<ProbePosition>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(cursorProbe, point);
                typeof(QuestSiteProbe).GetField("<CursorPose>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(cursorProbe, new Pose(point, Quaternion.identity));
                typeof(QuestSiteProbe).GetField("<IsActive>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(cursorProbe, true);
                foreach (var style in new[] { QuestCursorStyle.Sphere, QuestCursorStyle.Halo, QuestCursorStyle.Cage, QuestCursorStyle.Stylus })
                {
                    lab.Settings.CursorStyle = style;
                    feedback.Refresh(Camera.main);
                    int expected = style == QuestCursorStyle.Sphere ? 0 : style == QuestCursorStyle.Cage ? 3 : style == QuestCursorStyle.Stylus ? 2 : 1;
                    Assert.That(backs.Count(l => l.enabled), Is.EqualTo(expected));
                    Assert.That(fronts.Count(l => l.enabled), Is.EqualTo(expected));
                    if (expected > 0)
                    {
                        Assert.That(Vector3.Distance(backs[0].GetPosition(0), point), Is.EqualTo(lab.Settings.CursorGuideRadius).Within(.000001f));
                        Assert.That(backs[0].GetComponent<Collider>(), Is.Null, "Visual aids must not add selectable or blocking geometry.");
                        Assert.That(backs[0].sharedMaterial.GetFloat("_ZWrite"), Is.Zero, "The black outline must not hide the coloured stroke with its own depth.");
                        Assert.That(fronts[0].sharedMaterial.renderQueue, Is.GreaterThan(backs[0].sharedMaterial.renderQueue), "Draw all outlines before the coloured strokes.");
                        Assert.That(fronts[0].sharedMaterial.shader.name, Is.EqualTo("Universal Render Pipeline/Unlit"), "Use normal depth-tested unlit rendering, rather than the always-visible UI overlay.");
                    }

                    if (style == QuestCursorStyle.Stylus)
                    {
                        Assert.That(backs[3].GetPosition(1), Is.EqualTo(point), "The stylus tip is the actual contact point.");
                        Assert.That(Vector3.Distance(backs[3].GetPosition(0), point), Is.EqualTo(.04f).Within(.000001f));
                    }
                }

                cursorProbe.Cancel();
                feedback.Refresh(Camera.main);
                Assert.That(fronts.Concat(backs).Any(l => l.enabled), Is.False, "Release, focus loss and tracking loss hide every cursor aid.");
                await view.ClearAsync();
                Assert.That(view.Scenes, Is.Empty);
            }
            finally
            {
                if (view != null) await view.ClearAsync();
                await SceneManager.UnloadSceneAsync(scene);
            }
        }

        private sealed class Targets : IDisposable
        {
            public readonly GameObject Root = new("Assistance test targets");
            private readonly GameObject data = new("Inactive column");
            private readonly Column3D column;
            public readonly List<QuestSiteTarget> Items = new();

            public Targets()
            {
                data.SetActive(false);
                column = data.AddComponent<Column3DAnatomy>();
                typeof(Column3D).GetField("<ColumnData>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(column, new AnatomicColumn("Test", new BaseConfiguration(), new AnatomicConfiguration(), "test-column"));
            }

            public QuestSiteTarget Add(string name, Vector3 position)
            {
                var go = new GameObject(name, typeof(SphereCollider), typeof(Site));
                go.transform.SetParent(Root.transform, false);
                go.transform.localPosition = position;
                go.GetComponent<SphereCollider>().radius = .001f;
                var site = go.GetComponent<Site>();
                site.Information = new SiteInformation { Name = name, Patient = new Patient { ID = "test" } };
                var target = new QuestSiteTarget(column, site);
                Items.Add(target);
                return target;
            }

            public void Dispose()
            {
                Object.DestroyImmediate(Root);
                Object.DestroyImmediate(data);
            }
        }
    }
}
#endif
