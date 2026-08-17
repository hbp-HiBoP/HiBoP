using System;
using HBP.Core.DLL;
using HBP.Core.Enums;
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
            Action onChanged = () => ++changeCount;
            ActivityProjectionSettings.OnChanged += onChanged;
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
                ActivityProjectionSettings.OnChanged -= onChanged;
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

        [Test]
        [Category("NativeMigration")]
        public void DLLDebugManager_AppliesSerializedProjectionSettings()
        {
            GameObject gameObject = new("DLL Debug Manager test");
            try
            {
                DLLDebugManager manager = gameObject.AddComponent<DLLDebugManager>();
                SerializedObject serializedManager = new(manager);
                SerializedProperty dimension = serializedManager.FindProperty("m_VolumeGridDimension");
                SerializedProperty interpolation = serializedManager.FindProperty("m_VolumeInterpolation");

                dimension.intValue = 96;
                interpolation.intValue = (int)VolumeInterpolation.Trilinear;
                serializedManager.ApplyModifiedPropertiesWithoutUndo();
                manager.ApplyActivityProjectionSettings();

                Assert.That(manager.ActivityProjectionVolumeGridDimension, Is.EqualTo(96));
                Assert.That(manager.ActivityProjectionVolumeInterpolation, Is.EqualTo(VolumeInterpolation.Trilinear));
                Assert.That(ActivityProjectionSettings.VolumeGridDimension, Is.EqualTo(96));
                Assert.That(ActivityProjectionSettings.VolumeInterpolation, Is.EqualTo(VolumeInterpolation.Trilinear));

                dimension.intValue = 1;
                interpolation.intValue = 99;
                serializedManager.ApplyModifiedPropertiesWithoutUndo();
                manager.ApplyActivityProjectionSettings();

                Assert.That(manager.ActivityProjectionVolumeGridDimension, Is.EqualTo(ActivityProjectionSettings.DefaultVolumeGridDimension));
                Assert.That(manager.ActivityProjectionVolumeInterpolation, Is.EqualTo(ActivityProjectionSettings.DefaultVolumeInterpolation));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }
}
