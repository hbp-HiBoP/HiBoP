using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HBP.Sync
{
    public sealed class SignalCurve
    {
        public string Name = "", Id = "", Resource = "";
        public bool Enabled;
        public float[] Color = { 1, 1, 1, 1 };
        public float Thickness = 3;
        public bool HasShape;
        public SignalCurve[] Children = Array.Empty<SignalCurve>();
    }

    public sealed class SignalGraph
    {
        public float XMin, XMax, YMin, YMax;
        public string XLabel = "Latency", XUnit = "ms", YLabel = "Activity", YUnit = "uV";
        public string[] ColumnIds = Array.Empty<string>(), SubBlocIds = Array.Empty<string>();
        public SignalCurve[] Curves = Array.Empty<SignalCurve>();
    }

    public sealed class SignalEvent
    {
        public string Name = "";
        public int Trial;
        public float Position;
        public float[] Color = { 1, 1, 1, 1 };
    }

    public sealed class SignalMatrix
    {
        public string Title = "", Resource = "", Unit = "";
        public float Start, End, WindowStart, WindowEnd;
        public int Rows, Columns;
        public SignalEvent[] Events = Array.Empty<SignalEvent>();
    }

    public sealed class SignalMatrixRow
    {
        public string Title = "", Group = "";
        public string GroupTitle = "", BlocId = "", BlocTitle = "", ChannelId = "", ChannelTitle = "";
        public float Min, Max;
        public bool[] Selected = Array.Empty<bool>();
        public SignalMatrix[] Cells = Array.Empty<SignalMatrix>();
    }

    /// <summary>Transient presentation, deliberately outside the scientific mutation journal.</summary>
    public sealed class SignalSnapshot
    {
        public string Title = "";
        public long Revision;
        public int ActiveGraph, SmoothingFactor = 1, Format;
        public bool Smooth, Smooth2D;
        public float TrialHeight, TrialRatio, BlocRatio, ProtocolRatio;
        public byte[] Colormap = Array.Empty<byte>();
        public SignalGraph[] Graphs = Array.Empty<SignalGraph>();
        public SignalMatrixRow[] Matrices = Array.Empty<SignalMatrixRow>();
        public const long DefaultBudgetBytes = 128L * 1024 * 1024;
    }

    /// <summary>Bounded binary contracts; all counts are checked before allocation.</summary>
    public static class SignalSnapshotCodec
    {
        public const int MaximumMetadataBytes = 8 * 1024 * 1024;
        public const int MaximumItems = 100000;
        private static readonly UTF8Encoding Utf8 = new(false, true);

        public static byte[] Encode(SignalSnapshot s)
        {
            using var stream = new MemoryStream();
            using var w = new BinaryWriter(stream, Utf8, true);
            w.Write(2);
            Text(w, s.Title);
            w.Write(s.Revision);
            w.Write(s.ActiveGraph);
            w.Write(s.Smooth);
            w.Write(s.Smooth2D);
            w.Write(s.SmoothingFactor);
            w.Write(s.Format);
            w.Write(s.TrialHeight);
            w.Write(s.TrialRatio);
            w.Write(s.BlocRatio);
            w.Write(s.ProtocolRatio);
            w.Write(s.Colormap.Length);
            w.Write(s.Colormap);
            w.Write(s.Graphs.Length);
            foreach (var g in s.Graphs)
            {
                w.Write(g.XMin);
                w.Write(g.XMax);
                w.Write(g.YMin);
                w.Write(g.YMax);
                Text(w, g.XLabel);
                Text(w, g.XUnit);
                Text(w, g.YLabel);
                Text(w, g.YUnit);
                w.Write(g.ColumnIds.Length);
                for (int i = 0; i < g.ColumnIds.Length; i++)
                {
                    Text(w, g.ColumnIds[i]);
                    Text(w, g.SubBlocIds[i]);
                }

                Curves(w, g.Curves, 0);
            }

            w.Write(s.Matrices.Length);
            foreach (var row in s.Matrices)
            {
                Text(w, row.Title);
                Text(w, row.Group);
                Text(w, row.GroupTitle);
                Text(w, row.BlocId);
                Text(w, row.BlocTitle);
                Text(w, row.ChannelId);
                Text(w, row.ChannelTitle);
                w.Write(row.Min);
                w.Write(row.Max);
                w.Write(row.Selected.Length);
                foreach (bool value in row.Selected) w.Write(value);
                w.Write(row.Cells.Length);
                foreach (var c in row.Cells)
                {
                    Text(w, c.Title);
                    Text(w, c.Resource);
                    Text(w, c.Unit);
                    w.Write(c.Start);
                    w.Write(c.End);
                    w.Write(c.WindowStart);
                    w.Write(c.WindowEnd);
                    w.Write(c.Rows);
                    w.Write(c.Columns);
                    w.Write(c.Events.Length);
                    foreach (var e in c.Events)
                    {
                        Text(w, e.Name);
                        w.Write(e.Trial);
                        w.Write(e.Position);
                        Color(w, e.Color);
                    }
                }
            }

            if (stream.Length > MaximumMetadataBytes) throw new InvalidDataException("Signal metadata exceed capacity.");
            return stream.ToArray();
        }

        public static SignalSnapshot Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length > MaximumMetadataBytes) throw new InvalidDataException("Signal metadata exceed capacity.");
            using var stream = new MemoryStream(bytes, false);
            using var r = new BinaryReader(stream, Utf8, true);
            if (r.ReadInt32() != 2) throw new InvalidDataException("Unsupported Signals version. Update both applications.");
            var s = new SignalSnapshot { Title = Text(r), Revision = r.ReadInt64(), ActiveGraph = r.ReadInt32(), Smooth = r.ReadBoolean(), Smooth2D = r.ReadBoolean(), SmoothingFactor = r.ReadInt32(), Format = r.ReadInt32(), TrialHeight = Finite(r), TrialRatio = Finite(r), BlocRatio = Finite(r), ProtocolRatio = Finite(r) };
            int colors = Count(r, 65536);
            s.Colormap = Exact(r, colors);
            int count = Count(r, 1024);
            s.Graphs = new SignalGraph[count];
            for (int i = 0; i < count; i++)
            {
                var g = new SignalGraph { XMin = Finite(r), XMax = Finite(r), YMin = Finite(r), YMax = Finite(r) };
                g.XLabel = Text(r);
                g.XUnit = Text(r);
                g.YLabel = Text(r);
                g.YUnit = Text(r);
                int mappings = Count(r, 1024);
                g.ColumnIds = new string[mappings];
                g.SubBlocIds = new string[mappings];
                for (int j = 0; j < mappings; j++)
                {
                    g.ColumnIds[j] = Text(r);
                    g.SubBlocIds[j] = Text(r);
                }

                g.Curves = Curves(r, 0);
                s.Graphs[i] = g;
            }

            count = Count(r, 10000);
            s.Matrices = new SignalMatrixRow[count];
            for (int i = 0; i < count; i++)
            {
                var row = new SignalMatrixRow { Title = Text(r), Group = Text(r), GroupTitle = Text(r), BlocId = Text(r), BlocTitle = Text(r), ChannelId = Text(r), ChannelTitle = Text(r), Min = Finite(r), Max = Finite(r) };
                int trials = Count(r);
                row.Selected = new bool[trials];
                for (int t = 0; t < trials; t++) row.Selected[t] = r.ReadBoolean();
                int cells = Count(r, 1024);
                row.Cells = new SignalMatrix[cells];
                for (int c = 0; c < cells; c++)
                {
                    var cell = new SignalMatrix { Title = Text(r), Resource = Text(r), Unit = Text(r), Start = Finite(r), End = Finite(r), WindowStart = Finite(r), WindowEnd = Finite(r), Rows = Dimension(r, MaximumItems), Columns = Dimension(r, 16000000) };
                    if ((long)cell.Rows * cell.Columns > SignalSnapshot.DefaultBudgetBytes / 4) throw new InvalidDataException("Signal samples exceed capacity.");
                    int events = Count(r);
                    cell.Events = new SignalEvent[events];
                    for (int e = 0; e < events; e++)
                    {
                        var value = new SignalEvent { Name = Text(r), Trial = r.ReadInt32(), Position = Finite(r), Color = Color(r) };
                        if (value.Trial < 0 || value.Trial >= cell.Rows || value.Position < 0 || value.Position > 1) throw new InvalidDataException("Invalid signal event.");
                        cell.Events[e] = value;
                    }

                    if (cell.Rows != 0 && cell.Rows != trials) throw new InvalidDataException("Inconsistent trial selection.");
                    row.Cells[c] = cell;
                }

                s.Matrices[i] = row;
            }

            if (stream.Position != stream.Length || s.Revision < 0 || s.SmoothingFactor < 1 || s.SmoothingFactor > 128 || s.Format < 0 || s.Format > 3 || colors % 4 != 0 || (s.Graphs.Length > 0 && (s.ActiveGraph < 0 || s.ActiveGraph >= s.Graphs.Length))) throw new InvalidDataException("Invalid Signals presentation.");
            return s;
        }

        private static void Curves(BinaryWriter w, SignalCurve[] curves, int depth)
        {
            if (depth > 16) throw new InvalidDataException("Curve nesting exceeds capacity.");
            w.Write(curves.Length);
            foreach (var c in curves)
            {
                Text(w, c.Name);
                Text(w, c.Id);
                Text(w, c.Resource);
                w.Write(c.Enabled);
                Color(w, c.Color);
                w.Write(c.Thickness);
                w.Write(c.HasShape);
                Curves(w, c.Children, depth + 1);
            }
        }

        private static SignalCurve[] Curves(BinaryReader r, int depth)
        {
            if (depth > 16) throw new InvalidDataException("Curve nesting exceeds capacity.");
            int count = Count(r);
            var result = new SignalCurve[count];
            for (int i = 0; i < count; i++) result[i] = new SignalCurve { Name = Text(r), Id = Text(r), Resource = Text(r), Enabled = r.ReadBoolean(), Color = Color(r), Thickness = Finite(r), HasShape = r.ReadBoolean(), Children = Curves(r, depth + 1) };
            return result;
        }

        private static void Text(BinaryWriter w, string text)
        {
            byte[] bytes = Utf8.GetBytes(text ?? "");
            if (bytes.Length > 16384) throw new InvalidDataException("Signal text exceeds capacity.");
            w.Write(bytes.Length);
            w.Write(bytes);
        }

        private static string Text(BinaryReader r) => Utf8.GetString(Exact(r, Count(r, 16384)));

        private static void Color(BinaryWriter w, float[] c)
        {
            for (int i = 0; i < 4; i++) w.Write(c[i]);
        }

        private static float[] Color(BinaryReader r) => new[] { Finite(r), Finite(r), Finite(r), Finite(r) };

        private static float Finite(BinaryReader r)
        {
            float value = r.ReadSingle();
            if (float.IsNaN(value) || float.IsInfinity(value)) throw new InvalidDataException("Nonfinite presentation setting.");
            return value;
        }

        private static int Dimension(BinaryReader r, int max)
        {
            int value = r.ReadInt32();
            if (value < 0 || value > max) throw new InvalidDataException("Invalid signal dimensions.");
            return value;
        }

        internal static int Count(BinaryReader r, int max = MaximumItems)
        {
            int value = r.ReadInt32();
            if (value < 0 || value > max || value > r.BaseStream.Length - r.BaseStream.Position) throw new InvalidDataException("Invalid signal count.");
            return value;
        }

        internal static byte[] Exact(BinaryReader r, int count)
        {
            byte[] result = r.ReadBytes(count);
            if (result.Length != count) throw new EndOfStreamException();
            return result;
        }
    }
}
