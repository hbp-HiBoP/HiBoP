using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HBP.Core.Preferences;
using HBP.Data.Informations.Graphs;
using HBP.Sync;
using HBP.UI.Informations.Graphs;
using UnityEngine;

namespace HBP.UI.Informations
{
    /// <summary>Copies only presentation resources, while their data-manager owners are alive on the Unity thread.</summary>
    internal sealed class SignalPresentationCapture
    {
        internal readonly Dictionary<string, byte[]> Resources = new(StringComparer.Ordinal);
        private readonly Dictionary<object, string> keys = new();
        private readonly Dictionary<object, string> curveKeys = new();
        private long samplesRevision = -1, curvesRevision = -1;
        private readonly HashSet<string> used = new(StringComparer.Ordinal);

        internal SignalSnapshot Capture(InformationsWrapper wrapper)
        {
            wrapper.FlushPresentation();
            var p = wrapper.Presentation;
            if (samplesRevision != p.SamplesRevision)
            {
                keys.Clear();
                samplesRevision = p.SamplesRevision;
            }

            if (curvesRevision != p.CurvesRevision)
            {
                curveKeys.Clear();
                curvesRevision = p.CurvesRevision;
            }

            used.Clear();
            var preferences = PersistentDataManager.UserPreferences.Visualization.TrialMatrix;
            var s = new SignalSnapshot
            {
                Title = wrapper.Scene.Visualization.Name + " — " + string.Join(", ", p.Channels.Select(c => c.Channel + " (" + c.Patient.Name + ")")),
                Revision = p.Revision, ActiveGraph = Mathf.Clamp(p.ActiveGraph, 0, Math.Max(0, p.Graphs.Length - 1)),
                Smooth = preferences.TrialSmoothing, Smooth2D = preferences.Smooth2D, SmoothingFactor = Math.Max(1, preferences.NumberOfIntermediateValues),
                Format = (int)preferences.SubBlocFormat, TrialHeight = preferences.TrialHeight, TrialRatio = preferences.TrialRatio, BlocRatio = preferences.BlocRatio, ProtocolRatio = preferences.ProtocolRatio
            };
            var colors = wrapper.ColorMap;
            if (colors != null)
            {
                s.Colormap = new byte[colors.width * 4];
                for (int x = 0; x < colors.width; x++)
                {
                    Color32 c = colors.GetPixel(x, 0);
                    int i = 4 * x;
                    s.Colormap[i] = c.r;
                    s.Colormap[i + 1] = c.g;
                    s.Colormap[i + 2] = c.b;
                    s.Colormap[i + 3] = c.a;
                }
            }

            Core.Data.RunningStatistics statistics = new();
            foreach (var curve in p.AllCurves())
                if (curve.Data != null)
                    for (int i = 0; i < curve.Data.Count; i++)
                        statistics.Add(curve.Data.GetOrdinate(i));
            Vector2 yrange = p.UseDefaultRange ? Core.Data.StreamingStatistics.CalculateValueLimit(statistics, 5) : p.OrdinateRange;
            s.Graphs = p.Graphs.Select((graph, i) =>
            {
                Vector2 xrange = p.UseDefaultRange || i >= p.AbscissaRanges.Length ? graph.Item2.ToVector2() : p.AbscissaRanges[i];
                var mapping = i < p.SubBlocsByColumn.Length ? p.SubBlocsByColumn[i].Item1 : Array.Empty<Tuple<Core.Data.Bloc, Core.Data.SubBloc>>();
                var columns = p.Columns.Where(c => mapping.Any(m => m.Item1 == c.Data.Bloc)).ToArray();
                return new SignalGraph
                {
                    XMin = xrange.x, XMax = xrange.y, YMin = yrange.x, YMax = yrange.y,
                    Curves = graph.Item1.Select(Curve).ToArray(),
                    ColumnIds = columns.Select(c => wrapper.ColumnIdFor(c)).ToArray(),
                    SubBlocIds = columns.Select(c => mapping.First(m => m.Item1 == c.Data.Bloc).Item2.ID).ToArray()
                };
            }).ToArray();
            var rows = new List<SignalMatrixRow>();
            foreach (var data in p.Matrices.Data)
            {
                Vector2 limit = p.GetMatrixLimits(data);
                foreach (var bloc in data.Blocs)
                foreach (var channel in bloc.ChannelBlocs)
                {
                    var row = new SignalMatrixRow { Group = data.DataStruct.Dataset.ID + "|" + data.DataStruct.Name + "|" + ((data.DataStruct as HBP.Data.Informations.TrialMatrix.TrialMatrixGrid.CCEPTrialMatrixData)?.Source?.Patient.ID ?? "") + "|" + ((data.DataStruct as HBP.Data.Informations.TrialMatrix.TrialMatrixGrid.CCEPTrialMatrixData)?.Source?.Channel ?? ""), Title = data.Title + " / " + bloc.Title + " / " + channel.Channel.Channel + " (" + channel.Channel.Patient.Name + ")", Min = limit.x, Max = limit.y, Selected = (bool[])channel.TrialIsSelected.Clone() };
                    row.GroupTitle = data.Title;
                    row.BlocId = bloc.Data.ID;
                    row.BlocTitle = bloc.Title;
                    row.ChannelId = channel.Channel.Patient.ID + "|" + channel.Channel.Channel;
                    row.ChannelTitle = channel.Channel.Channel + " (" + channel.Channel.Patient.Name + ")";
                    row.Cells = channel.SubBlocs.Select(sub =>
                    {
                        var cell = new SignalMatrix { Title = sub.SubBlocProtocol?.Name ?? "", WindowStart = sub.Window.Start, WindowEnd = sub.Window.End };
                        if (sub.IsFiller) return cell;
                        cell.Rows = sub.SubTrials.Length;
                        if (cell.Rows == 0) return cell;
                        cell.Columns = sub.SubTrials[0].Data.Values.Length;
                        cell.Start = sub.SubBlocProtocol.Window.Start;
                        cell.End = sub.SubBlocProtocol.Window.End;
                        cell.Unit = sub.SubTrials[0].Data.Unit;
                        if (cell.Columns == 0) return cell;
                        if (!keys.TryGetValue(sub, out string key))
                        {
                            long bytes = (long)cell.Rows * cell.Columns * 4;
                            EnsureCaptureCapacity(bytes);
                            var buffer = new byte[(int)bytes];
                            for (int t = 0; t < cell.Rows; t++)
                            {
                                if (sub.SubTrials[t].Data.Values.Length != cell.Columns) throw new InvalidDataException("Incomplete matrix trials cannot be displayed.");
                                Buffer.BlockCopy(sub.SubTrials[t].Data.Values, 0, buffer, t * cell.Columns * 4, cell.Columns * 4);
                            }

                            key = Register(buffer);
                            keys[sub] = key;
                        }

                        cell.Resource = key;
                        used.Add(key);
                        var events = new List<SignalEvent>();
                        foreach (var ev in sub.SubBlocProtocol.Events)
                            for (int t = 0; t < cell.Rows; t++)
                            {
                                int source = ev.Type == Core.Enums.MainSecondaryEnum.Main ? 0 : t;
                                if (!sub.SubTrials[source].Data.InformationsByEvent.TryGetValue(ev, out var info)) continue;
                                foreach (var occurrence in info.Occurences)
                                {
                                    float position = (occurrence.IndexFromStart + .5f) / cell.Columns;
                                    if (position >= 0 && position <= 1) events.Add(new SignalEvent { Name = ev.Name, Trial = t, Position = position, Color = new[] { 0f, 0f, 0f, 220f / 255 } });
                                    if (ev.Type == Core.Enums.MainSecondaryEnum.Main) break;
                                }
                            }

                        cell.Events = events.ToArray();
                        return cell;
                    }).ToArray();
                    rows.Add(row);
                }
            }

            s.Matrices = rows.ToArray();
            foreach (var key in Resources.Keys.Where(k => !used.Contains(k)).ToArray()) Resources.Remove(key);
            return s;
        }

