using System.Collections;
using HBP.Core.DLL.HbpCore;
using HBP.Core.Tools;
using HBP.Tests.PlayMode.Utilities;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace HBP.Tests.PlayMode.Workflow
{
    public class WorkflowPlayModeArchitectureTests
    {
        [UnityTest]
        [Category("PlayMode.Workflow")]
        public IEnumerator WorkflowHarness_RedirectsApplicationStateAndLoadsSyntheticProject()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("WorkflowHarness");

            var project = PlayModeProjectHarness.CreateAndLoadMinimalProject("workflow-playmode-project");
            scene.CreateCamera();

            yield return null;

            Assert.That(ApplicationState.LoadedProject, Is.SameAs(project));
            Assert.That(ApplicationState.TMPFolder, Does.StartWith(temp.Path));
            Assert.That(ApplicationState.DatabasePath, Does.StartWith(temp.Path));
            Assert.That(scene.Scene.isLoaded, Is.True);
        }

        [UnityTest]
        [Category("PlayMode.Workflow")]
        [Category("NativeDll")]
        public IEnumerator WorkflowHarness_StartsWithHbpCorePresent_WhenInstalled()
        {
            if (!HbpCoreRuntime.TryGetVersion(out string version, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("WorkflowHarnessHbpCorePresent");

            PlayModeProjectHarness.CreateAndLoadMinimalProject("workflow-playmode-project-hbp-core");
            scene.CreateCamera();

            yield return null;

            Assert.That(version, Is.Not.Empty);
            Assert.That(ApplicationState.LoadedProject, Is.Not.Null);
            Assert.That(scene.Scene.isLoaded, Is.True);
        }
    }
}
