#if UNITY_EDITOR
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Enums;
using HBP.Quest;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace HBP.Tests.Quest
{
    public sealed class QuestPointerOverlayTests
    {
        [TestCase(false), TestCase(true)]
        public async Task Reticle_RendersAboveDialogAndRestoresNormalRendering(bool nearerGeometry)
        {
            var host = new GameObject("Dialog host", typeof(RectTransform), typeof(Canvas));
            var head = new GameObject("Head", typeof(Camera));
            var cursor = new GameObject("Pointer", typeof(EventSystem));
            cursor.SetActive(false);
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var texture = new RenderTexture(128, 128, 24);
            var pixels = new Texture2D(128, 128, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            QuestDialogBox dialog = null;
            Task<int> pending = null;
            try
            {
                var camera = head.GetComponent<Camera>();
                camera.nearClipPlane = .01f;
                camera.orthographic = true;
                camera.orthographicSize = .15f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.targetTexture = texture;
                host.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                host.GetComponent<Canvas>().worldCamera = camera;
                dialog = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Dialog Box.prefab"), host.transform).GetComponent<QuestDialogBox>();
                pending = dialog.OpenAsync(DialogBoxType.Warning, "Title", "Message", new[] { "Accept", "Cancel" }).AsTask();
                dialog.GetComponent<QuestLoadingFollower>().enabled = false;
                var renderer = marker.GetComponent<Renderer>();
                var original = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Themes/Quest/Quest Pointer.mat");
                renderer.sharedMaterial = original;
                renderer.sortingOrder = 17;
                marker.transform.localScale = Vector3.one * .03f;
                blocker.SetActive(nearerGeometry);
                blocker.transform.position = Vector3.forward * .5f;
                blocker.transform.localScale = new Vector3(1, 1, .01f);
                var pointer = cursor.AddComponent<QuestPointerInput>();
                QuestWindowInteractionTests.Set(pointer, "policy", AssetDatabase.LoadAssetAtPath<QuestInteractionPolicy>("Assets/Resources/Themes/Quest/Quest Interaction Policy.asset"));
                QuestWindowInteractionTests.Set(pointer, "overlayFeedbackMaterial", AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Themes/Quest/Quest Pointer Overlay.mat"));
                QuestWindowInteractionTests.Set(pointer, "feedbackProperties", new MaterialPropertyBlock());
                var feedback = typeof(QuestPointerInput).GetMethod("Feedback", BindingFlags.Instance | BindingFlags.NonPublic);
                feedback.Invoke(pointer, new object[] { null, marker.transform, true, Vector3.zero, dialog.transform.position, true, true });
                await UniTask.NextFrame();
                Canvas.ForceUpdateCanvases();
                Color foreground = ReadCenter(camera, texture, pixels);
                Assert.That(foreground.b, Is.GreaterThan(foreground.r + .15f), "The blue impact point must be drawn above the dialog and any nearer geometry.");
                feedback.Invoke(pointer, new object[] { null, marker.transform, true, Vector3.zero, dialog.transform.position, true, false });
                Assert.That(renderer.sharedMaterial, Is.SameAs(original));
                Assert.That(renderer.sortingOrder, Is.EqualTo(17));
                Color normal = ReadCenter(camera, texture, pixels);
                Assert.That(normal.b - normal.r, Is.LessThan(.1f), "The test dialog must cover the ordinary cursor, reproducing the original render-order problem.");
            }
            finally
            {
                if (dialog != null) dialog.Close();
                if (pending != null) await pending;
                RenderTexture.active = previous;
                head.GetComponent<Camera>().targetTexture = null;
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(head);
                Object.DestroyImmediate(cursor);
                Object.DestroyImmediate(marker);
                Object.DestroyImmediate(blocker);
                Object.DestroyImmediate(pixels);
                Object.DestroyImmediate(texture);
            }
        }

        private static Color ReadCenter(Camera camera, RenderTexture texture, Texture2D pixels)
        {
            camera.Render();
            RenderTexture.active = texture;
            pixels.ReadPixels(new Rect(0, 0, 128, 128), 0, 0);
            pixels.Apply();
            return pixels.GetPixel(64, 64);
        }
    }
}
#endif
