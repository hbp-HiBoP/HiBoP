using System;
using System.IO;
using System.Reflection;
using System.Linq;
using HBP.Core.DLL;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using NUnit.Framework;
using UnityEngine;
using CoreVolume = HBP.Core.DLL.Volume;

namespace HBP.Tests.Transfer.Scene
{
    [Category("Sync.SceneFocused")]
    public sealed class MRIManagerPublicationSelectionTests
    {
        [Test]
        public void MRISelectionDuringInitialPublication_AbortsDeliveryAndRequestsFreshCapture()
        {
            GameObject root = new("MRI publication selection test");
            var scene = root.AddComponent<Base3DScene>();
            var manager = root.AddComponent<MRIManager>();
            CoreVolume firstVolume = null;
            CoreVolume secondVolume = null;
            BrainMaterials brainMaterials = null;
            string firstPath = Path.Combine(Application.temporaryCachePath, "sync-mri-select-a-" + Guid.NewGuid().ToString("N") + ".nii");
            string secondPath = Path.Combine(Application.temporaryCachePath, "sync-mri-select-b-" + Guid.NewGuid().ToString("N") + ".nii");
            IDisposable publicationOwner = null;
            string stage = "creating MRI fixtures";

            try
            {
                CreateMinimalNifti(firstPath);
                CreateMinimalNifti(secondPath);
                stage = "loading MRI fixtures";
                firstVolume = LoadVolume(firstPath);
                secondVolume = LoadVolume(secondPath);
                manager.MRIs.Add(new MRI3D("MRI A", firstVolume));
                manager.MRIs.Add(new MRI3D("MRI B", secondVolume));
                stage = "wiring scene references";
                SetPrivateField(manager, "m_Scene", scene);
                SetPrivateField(scene, "m_MRIManager", manager);
                SetPrivateField(scene, "<Visualization>k__BackingField", new HBP.Core.Data.Visualization { ID = Guid.NewGuid().ToString("N") });
                brainMaterials = new BrainMaterials();
                typeof(Base3DScene).GetProperty(nameof(Base3DScene.BrainMaterials)).SetValue(scene, brainMaterials);

                stage = "resolving the v2 publication owner";
                Type ownerType = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("HBP.Quest.Desktop.DesktopV2ReplicaSession", throwOnError: false)).FirstOrDefault(type => type != null);
                Assert.That(ownerType, Is.Not.Null, "The v2 Desktop publication owner must be loaded for this integration test.");
                var constructor = ownerType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Base3DScene), typeof(string), typeof(string) }, null);
                Assert.That(constructor, Is.Not.Null);
                stage = "constructing the v2 publication owner";
                publicationOwner = (IDisposable)constructor.Invoke(new object[] { scene, Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N") });

                stage = "selecting the alternate MRI during publication";
                manager.Select("MRI B");

                Assert.That(manager.SelectedMRIID, Is.EqualTo(1));
                Assert.That((bool)ownerType.GetProperty("RequiresPublicationRestart").GetValue(publicationOwner), Is.True);
                var abortToken = (System.Threading.CancellationToken)ownerType.GetProperty("PublicationAbortToken").GetValue(publicationOwner);
                Assert.That(abortToken.IsCancellationRequested, Is.True, "Changing the selected MRI during the initial delivery must abort it before publication completes.");
            }
            catch (Exception exception)
            {
                Assert.Fail($"Failure while {stage}: {exception}");
            }
            finally
            {
                publicationOwner?.Dispose();
                firstVolume?.Dispose();
                secondVolume?.Dispose();
                DestroyTestBrainMaterials(brainMaterials);
                UnityEngine.Object.DestroyImmediate(root);
                if (File.Exists(firstPath)) File.Delete(firstPath);
                if (File.Exists(secondPath)) File.Delete(secondPath);
            }
        }

        [Test]
        public void MeshSelectionDuringInitialPublication_AbortsDeliveryAndRequestsFreshCapture()
        {
            using var fixture = new PublicationOwnerFixture();
            fixture.MeshManager.Meshes.Add(null);
            fixture.MeshManager.Meshes.Add(null);

            MethodInfo selectAtIndex = typeof(MeshManager).GetMethod("SelectAtIndex", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(selectAtIndex, Is.Not.Null);
            TargetInvocationException selectionFailure = Assert.Throws<TargetInvocationException>(() => selectAtIndex.Invoke(fixture.MeshManager, new object[] { 1 }));

            Assert.That(selectionFailure.InnerException, Is.TypeOf<NullReferenceException>(), "The fixture intentionally stops after the manager raises its selection event.");
            fixture.AssertRestartRequested();
        }

        [Test]
        public void ImplantationSelectionDuringInitialPublication_AbortsDeliveryAndRequestsFreshCapture()
        {
            using var fixture = new PublicationOwnerFixture();
            var first = new Implantation3D("Implantation A", new System.Collections.Generic.List<Implantation3D.SiteInfo>(), Array.Empty<HBP.Core.Data.Patient>());
            var second = new Implantation3D("Implantation B", new System.Collections.Generic.List<Implantation3D.SiteInfo>(), Array.Empty<HBP.Core.Data.Patient>());
            fixture.ImplantationManager.Implantations.Add(first);
            fixture.ImplantationManager.Implantations.Add(second);

            Assert.Throws<NullReferenceException>(() => fixture.ImplantationManager.SelectPrepared(second));

            fixture.AssertRestartRequested();
        }

        [Test]
        public void ReconnectGraceExpiryDuringInitialPublication_ClosesOwnerBeforeItCanReportLive()
        {
            using var fixture = new PublicationOwnerFixture();
            fixture.SetAwaitingInitialBarrier();
            fixture.ReportConnectionClosed("The Quest v2 replica reconnect grace expired.");

            Assert.That(fixture.IsLive, Is.False);
            Assert.That(fixture.IsClosed, Is.True);
            Assert.That(fixture.RequiresPublicationRestart, Is.False);
            Assert.That(fixture.PublicationAbortRequested, Is.True);
        }

        private static CoreVolume LoadVolume(string path)
        {
            var volume = new CoreVolume();
            Assert.That(volume.LoadNIFTIFile(path), Is.True);
            return volume;
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {target.GetType().Name}.{name}.");
            field.SetValue(target, value);
        }

        private static void DestroyTestBrainMaterials(BrainMaterials brainMaterials)
        {
            if (brainMaterials == null) return;
            foreach (string fieldName in new[] { "m_Brain", "m_TransparentBrain", "m_Cut", "m_TransparentCut" })
            {
                Material material = (Material)typeof(BrainMaterials).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(brainMaterials);
                if (material) UnityEngine.Object.DestroyImmediate(material);
            }
        }

        private static void CreateMinimalNifti(string path)
        {
            using var stream = new FileStream(path, FileMode.CreateNew);
            using var writer = new BinaryWriter(stream);
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

        private sealed class PublicationOwnerFixture : IDisposable
        {
            private readonly GameObject m_Root = new("Resource publication selection test");
            private readonly BrainMaterials m_BrainMaterials;
            private readonly IDisposable m_PublicationOwner;
            private readonly Type m_OwnerType;

            public MeshManager MeshManager { get; }
            public ImplantationManager ImplantationManager { get; }
            public bool IsLive => (bool)m_OwnerType.GetProperty("IsLive").GetValue(m_PublicationOwner);
            public bool IsClosed => (bool)m_OwnerType.GetProperty("IsClosed").GetValue(m_PublicationOwner);
            public bool RequiresPublicationRestart => (bool)m_OwnerType.GetProperty("RequiresPublicationRestart").GetValue(m_PublicationOwner);
            public bool PublicationAbortRequested => ((System.Threading.CancellationToken)m_OwnerType.GetProperty("PublicationAbortToken").GetValue(m_PublicationOwner)).IsCancellationRequested;

            public PublicationOwnerFixture()
            {
                var scene = m_Root.AddComponent<Base3DScene>();
                var mriManager = m_Root.AddComponent<MRIManager>();
                MeshManager = m_Root.AddComponent<MeshManager>();
                ImplantationManager = m_Root.AddComponent<ImplantationManager>();
                SetPrivateField(mriManager, "m_Scene", scene);
                SetPrivateField(scene, "m_MRIManager", mriManager);
                SetPrivateField(scene, "m_MeshManager", MeshManager);
                SetPrivateField(scene, "m_ImplantationManager", ImplantationManager);
                SetPrivateField(scene, "<Visualization>k__BackingField", new HBP.Core.Data.Visualization { ID = Guid.NewGuid().ToString("N") });
                m_BrainMaterials = new BrainMaterials();
                typeof(Base3DScene).GetProperty(nameof(Base3DScene.BrainMaterials)).SetValue(scene, m_BrainMaterials);

                m_OwnerType = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("HBP.Quest.Desktop.DesktopV2ReplicaSession", throwOnError: false)).FirstOrDefault(type => type != null);
                Assert.That(m_OwnerType, Is.Not.Null, "The v2 Desktop publication owner must be loaded for this integration test.");
                var constructor = m_OwnerType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Base3DScene), typeof(string), typeof(string) }, null);
                Assert.That(constructor, Is.Not.Null);
                m_PublicationOwner = (IDisposable)constructor.Invoke(new object[] { scene, Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N") });
            }

            public void AssertRestartRequested()
            {
                Assert.That((bool)m_OwnerType.GetProperty("RequiresPublicationRestart").GetValue(m_PublicationOwner), Is.True);
                var abortToken = (System.Threading.CancellationToken)m_OwnerType.GetProperty("PublicationAbortToken").GetValue(m_PublicationOwner);
                Assert.That(abortToken.IsCancellationRequested, Is.True, "Changing a prepared resource during delivery must abort it before publication completes.");
            }

            public void SetAwaitingInitialBarrier()
            {
                FieldInfo stateField = m_OwnerType.GetField("m_State", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(stateField, Is.Not.Null);
                stateField.SetValue(m_PublicationOwner, Enum.Parse(stateField.FieldType, "AwaitingQuestApply"));
            }

            public void ReportConnectionClosed(string reason)
            {
                MethodInfo close = m_OwnerType.GetMethod("MarkConnectionClosed", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(close, Is.Not.Null);
                close.Invoke(m_PublicationOwner, new object[] { reason });
            }

            public void Dispose()
            {
                m_PublicationOwner.Dispose();
                DestroyTestBrainMaterials(m_BrainMaterials);
                UnityEngine.Object.DestroyImmediate(m_Root);
            }
        }
    }
}
