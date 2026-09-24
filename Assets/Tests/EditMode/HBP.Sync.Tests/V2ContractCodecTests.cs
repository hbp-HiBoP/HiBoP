using System;
using System.IO;
using System.Text;
using HBP.Sync;
using HBP.Sync.Testing;
using NUnit.Framework;

namespace HBP.Sync.Tests
{
    [Category(SyncTestCategories.Fast)]
    public class V2ContractCodecTests
    {
        [Test]
        public void Identities_RejectEmptyOversizeAndMalformedValues()
        {
            Assert.Throws<ArgumentException>(() => new SessionId(Guid.Empty));
            Assert.Throws<ArgumentException>(() => SessionId.FromBytes(new byte[15]));
            Assert.Throws<ArgumentException>(() => new ColumnId(string.Empty));
            Assert.Throws<ArgumentException>(() => new SiteId(new string('x', 257)));
            Assert.Throws<ArgumentException>(() => new CutId("bad\ncut"));
            Assert.Throws<ArgumentException>(() => new ResourceId("broken\uD800"));

            var normalized = new ColumnId("e\u0301");
            Assert.That(normalized.Value, Is.EqualTo("\u00E9"));
            Assert.That(normalized.Value, Is.Not.EqualTo("e\u0301"));
            Assert.That(new ColumnId("same"), Is.Not.EqualTo(new SiteId("same")));
            Assert.That(new ResourceId("same"), Is.Not.EqualTo(new TopologyId("same")));

            var session = new SessionId(GuidFor(1));
            Assert.That(SessionId.FromBytes(session.ToByteArray()), Is.EqualTo(session));

            var duplicate = CreateDesktopEnvelope(new SetTimelineAnchor(new ColumnId("column"), 1, false, false, 1, 0, 1), 1);
            Assert.Throws<ArgumentException>(() => new V2MutationEnvelope(new SessionId(duplicate.SceneId.Value), duplicate.SceneId, duplicate.IncarnationId, duplicate.OperationId, duplicate.ReliableStreamId, 1, 1, V2OriginDevice.Desktop, 1, null, duplicate.Mutation));
            Assert.Throws<ArgumentException>(() => new V2MutationEnvelope(duplicate.SessionId, duplicate.SceneId, duplicate.IncarnationId, duplicate.OperationId, duplicate.ReliableStreamId, 1, 1, V2OriginDevice.Desktop, 1, 0, duplicate.Mutation));
            var questCanonicalEcho = new V2MutationEnvelope(duplicate.SessionId, duplicate.SceneId, duplicate.IncarnationId, duplicate.OperationId, duplicate.ReliableStreamId, 2, 1, V2OriginDevice.Quest, 2, 0, duplicate.Mutation);
            Assert.That(questCanonicalEcho.CanonicalSequence, Is.EqualTo((ulong?)2));
            Assert.That(questCanonicalEcho.ObservedCanonicalSequence, Is.EqualTo((ulong?)0));
        }

