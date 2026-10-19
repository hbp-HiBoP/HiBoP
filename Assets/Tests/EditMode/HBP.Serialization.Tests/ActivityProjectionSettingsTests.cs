using UnityEngine.Events;
using System;
using HBP.Core.DLL;
using HBP.Core.Enums;
using HBP.Data.Module3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    public class ActivityProjectionSettingsTests
    {
        [TearDown]
        public void TearDown()
        {
            ActivityProjectionSettings.ResetDefaults();
        }

        [Test]
        [Category("NativeMigration")]
        public void DefaultsAndOverrides_AreCentralizedAndValidated()
        {
            ActivityProjectionSettings.ResetDefaults();
            Assert.That(ActivityProjectionSettings.VolumeGridDimension, Is.EqualTo(ActivityProjectionSettings.DefaultVolumeGridDimension));
            Assert.That(ActivityProjectionSettings.VolumeInterpolation, Is.EqualTo(ActivityProjectionSettings.DefaultVolumeInterpolation));

            ActivityProjectionSettings.VolumeGridDimension = 80;
            ActivityProjectionSettings.VolumeInterpolation = VolumeInterpolation.Trilinear;
            Assert.That(ActivityProjectionSettings.VolumeGridDimension, Is.EqualTo(80));
            Assert.That(ActivityProjectionSettings.VolumeInterpolation, Is.EqualTo(VolumeInterpolation.Trilinear));

            Assert.Throws<ArgumentOutOfRangeException>(() => ActivityProjectionSettings.VolumeGridDimension = 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => ActivityProjectionSettings.VolumeInterpolation = (VolumeInterpolation)99);
        }

        [Test]
        [Category("NativeMigration")]
        public void Changed_IsRaisedOnlyWhenProjectionGeometryChanges()
        {
            ActivityProjectionSettings.ResetDefaults();
            int changeCount = 0;
            UnityAction onChanged = () => ++changeCount;
            ActivityProjectionSettings.OnChanged.AddListener(onChanged);
            try
            {
                ActivityProjectionSettings.VolumeGridDimension = ActivityProjectionSettings.DefaultVolumeGridDimension;
                ActivityProjectionSettings.VolumeInterpolation = ActivityProjectionSettings.DefaultVolumeInterpolation;
                Assert.That(changeCount, Is.Zero);

                ActivityProjectionSettings.VolumeGridDimension = ActivityProjectionSettings.DefaultVolumeGridDimension + 1;
                ActivityProjectionSettings.VolumeInterpolation = VolumeInterpolation.Nearest;
                Assert.That(changeCount, Is.EqualTo(2));

                ActivityProjectionSettings.ResetDefaults();
                Assert.That(changeCount, Is.EqualTo(3));
            }
            finally
            {
                ActivityProjectionSettings.OnChanged.RemoveListener(onChanged);
            }
        }

        [Test]
        [Category("NativeMigration")]
        public void LocalizerExportGridSettings_AreIndependentFromInteractiveProjectionSettings()
        {
            ActivityProjectionSettings.VolumeGridDimension = 96;
            ActivityProjectionSettings.VolumeInterpolation = VolumeInterpolation.Nearest;

            LocalizerExportGridSettings exportSettings = new(LocalizerExportGridSettings.DefaultMaximumDimension);

            Assert.That(exportSettings.MaximumDimension, Is.EqualTo(80));
            Assert.That(exportSettings.Interpolation, Is.EqualTo(VolumeInterpolation.Trilinear));
            Assert.That(ActivityProjectionSettings.VolumeGridDimension, Is.EqualTo(96));
            Assert.That(ActivityProjectionSettings.VolumeInterpolation, Is.EqualTo(VolumeInterpolation.Nearest));
        }

        [Test]
        [Category("NativeMigration")]
        public void LocalizerExportGridSettings_AnnounceNativeGridDimensionsAndLargeExports()
        {
            Vector3Int referenceDimensions = new(208, 256, 219);
            LocalizerExportGridSettings defaultSettings = new(LocalizerExportGridSettings.DefaultMaximumDimension);
            LocalizerExportGridSettings fullResolutionSettings = new(256);

            Assert.That(defaultSettings.CalculateDimensions(referenceDimensions), Is.EqualTo(new Vector3Int(65, 80, 68)));
            Assert.That(defaultSettings.CalculateVoxelCount(referenceDimensions), Is.EqualTo(353_600));
            Assert.That(defaultSettings.RequiresLargeExportConfirmation(referenceDimensions), Is.False);
            Assert.That(fullResolutionSettings.CalculateDimensions(referenceDimensions), Is.EqualTo(referenceDimensions));
            Assert.That(fullResolutionSettings.RequiresLargeExportConfirmation(referenceDimensions), Is.True);
        }

        [Test]
        [Category("NativeMigration")]
        public void LocalizerExportGridSettings_RejectInvalidDimensions()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LocalizerExportGridSettings(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LocalizerExportGridSettings(LocalizerExportGridSettings.MaximumAllowedDimension + 1));

            LocalizerExportGridSettings settings = new(LocalizerExportGridSettings.DefaultMaximumDimension);
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.CalculateDimensions(new Vector3Int(1, 256, 219)));
        }

        [TestCase("m_LogDLLToUnity")]
        [TestCase("m_LogDLLToFile")]
        [TestCase("m_GetInformationAboutDLLObjects")]
        [Category("NativeMigration")]
        public void DLLDebugManager_EditingDebugOptions_PreservesSessionProjectionSettings(string propertyName)
        {
            GameObject root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Managers/DLL Debug Manager.prefab");
            int changeCount = 0;
            UnityAction onChanged = () => ++changeCount;
            try
            {
                ActivityProjectionSettings.VolumeGridDimension = 96;
                ActivityProjectionSettings.VolumeInterpolation = VolumeInterpolation.Nearest;
                ActivityProjectionSettings.OnChanged.AddListener(onChanged);

                SerializedObject serializedManager = new(root.GetComponent<DLLDebugManager>());
                SerializedProperty option = serializedManager.FindProperty(propertyName);
                option.boolValue = !option.boolValue;
                serializedManager.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(ActivityProjectionSettings.VolumeGridDimension, Is.EqualTo(96));
                Assert.That(ActivityProjectionSettings.VolumeInterpolation, Is.EqualTo(VolumeInterpolation.Nearest));
                Assert.That(changeCount, Is.Zero);
            }
            finally
            {
                ActivityProjectionSettings.OnChanged.RemoveListener(onChanged);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        [Category("NativeMigration")]
        public void ActivityProjectionConfiguration_AppliesSerializedSettingsAndValidatesOverrides()
        {
            GameObject gameObject = new("Activity projection configuration test");
            try
            {
                ActivityProjectionSettings.ResetDefaults();
                ActivityProjectionConfiguration configuration = gameObject.AddComponent<ActivityProjectionConfiguration>();
                SerializedObject serializedConfiguration = new(configuration);
                SerializedProperty dimension = serializedConfiguration.FindProperty("m_VolumeGridDimension");
                SerializedProperty interpolation = serializedConfiguration.FindProperty("m_VolumeInterpolation");

                dimension.intValue = 96;
                interpolation.intValue = (int)VolumeInterpolation.Nearest;
                serializedConfiguration.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(ActivityProjectionSettings.VolumeGridDimension, Is.EqualTo(ActivityProjectionSettings.DefaultVolumeGridDimension));
                Assert.That(ActivityProjectionSettings.VolumeInterpolation, Is.EqualTo(ActivityProjectionSettings.DefaultVolumeInterpolation));
                configuration.Apply();

                Assert.That(configuration.VolumeGridDimension, Is.EqualTo(96));
                Assert.That(configuration.VolumeInterpolation, Is.EqualTo(VolumeInterpolation.Nearest));
                Assert.That(ActivityProjectionSettings.VolumeGridDimension, Is.EqualTo(96));
                Assert.That(ActivityProjectionSettings.VolumeInterpolation, Is.EqualTo(VolumeInterpolation.Nearest));

                dimension.intValue = 1;
                interpolation.intValue = 99;
                serializedConfiguration.ApplyModifiedPropertiesWithoutUndo();
                configuration.Apply();

                Assert.That(configuration.VolumeGridDimension, Is.EqualTo(ActivityProjectionSettings.DefaultVolumeGridDimension));
                Assert.That(configuration.VolumeInterpolation, Is.EqualTo(ActivityProjectionSettings.DefaultVolumeInterpolation));
                Assert.That(ActivityProjectionSettings.VolumeGridDimension, Is.EqualTo(ActivityProjectionSettings.DefaultVolumeGridDimension));
                Assert.That(ActivityProjectionSettings.VolumeInterpolation, Is.EqualTo(ActivityProjectionSettings.DefaultVolumeInterpolation));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        [Category("NativeMigration")]
        public void ActivityProjectionConfiguration_ValidationPreservesSessionSettings()
        {
            GameObject gameObject = new("Activity projection validation test");
            int changeCount = 0;
            UnityAction onChanged = () => ++changeCount;
            try
            {
                ActivityProjectionConfiguration configuration = gameObject.AddComponent<ActivityProjectionConfiguration>();
                ActivityProjectionSettings.VolumeGridDimension = 96;
                ActivityProjectionSettings.VolumeInterpolation = VolumeInterpolation.Nearest;
                ActivityProjectionSettings.OnChanged.AddListener(onChanged);

                SerializedObject serializedConfiguration = new(configuration);
                serializedConfiguration.FindProperty("m_VolumeGridDimension").intValue = 1;
                serializedConfiguration.FindProperty("m_VolumeInterpolation").intValue = 99;
                serializedConfiguration.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(configuration.VolumeGridDimension, Is.EqualTo(ActivityProjectionSettings.DefaultVolumeGridDimension));
                Assert.That(configuration.VolumeInterpolation, Is.EqualTo(ActivityProjectionSettings.DefaultVolumeInterpolation));
                Assert.That(ActivityProjectionSettings.VolumeGridDimension, Is.EqualTo(96));
                Assert.That(ActivityProjectionSettings.VolumeInterpolation, Is.EqualTo(VolumeInterpolation.Nearest));
                Assert.That(changeCount, Is.Zero);
            }
            finally
            {
                ActivityProjectionSettings.OnChanged.RemoveListener(onChanged);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        [Category("NativeMigration")]
        public void ThreeDPrefab_OwnsMigratedProjectionConfigurationWithoutApplyingItInEditMode()
        {
            ActivityProjectionSettings.VolumeGridDimension = 96;
            ActivityProjectionSettings.VolumeInterpolation = VolumeInterpolation.Nearest;
            GameObject root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/3D/3D.prefab");
            try
            {
                ActivityProjectionConfiguration configuration = root.GetComponent<ActivityProjectionConfiguration>();
                Assert.That(configuration, Is.Not.Null);
                Assert.That(configuration.VolumeGridDimension, Is.EqualTo(80));
                Assert.That(configuration.VolumeInterpolation, Is.EqualTo(VolumeInterpolation.Trilinear));
                Assert.That(ActivityProjectionSettings.VolumeGridDimension, Is.EqualTo(96));
                Assert.That(ActivityProjectionSettings.VolumeInterpolation, Is.EqualTo(VolumeInterpolation.Nearest));

                GameObject debugPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Managers/DLL Debug Manager.prefab");
                Assert.That(debugPrefab.GetComponent<ActivityProjectionConfiguration>(), Is.Null);
                SerializedObject serializedManager = new(debugPrefab.GetComponent<DLLDebugManager>());
                Assert.That(serializedManager.FindProperty("m_VolumeGridDimension"), Is.Null);
                Assert.That(serializedManager.FindProperty("m_VolumeInterpolation"), Is.Null);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
