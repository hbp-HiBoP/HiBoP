using System;
using System.Collections.Generic;
using HBP.Core.Data;
using HBP.Core.Tools;
using NUnit.Framework;

namespace HBP.Tests.Serialization
{
    public class Stage6MemoryBudgetTests
    {
        [TearDown]
        public void TearDown()
        {
            DataManager.Clear();
            DataManager.ConfigureMemoryBudget(1024 * 1024, 0);
        }

        [Test]
        public void ExplicitLimit_IsConvertedExactlyToBytes()
        {
            Assert.That(MemoryCacheBudget.ResolveLimitBytes(512, 16384), Is.EqualTo(512L * 1024 * 1024));
        }

        [TestCase(65536, 58982)]
        [TestCase(8192, 6144)]
        [TestCase(2048, 0)]
        public void AutomaticLimit_UsesNinetyPercentWithAtLeastTwoGiBReserved(int totalMiB, long expectedMiB)
        {
            Assert.That(MemoryCacheBudget.ResolveLimitBytes(0, totalMiB), Is.EqualTo(expectedMiB * 1024 * 1024));
        }

        [Test]
        public void Trim_EvictsDerivedThenProjectionThenRaw()
        {
            MemoryCacheBudget budget = new();
            budget.Configure(3, 0);
            List<string> evicted = new();
            budget.Register("raw", MemoryCacheCategory.RawRecording, MiB(2), false, () => evicted.Add("raw"));
            budget.Register("projection", MemoryCacheCategory.NativeProjection, MiB(2), false, () => evicted.Add("projection"));
            budget.Register("derived", MemoryCacheCategory.ManagedDerived, MiB(2), false, () => evicted.Add("derived"));

            Assert.That(evicted, Is.EqualTo(new[] { "projection", "derived" }));
            Assert.That(budget.GetSnapshot().UsedBytes, Is.EqualTo(MiB(2)));
        }

        [Test]
        public void Trim_UsesLeastRecentlyUsedWithinCategory()
        {
            MemoryCacheBudget budget = new();
            budget.Configure(2, 0);
            List<string> evicted = new();
            budget.Register("first", MemoryCacheCategory.RawRecording, MiB(1), false, () => evicted.Add("first"));
            budget.Register("second", MemoryCacheCategory.RawRecording, MiB(1), false, () => evicted.Add("second"));
            budget.Touch("first");
            budget.Register("third", MemoryCacheCategory.RawRecording, MiB(1), false, () => evicted.Add("third"));

            Assert.That(evicted, Is.EqualTo(new[] { "second" }));
        }

        [Test]
        public void PinnedActiveEntry_CanExceedBudgetWithWarningAndNoEviction()
        {
            MemoryCacheBudget budget = new();
            budget.Configure(1, 0);
            bool evicted = false;
            MemoryCacheSnapshot warning = default;
            budget.BudgetExceeded += snapshot => warning = snapshot;

            budget.Register("active", MemoryCacheCategory.NativeProjection, MiB(3), true, () => evicted = true);

            Assert.That(evicted, Is.False);
            Assert.That(warning.IsOverBudget, Is.True);
            Assert.That(warning.PinnedBytes, Is.EqualTo(MiB(3)));
        }

        [Test]
        public void UnpinningAnOverBudgetEntry_MakesItEvictable()
        {
            MemoryCacheBudget budget = new();
            budget.Configure(1, 0);
            bool evicted = false;
            budget.Register("active", MemoryCacheCategory.ManagedDerived, MiB(2), true, () => evicted = true);

            budget.SetPinned("active", false);

            Assert.That(evicted, Is.True);
            Assert.That(budget.GetSnapshot().UsedBytes, Is.Zero);
        }

        [Test]
        public void RawRecordingCache_ReusesPinnedDataThenReconstructsAfterColdEviction()
        {
            MemoryCacheBudget budget = new();
            budget.Configure(1, 0);
            RawRecordingCache cache = new(budget);
            RawRecordingSourceKey key = new("large-recording");
            int loadCount = 0;

            DynamicData Load()
            {
                loadCount++;
                return new DynamicData(new Dictionary<string, float[]> { { "A1", new float[400000] } }, new Dictionary<string, string> { { "A1", "uV" } }, new Frequency(1000));
            }

            cache.Pin(key);
            DynamicData first = cache.GetOrLoad(key, Load);
            DynamicData hot = cache.GetOrLoad(key, Load);
            Assert.That(hot, Is.SameAs(first));
            Assert.That(loadCount, Is.EqualTo(1));

            cache.Unpin(key);
            Assert.That(cache.Count, Is.Zero);
            DynamicData reconstructed = cache.GetOrLoad(key, Load);

            Assert.That(reconstructed, Is.Not.SameAs(first));
            Assert.That(loadCount, Is.EqualTo(2));
        }

        [Test]
        public void Register_RejectsNegativeAccounting()
        {
            MemoryCacheBudget budget = new();
            Assert.Throws<ArgumentOutOfRangeException>(() => budget.Register("bad", MemoryCacheCategory.Texture, -1, false, null));
        }

        private static long MiB(long value) => value * 1024 * 1024;
    }
}
