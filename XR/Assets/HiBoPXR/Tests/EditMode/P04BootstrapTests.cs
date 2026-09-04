using System.Linq;
using CRNL.HiBoP.XR.Bootstrap.Editor;
using CRNL.HiBoP.XR.Bootstrap.Meta;
using NUnit.Framework;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;

namespace CRNL.HiBoP.XR.Bootstrap.Tests
{
    public sealed class P04BootstrapTests
    {
        [OneTimeSetUp]
        public void ApplyProjectSetup()
        {
            P04ProjectSetup.Apply();
        }

        [Test]
        public void ProjectSettingsMatchDecisionLock()
        {
            Assert.DoesNotThrow(P04ProjectSetup.Validate);
        }

        [Test]
        public void PrefabOwnsTheCompleteBootstrapHierarchy()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(P04ProjectSetup.PrefabPath);
            try
            {
                Assert.That(root.GetComponent<P04BootstrapController>(), Is.Not.Null);
                Assert.That(root.GetComponent<MetaOpenXRPassthroughProvider>(), Is.Not.Null);
                Assert.That(root.GetComponent<P04MetaDisplayConfigurator>(), Is.Not.Null);
                Assert.That(root.GetComponentInChildren<XROrigin>(true), Is.Not.Null);
                Assert.That(root.GetComponentInChildren<ARSession>(true), Is.Not.Null);
                Assert.That(root.GetComponentInChildren<ARCameraManager>(true), Is.Not.Null);
                Assert.That(root.GetComponentInChildren<TrackedPoseDriver>(true), Is.Not.Null);
                Assert.That(root.GetComponentsInChildren<P04DevicePoseTracker>(true).Length, Is.EqualTo(3));
                Assert.That(root.GetComponentsInChildren<P04HandWristTracker>(true).Length, Is.EqualTo(2));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void DiagnosticSceneContainsOnlyTheBootstrapPrefabInstance()
        {
            var scene = EditorSceneManager.OpenScene(P04ProjectSetup.ScenePath, OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();
            Assert.That(roots, Has.Length.EqualTo(1));
            Assert.That(PrefabUtility.IsAnyPrefabInstanceRoot(roots.Single()), Is.True);
            Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(roots.Single()), Is.EqualTo(AssetDatabase.LoadAssetAtPath<GameObject>(P04ProjectSetup.PrefabPath)));
        }
    }
}
