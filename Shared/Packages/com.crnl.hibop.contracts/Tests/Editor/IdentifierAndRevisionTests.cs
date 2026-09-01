using System;
using NUnit.Framework;

namespace CRNL.HiBoP.Contracts.Tests
{
    public class IdentifierAndRevisionTests
    {
        [Test]
        public void IdentifierUsesCanonicalBigEndianBytesAndText()
        {
            ContractId id = new(0x0011223344556677, 0x8899aabbccddeeff);
            byte[] bytes = new byte[ContractId.ByteLength];

            id.WriteBytes(bytes);

            Assert.That(bytes, Is.EqualTo(new byte[]
            {
                0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
                0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff,
            }));
            Assert.That(id.ToString(), Is.EqualTo("00112233445566778899aabbccddeeff"));
            Assert.That(ContractId.FromBytes(bytes), Is.EqualTo(id));
            Assert.That(ContractId.Parse(id.ToString().ToUpperInvariant()), Is.EqualTo(id));
        }

        [Test]
        public void IdentifierEqualityAndHashAreValueBased()
        {
            ContractId left = new(17, 23);
            ContractId same = new(17, 23);
            ContractId different = new(17, 24);

            Assert.That(left, Is.EqualTo(same));
            Assert.That(left.GetHashCode(), Is.EqualTo(same.GetHashCode()));
            Assert.That(left, Is.Not.EqualTo(different));
            Assert.That(left < different, Is.True);
        }

        [Test]
        public void ZeroIdentifierIsRejectedEverywhere()
        {
            Assert.Throws<ArgumentException>(() => _ = new ContractId(0, 0));
            Assert.That(ContractId.TryParse(new string('0', ContractId.TextLength), out _), Is.False);
            Assert.Throws<FormatException>(() => ContractId.Parse(new string('0', ContractId.TextLength)));
            Assert.Throws<ArgumentException>(() => ContractId.FromBytes(new byte[ContractId.ByteLength]));
        }

        [Test]
        public void RevisionsAdvanceAndDetectOverflow()
        {
            Assert.That(new StateRevision(41).Next(), Is.EqualTo(new StateRevision(42)));
            Assert.That(new ScopeRevision(6).Next(), Is.EqualTo(new ScopeRevision(7)));
            Assert.That(new InteractionSequence(8).Next(), Is.EqualTo(new InteractionSequence(9)));
            Assert.Throws<OverflowException>(() => new StateRevision(ulong.MaxValue).Next());
            Assert.Throws<OverflowException>(() => new ScopeRevision(ulong.MaxValue).Next());
            Assert.Throws<OverflowException>(() => new InteractionSequence(ulong.MaxValue).Next());
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new InteractionSequence(0));
        }

        [Test]
        public void AssetHashRoundTripsCanonicalSha256Representation()
        {
            AssetHash hash = new(1, 2, 3, 4);
            byte[] bytes = new byte[AssetHash.ByteLength];

            hash.WriteBytes(bytes);

            Assert.That(AssetHash.FromBytes(bytes), Is.EqualTo(hash));
            Assert.That(AssetHash.Parse(hash.ToString()), Is.EqualTo(hash));
            Assert.That(hash.ToString(), Has.Length.EqualTo(AssetHash.TextLength));
            Assert.That(AssetHash.TryParse(new string('0', AssetHash.TextLength), out _), Is.False);
        }

        [Test]
        public void OptionalDistinguishesAbsentFromDefaultValue()
        {
            Optional<ulong> absent = Optional<ulong>.None;
            Optional<ulong> zero = Optional<ulong>.Some(0);

            Assert.That(absent.HasValue, Is.False);
            Assert.That(zero.HasValue, Is.True);
            Assert.That(zero.Value, Is.Zero);
            Assert.That(absent, Is.Not.EqualTo(zero));
            Assert.Throws<InvalidOperationException>(() => _ = absent.Value);
        }
    }
}
