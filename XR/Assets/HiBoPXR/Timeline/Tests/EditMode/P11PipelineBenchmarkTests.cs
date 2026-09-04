using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.Protocol;
using CRNL.HiBoP.RenderModel;
using NUnit.Framework;

namespace CRNL.HiBoP.XR.Timeline.Tests
{
    public class P11PipelineBenchmarkTests
    {
        private const int SurfaceVertices = 69104;
        private const int Iterations = 20;

        [Test]
        public void MeasureFloat32Pipeline_ForOneThreeAndEightColumns()
        {
            Measure("D2", 1, 150);
            Measure("D2", 3, 150);
            Measure("D3", 8, 37500);
        }

        private static void Measure(string dataset, int columns, int sites)
        {
            var extract = new List<double>(Iterations);
            var serialize = new List<double>(Iterations);
            var transfer = new List<double>(Iterations);
            var decode = new List<double>(Iterations);
            var upload = new List<double>(Iterations);
            var commit = new List<double>(Iterations);
            var endToEnd = new List<double>(Iterations);
            var watch = new Stopwatch();
            SessionEpoch session = new(Id(1), 1);
            ContractId timeline = Id(2);
            var committer = new AtomicDynamicFrameCommitter<double>(session, timeline, Checksum);
            int payloadBytes = 0;
            byte[] hashPayload = null;

            for (int iteration = 1; iteration <= Iterations; iteration++)
            {
                watch.Restart();
                DynamicFrameBundle bundle = CreateBundle(session, timeline, columns, sites, (ulong)iteration);
                extract.Add(watch.Elapsed.TotalMilliseconds);

                watch.Restart();
                EncodedDynamicFrameBundle encoded = DynamicFrameBundleCodec.Encode(bundle);
                serialize.Add(watch.Elapsed.TotalMilliseconds);
                payloadBytes = encoded.Descriptor.ByteLength;

                watch.Restart();
                byte[] loopback = encoded.CopyPayload();
                transfer.Add(watch.Elapsed.TotalMilliseconds);
                hashPayload = loopback;

                DynamicFrameApplyResult result = committer.TryDecodePrepareAndCommit(encoded.Descriptor, loopback, out DynamicFramePipelineTiming timing, out Exception error);
                Assert.That(result, Is.EqualTo(DynamicFrameApplyResult.Committed), error?.ToString());
                decode.Add(timing.DecodeMilliseconds);
                upload.Add(timing.PrepareMilliseconds);
                commit.Add(timing.CommitMilliseconds);
                endToEnd.Add(extract[^1] + serialize[^1] + transfer[^1] + timing.TotalMilliseconds);
            }

            Write(dataset, columns, sites, payloadBytes, "extract", extract);
            Write(dataset, columns, sites, payloadBytes, "serialize", serialize);
            Write(dataset, columns, sites, payloadBytes, "transfer-loopback-copy", transfer);
            Write(dataset, columns, sites, payloadBytes, "decode", decode);
            Write(dataset, columns, sites, payloadBytes, "prepare-upload", upload);
            Write(dataset, columns, sites, payloadBytes, "atomic-commit", commit);
            Write(dataset, columns, sites, payloadBytes, "end-to-end-loopback", endToEnd);
            MeasureHash(dataset, columns, sites, payloadBytes, "hash-default", hashPayload, () => SHA256.Create());
        }

        private static void MeasureHash(string dataset, int columns, int sites, int bytes, string stage, byte[] payload, Func<SHA256> create)
        {
            var samples = new List<double>(Iterations);
            var watch = new Stopwatch();
            for (int iteration = 0; iteration < Iterations; iteration++)
            {
                watch.Restart();
                using SHA256 sha256 = create();
                sha256.ComputeHash(payload);
                samples.Add(watch.Elapsed.TotalMilliseconds);
            }

            Write(dataset, columns, sites, bytes, stage, samples);
        }

        private static void Write(string dataset, int columns, int sites, int bytes, string stage, List<double> samples)
        {
            samples.Sort();
            TestContext.Out.WriteLine($"P11_METRIC dataset={dataset} columns={columns} sitesPerColumn={sites} bytes={bytes} stage={stage} p50Ms={Percentile(samples, 0.50):F3} p95Ms={Percentile(samples, 0.95):F3} maxMs={samples[^1]:F3}");
        }

        private static double Percentile(IReadOnlyList<double> sorted, double percentile)
        {
            int index = Math.Max(0, (int)Math.Ceiling(sorted.Count * percentile) - 1);
            return sorted[index];
        }

        private static double Checksum(DynamicFrameBundle bundle)
        {
            double checksum = 0d;
            for (int column = 0; column < bundle.ColumnFrames.Count; column++)
            {
                ColumnFrame frame = bundle.ColumnFrames[column];
                for (int index = 0; index < frame.Surface.Value.VertexCount; index++)
                    checksum += frame.Surface.Value.ActivityValues[index] + frame.Surface.Value.OpacityValues[index] + frame.Surface.Value.ActiveMask[index];
                for (int index = 0; index < frame.Sites.Value.SiteCount; index++)
                    checksum += frame.Sites.Value.Positions[index].X + frame.Sites.Value.Sizes[index] + frame.Sites.Value.Visibility[index] + (byte)frame.Sites.Value.Flags[index];
                for (int index = 0; index < frame.CutOverlays[0].Pixels.Count; index++)
                    checksum += frame.CutOverlays[0].Pixels[index].R;
            }

            return checksum;
        }

