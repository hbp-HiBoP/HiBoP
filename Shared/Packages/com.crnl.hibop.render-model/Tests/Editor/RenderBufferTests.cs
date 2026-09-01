using NUnit.Framework;

namespace CRNL.HiBoP.RenderModel.Tests
{
    public class RenderBufferTests
    {
        [Test]
        public void CopyFrom_IsAnExplicitDefensiveSnapshot()
        {
            float[] source = { 1f, 2f, 3f };
            RenderBuffer<float> buffer = RenderBuffer<float>.CopyFrom(source);

            source[1] = 99f;

            Assert.That(buffer[1], Is.EqualTo(2f));
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 1f, 2f, 3f }));
        }

        [Test]
        public void PublicIndexerCannotExposeStorageAndToArrayIsAnExplicitCopy()
        {
            RenderBuffer<int> buffer = RenderBuffer<int>.TakeOwnership(new[] { 4, 5, 6 });

            int[] copy = buffer.ToArray();
            copy[0] = 100;

            Assert.That(buffer[0], Is.EqualTo(4));
            Assert.That(buffer.Count, Is.EqualTo(3));
            Assert.That(typeof(RenderBuffer<int>).GetProperty("Memory"), Is.Null);
            Assert.That(typeof(RenderBuffer<int>).GetProperty("Span"), Is.Null);
        }
    }
}