        [Test]
        public void Envelope_RoundTripsCommittedDesktopAndQuestMutations()
        {
            var siteColor = new SetSiteColor(new ColumnId("column-A"), new SiteId("site-full-17"), -0.25f, 0.5f, 1.5f, 1f);
            V2MutationEnvelope desktop = CreateDesktopEnvelope(siteColor, 2, 27);
            byte[] desktopFrame = V2MutationEnvelopeCodec.Encode(desktop);

            Assert.That(Encoding.ASCII.GetString(desktopFrame, 0, 4), Is.EqualTo("HBS2"));
            Assert.That(ReadUInt16(desktopFrame, 4), Is.EqualTo(2));
            Assert.That(ReadUInt16(desktopFrame, 6), Is.EqualTo(1));
            Assert.That(ReadUInt16(desktopFrame, 8), Is.EqualTo(1));
            Assert.That(ReadUInt16(desktopFrame, 10), Is.EqualTo(V2MutationEnvelopeCodec.HeaderLength));
            Assert.That(ReadUInt32(desktopFrame, 12), Is.EqualTo((uint)(desktopFrame.Length - V2MutationEnvelopeCodec.HeaderLength)));
            Assert.That(desktopFrame[128], Is.EqualTo((byte)V2OriginDevice.Desktop));
            Assert.That(V2MutationEnvelopeCodec.Decode(desktopFrame).OperationId, Is.EqualTo(desktop.OperationId));

            V2MutationEnvelope decodedDesktop = V2MutationEnvelopeCodec.Decode(desktopFrame);
            Assert.That(decodedDesktop.SessionId, Is.EqualTo(desktop.SessionId));
            Assert.That(decodedDesktop.SceneId, Is.EqualTo(desktop.SceneId));
            Assert.That(decodedDesktop.IncarnationId, Is.EqualTo(desktop.IncarnationId));
            Assert.That(decodedDesktop.ReliableStreamId, Is.EqualTo(desktop.ReliableStreamId));
            Assert.That(decodedDesktop.ReliableFrameSequence, Is.EqualTo(desktop.ReliableFrameSequence));
            Assert.That(decodedDesktop.OriginSequence, Is.EqualTo(desktop.OriginSequence));
            Assert.That(decodedDesktop.CanonicalSequence, Is.EqualTo((ulong?)27));
            AssertSiteColor((SetSiteColor)decodedDesktop.Mutation, siteColor);

            var timeline = new SetTimelineAnchor(new ColumnId("column-B"), 3, true, false, 2, 0, 1000000);
            V2MutationEnvelope quest = CreateQuestEnvelope(timeline, 3, 0);
            byte[] questFrame = V2MutationEnvelopeCodec.Encode(quest);
            Assert.That(ReadUInt16(questFrame, 8), Is.EqualTo(2));
            Assert.That(ReadUInt64(questFrame, 112), Is.Zero);
            Assert.That(ReadUInt64(questFrame, 120), Is.Zero);
            Assert.That(questFrame[128], Is.EqualTo((byte)V2OriginDevice.Quest));

            V2MutationEnvelope decodedQuest = V2MutationEnvelopeCodec.Decode(questFrame);
            Assert.That(decodedQuest.OriginDevice, Is.EqualTo(V2OriginDevice.Quest));
            Assert.That(decodedQuest.CanonicalSequence, Is.Null);
            Assert.That(decodedQuest.ObservedCanonicalSequence, Is.EqualTo((ulong?)0));
            AssertTimelineAnchor((SetTimelineAnchor)decodedQuest.Mutation, timeline);

            var questCanonicalEcho = new V2MutationEnvelope(quest.SessionId, quest.SceneId, quest.IncarnationId, quest.OperationId, quest.ReliableStreamId, quest.ReliableFrameSequence + 1, quest.OriginSequence, V2OriginDevice.Quest, 28, 0, timeline);
            V2MutationEnvelope decodedQuestEcho = V2MutationEnvelopeCodec.Decode(V2MutationEnvelopeCodec.Encode(questCanonicalEcho));
            Assert.That(decodedQuestEcho.OperationId, Is.EqualTo(quest.OperationId));
            Assert.That(decodedQuestEcho.CanonicalSequence, Is.EqualTo((ulong?)28));
            Assert.That(decodedQuestEcho.ObservedCanonicalSequence, Is.EqualTo((ulong?)0));

            var canonicalEcho = new V2MutationEnvelope(desktop.SessionId, desktop.SceneId, desktop.IncarnationId, desktop.OperationId, desktop.ReliableStreamId, desktop.ReliableFrameSequence + 1, desktop.OriginSequence, V2OriginDevice.Desktop, 28, null, desktop.Mutation);
            Assert.That(V2MutationEnvelopeCodec.Decode(V2MutationEnvelopeCodec.Encode(canonicalEcho)).OperationId, Is.EqualTo(desktop.OperationId));
        }

