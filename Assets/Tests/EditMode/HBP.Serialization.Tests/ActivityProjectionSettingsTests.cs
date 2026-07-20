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
            Assert.That(ActivityProjectionSettings.VolumeGridDimension,
                Is.EqualTo(ActivityProjectionSettings.DefaultVolumeGridDimension));
            Assert.That(ActivityProjectionSettings.VolumeInterpolation,
                Is.EqualTo(ActivityProjectionSettings.DefaultVolumeInterpolation));

            ActivityProjectionSettings.VolumeGridDimension = 80;
            ActivityProjectionSettings.VolumeInterpolation = VolumeInterpolation.Trilinear;
            Assert.That(ActivityProjectionSettings.VolumeGridDimension, Is.EqualTo(80));
            Assert.That(ActivityProjectionSettings.VolumeInterpolation, Is.EqualTo(VolumeInterpolation.Trilinear));

            Assert.Throws<ArgumentOutOfRangeException>(() => ActivityProjectionSettings.VolumeGridDimension = 1);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ActivityProjectionSettings.VolumeInterpolation = (VolumeInterpolation)99);
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

                Assert.That(manager.ActivityProjectionVolumeGridDimension,
                    Is.EqualTo(ActivityProjectionSettings.DefaultVolumeGridDimension));
                Assert.That(manager.ActivityProjectionVolumeInterpolation,
                    Is.EqualTo(ActivityProjectionSettings.DefaultVolumeInterpolation));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }
}
