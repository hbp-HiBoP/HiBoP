using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace HBP.Transfer.Anatomy
{
    /// <summary>Prepared input values, never projected UVs. Channel order is the contact/native order.
    /// SurfaceValues hold sample Index; SiteValues are the exact Desktop TemporalSample.Evaluate result.
    /// Availability: 0 missing channel, 1 empty prepared channel, 2 available (possibly masked).</summary>
    public sealed class IEEGInstant
    {
        public string DatasetId { get; }
        public string DataName { get; }
        public string BlocId { get; }
        public string SubBlocId { get; }
        public string PreparedSha256 { get; }
        public int NavigationIndex { get; }
        public int NavigationLength { get; }
        public float NavigationHz { get; }
        public float LocalTimeMilliseconds { get; }
        public int ProjectionIndex { get; }
        public int ProjectionLength { get; }
        public float ProjectionHz { get; }
        public int SamplingPolicy { get; }
        public float Alpha { get; }
        public float SpanMin { get; }
        public float Middle { get; }
        public float SpanMax { get; }
        public IReadOnlyList<string> ChannelIds { get; }
        public IReadOnlyList<string> Units { get; }
        public AnatomyBuffer<byte> Availability { get; }
        public AnatomyBuffer<float> SurfaceValues { get; }
        public AnatomyBuffer<float> SiteValues { get; }
        public string Summary => string.Format(CultureInfo.InvariantCulture, "iEEG {0} | {1} ms | index {2} -> {3}, alpha {4} | {5}", DataName, LocalTimeMilliseconds, NavigationIndex, ProjectionIndex, Alpha, string.Join(", ", Units.Distinct().Select(unit => unit.Length == 0 ? "unit unspecified" : unit)));

        public IEEGInstant(string datasetId, string dataName, string blocId, string subBlocId, string preparedSha256, int navigationIndex, int navigationLength, float navigationHz, float localTimeMilliseconds, int projectionIndex, int projectionLength, float projectionHz, int samplingPolicy, float alpha, float spanMin, float middle, float spanMax, string[] channelIds, string[] units, byte[] availability, float[] surfaceValues, float[] siteValues)
        {
            foreach (string text in new[] { datasetId, dataName, blocId, subBlocId, preparedSha256 }) AnatomySnapshotCodec.ValidateText(text);
            if (preparedSha256.Length != 64 || preparedSha256.Any(c => !(c >= '0' && c <= '9' || c >= 'a' && c <= 'f'))) throw new ArgumentException("Invalid prepared data SHA-256.");
            AnatomySnapshot.ValidateFinite(new[] { navigationHz, localTimeMilliseconds, projectionHz, alpha, spanMin, middle, spanMax });
            if (navigationIndex < 0 || navigationIndex >= navigationLength || projectionIndex < 0 || projectionIndex >= projectionLength || navigationHz <= 0 || projectionHz <= 0 || samplingPolicy < 0 || samplingPolicy > 2 || alpha < 0 || alpha >= 1 || (alpha != 0 && (samplingPolicy != 2 || projectionIndex == projectionLength - 1)) || spanMin > middle || middle > spanMax)
                throw new ArgumentException("Invalid iEEG time or prepared normalization parameters.");
            if (channelIds == null || units == null || availability == null || surfaceValues == null || siteValues == null || channelIds.Length > AnatomyContactsCodec.MaximumCount || units.Length != channelIds.Length || availability.Length != channelIds.Length || surfaceValues.Length != channelIds.Length || siteValues.Length != channelIds.Length)
                throw new ArgumentException("Inconsistent iEEG channel buffers.");
            if (channelIds.Distinct(StringComparer.Ordinal).Count() != channelIds.Length) throw new ArgumentException("Duplicate iEEG channels.");
            AnatomySnapshot.ValidateFinite(surfaceValues);
            AnatomySnapshot.ValidateFinite(siteValues);
            for (int i = 0; i < channelIds.Length; i++)
            {
                AnatomySnapshotCodec.ValidateText(channelIds[i]);
                if (units[i] == null || Encoding.UTF8.GetByteCount(units[i]) > AnatomySnapshotCodec.MaximumTextBytes || availability[i] > 2 || (availability[i] != 2 && (surfaceValues[i] != 0 || siteValues[i] != 0)) || (alpha == 0 && surfaceValues[i] != siteValues[i]))
                    throw new ArgumentException("Invalid iEEG unit, availability or sampled values.");
            }

            DatasetId = datasetId;
            DataName = dataName;
            BlocId = blocId;
            SubBlocId = subBlocId;
            PreparedSha256 = preparedSha256;
            NavigationIndex = navigationIndex;
            NavigationLength = navigationLength;
            NavigationHz = navigationHz;
            LocalTimeMilliseconds = localTimeMilliseconds;
            ProjectionIndex = projectionIndex;
            ProjectionLength = projectionLength;
            ProjectionHz = projectionHz;
            SamplingPolicy = samplingPolicy;
            Alpha = alpha;
            SpanMin = spanMin;
            Middle = middle;
            SpanMax = spanMax;
            ChannelIds = Array.AsReadOnly((string[])channelIds.Clone());
            Units = Array.AsReadOnly((string[])units.Clone());
            Availability = new AnatomyBuffer<byte>((byte[])availability.Clone());
            SurfaceValues = new AnatomyBuffer<float>((float[])surfaceValues.Clone());
            SiteValues = new AnatomyBuffer<float>((float[])siteValues.Clone());
        }

        internal void Validate(AnatomyContacts contacts, AnatomyProjection projection)
        {
            if (projection == null || contacts.Sites.Count != ChannelIds.Count) throw new ArgumentException("iEEG requires projection inputs and matching contacts.");
            for (int i = 0; i < ChannelIds.Count; i++)
            {
                var site = contacts.Sites[i];
                if (ChannelIds[i] != contacts.PatientIds[site.PatientIndex] + "_" + site.Name || (Availability[i] != 2 && (site.Flags & AnatomySiteFlags.Masked) == 0))
                    throw new ArgumentException("iEEG channel association or absent-channel mask disagrees with contacts.");
            }
        }
    }

    internal static class IEEGInstantCodec
    {
        private static readonly UTF8Encoding Utf8 = new(false, true);
        internal static long Length(IEEGInstant x) => 52L + new[] { x.DatasetId, x.DataName, x.BlocId, x.SubBlocId, x.PreparedSha256 }.Sum(t => 4 + Utf8.GetByteCount(t)) + x.ChannelIds.Select((id, i) => 17L + Utf8.GetByteCount(id) + Utf8.GetByteCount(x.Units[i])).Sum();

        internal static void Write(BinaryWriter w, IEEGInstant x)
        {
            void Text(string t)
            {
                byte[] b = Utf8.GetBytes(t);
                w.Write(b.Length);
                w.Write(b);
            }

            foreach (string t in new[] { x.DatasetId, x.DataName, x.BlocId, x.SubBlocId, x.PreparedSha256 }) Text(t);
            w.Write(x.NavigationIndex);
            w.Write(x.NavigationLength);
            w.Write(x.NavigationHz);
            w.Write(x.LocalTimeMilliseconds);
            w.Write(x.ProjectionIndex);
            w.Write(x.ProjectionLength);
            w.Write(x.ProjectionHz);
            w.Write(x.SamplingPolicy);
            w.Write(x.Alpha);
            w.Write(x.SpanMin);
            w.Write(x.Middle);
            w.Write(x.SpanMax);
            w.Write(x.ChannelIds.Count);
            for (int i = 0; i < x.ChannelIds.Count; i++)
            {
                Text(x.ChannelIds[i]);
                Text(x.Units[i]);
                w.Write(x.Availability[i]);
                w.Write(x.SurfaceValues[i]);
                w.Write(x.SiteValues[i]);
            }
        }

        internal static IEEGInstant Read(BinaryReader r, long end)
        {
            string Text()
            {
                int n = r.ReadInt32();
                if (n < 0 || n > AnatomySnapshotCodec.MaximumTextBytes || n > end - r.BaseStream.Position) throw new InvalidDataException("Invalid iEEG text length.");
                return Utf8.GetString(r.ReadBytes(n));
            }

            string dataset = Text(), name = Text(), bloc = Text(), subBloc = Text(), hash = Text();
            int nav = r.ReadInt32(), navLength = r.ReadInt32();
            float navHz = r.ReadSingle(), time = r.ReadSingle();
            int index = r.ReadInt32(), length = r.ReadInt32();
            float hz = r.ReadSingle();
            int policy = r.ReadInt32();
            float alpha = r.ReadSingle();
            float min = r.ReadSingle(), middle = r.ReadSingle(), max = r.ReadSingle();
            int count = r.ReadInt32();
            if (count < 0 || count > AnatomyContactsCodec.MaximumCount || count * 17L > end - r.BaseStream.Position) throw new InvalidDataException("Invalid iEEG channel count.");
            var ids = new string[count];
            var units = new string[count];
            var availability = new byte[count];
            var surface = new float[count];
            var sites = new float[count];
            for (int i = 0; i < count; i++)
            {
                ids[i] = Text();
                units[i] = Text();
                availability[i] = r.ReadByte();
                surface[i] = r.ReadSingle();
                sites[i] = r.ReadSingle();
            }

            if (r.BaseStream.Position != end) throw new InvalidDataException("Invalid iEEG section length.");
            return new IEEGInstant(dataset, name, bloc, subBloc, hash, nav, navLength, navHz, time, index, length, hz, policy, alpha, min, middle, max, ids, units, availability, surface, sites);
        }
    }
}
