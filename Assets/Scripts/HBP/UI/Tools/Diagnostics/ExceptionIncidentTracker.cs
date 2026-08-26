using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace HBP.UI.Tools
{
    internal sealed class CompactStackFrame
    {
        public string Method { get; }
        public string File { get; }
        public int Line { get; }
        public bool IsProjectFrame { get; }

        public CompactStackFrame(string method, string file, int line, bool isProjectFrame)
        {
            Method = method;
            File = file;
            Line = line;
            IsProjectFrame = isProjectFrame;
        }

        public string ToDisplayString()
        {
            return !string.IsNullOrEmpty(File) && Line > 0 ? $"{Method} [{File}:{Line}]" : Method;
        }
    }

    internal sealed class CompactExceptionInfo
    {
        public string Type { get; }
        public string Message { get; }
        public string Fingerprint { get; }
        public string ExecutionContext { get; }
        public IReadOnlyList<CompactStackFrame> Frames { get; }

        public CompactExceptionInfo(string type, string message, string fingerprint, string executionContext, IReadOnlyList<CompactStackFrame> frames)
        {
            Type = type;
            Message = message;
            Fingerprint = fingerprint;
            ExecutionContext = executionContext;
            Frames = frames;
        }
    }

    internal static class CompactExceptionParser
    {
        private const int MAX_RELEVANT_FRAMES = 10;

        private static readonly Regex s_FileFrame = new(@"^(?:at\s+)?(?<method>.*?)(?:\s+\[[^\]]+\])?\s+in\s+(?<path>.+):(?<line>\d+)$", RegexOptions.CultureInvariant);
        private static readonly Regex s_EditorFrame = new(@"^(?:at\s+)?(?<method>.*?)\s+\(at\s+(?<path>.+):(?<line>\d+)\)$", RegexOptions.CultureInvariant);
        private static readonly Regex s_IlOffset = new(@"\s+\[0x[0-9a-fA-F]+\]", RegexOptions.CultureInvariant);
        private static readonly Regex s_GenericType = new(@"`\d+(?:\[[^\]]*\])?", RegexOptions.CultureInvariant);
        private static readonly Regex s_Lambda = new(@"^(?<owner>.+?)\+<>c(?:__DisplayClass[^.]*)?\.<(?<method>[^>]+)>b__\d+(?:_\d+)?(?:\s*\(.*\))?$", RegexOptions.CultureInvariant);
        private static readonly Regex s_AsyncStateMachine = new(@"^(?<owner>.+?)\+<(?<method>[^>]+)>d__\d+\.MoveNext(?:\s*\(.*\))?$", RegexOptions.CultureInvariant);
        private static readonly Regex s_Whitespace = new(@"\s+", RegexOptions.CultureInvariant);

        public static CompactExceptionInfo Parse(string condition, string stackTrace)
        {
            ParseCondition(condition, out string exceptionType, out string message);
            List<CompactStackFrame> frames = ParseFrames(stackTrace);
            string executionContext = GetExecutionContext(frames);
            string fingerprint = ComputeFingerprint(exceptionType, executionContext, frames);
            return new CompactExceptionInfo(exceptionType, message, fingerprint, executionContext, frames);
        }

        private static void ParseCondition(string condition, out string exceptionType, out string message)
        {
            string compactCondition = CollapseWhitespace(condition);
            int separator = compactCondition.IndexOf(':');
            string rawType = separator >= 0 ? compactCondition[..separator] : compactCondition;
            int namespaceSeparator = rawType.LastIndexOf('.');
            exceptionType = namespaceSeparator >= 0 ? rawType[(namespaceSeparator + 1)..] : rawType;
            exceptionType = string.IsNullOrWhiteSpace(exceptionType) ? "Exception" : exceptionType.Trim();
            message = separator >= 0 ? compactCondition[(separator + 1)..].Trim() : string.Empty;
        }

        private static List<CompactStackFrame> ParseFrames(string stackTrace)
        {
            List<CompactStackFrame> parsedFrames = new();
            if (string.IsNullOrWhiteSpace(stackTrace))
            {
                return parsedFrames;
            }

            string[] lines = stackTrace.Replace("\r", string.Empty).Split('\n');
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (parsedFrames.Count > 0 && IsUnityLogExceptionBoundary(trimmed))
                {
                    break;
                }

                CompactStackFrame frame = ParseFrame(line);
                if (frame == null || string.IsNullOrWhiteSpace(frame.Method))
                {
                    continue;
                }

                if (parsedFrames.Count == 0 || parsedFrames[^1].ToDisplayString() != frame.ToDisplayString())
                {
                    parsedFrames.Add(frame);
                }
            }

            return SelectRelevantFrames(parsedFrames);
        }

        private static CompactStackFrame ParseFrame(string line)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("---", StringComparison.Ordinal))
            {
                return null;
            }

            Match match = s_FileFrame.Match(trimmed);
            if (!match.Success)
            {
                match = s_EditorFrame.Match(trimmed);
            }

            string rawMethod;
            string file = null;
            int lineNumber = 0;
            if (match.Success)
            {
                rawMethod = match.Groups["method"].Value;
                string path = match.Groups["path"].Value;
                file = GetFileName(path);
                int.TryParse(match.Groups["line"].Value, out lineNumber);
            }
            else
            {
                rawMethod = trimmed.StartsWith("at ", StringComparison.Ordinal) ? trimmed[3..] : trimmed;
            }

            bool isProjectFrame = rawMethod.StartsWith("HBP.", StringComparison.Ordinal) || trimmed.Contains("/Assets/Scripts/HBP/", StringComparison.Ordinal) || trimmed.Contains("\\Assets\\Scripts\\HBP\\", StringComparison.Ordinal);
            return new CompactStackFrame(SimplifyMethod(rawMethod), file, lineNumber, isProjectFrame);
        }

        private static List<CompactStackFrame> SelectRelevantFrames(List<CompactStackFrame> frames)
        {
            if (frames.Count <= MAX_RELEVANT_FRAMES)
            {
                return frames;
            }

            List<CompactStackFrame> result = new(MAX_RELEVANT_FRAMES) { frames[0] };
            bool externalBoundaryIncluded = !frames[0].IsProjectFrame;
            for (int i = 1; i < frames.Count && result.Count < MAX_RELEVANT_FRAMES; i++)
            {
                CompactStackFrame frame = frames[i];
                if (frame.IsProjectFrame || !externalBoundaryIncluded)
                {
                    result.Add(frame);
                    externalBoundaryIncluded |= !frame.IsProjectFrame;
                }
            }

            return result;
        }

        private static string SimplifyMethod(string rawMethod)
        {
            string method = s_IlOffset.Replace(rawMethod.Trim(), string.Empty);
            Match lambda = s_Lambda.Match(method);
            if (lambda.Success)
            {
                method = $"{lambda.Groups["owner"].Value}.{lambda.Groups["method"].Value}/lambda";
            }
            else
            {
                Match stateMachine = s_AsyncStateMachine.Match(method);
                if (stateMachine.Success)
                {
                    method = $"{stateMachine.Groups["owner"].Value}.{stateMachine.Groups["method"].Value}/async";
                }
            }

            method = s_GenericType.Replace(method, "<T>");
            int arguments = method.IndexOf('(');
            if (arguments >= 0)
            {
                method = method[..arguments].TrimEnd();
            }

            int methodSeparator = method.LastIndexOf(':');
            if (methodSeparator > 0 && method.IndexOf("::", StringComparison.Ordinal) < 0)
            {
                method = method[..methodSeparator] + "." + method[(methodSeparator + 1)..];
            }

            method = method.Replace('+', '.');
            if (method.StartsWith("HBP.", StringComparison.Ordinal))
            {
                method = method[4..];
            }
            else if (method.StartsWith("System.Linq.", StringComparison.Ordinal))
            {
                method = "Linq." + method[12..];
            }

            return method.Trim();
        }

        private static string GetFileName(string path)
        {
            string normalized = path.Replace('\\', '/');
            if (normalized.StartsWith("<", StringComparison.Ordinal))
            {
                return null;
            }

            int separator = normalized.LastIndexOf('/');
            return separator >= 0 ? normalized[(separator + 1)..] : normalized;
        }

        private static bool IsUnityLogExceptionBoundary(string line)
        {
            return line.Contains("UnityEngine.Debug:LogException", StringComparison.Ordinal) || line.Contains("UnityEngine.Debug.LogException", StringComparison.Ordinal) || line.Contains("UnityEngine.DebugLogHandler:LogException", StringComparison.Ordinal) || line.Contains("UnityEngine.DebugLogHandler.LogException", StringComparison.Ordinal);
        }

        private static string GetExecutionContext(IReadOnlyList<CompactStackFrame> frames)
        {
            foreach (CompactStackFrame frame in frames)
            {
                if (frame.Method.Contains("ThreadPool", StringComparison.Ordinal) || frame.Method.Contains("QueueUserWorkItem", StringComparison.Ordinal))
                {
                    return "ThreadPool";
                }
            }

            foreach (CompactStackFrame frame in frames)
            {
                if (frame.Method.EndsWith(".Update", StringComparison.Ordinal)) return "Update";
                if (frame.Method.Contains("/async", StringComparison.Ordinal) || frame.Method.EndsWith(".MoveNext", StringComparison.Ordinal)) return "Async";
                if (frame.Method.Contains("UnityEvent", StringComparison.Ordinal)) return "UnityEvent";
            }

            return string.Empty;
        }

        private static string ComputeFingerprint(string exceptionType, string executionContext, IReadOnlyList<CompactStackFrame> frames)
        {
            StringBuilder canonical = new(exceptionType);
            if (!string.IsNullOrEmpty(executionContext))
            {
                canonical.Append("|ctx=").Append(executionContext);
            }

            int projectFrames = 0;
            for (int i = 0; i < frames.Count && projectFrames < 4; i++)
            {
                CompactStackFrame frame = frames[i];
                if (!frame.IsProjectFrame && projectFrames > 0)
                {
                    continue;
                }

                canonical.Append('|').Append(frame.Method).Append('|').Append(frame.File).Append(':').Append(frame.Line);
                projectFrames++;
            }

            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            foreach (char character in canonical.ToString())
            {
                hash ^= character;
                hash *= prime;
            }

            return hash.ToString("X16");
        }

        private static string CollapseWhitespace(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : s_Whitespace.Replace(value.Trim(), " ");
        }
    }

    internal sealed class IncidentExceptionSnapshot
    {
        public CompactExceptionInfo Exception { get; }
        public int Count { get; }
        public TimeSpan FirstOffset { get; }
        public TimeSpan LastOffset { get; }

        public IncidentExceptionSnapshot(CompactExceptionInfo exception, int count, TimeSpan firstOffset, TimeSpan lastOffset)
        {
            Exception = exception;
            Count = count;
            FirstOffset = firstOffset;
            LastOffset = lastOffset;
        }
    }

    internal sealed class ExceptionIncidentSnapshot
    {
        public string Id { get; }
        public DateTime StartedUtc { get; }
        public DateTime LastSeenUtc { get; }
        public int TotalOccurrences { get; }
        public int AdditionalDistinctExceptions { get; }
        public IReadOnlyList<IncidentExceptionSnapshot> Exceptions { get; }
        public IReadOnlyList<CompactExceptionInfo> AdditionalExceptions { get; }

        public ExceptionIncidentSnapshot(string id, DateTime startedUtc, DateTime lastSeenUtc, int totalOccurrences, int additionalDistinctExceptions, IReadOnlyList<IncidentExceptionSnapshot> exceptions, IReadOnlyList<CompactExceptionInfo> additionalExceptions = null)
        {
            Id = id;
            StartedUtc = startedUtc;
            LastSeenUtc = lastSeenUtc;
            TotalOccurrences = totalOccurrences;
            AdditionalDistinctExceptions = additionalDistinctExceptions;
            Exceptions = exceptions;
            AdditionalExceptions = additionalExceptions ?? Array.Empty<CompactExceptionInfo>();
        }
    }

    internal sealed class ExceptionIncidentTracker
    {
        private const int MAX_DISTINCT_EXCEPTIONS = 8;
        private const int MAX_OVERFLOW_DETAILS = 2;

        private readonly TimeSpan m_QuietPeriod;
        private readonly Dictionary<string, DateTime> m_SuppressedFingerprints = new();
        private ActiveIncident m_ActiveIncident;

        public ExceptionIncidentTracker(TimeSpan quietPeriod)
        {
            m_QuietPeriod = quietPeriod;
        }

        public bool Add(string condition, string stackTrace, DateTime timestampUtc)
        {
            CompactExceptionInfo exception = CompactExceptionParser.Parse(condition, stackTrace);
            RemoveQuietFingerprints(timestampUtc);

            if (m_ActiveIncident != null)
            {
                m_ActiveIncident.Add(exception, timestampUtc);
                return false;
            }

            if (m_SuppressedFingerprints.ContainsKey(exception.Fingerprint))
            {
                m_SuppressedFingerprints[exception.Fingerprint] = timestampUtc;
                return false;
            }

            m_ActiveIncident = new ActiveIncident(exception, timestampUtc);
            return true;
        }

        public ExceptionIncidentSnapshot CreateSnapshot()
        {
            return m_ActiveIncident?.CreateSnapshot();
        }

        public void CloseActiveIncident(DateTime timestampUtc)
        {
            if (m_ActiveIncident == null)
            {
                return;
            }

            foreach (string fingerprint in m_ActiveIncident.Fingerprints)
            {
                m_SuppressedFingerprints[fingerprint] = timestampUtc;
            }

            m_ActiveIncident = null;
        }

        private void RemoveQuietFingerprints(DateTime timestampUtc)
        {
            List<string> expired = null;
            foreach (KeyValuePair<string, DateTime> pair in m_SuppressedFingerprints)
            {
                if (timestampUtc - pair.Value >= m_QuietPeriod)
                {
                    expired ??= new List<string>();
                    expired.Add(pair.Key);
                }
            }

            if (expired == null)
            {
                return;
            }

            foreach (string fingerprint in expired)
            {
                m_SuppressedFingerprints.Remove(fingerprint);
            }
        }

        private sealed class ActiveIncident
        {
            private readonly List<MutableExceptionGroup> m_Groups = new();
            private readonly Dictionary<string, MutableExceptionGroup> m_GroupsByFingerprint = new();
            private readonly HashSet<string> m_OverflowFingerprints = new();
            private readonly List<CompactExceptionInfo> m_OverflowExceptions = new();
            private int m_AdditionalDistinctExceptions;
            private int m_TotalOccurrences;

            public string Id { get; }
            public DateTime StartedUtc { get; }
            public DateTime LastSeenUtc { get; private set; }

            public IEnumerable<string> Fingerprints
            {
                get
                {
                    foreach (string fingerprint in m_GroupsByFingerprint.Keys)
                    {
                        yield return fingerprint;
                    }

                    foreach (string fingerprint in m_OverflowFingerprints)
                    {
                        yield return fingerprint;
                    }
                }
            }

            public ActiveIncident(CompactExceptionInfo exception, DateTime timestampUtc)
            {
                StartedUtc = timestampUtc;
                LastSeenUtc = timestampUtc;
                Id = $"{timestampUtc:HHmmss}-{exception.Fingerprint[..6]}";
                Add(exception, timestampUtc);
            }

            public void Add(CompactExceptionInfo exception, DateTime timestampUtc)
            {
                LastSeenUtc = timestampUtc;
                m_TotalOccurrences++;
                if (m_GroupsByFingerprint.TryGetValue(exception.Fingerprint, out MutableExceptionGroup group))
                {
                    group.Count++;
                    group.LastSeenUtc = timestampUtc;
                    return;
                }

                if (m_Groups.Count >= MAX_DISTINCT_EXCEPTIONS)
                {
                    if (m_OverflowFingerprints.Add(exception.Fingerprint))
                    {
                        m_AdditionalDistinctExceptions++;
                        if (m_OverflowExceptions.Count < MAX_OVERFLOW_DETAILS)
                        {
                            m_OverflowExceptions.Add(exception);
                        }
                    }

                    return;
                }

                group = new MutableExceptionGroup(exception, timestampUtc);
                m_Groups.Add(group);
                m_GroupsByFingerprint.Add(exception.Fingerprint, group);
            }

            public ExceptionIncidentSnapshot CreateSnapshot()
            {
                List<IncidentExceptionSnapshot> groups = new(m_Groups.Count);
                foreach (MutableExceptionGroup group in m_Groups)
                {
                    groups.Add(new IncidentExceptionSnapshot(group.Exception, group.Count, group.FirstSeenUtc - StartedUtc, group.LastSeenUtc - StartedUtc));
                }

                return new ExceptionIncidentSnapshot(Id, StartedUtc, LastSeenUtc, m_TotalOccurrences, m_AdditionalDistinctExceptions, groups, new List<CompactExceptionInfo>(m_OverflowExceptions));
            }
        }

        private sealed class MutableExceptionGroup
        {
            public CompactExceptionInfo Exception { get; }
            public int Count { get; set; }
            public DateTime FirstSeenUtc { get; }
            public DateTime LastSeenUtc { get; set; }

            public MutableExceptionGroup(CompactExceptionInfo exception, DateTime timestampUtc)
            {
                Exception = exception;
                Count = 1;
                FirstSeenUtc = timestampUtc;
                LastSeenUtc = timestampUtc;
            }
        }
    }

    internal static class IncidentDiscordFormatter
    {
        private const int MAX_CALLER_FRAMES = 3;
        private const int MAX_MESSAGE_LENGTH = 240;

        public static string Format(ExceptionIncidentSnapshot incident, int maxLength)
        {
            if (incident == null || maxLength <= 0)
            {
                return null;
            }

            int overflowBudget = incident.AdditionalDistinctExceptions > 0 ? Math.Min(maxLength, Math.Min(400, Math.Max(80, maxLength / 3))) : 0;
            string overflow = FormatOverflow(incident, overflowBudget);
            int detailsBudget = maxLength - overflow.Length;
            StringBuilder result = new();
            TimeSpan duration = incident.LastSeenUtc - incident.StartedUtc;
            int distinctExceptions = incident.Exceptions.Count + incident.AdditionalDistinctExceptions;
            AppendLineClipped(result, $"INC {incident.Id} | occ={incident.TotalOccurrences} | distinct={distinctExceptions} | {FormatOffset(duration)}", detailsBudget);

            for (int i = 0; i < incident.Exceptions.Count && result.Length < detailsBudget; i++)
            {
                int remainingGroups = incident.Exceptions.Count - i;
                int groupBudget = (detailsBudget - result.Length) / remainingGroups;
                if (groupBudget <= 0)
                {
                    break;
                }

                string group = FormatGroup(incident.Exceptions[i], i + 1, groupBudget);
                result.Append(group);
            }

            result.Append(overflow);

            return result.ToString().TrimEnd();
        }

        public static string SanitizeForDiscord(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            StringBuilder sanitized = new(value.Length);
            foreach (char character in value)
            {
                if (character == '@')
                {
                    sanitized.Append("@\u200B");
                }
                else if (character == '`')
                {
                    sanitized.Append('\'');
                }
                else if (!char.IsControl(character) || character == '\n' || character == '\t')
                {
                    sanitized.Append(character);
                }
            }

            return sanitized.ToString();
        }

        private static string FormatGroup(IncidentExceptionSnapshot group, int index, int maxLength)
        {
            CompactExceptionInfo exception = group.Exception;
            StringBuilder result = new();
            string repetitions = group.Count > 1 ? $" x{group.Count}" : string.Empty;
            string interval = group.LastOffset > group.FirstOffset ? $" {FormatOffset(group.FirstOffset)}..{FormatOffset(group.LastOffset)}" : $" {FormatOffset(group.FirstOffset)}";
            string context = string.IsNullOrEmpty(exception.ExecutionContext) ? string.Empty : $" ctx={exception.ExecutionContext}";
            AppendLineClipped(result, $"E{index} {exception.Type}{repetitions}{interval}{context} fp={exception.Fingerprint[..8]}", maxLength);

            bool topFrameWasAppended = false;
            if (exception.Frames.Count > 0)
            {
                topFrameWasAppended = TryAppendLine(result, "AT " + SanitizeForDiscord(exception.Frames[0].ToDisplayString()), maxLength);
            }

            int callerFrames = Math.Min(MAX_CALLER_FRAMES, exception.Frames.Count - 1);
            for (int i = 1; topFrameWasAppended && i <= callerFrames && result.Length < maxLength; i++)
            {
                if (!TryAppendLine(result, "<- " + SanitizeForDiscord(exception.Frames[i].ToDisplayString()), maxLength))
                {
                    break;
                }
            }

            if (!string.IsNullOrWhiteSpace(exception.Message) && result.Length < maxLength)
            {
                string message = SanitizeForDiscord(exception.Message);
                if (message.Length > MAX_MESSAGE_LENGTH)
                {
                    message = message[..(MAX_MESSAGE_LENGTH - 3)] + "...";
                }

                AppendLineClipped(result, "MSG " + message, maxLength);
            }

            return result.ToString();
        }

        private static string FormatOverflow(ExceptionIncidentSnapshot incident, int maxLength)
        {
            StringBuilder result = new();
            for (int i = 0; i < incident.AdditionalExceptions.Count && result.Length < maxLength; i++)
            {
                CompactExceptionInfo exception = incident.AdditionalExceptions[i];
                string location = exception.Frames.Count > 0 ? " AT " + exception.Frames[0].ToDisplayString() : string.Empty;
                AppendLineClipped(result, SanitizeForDiscord($"E{incident.Exceptions.Count + i + 1}+ {exception.Type} fp={exception.Fingerprint[..8]}{location}"), maxLength);
            }

            int undisplayed = incident.AdditionalDistinctExceptions - incident.AdditionalExceptions.Count;
            if (undisplayed > 0)
            {
                AppendLineClipped(result, $"MORE +{undisplayed} distinct", maxLength);
            }

            return result.ToString();
        }

        private static string FormatOffset(TimeSpan offset)
        {
            if (offset.TotalSeconds < 1)
            {
                return $"+{Math.Max(0, (int)offset.TotalMilliseconds)}ms";
            }

            return $"+{offset.TotalSeconds:0.##}s";
        }

        private static bool TryAppendLine(StringBuilder builder, string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length + 1 > maxLength - builder.Length)
            {
                return false;
            }

            builder.Append(value).Append('\n');
            return true;
        }

        private static void AppendLineClipped(StringBuilder builder, string value, int maxLength)
        {
            int remaining = maxLength - builder.Length;
            if (remaining <= 1 || string.IsNullOrEmpty(value))
            {
                return;
            }

            int contentLength = remaining - 1;
            if (value.Length <= contentLength)
            {
                builder.Append(value).Append('\n');
                return;
            }

            if (contentLength <= 3)
            {
                builder.Append(value, 0, contentLength).Append('\n');
                return;
            }

            builder.Append(value, 0, contentLength - 3).Append("...\n");
        }
    }
}
