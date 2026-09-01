using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CRNL.HiBoP.Contracts.Tests
{
    public class ScopeAndValueTests
    {
        [TestCase(ScopeType.Project)]
        [TestCase(ScopeType.Visualization)]
        [TestCase(ScopeType.Column)]
        [TestCase(ScopeType.Site)]
        [TestCase(ScopeType.Cut)]
        [TestCase(ScopeType.Roi)]
        [TestCase(ScopeType.Timeline)]
        public void DesktopOwnsScientificScopes(ScopeType type)
        {
            Assert.That(ScopeOwnership.GetOwner(type), Is.EqualTo(ScopeOwner.Desktop));
        }

        [Test]
        public void QuestOwnsBrainInstanceScope()
        {
            Assert.That(ScopeOwnership.GetOwner(ScopeType.BrainInstance), Is.EqualTo(ScopeOwner.Quest));
        }

        [Test]
        public void ScopeIdentityIsSeparateFromEntityMeaning()
        {
            ContractId sameOpaqueValue = new(1, 9);
            ScopeKey column = new(ScopeType.Column, sameOpaqueValue);
            ScopeKey site = new(ScopeType.Site, sameOpaqueValue);

            Assert.That(column, Is.Not.EqualTo(site));
        }

        [Test]
        public void NumberVectorTakesDefensiveCopyAndRedactsValuesInToString()
        {
            double[] source = { 1.25, 2.5, 5.0 };
            ContractValue value = ContractValue.FromNumbers(source);
            source[0] = 999;

            Assert.That(value.Numbers[0], Is.EqualTo(1.25));
            Assert.That(value.ToString(), Is.EqualTo("ContractValue(kind=NumberVector, count=3)"));
            Assert.That(value.ToString(), Does.Not.Contain("1.25"));
        }

        [Test]
        public void ContractValuesRejectNonFiniteNumbersAndInvalidIds()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ContractValue.FromNumber(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => ContractValue.FromNumbers(new[] { double.PositiveInfinity }));
            Assert.Throws<ArgumentException>(() => ContractValue.FromId(default));
        }

        [Test]
        public void WrongUnionArmCannotBeReadAsDefaultSentinel()
        {
            ContractValue boolean = ContractValue.FromBoolean(false);

            Assert.Throws<InvalidOperationException>(() => _ = boolean.Number);
            Assert.Throws<InvalidOperationException>(() => _ = boolean.Ids);
        }

        [Test]
        public void ValueEqualityIncludesOrderedContents()
        {
            ContractValue left = ContractValue.FromIds(new[] { new ContractId(1, 1), new ContractId(1, 2) });
            ContractValue same = ContractValue.FromIds(new List<ContractId> { new(1, 1), new(1, 2) });
            ContractValue reordered = ContractValue.FromIds(new[] { new ContractId(1, 2), new ContractId(1, 1) });

            Assert.That(left, Is.EqualTo(same));
            Assert.That(left.GetHashCode(), Is.EqualTo(same.GetHashCode()));
            Assert.That(left, Is.Not.EqualTo(reordered));
        }

        [Test]
        public void ContractVersionMakesMajorIncompatibilityExplicit()
        {
            ContractVersion v1 = ContractVersion.V1;
            ContractVersion v2 = new(2, 0);

            Assert.That(v1.Major, Is.Not.EqualTo(v2.Major));
            Assert.That(v1.CompareTo(v2), Is.LessThan(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new ContractVersion(0, 1));
        }
    }
}
