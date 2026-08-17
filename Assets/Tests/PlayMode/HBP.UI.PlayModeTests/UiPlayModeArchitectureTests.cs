using System.Collections;
using HBP.Tests.PlayMode.Utilities;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HBP.Tests.PlayMode.UI
{
    public class UiPlayModeArchitectureTests
    {
        [UnityTest]
        [Category("PlayMode.UI")]
        public IEnumerator WindowHarness_CreatesCanvasAndEventSystem()
        {
            using PlayModeSceneScope scene = new("UiHarness");
            PlayModeWindowHarness window = new(scene.Scene, "UI Window Harness");

            yield return null;

            Assert.That(window.Canvas, Is.Not.Null);
            Assert.That(window.Canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(window.Root.GetComponent<GraphicRaycaster>(), Is.Not.Null);
            Assert.That(window.EventSystem.GetComponent<StandaloneInputModule>(), Is.Not.Null);
        }
    }
}
