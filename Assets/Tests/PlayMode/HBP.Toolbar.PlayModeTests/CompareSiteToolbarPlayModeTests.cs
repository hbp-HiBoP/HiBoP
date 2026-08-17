using System.Reflection;
using HBP.Tests.PlayMode.Utilities;
using HBP.UI.Toolbar;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Tests.PlayMode.Toolbar
{
    public class CompareSiteToolbarPlayModeTests
    {
        [Test]
        [Category("PlayMode.CompareSiteToolbar")]
        public void CompareSiteTool_ToggleStoresAndClearsComparedSite()
        {
            using PlayModeSceneScope scene = new("CompareSiteToolbarCompareSiteToolbar");
            using PlayModeModule3DTestHarness module3D = new(scene.Scene);
            module3D.SourceColumn.SelectSite(module3D.SourceSiteA);
            GameObject toolObject = new("Compare Site Tool");
            Toggle toggle = toolObject.AddComponent<Toggle>();
            CompareSite compareSite = toolObject.AddComponent<CompareSite>();
            SetPrivateField(compareSite, "m_Toggle", toggle);
            compareSite.SelectedScene = module3D.Scene;
            compareSite.SelectedColumn = module3D.SourceColumn;
            compareSite.Initialize();

            compareSite.UpdateInteractable();
            toggle.isOn = true;

            Assert.That(toggle.interactable, Is.True);
            Assert.That(module3D.ImplantationManager.ComparingSites, Is.True);
            Assert.That(module3D.ImplantationManager.SiteToCompare, Is.SameAs(module3D.SourceSiteA));

            toggle.isOn = false;

            Assert.That(module3D.ImplantationManager.ComparingSites, Is.False);
            Assert.That(module3D.ImplantationManager.SiteToCompare, Is.Null);
        }

        private static void SetPrivateField<T>(T target, string fieldName, object value)
        {
            FieldInfo field = typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"{typeof(T).FullName}.{fieldName}");
            field.SetValue(target, value);
        }
    }
}
