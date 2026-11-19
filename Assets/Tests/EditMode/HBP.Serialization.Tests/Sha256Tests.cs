using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HBP.Core.Tools;
using NUnit.Framework;

namespace HBP.Tests.Serialization
{
    public class Sha256Tests
    {
        [TestCase("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
        [TestCase("abc", "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")]
        public void KnownVectors(string input, string expected)
        {
            byte[] actual = Sha256.ComputeHash(Encoding.ASCII.GetBytes(input));
            Assert.That(BitConverter.ToString(actual).Replace("-", "").ToLowerInvariant(), Is.EqualTo(expected));
        }

        [Test]
        public void IncrementalOffsetsResetAndReuseMatchReference()
        {
            using var reference = SHA256.Create();
            using var native = Sha256.Create();
            foreach (int length in new[] { 0, 55, 64, 262144, 262161, 1048576 })
            {
                byte[] data = new byte[length + 7];
                new Random(71).NextBytes(data);
                byte[] expected = reference.ComputeHash(data, 7, length);
                Assert.That(native.HashSize, Is.EqualTo(256));
                Assert.That(native.ComputeHash(data, 7, length), Is.EqualTo(expected));
                native.Initialize();
                int split = length / 2;
                native.TransformBlock(data, 7, split, null, 0);
                native.TransformFinalBlock(data, 7 + split, length - split);
                Assert.That(native.Hash, Is.EqualTo(expected));
            }
        }

        [Test]
        public void StreamHashHandlesShortReadsAndLeavesStreamOpenAtEof()
        {
            byte[] data = new byte[524305];
            new Random(72).NextBytes(data);
            using var reference = SHA256.Create();
            using var stream = new ShortReadStream(data) { Position = 7 };
            Assert.That(Sha256.ComputeHash(stream), Is.EqualTo(reference.ComputeHash(data, 7, data.Length - 7)));
            Assert.That(stream.CanRead, Is.True);
            Assert.That(stream.Position, Is.EqualTo(stream.Length));
            Assert.That(Sha256.ComputeHash(stream), Is.EqualTo(reference.ComputeHash(Array.Empty<byte>())));
        }

        [Test]
        public void FileHelperRetainsLowercaseHexContract()
        {
            string path = Path.GetTempFileName();
            try
            {
                File.WriteAllText(path, "abc", new UTF8Encoding(false));
                Assert.That(StandardData.HashFile(path), Is.EqualTo("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void ContextsAreIndependentAndDisposedContextsCannotBeReused()
        {
            Parallel.For(0, 16, i =>
            {
                byte[] data = new byte[65537 + i];
                new Random(i).NextBytes(data);
                using var reference = SHA256.Create();
                Assert.That(Sha256.ComputeHash(data), Is.EqualTo(reference.ComputeHash(data)));
            });
            var disposed = Sha256.Create();
            disposed.Dispose();
            Assert.Throws<ObjectDisposedException>(() => disposed.ComputeHash(new byte[1]));
            Assert.Throws<ArgumentNullException>(() => Sha256.ComputeHash((Stream)null));
        }

        private sealed class ShortReadStream : MemoryStream
        {
            public ShortReadStream(byte[] data) : base(data)
            {
            }

            public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, 997));
        }
    }
}
