using System;
using System.Runtime.InteropServices;
using HBP.Data.Module3D;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    [LegacyParityOnly]
    public class NativeParityRoiTests
    {
        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        [Category(NativeParityAssert.IntentionalCorrection)]
        public void ManagedROI_SphereMaskMatchesLegacyHbpExport()
        {
            Vector3[] unitySpheres = { Vector3.zero, new Vector3(3, 0, 0) };
            float[] radii = { 1.0f, 0.5f };
            Vector3[] unityPoints =
            {
                Vector3.zero,
                new Vector3(0.5f, 0, 0),
                new Vector3(1.0f, 0, 0),
                new Vector3(2.0f, 0, 0),
                new Vector3(3.0f, 0, 0)
            };

            using ManagedRoiScope managed = new(unitySpheres, radii);
            bool[] managedInside = new bool[unityPoints.Length];
            for (int i = 0; i < unityPoints.Length; ++i)
            {
                managedInside[i] = managed.ROI.Contains(unityPoints[i]);
            }

            bool[] legacyInside = ExecuteLegacyOrIgnore(() => ComputeLegacyInside(unitySpheres, radii, unityPoints));

            Assert.That(managedInside, Is.EqualTo(legacyInside));
        }

        private static bool[] ComputeLegacyInside(Vector3[] unitySpheres, float[] radii, Vector3[] unityPoints)
        {
            IntPtr roi = IntPtr.Zero;
            IntPtr rawSites = IntPtr.Zero;
            try
            {
                roi = create_ROI();
                rawSites = create_RawSiteList();

                for (int i = 0; i < unitySpheres.Length; ++i)
                {
                    Vector3 nativeCenter = ToLegacyNativePosition(unitySpheres[i]);
                    addSphere_ROI(roi, radii[i], new[] { nativeCenter.x, nativeCenter.y, nativeCenter.z });
                }

                for (int i = 0; i < unityPoints.Length; ++i)
                {
                    Vector3 nativePosition = ToLegacyNativePosition(unityPoints[i]);
                    add_site_RawSiteList(rawSites, $"P{i}", nativePosition.x, nativePosition.y, nativePosition.z, 0, i);
                }

                bool[] inside = new bool[unityPoints.Length];
                for (int i = 0; i < unityPoints.Length; ++i)
                {
                    inside[i] = isInside_ROI(roi, rawSites, i) == 1;
                }

                return inside;
            }
            finally
            {
                if (rawSites != IntPtr.Zero)
                {
                    delete_RawSiteList(rawSites);
                }

                if (roi != IntPtr.Zero)
                {
                    delete_ROI(roi);
                }
            }
        }

        private static Vector3 ToLegacyNativePosition(Vector3 unityPosition)
        {
            return new Vector3(-unityPosition.x, unityPosition.y, unityPosition.z);
        }

        private static T ExecuteLegacyOrIgnore<T>(Func<T> action)
        {
            try
            {
                return action();
            }
            catch (Exception exception) when (IsMissingNativeDependency(exception))
            {
                Assert.Ignore($"hbp_export ROI comparison unavailable: {exception.Message}");
                throw;
            }
        }

        private static bool IsMissingNativeDependency(Exception exception)
        {
            for (Exception current = exception; current != null; current = current.InnerException)
            {
                if (current is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class ManagedRoiScope : IDisposable
        {
            private readonly GameObject m_RoiObject;

            public ROI ROI { get; }

            public ManagedRoiScope(Vector3[] centers, float[] radii)
            {
                m_RoiObject = new GameObject("Managed ROI Parity");
                ROI = m_RoiObject.AddComponent<ROI>();
                for (int i = 0; i < centers.Length; ++i)
                {
                    GameObject sphereObject = new($"Managed ROI Sphere {i}");
                    sphereObject.transform.SetParent(m_RoiObject.transform, false);
                    sphereObject.AddComponent<MeshFilter>();
                    Sphere sphere = sphereObject.AddComponent<Sphere>();
                    sphere.Initialize(0, sphereObject.name, radii[i], centers[i]);
                    ROI.Spheres.Add(sphere);
                }
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(m_RoiObject);
            }
        }

        [DllImport("hbp_export", EntryPoint = "create_ROI", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create_ROI();

        [DllImport("hbp_export", EntryPoint = "delete_ROI", CallingConvention = CallingConvention.Cdecl)]
        private static extern void delete_ROI(IntPtr roi);

        [DllImport("hbp_export", EntryPoint = "addSphere_ROI", CallingConvention = CallingConvention.Cdecl)]
        private static extern void addSphere_ROI(IntPtr roi, float radius, float[] center);

        [DllImport("hbp_export", EntryPoint = "isInside_ROI", CallingConvention = CallingConvention.Cdecl)]
        private static extern int isInside_ROI(IntPtr roi, IntPtr rawSiteList, int id);

        [DllImport("hbp_export", EntryPoint = "create_RawSiteList", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create_RawSiteList();

        [DllImport("hbp_export", EntryPoint = "delete_RawSiteList", CallingConvention = CallingConvention.Cdecl)]
        private static extern void delete_RawSiteList(IntPtr rawSiteList);

        [DllImport("hbp_export", EntryPoint = "add_site_RawSiteList", CallingConvention = CallingConvention.Cdecl)]
        private static extern void add_site_RawSiteList(IntPtr rawSiteList, string name, float x, float y, float z, int patientIndex, int index);
    }
}