        [Test]
        public void Envelope_RejectsOversizeTruncationAndTrailingBytesBeforeBodyAllocation()
        {
            byte[] valid = V2MutationEnvelopeCodec.Encode(CreateDesktopEnvelope(new SetSiteColor(new ColumnId("column"), new SiteId("site"), 0f, 0.5f, 1f, 1f), 4));

            byte[] oversizedLength = (byte[])valid.Clone();
            WriteUInt32(oversizedLength, 12, (uint)V2MutationEnvelopeCodec.MaximumPayloadBytes + 1);
            Assert.Throws<InvalidDataException>(() => V2MutationEnvelopeCodec.Decode(oversizedLength));

            byte[] truncated = (byte[])valid.Clone();
            Array.Resize(ref truncated, truncated.Length - 1);
            Assert.Throws<InvalidDataException>(() => V2MutationEnvelopeCodec.Decode(truncated));

            byte[] trailing = (byte[])valid.Clone();
            Array.Resize(ref trailing, trailing.Length + 1);
            Assert.Throws<InvalidDataException>(() => V2MutationEnvelopeCodec.Decode(trailing));

            byte[] trailingPayload = (byte[])valid.Clone();
            Array.Resize(ref trailingPayload, trailingPayload.Length + 1);
            WriteUInt32(trailingPayload, 12, ReadUInt32(trailingPayload, 12) + 1);
            Assert.Throws<InvalidDataException>(() => V2MutationEnvelopeCodec.Decode(trailingPayload));

            Assert.Throws<InvalidDataException>(() => V2MutationEnvelopeCodec.Decode(new byte[V2MutationEnvelopeCodec.MaximumFrameBytes + 1]));
        }

        [Test]
        public void Envelope_RejectsUnsupportedVersionKindFlagsAndReservedBytes()
        {
            byte[] valid = V2MutationEnvelopeCodec.Encode(CreateDesktopEnvelope(new SetTimelineAnchor(new ColumnId("column"), 0, false, false, 1, 0, 1), 5));

            AssertEnvelopeHeaderMutationRejected(valid, bytes => WriteUInt16(bytes, 4, 3));
            AssertEnvelopeHeaderMutationRejected(valid, bytes => WriteUInt16(bytes, 6, 2));
            AssertEnvelopeHeaderMutationRejected(valid, bytes => WriteUInt16(bytes, 8, 4));
            AssertEnvelopeHeaderMutationRejected(valid, bytes => WriteUInt16(bytes, 10, 135));
            AssertEnvelopeHeaderMutationRejected(valid, bytes => bytes[0] = (byte)'X');
            AssertEnvelopeHeaderMutationRejected(valid, bytes => bytes[128] = 3);
            AssertEnvelopeHeaderMutationRejected(valid, bytes => WriteUInt16(bytes, 8, 0));
            AssertEnvelopeHeaderMutationRejected(valid, bytes => WriteUInt16(bytes, 8, 3));
            AssertEnvelopeHeaderMutationRejected(valid, bytes => bytes[129] = 1);
            AssertEnvelopeHeaderMutationRejected(valid, bytes => WriteUInt16(bytes, V2MutationEnvelopeCodec.HeaderLength, 2));
            AssertEnvelopeHeaderMutationRejected(valid, bytes => WriteUInt16(bytes, V2MutationEnvelopeCodec.HeaderLength + 2, 99));
        }

        [Test]
        public void SiteColor_RoundTripsRequestedRgbaAndStableSiteIdentity()
        {
            var requested = new SetSiteColor(new ColumnId("column-9"), new SiteId("site-full-id-901"), -0.5f, 1.25f, 2f, 0.75f);
            V2MutationEnvelope decoded = V2MutationEnvelopeCodec.Decode(V2MutationEnvelopeCodec.Encode(CreateDesktopEnvelope(requested, 6)));
            var actual = (SetSiteColor)decoded.Mutation;

            Assert.That(actual.ColumnId.Value, Is.EqualTo("column-9"));
            Assert.That(actual.FullSiteId.Value, Is.EqualTo("site-full-id-901"));
            Assert.That(actual.ColumnId, Is.EqualTo(requested.ColumnId));
            Assert.That(actual.FullSiteId, Is.EqualTo(requested.FullSiteId));
            AssertSiteColor(actual, requested);
        }