        private static DynamicFrameBundle CreateBundle(SessionEpoch session, ContractId timeline, int columnCount, int siteCount, ulong sequence)
        {
            var expectations = new DynamicColumnExpectation[columnCount];
            var frames = new ColumnFrame[columnCount];
            RenderTemporalSample sample = new((int)(sequence % 16), 0.75f);
            StateRevision stateRevision = new(sequence);
            for (int column = 0; column < columnCount; column++)
            {
                ContractId columnId = Id((ulong)(10 + column));
                ContractId cutId = Id((ulong)(100 + column));
                AssetHash surfaceHash = Hash((ulong)(20 + column));
                expectations[column] = new DynamicColumnExpectation(columnId, DynamicColumnContent.Surface | DynamicColumnContent.Sites, new[] { cutId });
                SurfaceFrame surface = CreateSurface(surfaceHash, stateRevision, sample, column);
                SiteRenderFrame sites = CreateSites(Hash((ulong)(40 + column)), stateRevision, sample, siteCount, column, (int)sequence);
                CutOverlayFrame overlay = CreateOverlay(cutId, columnId, stateRevision, sample, column, (int)sequence);
                frames[column] = new ColumnFrame(columnId, surfaceHash, new ScopeRevision(sequence), Optional<SurfaceFrame>.Some(surface), Optional<SiteRenderFrame>.Some(sites), new[] { overlay });
            }

            return new DynamicFrameBundle(session, timeline, new ScopeRevision(sequence), sequence, sequence / 10d, sample, stateRevision, expectations, frames);
        }

        private static SurfaceFrame CreateSurface(AssetHash hash, StateRevision revision, RenderTemporalSample sample, int column)
        {
            float[] activity = new float[SurfaceVertices];
            float[] opacity = new float[SurfaceVertices];
            byte[] active = new byte[SurfaceVertices];
            for (int index = 0; index < SurfaceVertices; index++)
            {
                activity[index] = ((index * 17 + column * 31 + sample.Index * 13) % 1009) / 1008f;
                opacity[index] = 1f;
                active[index] = 1;
            }

            return new SurfaceFrame(hash, revision, sample, TemporalApplication.SampleAndHold, RenderBuffer<float>.TakeOwnership(activity), RenderBuffer<float>.TakeOwnership(opacity), RenderBuffer<byte>.TakeOwnership(active));
        }

        private static SiteRenderFrame CreateSites(AssetHash hash, StateRevision revision, RenderTemporalSample sample, int count, int column, int timelineIndex)
        {
            Float3[] positions = new Float3[count];
            Rgba32[] colors = new Rgba32[count];
            float[] sizes = new float[count];
            byte[] visibility = new byte[count];
            SiteRenderFlags[] flags = new SiteRenderFlags[count];
            for (int index = 0; index < count; index++)
            {
                float lower = ((index * 17 + column * 31 + timelineIndex * 13) % 1009) / 1008f;
                float upper = ((index * 17 + column * 31 + (timelineIndex + 1) * 13) % 1009) / 1008f;
                float value = sample.EvaluateLinear(lower, upper);
                positions[index] = new Float3(index % 250, index / 250, column);
                colors[index] = new Rgba32((byte)(value * 255f), 64, 192, 255);
                sizes[index] = 2f + value;
                visibility[index] = 1;
                flags[index] = SiteRenderFlags.None;
            }

            return new SiteRenderFrame(hash, revision, sample, TemporalApplication.Linear, RenderBuffer<Float3>.TakeOwnership(positions), RenderBuffer<Rgba32>.TakeOwnership(colors), RenderBuffer<float>.TakeOwnership(sizes), RenderBuffer<byte>.TakeOwnership(visibility), RenderBuffer<SiteRenderFlags>.TakeOwnership(flags));
        }

        private static CutOverlayFrame CreateOverlay(ContractId cutId, ContractId columnId, StateRevision revision, RenderTemporalSample sample, int column, int timelineIndex)
        {
            const int size = 64;
            Rgba32[] pixels = new Rgba32[size * size];
            for (int index = 0; index < pixels.Length; index++)
                pixels[index] = new Rgba32((byte)((index + column + timelineIndex) % 256), 32, 224, 255);
            return new CutOverlayFrame(cutId, columnId, revision, size, size, sample, TemporalApplication.SampleAndHold, new ScopeRevision((ulong)timelineIndex), RenderBuffer<Rgba32>.TakeOwnership(pixels));
        }

        private static ContractId Id(ulong value) => new(value, value + 1);
        private static AssetHash Hash(ulong value) => new(value, value + 1, value + 2, value + 3);
    }
}
