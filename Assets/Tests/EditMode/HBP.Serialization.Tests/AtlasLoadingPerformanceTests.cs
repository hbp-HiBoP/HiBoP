using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBP.Core.Object3D;
using HBP.Core.Tools;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    public class AtlasLoadingPerformanceTests
    {
        [Serializable]
        private class Measurement
        {
            public string atlas;
            public double seconds;
            public long workingSetBefore;
            public long workingSetAfter;
            public long peakWorkingSet;
            public int volumes;
            public string[] sourceHashes;
        }

        [TestCase("localizer:VISU")]
        [TestCase("difumo:1024")]
        [Explicit("Loads large installed scientific atlases; run explicitly for performance qualification.")]
        public async Task InstalledAtlasLoad(string id)
        {
            string folder = Path.Combine(ApplicationState.DataPath, "Atlases", AtlasResources.Find(id).Directory);
            if (!Directory.Exists(folder)) Assert.Ignore("Atlas is not installed: " + folder);
            Assert.That(AtlasResources.IsLoaded(id), Is.False, "Benchmark requires an unloaded atlas.");
            using var process = Process.GetCurrentProcess();
            var measurement = new Measurement { atlas = id, workingSetBefore = ReportedBytes(process.WorkingSet64) };
            try
            {
                var watch = Stopwatch.StartNew();
                var result = await AtlasResources.LoadAsync(id);
                watch.Stop();
                Assert.That(result.State, Is.EqualTo(AtlasLoadState.Loaded), result.Error);
                var fmris = id.StartsWith("localizer:", StringComparison.Ordinal) ? Object3DManager.Localizers.Protocols.Single(p => "localizer:" + p.Name == id).Datas.SelectMany(d => d.Blocs).Select(b => b.FMRI).ToArray() : new[] { Object3DManager.DiFuMo.FMRIs[id.Substring(7)] };
                process.Refresh();
                measurement.seconds = watch.Elapsed.TotalSeconds;
                measurement.workingSetAfter = ReportedBytes(process.WorkingSet64);
                measurement.peakWorkingSet = ReportedBytes(process.PeakWorkingSet64);
                measurement.volumes = fmris.Sum(f => f.Volumes.Count);
                measurement.sourceHashes = fmris.Select(f => f.SourceHash).ToArray();
                UnityEngine.Debug.Log("ATLAS_LOAD_BENCHMARK " + JsonUtility.ToJson(measurement));
            }
            finally
            {
                AtlasResources.Unload(id);
            }
        }

        // Unity Mono can return zero when process memory counters are unavailable.
        private static long ReportedBytes(long bytes) => bytes > 0 ? bytes : -1;
    }
}
