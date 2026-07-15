using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HBP.Core.Data;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    public class ManagedPatientElectrodePtsTests
    {
        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        [LegacyParityOnly]
        public void ManagedPtsReader_MatchesLegacyPatientElectrodeGroupingNamesAndPositions()
        {
            string path = TemporaryPtsPath();
            try
            {
                File.WriteAllLines(path, new[]
                {
                    "ptsfile",
                    "1\t1\t1",
                    "3",
                    "A1\t1.250000\t-2.500000\t3.750000",
                    "A2 4.000000 5.500000 6.000000",
                    "B1\t-7.000000\t8.000000\t9.250000"
                });

                List<Site> managed = Site.LoadSitesFromPTSFile("Patient", path);
                IReadOnlyList<LegacyPatientElectrodesBridge.LegacyElectrode> legacy = LegacyPatientElectrodesBridge.LoadPts(path, "Patient");

                Assert.That(legacy.SelectMany(electrode => electrode.Sites).Select(site => site.Name), Is.EqualTo(managed.Select(site => site.Name)));
                Assert.That(legacy.Select(electrode => electrode.Name), Is.EqualTo(new[] { "A", "B" }));
                Vector3[] managedPositions = managed
                    .Select(site => site.Coordinates.Single(coordinate => coordinate.ReferenceSystem == "Patient").Position.ToVector3())
                    .ToArray();
                Vector3[] legacyPositions = legacy.SelectMany(electrode => electrode.Sites).Select(site => site.Position).ToArray();
                Assert.That(managedPositions, Is.EqualTo(legacyPositions));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        [Category("NativeMigration")]
        public void ManagedPtsWriter_UsesHistoricalFormatAndRoundTripsSelectedReferenceSystem()
        {
            string path = TemporaryPtsPath();
            Site[] source =
            {
                CreateSite("A1", new Vector3(1.25f, -2.5f, 3.75f), new Vector3(101, 102, 103)),
                CreateSite("B12", new Vector3(-4, 5.5f, 6), new Vector3(104, 105, 106))
            };

            try
            {
                Site.SaveSitesToPTSFile(source, "Patient", path);

                string[] lines = File.ReadAllLines(path);
                Assert.That(lines, Is.EqualTo(new[]
                {
                    "ptsfile",
                    "1\t1\t1",
                    "2",
                    "A1\t1.250000\t-2.500000\t3.750000",
                    "B12\t-4.000000\t5.500000\t6.000000"
                }));

                List<Site> roundTrip = Site.LoadSitesFromPTSFile("Patient", path);
                Assert.That(roundTrip.Select(site => site.Name), Is.EqualTo(source.Select(site => site.Name)));
                Assert.That(
                    roundTrip.Select(site => site.Coordinates.Single().Position.ToVector3()),
                    Is.EqualTo(source.Select(site => site.Coordinates.Single(coordinate => coordinate.ReferenceSystem == "Patient").Position.ToVector3())));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        [LegacyParityOnly]
        public void ManagedPtsWriter_OutputLoadsThroughLegacyPatientElectrodes()
        {
            string path = TemporaryPtsPath();
            Site[] source =
            {
                CreateSite("A1", new Vector3(1.25f, -2.5f, 3.75f), Vector3.zero),
                CreateSite("A2", new Vector3(4, 5, 6), Vector3.zero),
                CreateSite("B12", new Vector3(-7, 8, 9.25f), Vector3.zero)
            };

            try
            {
                Site.SaveSitesToPTSFile(source, "Patient", path);
                IReadOnlyList<LegacyPatientElectrodesBridge.LegacyElectrode> legacy = LegacyPatientElectrodesBridge.LoadPts(path, "Patient");

                Assert.That(legacy.Select(electrode => electrode.Name), Is.EqualTo(new[] { "A", "B" }));
                Assert.That(legacy.SelectMany(electrode => electrode.Sites).Select(site => site.Name), Is.EqualTo(source.Select(site => site.Name)));
                Assert.That(
                    legacy.SelectMany(electrode => electrode.Sites).Select(site => site.Position),
                    Is.EqualTo(source.Select(site => site.Coordinates.Single(coordinate => coordinate.ReferenceSystem == "Patient").Position.ToVector3())));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        [Category("NativeMigration")]
        public void ManagedPtsWriter_RejectsSiteWithoutRequestedCoordinate()
        {
            string path = TemporaryPtsPath();
            Site site = new("A1", new[] { new Coordinate("MNI", Vector3.zero) }, Array.Empty<BaseTagValue>());

            try
            {
                Assert.That(
                    () => Site.SaveSitesToPTSFile(new[] { site }, "Patient", path),
                    Throws.TypeOf<InvalidDataException>().With.Message.Contains("A1"));
                Assert.That(File.Exists(path), Is.False);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        private static Site CreateSite(string name, Vector3 patient, Vector3 mni)
        {
            return new Site(
                name,
                new[] { new Coordinate("Patient", patient), new Coordinate("MNI", mni) },
                Array.Empty<BaseTagValue>());
        }

        private static string TemporaryPtsPath()
        {
            return Path.Combine(Path.GetTempPath(), $"hibop_managed_pts_{Guid.NewGuid():N}.pts");
        }
    }
}
