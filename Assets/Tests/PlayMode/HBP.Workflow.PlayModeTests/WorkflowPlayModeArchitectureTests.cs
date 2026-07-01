using System.Collections;
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
    }
}