        [Test]
        public void SiteColor_RejectsNonfiniteAndMalformedFields()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SetSiteColor(new ColumnId("c"), new SiteId("s"), float.NaN, 0f, 0f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SetSiteColor(new ColumnId("c"), new SiteId("s"), 0f, float.PositiveInfinity, 0f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SetSiteColor(new ColumnId("c"), new SiteId("s"), -0f, 0f, 0f, 1f));

            byte[] valid = V2MutationEnvelopeCodec.Encode(CreateDesktopEnvelope(new SetSiteColor(new ColumnId("column"), new SiteId("site"), 0f, 0f, 0f, 1f), 7));
            int rgbaOffset = SiteColorRgbaOffset(valid);

            byte[] nonfinite = (byte[])valid.Clone();
            WriteUInt32(nonfinite, rgbaOffset, 0x7FC00000);
            Assert.Throws<InvalidDataException>(() => V2MutationEnvelopeCodec.Decode(nonfinite));

            byte[] negativeZero = (byte[])valid.Clone();
            WriteUInt32(negativeZero, rgbaOffset, 0x80000000);
            Assert.Throws<InvalidDataException>(() => V2MutationEnvelopeCodec.Decode(negativeZero));

            byte[] malformedUtf8 = (byte[])valid.Clone();
            int firstColumnByte = V2MutationEnvelopeCodec.HeaderLength + 4 + 2;
            malformedUtf8[firstColumnByte] = 0xFF;
            Assert.Throws<InvalidDataException>(() => V2MutationEnvelopeCodec.Decode(malformedUtf8));

            byte[] controlCharacter = (byte[])valid.Clone();
            controlCharacter[firstColumnByte] = 1;
            Assert.Throws<InvalidDataException>(() => V2MutationEnvelopeCodec.Decode(controlCharacter));
        }

        [Test]
        public void CutDefinition_RoundTripsCompleteCustomAndAxisDefinitions()
        {
            var custom = new SetCutDefinition(new CutId("custom-cut-4"), V2CutOrientation.Custom, true, 65535, 0.25f, 0.25f, -0.5f, 0.75f);
            var axial = new SetCutDefinition(new CutId("axis-cut"), V2CutOrientation.Axial, false, 1, 1f, 0f, 1f, 0f);

            var decodedCustom = (SetCutDefinition)V2MutationPayloadCodec.Decode(V2MutationPayloadCodec.Encode(custom));
            var decodedAxial = (SetCutDefinition)V2MutationPayloadCodec.Decode(V2MutationPayloadCodec.Encode(axial));

            AssertCutDefinition(decodedCustom, custom);
            AssertCutDefinition(decodedAxial, axial);
        }

