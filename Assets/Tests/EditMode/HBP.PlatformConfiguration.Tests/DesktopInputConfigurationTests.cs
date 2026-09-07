using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HBP.UI.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace HBP.Tests.PlatformConfiguration
{
    public class DesktopInputConfigurationTests
    {
        [Test]
        public void InputPrefab_SerializesAllUIActionsAndTextBackend()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Managers/Input Manager.prefab");
            var module = prefab.GetComponent<InputSystemUIInputModule>();
            Assert.That(module, Is.Not.Null);
            Assert.That(prefab.GetComponents<BaseInputModule>().Length, Is.EqualTo(1));
            Assert.That(module.actionsAsset, Is.Not.Null);
            foreach (var action in new[] { module.point, module.leftClick, module.rightClick, module.middleClick, module.scrollWheel, module.move, module.submit, module.cancel })
            {
                Assert.That(action, Is.Not.Null, "UI action reference must be serialized, not assigned defaults at runtime.");
                Assert.That(action.action.actionMap.asset, Is.SameAs(module.actionsAsset));
                Assert.That(AssetDatabase.GetAssetPath(action), Is.EqualTo("Assets/Settings/DesktopUI.inputactions"));
            }

            Assert.That(module.scrollDeltaPerTick, Is.EqualTo(1));
            Assert.That(module.moveRepeatDelay, Is.EqualTo(0.5f));
            Assert.That(module.moveRepeatRate, Is.EqualTo(0.1f));
            var backend = prefab.GetComponent<InputFieldBackend>();
            Assert.That(backend, Is.Not.Null);
            Assert.That(new SerializedObject(backend).FindProperty("m_Module").objectReferenceValue, Is.SameAs(module));
        }

        [Test]
        public void InputSettings_ResetAllDevicesOnFocusLoss_AndNormalizeWheel()
        {
            Assert.That(AssetDatabase.GetAssetPath(InputSystem.settings), Is.EqualTo("Assets/Settings/DesktopInputSettings.asset"));
            Assert.That(InputSystem.settings.backgroundBehavior, Is.EqualTo(InputSettings.BackgroundBehavior.ResetAndDisableAllDevices));
            Assert.That(InputSystem.settings.scrollDeltaBehavior, Is.EqualTo(InputSettings.ScrollDeltaBehavior.UniformAcrossAllPlatforms));
        }

        [Test]
        public void DeliveredSceneAndResources_HaveNoLegacyModulesOrLegacyScriptReads()
        {
            var roots = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).Concat(AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && p.Contains("/Resources/") && !AssetDatabase.IsValidFolder(p))).ToArray();
            var dependencies = AssetDatabase.GetDependencies(roots, true);
            string resultRoot = Path.GetFullPath(".test-results/quest-002-a");
            Directory.CreateDirectory(resultRoot);
            File.WriteAllLines(Path.Combine(resultRoot, "delivered-dependencies.txt"), dependencies.OrderBy(p => p));
            foreach (string path in dependencies.Where(p => p.StartsWith("Assets/")))
            {
                if (path.EndsWith(".prefab") || path.EndsWith(".unity"))
                {
                    string yaml = File.ReadAllText(path);
                    Assert.That(yaml, Does.Not.Contain("4f231c4fb786f3946a6b90b886c48677"), path);
                    Assert.That(yaml, Does.Not.Contain("StandaloneInputModule"), path);
                }

                if (!path.EndsWith(".cs") || path.Contains("/Editor/")) continue;
                string source = File.ReadAllText(path);
                source = Regex.Replace(source, @"/\*.*?\*/|//[^\r\n]*", "", RegexOptions.Singleline);
                Assert.That(Regex.IsMatch(source, @"\b(?:UnityEngine\.)?Input\s*\."), Is.False, path);
            }

            // Audit all owned runtime code as well, including dynamically loaded paths.
            foreach (string path in Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories).Where(p => !p.Replace('\\', '/').Contains("/Editor/")))
            {
                string source = Regex.Replace(File.ReadAllText(path), @"/\*.*?\*/|//[^\r\n]*", "", RegexOptions.Singleline);
                Assert.That(Regex.IsMatch(source, @"\b(?:UnityEngine\.)?Input\s*\."), Is.False, path);
            }
        }
    }
}
