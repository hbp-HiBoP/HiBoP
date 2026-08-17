using System.Collections;
using HBP.Tests.PlayMode.Utilities;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HBP.Tests.PlayMode.Module3D
{
    public class Module3DPlayModeArchitectureTests
    {
        [UnityTest]
        [Category("PlayMode.Module3D")]
        public IEnumerator Module3DHarness_CreatesSceneCameraLightAndRoot()
        {
            using PlayModeSceneScope scene = new("Module3DHarness");
            Camera camera = scene.CreateCamera();
            Light light = scene.CreateDirectionalLight();
            GameObject moduleRoot = new("Module3D Harness Root");
            SceneManager.MoveGameObjectToScene(moduleRoot, scene.Scene);

            yield return null;

            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(scene.Scene));
            Assert.That(camera, Is.Not.Null);
            Assert.That(light.type, Is.EqualTo(LightType.Directional));
            Assert.That(moduleRoot.scene, Is.EqualTo(scene.Scene));
        }
    }
}
