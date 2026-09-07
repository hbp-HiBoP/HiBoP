using System;
using System.Collections.Generic;
using System.Diagnostics;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.Protocol;
using CRNL.HiBoP.RenderModel;
using NUnit.Framework;

namespace CRNL.HiBoP.XR.Cuts.Tests
{
    public class P12CutPipelineBenchmarkTests
    {
        private const int Iterations = 10;
        private const double BestMeasuredQuestMegabitsPerSecond = 38.873;

        [Test]
        [Timeout(360000)]
        public void MeasureOptimisticRemotePipeline_ForCurrentAndExtremeCuts()
        {
            Measure("D2-current", 1, 512);
            Measure("D2-current", 3, 512);
            Measure("D4-extreme", 8, 2048);
        }

        private static void Measure(string dataset, int columnCount, int textureSize)
        {
            SessionEpoch session = new(Id(1), 1);
            CutRenderResult result = CreateResult(columnCount, textureSize);
            var columns = new ContractId[columnCount];
            for (int column = 0; column < columns.Length; column++)
                columns[column] = Id((ulong)(10 + column));
            var manifest = new CutResultManifest(columns, textureSize, textureSize);
            int iterations = dataset == "D4-extreme" ? 5 : Iterations;
            var encode = new List<double>(iterations);
            var copy = new List<double>(iterations);
            var decode = new List<double>(iterations);
            var endToEnd = new List<double>(iterations);
            var watch = new Stopwatch();
            int payloadBytes = 0;
            int pixelCount = checked(textureSize * textureSize);

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                watch.Restart();
                EncodedCutResult encoded = CutResultCodec.Encode(session, result, manifest);
                encode.Add(watch.Elapsed.TotalMilliseconds);
                payloadBytes = encoded.Descriptor.ByteLength;

                watch.Restart();
                byte[] loopback = (byte[])encoded.Payload.Clone();
                copy.Add(watch.Elapsed.TotalMilliseconds);

                watch.Restart();
                DecodedCutResult decoded = CutResultCodec.Decode(encoded.Descriptor, loopback, encoded.Descriptor.ByteLength);
                decode.Add(watch.Elapsed.TotalMilliseconds);
                Assert.That(decoded.Result.Overlays.Count, Is.EqualTo(columnCount));
                Assert.That(decoded.Result.Overlays[columnCount - 1].Pixels[pixelCount - 1], Is.EqualTo(result.Overlays[columnCount - 1].Pixels[pixelCount - 1]));
                endToEnd.Add(encode[^1] + copy[^1] + decode[^1]);
            }

            double optimisticNetworkMilliseconds = payloadBytes * 8d / (BestMeasuredQuestMegabitsPerSecond * 1_000_000d) * 1000d;
            Write(dataset, columnCount, textureSize, payloadBytes, "encode", encode);
            Write(dataset, columnCount, textureSize, payloadBytes, "loopback-copy", copy);
            Write(dataset, columnCount, textureSize, payloadBytes, "decode", decode);
            Write(dataset, columnCount, textureSize, payloadBytes, "host-codec-e2e", endToEnd);
            TestContext.Out.WriteLine($"P12_BOUND dataset={dataset} columns={columnCount} texture={textureSize} bytes={payloadBytes} bestMeasuredQuestMbps={BestMeasuredQuestMegabitsPerSecond:F3} optimisticNetworkMs={optimisticNetworkMilliseconds:F3}");

            if (dataset == "D4-extreme")
                Assert.That(optimisticNetworkMilliseconds, Is.GreaterThan(250d), "The D4 decision test must preserve the measured remote lower-bound failure until a new strategy is approved.");
        }

        private static CutRenderResult CreateResult(int columnCount, int textureSize)
        {
            ContractId cutId = Id(2);
            StateRevision state = new(1);
            RenderTemporalSample sample = new(0, 0);
            int pixelCount = checked(textureSize * textureSize);
            var overlays = new CutOverlayFrame[columnCount];
            for (int column = 0; column < columnCount; column++)
            {
                var pixels = new Rgba32[pixelCount];
                for (int index = 0; index < pixels.Length; index++)
                    pixels[index] = new Rgba32((byte)(index + column), (byte)(index >> 8), (byte)(column * 17), 255);
                overlays[column] = new CutOverlayFrame(cutId, Id((ulong)(10 + column)), state, textureSize, textureSize, sample, TemporalApplication.SampleAndHold, new ScopeRevision(1), RenderBuffer<Rgba32>.TakeOwnership(pixels));
            }

            return new CutRenderResult(cutId, Id(3), new InteractionSequence(1), new ScopeRevision(1), new ScopeRevision(1), state, sample, new Plane3F(new Float3(0, 0, 1), 0), Hash(20), Optional<CutGeometryAsset>.None, Hash(30), Optional<TextureAsset>.None, overlays);
        }

        private static void Write(string dataset, int columns, int textureSize, int bytes, string stage, List<double> samples)
        {
            samples.Sort();
            TestContext.Out.WriteLine($"P12_METRIC dataset={dataset} columns={columns} texture={textureSize} bytes={bytes} stage={stage} p50Ms={Percentile(samples, 0.50):F3} p95Ms={Percentile(samples, 0.95):F3} maxMs={samples[^1]:F3}");
        }

        private static double Percentile(IReadOnlyList<double> sorted, double percentile)
        {
            int index = Math.Max(0, (int)Math.Ceiling(sorted.Count * percentile) - 1);
            return sorted[index];
        }

        private static ContractId Id(ulong value) => new(value, value + 1);
        private static AssetHash Hash(ulong value) => new(value, value + 1, value + 2, value + 3);
    }
}
