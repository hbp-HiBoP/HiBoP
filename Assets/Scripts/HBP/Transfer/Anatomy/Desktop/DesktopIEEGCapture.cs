using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Cysharp.Threading.Tasks;
using HBP.Data.Module3D;

namespace HBP.Transfer.Anatomy.Desktop
{
    public static class DesktopIEEGCapture
    {
        /// <summary>All prepared data and parameters are read without yielding, on the Unity thread.
        /// The digest identifies full prepared series in contact order, including units and availability.</summary>
        public static IEEGInstant Capture(Column3DIEEG column)
        {
            if (!PlayerLoopHelper.IsMainThread) throw new InvalidOperationException("iEEG capture requires the Unity main thread.");
            var data = column.ColumnIEEGData;
            var timeline = column.Timeline;
            var projection = column.ProjectionTimeline;
            if (timeline == null || projection == null || data.Dataset == null || data.Bloc == null) throw new InvalidOperationException("iEEG data and timelines are not prepared.");
            int navigation = timeline.CurrentIndex;
            var pair = timeline.SubTimelinesBySubBloc.FirstOrDefault(p => p.Value == timeline.CurrentSubtimeline);
            if (pair.Key == null || !projection.SubTimelinesBySubBloc.ContainsKey(pair.Key)) throw new InvalidOperationException("iEEG selection has no matching projection sub-bloc.");
            var sample = column.CurrentProjectionSample;
            int count = column.Sites.Count;
            if (column.ActivityValuesBySiteID.Length != count || column.ActivityUnitsBySiteID.Length != count || column.ActivityValues.LongLength != (long)count * projection.Length)
                throw new InvalidOperationException("iEEG prepared buffers do not match the selected implantation/timeline.");
            var ids = new string[count];
            var units = new string[count];
            var availability = new byte[count];
            var surface = new float[count];
            var sites = new float[count];
            using var sha = SHA256.Create();
            using var hashing = new CryptoStream(Stream.Null, sha, CryptoStreamMode.Write);
            using var writer = new BinaryWriter(hashing, Encoding.UTF8, true);
            for (int i = 0; i < count; i++)
            {
                var site = column.Sites[i];
                if (site.Information.Index != i) throw new InvalidOperationException("iEEG site order is inconsistent.");
                ids[i] = site.Information.FullID;
                units[i] = column.ActivityUnitsBySiteID[i];
                bool present = data.Data.ProcessedValuesByChannel.TryGetValue(ids[i], out var prepared);
                availability[i] = !present ? (byte)0 : prepared.Length == 0 ? (byte)1 : (byte)2;
                var values = column.ActivityValuesBySiteID[i];
                if (values == null || values.Length != projection.Length || sample.Index < 0 || sample.Index >= values.Length || (availability[i] != 2 && !site.State.IsMasked)) throw new InvalidOperationException("iEEG channel length or missing-channel mask is inconsistent.");
                writer.Write(ids[i]);
                writer.Write(units[i]);
                writer.Write(availability[i]);
                writer.Write(values.Length);
                for (int j = 0; j < values.Length; j++)
                {
                    float value = values[j];
                    if (float.IsNaN(value) || float.IsInfinity(value) || column.ActivityValues[j * count + i] != value || (availability[i] == 2 ? prepared.Length != values.Length || prepared[j] != value : value != 0))
                        throw new InvalidOperationException("iEEG channel and native input buffers disagree or contain invalid values.");
                    writer.Write(value);
                }

                surface[i] = values[sample.Index];
                sites[i] = sample.Evaluate(values);
            }

            writer.Flush();
            hashing.FlushFinalBlock();
            var p = column.DynamicParameters;
            return new IEEGInstant(data.Dataset.ID, data.DataName, data.Bloc.ID, pair.Key.ID, BitConverter.ToString(sha.Hash).Replace("-", "").ToLowerInvariant(), navigation, timeline.Length, timeline.Frequency.Value, pair.Value.GetLocalTime(navigation), sample.Index, projection.Length, projection.Frequency.Value, (int)column.TemporalSampling, sample.Alpha, p.SpanMin, p.Middle, p.SpanMax, ids, units, availability, surface, sites);
        }
    }
}