        private SignalCurve Curve(Graph.Curve curve)
        {
            var result = new SignalCurve { Id = curve.ID, Name = curve.Name, Enabled = curve.Enabled, Color = Color(curve.Color), Children = curve.SubCurves.Select(Curve).ToArray() };
            if (curve.Data == null || curve.Data.Count == 0) return result;
            result.HasShape = curve.Data is ShapedCurveData;
            result.Thickness = curve.Data.Thickness;
            if (!curveKeys.TryGetValue(curve.Data, out string key))
            {
                EnsureCaptureCapacity((long)curve.Data.Count * 12);
                var bytes = new byte[curve.Data.Count * 12];
                using var stream = new MemoryStream(bytes);
                using var writer = new BinaryWriter(stream);
                for (int i = 0; i < curve.Data.Count; i++)
                {
                    Vector2 point = curve.Data.GetPoint(i);
                    writer.Write(point.x);
                    writer.Write(point.y);
                    writer.Write(curve.Data is ShapedCurveData shaped ? shaped.Shapes[i] : 0);
                }

                key = Register(bytes);
                curveKeys[curve.Data] = key;
            }

            result.Resource = key;
            used.Add(key);
            return result;
        }

        private string Register(byte[] bytes)
        {
            using var sha = HBP.Core.Tools.Sha256.Create();
            string key = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
            Resources[key] = bytes;
            return key;
        }

        private void EnsureCaptureCapacity(long bytes)
        {
            if (bytes < 0 || bytes > SignalSnapshot.DefaultBudgetBytes / 2 || Resources.Values.Sum(v => (long)v.Length) + bytes > SignalSnapshot.DefaultBudgetBytes / 2) throw new InvalidDataException("Signals exceed the 128 MiB capacity. Reduce the displayed content or smoothing on Desktop.");
        }

        private static float[] Color(UnityEngine.Color c) => new[] { c.r, c.g, c.b, c.a };
    }
}
