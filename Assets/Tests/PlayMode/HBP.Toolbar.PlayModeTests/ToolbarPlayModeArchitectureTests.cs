using System.Collections;
using HBP.Tests.PlayMode.Utilities;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HBP.Tests.PlayMode.Toolbar
{
    public class ToolbarPlayModeArchitectureTests
    {
        [UnityTest]
        [Category("PlayMode.Toolbar")]
        public IEnumerator ToolbarHarness_CreatesToolbarRootInsideIsolatedScene()
        {
            using PlayModeSceneScope scene = new("ToolbarHarness");
            GameObject toolbarRoot = new("Toolbar Harness Root");
            SceneManager.MoveGameObjectToScene(toolbarRoot, scene.Scene);
            toolbarRoot.AddComponent<RectTransform>();

            yield return null;

            Assert.That(toolbarRoot.scene, Is.EqualTo(scene.Scene));
            Assert.That(toolbarRoot.GetComponent<RectTransform>(), Is.Not.Null);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(scene.Scene));
        }
    }
}