        [Test]
        public void CutDefinition_RejectsInvalidOrientationNormalPositionAndCount()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => NewCut((V2CutOrientation)4, 1, 0.5f, 0f, 1f, 0f));
            Assert.Throws<ArgumentException>(() => NewCut(V2CutOrientation.Custom, 1, 0.5f, 0f, 0f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => NewCut(V2CutOrientation.Axial, 1, -0.01f, 0f, 1f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => NewCut(V2CutOrientation.Axial, 1, 1.01f, 0f, 1f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => NewCut(V2CutOrientation.Axial, 0, 0.5f, 0f, 1f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => NewCut(V2CutOrientation.Axial, 65536, 0.5f, 0f, 1f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => NewCut(V2CutOrientation.Axial, 1, 0.5f, float.NaN, 1f, 0f));

            byte[] frame = V2MutationEnvelopeCodec.Encode(CreateDesktopEnvelope(NewCut(V2CutOrientation.Axial, 2, 0.5f, 0f, 1f, 0f), 8));
            int orientationOffset = V2MutationEnvelopeCodec.HeaderLength + 4 + 2 + Encoding.UTF8.GetByteCount("cut");
            frame[orientationOffset] = 4;
            Assert.Throws<InvalidDataException>(() => V2MutationEnvelopeCodec.Decode(frame));
        }

        [Test]
        public void TimelineAnchor_RoundTripsFlagsStepAndMonotonicClock()
        {
            var requested = new SetTimelineAnchor(new ColumnId("timeline-column"), 17, true, true, 1000000, long.MaxValue, 1000000000000UL);
            var decoded = (SetTimelineAnchor)V2MutationPayloadCodec.Decode(V2MutationPayloadCodec.Encode(requested));
            AssertTimelineAnchor(decoded, requested);
        }

        [Test]
        public void TimelineAnchor_RejectsInvalidIndexStepFrequencyAndFlags()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => NewTimeline(-1, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => NewTimeline(0, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => NewTimeline(0, 1000001, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => NewTimeline(0, 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => NewTimeline(0, 1, 1000000000001UL));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SetTimelineAnchor(new ColumnId("c"), 0, false, false, 1, -1, 1));

            byte[] frame = V2MutationEnvelopeCodec.Encode(CreateDesktopEnvelope(NewTimeline(3, 2, 1000), 9));
            int playingOffset = V2MutationEnvelopeCodec.HeaderLength + 4 + 2 + Encoding.UTF8.GetByteCount("column") + 4;
            frame[playingOffset] = 2;
            Assert.Throws<InvalidDataException>(() => V2MutationEnvelopeCodec.Decode(frame));

            frame = V2MutationEnvelopeCodec.Encode(CreateDesktopEnvelope(NewTimeline(3, 2, 1000), 10));
            frame[playingOffset + 1] = 255;
            Assert.Throws<InvalidDataException>(() => V2MutationEnvelopeCodec.Decode(frame));
        }

        [Test]
        public void Descriptors_DeriveExactCoalescingTouchedKeysAndBarrierScope()
        {
            V2MutationEnvelope siteEnvelope = CreateDesktopEnvelope(new SetSiteColor(new ColumnId("column-A"), new SiteId("site-full-12"), 0f, 0f, 0f, 1f), 11);
            V2MutationDescriptor siteDescriptor = siteEnvelope.Descriptor;
            AssertDescriptor(siteDescriptor, siteEnvelope, V2TouchedKeyKind.SiteColor);
            Assert.That(siteDescriptor.CoalescingKey.ColumnId.Value, Is.EqualTo("column-A"));
            Assert.That(siteDescriptor.CoalescingKey.SiteId.Value, Is.EqualTo("site-full-12"));
            Assert.That(siteDescriptor.CoalescingKey.CutId, Is.Null);
            Assert.That(siteDescriptor.CoalescingKey, Is.Not.EqualTo(CreateDesktopEnvelope(new SetSiteColor(new ColumnId("column-A"), new SiteId("site-full-13"), 0f, 0f, 0f, 1f), 12).Descriptor.CoalescingKey));
            Assert.That(siteDescriptor.CoalescingKey, Is.Not.EqualTo(CreateDesktopEnvelope(new SetSiteColor(new ColumnId("column-B"), new SiteId("site-full-12"), 0f, 0f, 0f, 1f), 12).Descriptor.CoalescingKey));
            Assert.That(siteDescriptor.CoalescingKey, Is.Not.EqualTo(new V2MutationEnvelope(siteEnvelope.SessionId, siteEnvelope.SceneId, new IncarnationId(GuidFor(999)), siteEnvelope.OperationId, siteEnvelope.ReliableStreamId, 1, 1, V2OriginDevice.Desktop, 1, null, siteEnvelope.Mutation).Descriptor.CoalescingKey));
            Assert.That(siteDescriptor.CoalescingKey, Is.Not.EqualTo(new V2MutationEnvelope(siteEnvelope.SessionId, new SceneId(GuidFor(998)), siteEnvelope.IncarnationId, siteEnvelope.OperationId, siteEnvelope.ReliableStreamId, 1, 1, V2OriginDevice.Desktop, 1, null, siteEnvelope.Mutation).Descriptor.CoalescingKey));

            V2MutationEnvelope cutEnvelope = CreateDesktopEnvelope(NewCut(V2CutOrientation.Custom, 1, 0.5f, 0f, 1f, 0f), 13);
            AssertDescriptor(cutEnvelope.Descriptor, cutEnvelope, V2TouchedKeyKind.CutDefinition);
            Assert.That(cutEnvelope.Descriptor.CoalescingKey.CutId.Value, Is.EqualTo("cut"));
            Assert.That(cutEnvelope.Descriptor.CoalescingKey.ColumnId, Is.Null);
            Assert.That(cutEnvelope.Descriptor.CoalescingKey, Is.Not.EqualTo(CreateDesktopEnvelope(new SetCutDefinition(new CutId("other-cut"), V2CutOrientation.Custom, false, 1, 0.5f, 0f, 1f, 0f), 15).Descriptor.CoalescingKey));

            V2MutationEnvelope timelineEnvelope = CreateDesktopEnvelope(NewTimeline(4, 3, 10), 14);
            AssertDescriptor(timelineEnvelope.Descriptor, timelineEnvelope, V2TouchedKeyKind.TimelineAnchor);
            Assert.That(timelineEnvelope.Descriptor.CoalescingKey.ColumnId.Value, Is.EqualTo("column"));
            Assert.That(timelineEnvelope.Descriptor.CoalescingKey.SiteId, Is.Null);
            Assert.That(timelineEnvelope.Descriptor.CoalescingKey, Is.Not.EqualTo(CreateDesktopEnvelope(new SetTimelineAnchor(new ColumnId("other-column"), 4, true, false, 3, 123456789L, 10), 16).Descriptor.CoalescingKey));
        }

        [Test]
        public void CheckpointRecords_RoundTripThreeTypedFamiliesAndRejectUnknownSchema()
        {
            var site = new SetSiteColor(new ColumnId("checkpoint-column"), new SiteId("checkpoint-site"), 0.25f, 0.5f, 0.75f, 1f);
            var cut = NewCut(V2CutOrientation.Sagittal, 8, 0.375f, 1f, 0f, 0f);
            var timeline = NewTimeline(42, 7, 1000000);

            byte[] siteBytes = new SiteColorCheckpointRecord(site).Encode();
            byte[] cutBytes = new CutDefinitionCheckpointRecord(cut).Encode();
            byte[] timelineBytes = new TimelineAnchorCheckpointRecord(timeline).Encode();
            Assert.That(ReadUInt16(siteBytes, 0), Is.EqualTo((ushort)V2OperationType.SetSiteColor));
            Assert.That(ReadUInt16(siteBytes, 2), Is.EqualTo(1));
            Assert.That(ReadUInt16(siteBytes, 4), Is.EqualTo(siteBytes.Length - 6));
            AssertSiteColor(SiteColorCheckpointRecord.Decode(siteBytes).Value, site);
            AssertCutDefinition(CutDefinitionCheckpointRecord.Decode(cutBytes).Value, cut);
            AssertTimelineAnchor(TimelineAnchorCheckpointRecord.Decode(timelineBytes).Value, timeline);

            byte[] unknownSchema = (byte[])siteBytes.Clone();
            WriteUInt16(unknownSchema, 2, 2);
            Assert.Throws<InvalidDataException>(() => SiteColorCheckpointRecord.Decode(unknownSchema));

            byte[] unknownRecord = (byte[])siteBytes.Clone();
            WriteUInt16(unknownRecord, 0, 99);
            Assert.Throws<InvalidDataException>(() => SiteColorCheckpointRecord.Decode(unknownRecord));
        }

        private static V2MutationEnvelope CreateDesktopEnvelope(V2Mutation mutation, int seed, ulong canonicalSequence = 1)
        {
            int identityBase = seed * 10;
            return new V2MutationEnvelope(new SessionId(GuidFor(identityBase + 1)), new SceneId(GuidFor(identityBase + 2)), new IncarnationId(GuidFor(identityBase + 3)), new OperationId(GuidFor(identityBase + 4)), new ReliableStreamId(GuidFor(identityBase + 5)), 1, 1, V2OriginDevice.Desktop, canonicalSequence, null, mutation);
        }

        private static V2MutationEnvelope CreateQuestEnvelope(V2Mutation mutation, int seed, ulong observedCanonicalSequence)
        {
            int identityBase = seed * 10;
            return new V2MutationEnvelope(new SessionId(GuidFor(identityBase + 1)), new SceneId(GuidFor(identityBase + 2)), new IncarnationId(GuidFor(identityBase + 3)), new OperationId(GuidFor(identityBase + 4)), new ReliableStreamId(GuidFor(identityBase + 5)), 1, 1, V2OriginDevice.Quest, null, observedCanonicalSequence, mutation);
        }

        private static Guid GuidFor(int value) => new Guid(value, 0x1234, 0x5678, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        private static SetCutDefinition NewCut(V2CutOrientation orientation, uint numberOfCuts, float position, float normalX, float normalY, float normalZ) => new SetCutDefinition(new CutId("cut"), orientation, false, numberOfCuts, position, normalX, normalY, normalZ);

        private static SetTimelineAnchor NewTimeline(int index, int step, ulong frequency) => new SetTimelineAnchor(new ColumnId("column"), index, true, false, step, 123456789L, frequency);

        private static void AssertEnvelopeHeaderMutationRejected(byte[] valid, Action<byte[]> mutate)
        {
            byte[] changed = (byte[])valid.Clone();
            mutate(changed);
            Assert.Throws<InvalidDataException>(() => V2MutationEnvelopeCodec.Decode(changed));
        }

        private static void AssertDescriptor(V2MutationDescriptor descriptor, V2MutationEnvelope envelope, V2TouchedKeyKind kind)
        {
            Assert.That(descriptor.CoalescingKey.SceneId, Is.EqualTo(envelope.SceneId));
            Assert.That(descriptor.CoalescingKey.IncarnationId, Is.EqualTo(envelope.IncarnationId));
            Assert.That(descriptor.CoalescingKey.Kind, Is.EqualTo(kind));
            Assert.That(descriptor.SupportsLatestUnsentCoalescing, Is.True);
            Assert.That(descriptor.BarrierScope, Is.EqualTo(V2BarrierScope.None));
            Assert.That(descriptor.TouchedKeys.Count, Is.EqualTo(1));
            Assert.That(descriptor.TouchedKeys[0], Is.EqualTo(descriptor.CoalescingKey));
        }

        private static void AssertSiteColor(SetSiteColor actual, SetSiteColor expected)
        {
            Assert.That(actual.ColumnId, Is.EqualTo(expected.ColumnId));
            Assert.That(actual.FullSiteId, Is.EqualTo(expected.FullSiteId));
            Assert.That(actual.Red, Is.EqualTo(expected.Red));
            Assert.That(actual.Green, Is.EqualTo(expected.Green));
            Assert.That(actual.Blue, Is.EqualTo(expected.Blue));
            Assert.That(actual.Alpha, Is.EqualTo(expected.Alpha));
        }

        private static void AssertCutDefinition(SetCutDefinition actual, SetCutDefinition expected)
        {
            Assert.That(actual.CutId, Is.EqualTo(expected.CutId));
            Assert.That(actual.Orientation, Is.EqualTo(expected.Orientation));
            Assert.That(actual.Flip, Is.EqualTo(expected.Flip));
            Assert.That(actual.NumberOfCuts, Is.EqualTo(expected.NumberOfCuts));
            Assert.That(actual.Position, Is.EqualTo(expected.Position));
            Assert.That(actual.NormalX, Is.EqualTo(expected.NormalX));
            Assert.That(actual.NormalY, Is.EqualTo(expected.NormalY));
            Assert.That(actual.NormalZ, Is.EqualTo(expected.NormalZ));
        }

        private static void AssertTimelineAnchor(SetTimelineAnchor actual, SetTimelineAnchor expected)
        {
            Assert.That(actual.ColumnId, Is.EqualTo(expected.ColumnId));
            Assert.That(actual.Index, Is.EqualTo(expected.Index));
            Assert.That(actual.Playing, Is.EqualTo(expected.Playing));
            Assert.That(actual.Looping, Is.EqualTo(expected.Looping));
            Assert.That(actual.Step, Is.EqualTo(expected.Step));
            Assert.That(actual.MonotonicAnchorTicks, Is.EqualTo(expected.MonotonicAnchorTicks));
            Assert.That(actual.TickFrequency, Is.EqualTo(expected.TickFrequency));
        }

        private static int SiteColorRgbaOffset(byte[] frame)
        {
            int offset = V2MutationEnvelopeCodec.HeaderLength + 4;
            ushort columnLength = ReadUInt16(frame, offset);
            offset += 2 + columnLength;
            ushort siteLength = ReadUInt16(frame, offset);
            return offset + 2 + siteLength;
        }

        private static ushort ReadUInt16(byte[] bytes, int offset) => (ushort)(bytes[offset] | bytes[offset + 1] << 8);
        private static uint ReadUInt32(byte[] bytes, int offset) => (uint)(bytes[offset] | bytes[offset + 1] << 8 | bytes[offset + 2] << 16 | bytes[offset + 3] << 24);
        private static ulong ReadUInt64(byte[] bytes, int offset) => ReadUInt32(bytes, offset) | ((ulong)ReadUInt32(bytes, offset + 4) << 32);

        private static void WriteUInt16(byte[] bytes, int offset, ushort value)
        {
            bytes[offset] = (byte)value;
            bytes[offset + 1] = (byte)(value >> 8);
        }

        private static void WriteUInt32(byte[] bytes, int offset, uint value)
        {
            for (int i = 0; i < 4; i++)
                bytes[offset + i] = (byte)(value >> (8 * i));
        }
    }
}
