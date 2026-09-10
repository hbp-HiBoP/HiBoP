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
using HBP.Core.Object3D;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Transfer.Anatomy;
using HBP.Transfer.Anatomy.Desktop;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HBP.Tests.Transfer.Anatomy.Desktop
{
    public class DesktopAnatomyCaptureTests
    {
        [Test]
        public void SelectionAvailability_UsesTheCaptureRules()
        {
            Assert.That(DesktopAnatomyCapture.GetSelectionError(), Is.Null);
            m_Scene.SceneInformation.GeometryNeedsUpdate = true;
            Assert.That(DesktopAnatomyCapture.GetSelectionError(), Does.Contain("preparing"));
            m_Scene.SceneInformation.GeometryNeedsUpdate = false;
            m_Scene.SceneInformation.CutsNeedUpdate = false;
            m_Materials.BrainMaterial.SetFloat("_Atlas", 1);
            Assert.That(DesktopAnatomyCapture.GetSelectionError(), Does.Contain("coloration"));
        }

        [Test]
        public async Task DeliveryOffer_FreezesTheSelectedCaptureAndKeepsItsIdentity()
        {
            var expected = await DesktopAnatomyCapture.CaptureSelectedAsync("delivery", "session", 1);
            var preparing = DesktopAnatomyCapture.CaptureDeliverySelectedAsync("delivery", "session", 1);
            m_Mesh.vertices = new[] { Vector3.zero, Vector3.one, Vector3.up };
            var offer = await preparing;
            byte[] encoded = AnatomySnapshotCodec.Encode(expected);
            Assert.That(offer.TransferId, Is.EqualTo("delivery"));
            Assert.That(offer.SessionId, Is.EqualTo("session"));
            Assert.That(offer.EncodedBytes, Is.EqualTo(encoded.Length));
            Assert.That(offer.ContentHash, Is.EqualTo(new HBP.Transfer.Transport.DeliveryReceipt(HBP.Transfer.Transport.TransportIdentity.Hash(encoded), HBP.Transfer.Transport.DeliveryStatus.Published).ContentHash));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task DensityWorkerCompletesLifetimeOnFailureOrInvalidation(bool invalidate)
        {
            SetField(m_Scene, "m_ROIManager", m_Root.AddComponent<ROIManager>());
            typeof(Column3D).GetProperty("Sites").SetValue(m_Column, new List<HBP.Core.Object3D.Site>());
            // Deliberately inconsistent count fails in the common path after dispatch.
            m_Column.RawElectrodes.AddSite("S1", Vector3.zero, 0, 0);
            using var density = new HBP.Core.DLL.DensityGenerator();
            typeof(Column3D).GetProperty("ActivityGenerator").SetValue(m_Column, density);
            m_Scene.SceneInformation.GeneratorNeedsUpdate = false;
            var notifications = new List<bool>();
            int progressCount = 0;
            bool parameterApplied = false;
            m_Scene.OnProgressUpdateGenerator.AddListener((_, _) => progressCount++);
            m_Scene.OnUpdatingGenerators.AddListener(value =>
            {
                notifications.Add(value);
                if (value)
                {
                    typeof(Base3DScene).GetMethod("UpdateGeneratorParameters", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(m_Scene, new object[] { (Action)(() => parameterApplied = true) });
                    Assert.That(parameterApplied, Is.False, "Parameter writes must wait for the worker.");
                }

                if (value && invalidate) m_Scene.SceneInformation.GeneratorNeedsUpdate = true;
            });
            Exception error = null;
            try
            {
                var operation = (Cysharp.Threading.Tasks.UniTask)typeof(Base3DScene).GetMethod("ComputeGeneratorsAsync", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(m_Scene, null);
                await operation;
            }
            catch (Exception exception)
            {
                error = exception;
            }

            Assert.That(error, invalidate ? Is.Null : Is.TypeOf<ArgumentException>());
            Assert.That(typeof(Base3DScene).GetField("m_UpdatingGenerators", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(m_Scene), Is.False);
            Assert.That(((UniTask)typeof(Base3DScene).GetField("m_GeneratorWork", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(m_Scene)).Status.IsCompleted(), Is.True);
            Assert.That(notifications, Is.EqualTo(new[] { true, false }));
            Assert.That(parameterApplied, Is.True, "Deferred parameters apply even on failure/invalidation.");
            Assert.That(m_Scene.IsGeneratorUpToDate, Is.False);
            int completedProgressCount = progressCount;
            await Task.Delay(150);
            Assert.That(progressCount, Is.EqualTo(completedProgressCount), "The progress monitor must stop on every exit.");
        }

        private GameObject m_Root;
        private Base3DScene m_Scene;
        private Column3DAnatomy m_Column;
        private Mesh m_Mesh;
        private Texture2D m_Texture;
        private BrainMaterials m_Materials;
        private HBP.Core.DLL.Surface m_Surface;
        private object m_PreviousModule;
        private object m_PreviousPreferences;
        private HBP.Core.DLL.Volume m_Volume;
        private string m_VolumePath;

        [SetUp]
        public void SetUp()
        {
            m_PreviousModule = typeof(Singleton<Module3DMain>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            m_Root = new GameObject("Anatomy capture test");
            m_Root.SetActive(false);
            var preferencesField = typeof(Singleton<PersistentDataManager>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            m_PreviousPreferences = preferencesField.GetValue(null);
            var preferences = m_Root.AddComponent<PersistentDataManager>();
            preferencesField.SetValue(null, preferences);
            SetField(preferences, "m_UserPreferences", new UserPreferences());
            Module3DMain module = m_Root.AddComponent<Module3DMain>();
            typeof(Singleton<Module3DMain>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, module);
            m_Scene = m_Root.AddComponent<Base3DScene>();
            m_Column = m_Root.AddComponent<Column3DAnatomy>();
            m_Scene.Columns.Add(m_Column);
            SetField(m_Scene, "m_IsSelected", true);
            SetField(m_Column, "m_IsSelected", true);
            SetField(module, "m_Scenes", new List<Base3DScene> { m_Scene });
            SetField(m_Scene, "<Visualization>k__BackingField", new Visualization { ID = "viz opaque" });
            SetField(m_Column, "<ColumnData>k__BackingField", new AnatomicColumn("Selected anatomy", new BaseConfiguration(), new AnatomicConfiguration(), "column opaque"));
            m_Scene.SceneInformation.CompletelyLoaded = true;
            m_Scene.SceneInformation.ProjectionGridNeedsUpdate = false;
            m_Scene.SceneInformation.SurfaceProjectionNeedsUpdate = false;
            var mriManager = m_Root.AddComponent<MRIManager>();
            SetField(m_Scene, "m_MRIManager", mriManager);
            m_VolumePath = Path.Combine(Application.temporaryCachePath, "quest017-capture-" + Guid.NewGuid().ToString("N") + ".nii");
            using (var stream = new FileStream(m_VolumePath, FileMode.CreateNew))
            using (var writer = new BinaryWriter(stream))
            {
                stream.SetLength(376);
                writer.Write(348);
                stream.Position = 40;
                writer.Write((short)3);
                writer.Write((short)2);
                writer.Write((short)3);
                writer.Write((short)4);
                stream.Position = 70;
                writer.Write((short)2);
                writer.Write((short)8);
                stream.Position = 80;
                writer.Write(1f);
                writer.Write(2f);
                writer.Write(3f);
                stream.Position = 108;
                writer.Write(352f);
                stream.Position = 344;
                writer.Write(new byte[] { (byte)'n', (byte)'+', (byte)'1', 0 });
                stream.Position = 352;
                writer.Write(Enumerable.Range(0, 24).Select(i => (byte)i).ToArray());
            }

            m_Volume = new HBP.Core.DLL.Volume();
            Assert.That(m_Volume.LoadNIFTIFile(m_VolumePath), Is.True);
            mriManager.MRIs.Add(new MRI3D("MNI", m_Volume));
            MeshManager manager = m_Root.AddComponent<MeshManager>();
            SetField(m_Scene, "m_MeshManager", manager);
            SetField(m_Scene, "m_TriangleEraser", m_Root.AddComponent<TriangleEraser>());
            m_Mesh = new Mesh
            {
                vertices = new[] { new Vector3(-12, 2, 3), new Vector3(4, 5, 6), new Vector3(7, 18, 9) },
                normals = new[] { Vector3.forward, Vector3.up, Vector3.right },
                triangles = new[] { 2, 0, 1 },
                uv = new[] { new Vector2(0.1f, 0.2f), Vector2.zero, Vector2.one },
                uv2 = new[] { Vector2.one, Vector2.one, Vector2.one }
            };
            m_Surface = new HBP.Core.DLL.Surface();
            m_Surface.SetBuffers(m_Mesh.vertices, m_Mesh.triangles, m_Mesh.normals, m_Mesh.uv);
            manager.Meshes.Add(new LeftRightMesh3D("Test MNI", m_Surface, m_Surface, m_Surface, MeshType.MNI));
            SetField(manager, "<BrainSurface>k__BackingField", m_Surface);
            m_Root.AddComponent<MeshFilter>().sharedMesh = m_Mesh;
            m_Materials = new BrainMaterials();
            SetField(m_Scene, "<BrainMaterials>k__BackingField", m_Materials);
            m_Texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            m_Texture.SetPixel(0, 0, new Color(0.5f, 0.25f, 0.75f));
            m_Texture.Apply();
            m_Materials.SetBrainColorTexture(m_Texture);
            m_Materials.SetCuts(new List<HBP.Core.Object3D.Cut>(), 1, Quaternion.identity);
            m_Root.AddComponent<MeshRenderer>().sharedMaterial = m_Materials.BrainMaterial;
            SetField(m_Column, "<BrainMesh>k__BackingField", m_Root);
        }

        [TearDown]
        public void TearDown()
        {
            LeftRightMesh3D anatomy = (LeftRightMesh3D)m_Scene.MeshManager.Meshes[0];
            anatomy.SimplifiedLeft.Dispose();
            anatomy.SimplifiedRight.Dispose();
            anatomy.SimplifiedBoth.Dispose();
            Object.DestroyImmediate(m_Root);
            Object.DestroyImmediate(m_Mesh);
            Object.DestroyImmediate(m_Texture);
            foreach (FieldInfo field in typeof(BrainMaterials).GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                if (field.GetValue(m_Materials) is Material material)
                    Object.DestroyImmediate(material);
            m_Surface.Dispose();
            m_Volume.Dispose();
            File.Delete(m_VolumePath);
            typeof(Singleton<PersistentDataManager>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, m_PreviousPreferences);
            typeof(Singleton<Module3DMain>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, m_PreviousModule);
        }

        [Test]
        public async Task Capture_FreezesBuffersAndIdsBeforeReturningTask()
        {
            Vector3[] original = m_Mesh.vertices;
            Task<AnatomySnapshot> pending = Capture();
            m_Mesh.vertices = new[] { Vector3.zero, Vector3.one, Vector3.up };
            m_Column.ColumnData.ID = "edited-column";
            m_Scene.Visualization.ID = "edited-viz";
            m_Root.transform.position = new Vector3(3000, 70, -12);
            AnatomySnapshot snapshot = await pending;
            Assert.That(snapshot.VisualizationId, Is.EqualTo("viz opaque"));
            Assert.That(snapshot.ColumnId, Is.EqualTo("column opaque"));
            Assert.That(snapshot.Positions.ToArray(), Is.EqualTo(original.SelectMany(v => new[] { v.x, v.y, v.z }).ToArray()));
            Assert.That(snapshot.Indices.ToArray(), Is.EqualTo(new uint[] { 2, 0, 1 }));
            Assert.That(snapshot.Coordinates.AssetToBrain.ToArray(), Is.EqualTo(new float[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 }));
            Assert.That(snapshot.Coordinates.Unit, Is.EqualTo(AnatomyLengthUnit.Millimeter));
            Assert.That(snapshot.Winding, Is.EqualTo(AnatomyWinding.Clockwise));
        }

        [Test]
        public async Task Capture_UnchangedContentIsBitExactAndAppearanceIsLinear()
        {
            byte[] first = AnatomySnapshotCodec.Encode(await Capture());
            AnatomySnapshot snapshot = await Capture();
            Assert.That(AnatomySnapshotCodec.Encode(snapshot), Is.EqualTo(first));
            Color expected = ((Color)m_Texture.GetPixels32()[0]).linear;
            Assert.That(snapshot.Color.ToArray(), Is.EqualTo(new[] { expected.r, expected.g, expected.b, 1f }));
            Assert.That(m_Root.transform.position, Is.EqualTo(Vector3.zero));
        }

        [TestCase("selection")]
        [TestCase("column-type")]
        [TestCase("geometry")]
        [TestCase("functional")]
        [TestCase("transition")]
        [TestCase("hemisphere")]
        [TestCase("erasure")]
        [TestCase("deformation")]
        [TestCase("density")]
        [TestCase("opacity-transform")]
        [TestCase("palette")]
        [TestCase("property-block")]
        [TestCase("incomplete")]
        public void Capture_RefusesUnsupportedSelectionWithoutChangingIt(string scenario)
        {
            switch (scenario)
            {
                case "selection": SetField(m_Column, "m_IsSelected", false); break;
                case "column-type":
                    SetField(m_Column, "m_IsSelected", false);
                    Column3DIEEG other = m_Root.AddComponent<Column3DIEEG>();
                    m_Scene.Columns.Add(other);
                    SetField(other, "m_IsSelected", true);
                    break;
                case "geometry": m_Scene.SceneInformation.GeometryNeedsUpdate = true; break;
                case "functional": m_Scene.SceneInformation.FunctionalSurfaceNeedsUpdate = true; break;
                case "transition": SetField(m_Scene, "<IsSurfaceRepresentationTransitioning>k__BackingField", true); break;
                case "hemisphere": SetField(m_Scene.MeshManager, "<MeshPartToDisplay>k__BackingField", MeshPart.Left); break;
                case "erasure": SetField(m_Scene.TriangleEraser, "<MeshHasInvisibleTriangles>k__BackingField", true); break;
                case "deformation": m_Materials.BrainMaterial.SetFloat("_Amount", 0.4f); break;
                case "density": m_Mesh.uv2 = new[] { Vector2.zero, Vector2.one, Vector2.one }; break;
                case "opacity-transform": m_Materials.BrainMaterial.SetTextureOffset("_AoTex", new Vector2(0, -1)); break;
                case "palette":
                    m_Texture.Reinitialize(2, 1);
                    m_Texture.SetPixels(new[] { Color.red, Color.blue });
                    m_Texture.Apply();
                    break;
                case "property-block":
                    MaterialPropertyBlock block = new();
                    block.SetColor("_Color", Color.red);
                    m_Root.GetComponent<Renderer>().SetPropertyBlock(block);
                    break;
                case "incomplete": m_Mesh.triangles = Array.Empty<int>(); break;
            }

            Column3D selection = m_Scene.SelectedColumn;
            Assert.Throws<InvalidOperationException>(() => Capture());
            Assert.That(m_Scene.SelectedColumn, Is.SameAs(selection));
        }

        [Test]
        public async Task Capture_RejectsWorkerThread()
        {
            Exception caught = null;
            await Task.Run(async () =>
            {
                try
                {
                    await Capture();
                }
                catch (Exception exception)
                {
                    caught = exception;
                }
            });
            Assert.That(caught, Is.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Capture_RejectsUnrepresentedBoundarySetting()
        {
            var preferences = HBP.Core.Preferences.PersistentDataManager.UserPreferences.Visualization._3D;
            bool previous = preferences.SmoothActivityBoundaries;
            try
            {
                preferences.SmoothActivityBoundaries = false;
                var exception = Assert.Throws<InvalidOperationException>(() => Capture());
                Assert.That(exception.Message, Does.Contain("Smooth activity boundaries"));
            }
            finally
            {
                preferences.SmoothActivityBoundaries = previous;
            }
        }

        [Test]
        public void Capture_RejectsCancellationBeforeReadingSelection()
        {
            Assert.Throws<OperationCanceledException>(() => DesktopAnatomyCapture.CaptureSelectedAsync("transfer", "session", 1, new CancellationToken(true)));
        }

        private Implantation3D AddContacts()
        {
            var patient = new Patient { ID = "patient-stable", Name = "Synthetic" };
            var data = new HBP.Core.Data.Site { ID = "site-stable", Name = "A1" };
            patient.Sites.Add(data);
            m_Scene.Visualization.Patients = new List<Patient> { patient };
            var source = new Implantation3D.SiteInfo { Patient = patient, PatientIndex = 0, SiteData = data, Name = "A1", Electrode = "A", Index = 0, NativePosition = new Vector3(31.25f, -18.5f, 26.75f) };
            var implantation = new Implantation3D("MNI", new List<Implantation3D.SiteInfo> { source }, new[] { patient });
            var manager = m_Root.AddComponent<ImplantationManager>();
            manager.Implantations.Add(implantation);
            SetField(m_Scene, "m_ImplantationManager", manager);
            var go = new GameObject("Prepared contact");
            go.transform.SetParent(m_Root.transform);
            var site = go.AddComponent<HBP.Core.Object3D.Site>();
            site.Information = new SiteInformation { Patient = patient, SiteData = data, Name = "A1", Index = 0, DefaultPosition = source.UnityPosition };
            site.State = new SiteState { Color = new Color(.5f, .25f, .75f), IsFiltered = true };
            site.IsActive = true;
            go.AddComponent<MeshFilter>().sharedMesh = SharedMeshes.Site;
            go.AddComponent<MeshRenderer>().sharedMaterial = m_Materials.BrainMaterial;
            SetField(m_Column, "<Sites>k__BackingField", new List<HBP.Core.Object3D.Site> { site });
            m_Column.RawElectrodes.Dispose();
            SetField(m_Column, "<RawElectrodes>k__BackingField", new HBP.Core.DLL.RawSiteList(implantation.RawSiteList));
            return implantation;
        }

        [Test]
        public async Task Contacts_CaptureUsesScientificPositionsAndFreezesAppearanceAndAssociations()
        {
            var implantation = AddContacts();
            try
            {
                var site = m_Column.Sites[0];
                site.transform.localPosition = new Vector3(999, 888, 777);
                site.transform.localScale = Vector3.one * 3;
                m_Root.transform.SetPositionAndRotation(new Vector3(9000, -100, 40), Quaternion.Euler(15, 30, 60));
                Task<AnatomySnapshot> pending = Capture();
                site.Information.SiteData.ID = "edited";
                site.Information.DefaultPosition = Vector3.zero;
                site.State.IsBlackListed = true;
                site.transform.localScale = Vector3.one * 10;
                var contacts = (await pending).Contacts;
                Assert.That(contacts.Sites[0].Id, Is.EqualTo("site-stable"));
                Assert.That(contacts.Sites[0].Position.ToArray(), Is.EqualTo(new[] { -31.25f, -18.5f, 26.75f }));
                Assert.That(contacts.Sites[0].Diameter, Is.EqualTo(6));
                Assert.That(contacts.Sites[0].EffectiveMasked, Is.False);
                Assert.That(contacts.PatientIds, Is.EqualTo(new[] { "patient-stable" }));
                var position = contacts.Sites[0].Position;
                var native = new Vector3(HBP.Core.DLL.ReferenceSystemConversion.ConvertX(position[0]), position[1], position[2]);
                Assert.That(native, Is.EqualTo(implantation.SiteInfos[0].NativePosition));
                using var reconstructed = new HBP.Core.DLL.RawSiteList();
                reconstructed.SetPatients(new[] { new Patient { ID = contacts.PatientIds[0] } });
                reconstructed.AddSite(contacts.Sites[0].Name, native, contacts.Sites[0].PatientIndex, contacts.Sites[0].SourceIndex);
                Assert.That(reconstructed.NumberOfSites, Is.EqualTo(1));
            }
            finally
            {
                implantation.Clean();
            }
        }

        [TestCase("order")]
        [TestCase("association")]
        [TestCase("pending")]
        [TestCase("frame")]
        public void Contacts_RejectsInconsistentPreparedState(string scenario)
        {
            var implantation = AddContacts();
            try
            {
                if (scenario == "order") m_Column.Sites[0].Information.Index = 1;
                if (scenario == "association") implantation.SiteInfos[0].PatientIndex = 1;
                if (scenario == "pending") m_Scene.SceneInformation.SitesNeedUpdate = true;
                if (scenario == "frame") implantation.Name = "Patient";
                Assert.Throws<InvalidOperationException>(() => Capture());
            }
            finally
            {
                implantation.Clean();
            }
        }

        [Test]
        public async Task Contacts_AllMaskFlagCombinationsUseTheSharedDesktopRule()
        {
            var implantation = AddContacts();
            try
            {
                var state = m_Column.Sites[0].State;
                for (int bits = 0; bits < 16; bits++)
                {
                    state.IsMasked = (bits & 1) != 0;
                    state.IsBlackListed = (bits & 2) != 0;
                    state.IsOutOfROI = (bits & 4) != 0;
                    state.IsFiltered = (bits & 8) != 0;
                    var site = (await Capture()).Contacts.Sites[0];
                    Assert.That((byte)site.Flags, Is.EqualTo(bits));
                    Assert.That(site.EffectiveMasked, Is.EqualTo((bits & 1) != 0 || (bits & 2) != 0 || (bits & 8) == 0));
                    Assert.That(state.IsEffectivelyMasked(true), Is.EqualTo((bits & 1) != 0 || (bits & 2) != 0 || (bits & 4) != 0 || (bits & 8) == 0));
                }
            }
            finally
            {
                implantation.Clean();
            }
        }

        [Test]
        public void Projection_MissingReferenceIsRejected()
        {
            m_Scene.MRIManager.MRIs.Clear();
            Assert.Throws<InvalidOperationException>(() => Capture());
        }

        [Test]
        public async Task Projection_SourceChangesAreRejectedEvenWithIdenticalDimensions()
        {
            byte[] changed = File.ReadAllBytes(m_VolumePath);
            changed[352] ^= 1;
            File.WriteAllBytes(m_VolumePath, changed);
            Exception caught = null;
            try
            {
                await Capture();
            }
            catch (Exception exception)
            {
                caught = exception;
            }

            Assert.That(caught, Is.TypeOf<InvalidOperationException>());
            Assert.That(caught.Message, Does.Contain("changed since native loading"));
        }

        [Test]
        public async Task Projection_CapturesActualSettingsAndImmutableBytes()
        {
            m_Column.AnatomyParameters.InfluenceDistance = 23.5f;
            m_Column.ActivityAlpha = .25f;
            var snapshot = await Capture();
            Assert.That(snapshot.SchemaVersion, Is.EqualTo(3));
            Assert.That(snapshot.Projection.VolumeBytes.ToArray(), Is.EqualTo(File.ReadAllBytes(m_VolumePath)));
            Assert.That(snapshot.Projection.InfluenceDistance, Is.EqualTo(23.5f));
            Assert.That(snapshot.Projection.ActivityAlpha, Is.EqualTo(.25f));
            Assert.That(snapshot.Projection.GridDimension, Is.EqualTo(HBP.Core.DLL.ActivityProjectionSettings.VolumeGridDimension));
            Assert.That(snapshot.Projection.Interpolation, Is.EqualTo((int)HBP.Core.DLL.ActivityProjectionSettings.VolumeInterpolation));
            Assert.That(snapshot.Projection.InfluenceByDistance, Is.EqualTo((int)PersistentDataManager.UserPreferences.Visualization._3D.SiteInfluenceByDistance));
        }

        private static Task<AnatomySnapshot> Capture() => DesktopAnatomyCapture.CaptureSelectedAsync("transfer", "session", 1);

        private static void SetField(object target, string name, object value)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field == null) continue;
                field.SetValue(target, value);
                return;
            }

            throw new MissingFieldException(name);
        }
    }
}
