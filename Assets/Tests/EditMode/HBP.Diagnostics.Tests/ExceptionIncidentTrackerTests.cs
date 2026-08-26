using System;
using HBP.UI.Tools;
using NUnit.Framework;

namespace HBP.Tests.Diagnostics
{
    public class ExceptionIncidentTrackerTests
    {
        private const string STACK_TRACE = @"  at HBP.Core.Data.Group+<>c.<set_Patients>b__9_0 (HBP.Core.Data.Patient p) [0x00000] in C:\HBP\Software\HiBoP\Assets\Scripts\HBP\Core\Data\Patient\Group.cs:58
  at System.Linq.Enumerable+SelectListIterator`2[TSource,TResult].ToList () [0x0002a] in <4c52445c10234503b70617968082b34b>:0
  at HBP.UI.Tools.Lists.SelectableList`1[T].Update () [0x00001] in C:\HBP\Software\HiBoP\Assets\Scripts\HBP\UI\Tools\Lists\SelectableList.cs:358";

        private const string DEBUG_LOG_STACK_TRACE = @"  at HBP.UI.Tools.BugReporterManualTestTrigger.ThrowIndexedException (System.Int32 index) in C:\HBP\Software\HiBoP\Assets\Scripts\HBP\UI\Tools\Diagnostics\BugReporterManualTestTrigger.cs:168
  at HBP.UI.Tools.BugReporterManualTestTrigger.LogIndexedException (System.Int32 index) in C:\HBP\Software\HiBoP\Assets\Scripts\HBP\UI\Tools\Diagnostics\BugReporterManualTestTrigger.cs:156
  at HBP.UI.Tools.BugReporterManualTestTrigger.Update () in C:\HBP\Software\HiBoP\Assets\Scripts\HBP\UI\Tools\Diagnostics\BugReporterManualTestTrigger.cs:52
UnityEngine.Debug:LogException(Exception)
HBP.UI.Tools.BugReporterManualTestTrigger:LogIndexedException(Int32, String) (at Assets/Scripts/HBP/UI/Tools/Diagnostics/BugReporterManualTestTrigger.cs:160)";

        [Test]
        public void Parser_RemovesPathsOffsetsAndCompilerGeneratedLambdaNames()
        {
            CompactExceptionInfo exception = CompactExceptionParser.Parse("NullReferenceException: Object reference not set to an instance of an object", STACK_TRACE);

            Assert.That(exception.Type, Is.EqualTo("NullReferenceException"));
            Assert.That(exception.Message, Is.EqualTo("Object reference not set to an instance of an object"));
            Assert.That(exception.Frames[0].ToDisplayString(), Is.EqualTo("Core.Data.Group.set_Patients/lambda [Group.cs:58]"));
            Assert.That(exception.Frames[1].Method, Does.StartWith("Linq.Enumerable"));
            Assert.That(exception.Frames[2].ToDisplayString(), Is.EqualTo("UI.Tools.Lists.SelectableList<T>.Update [SelectableList.cs:358]"));
            Assert.That(IncidentDiscordFormatter.Format(CreateSingleExceptionIncident(exception), 1000), Does.Not.Contain("C:\\HBP"));
            Assert.That(IncidentDiscordFormatter.Format(CreateSingleExceptionIncident(exception), 1000), Does.Not.Contain("0x00000"));
        }

        [Test]
        public void Parser_DropsTheUnityDebugLoggerTail()
        {
            CompactExceptionInfo exception = CompactExceptionParser.Parse("InvalidOperationException: failure", DEBUG_LOG_STACK_TRACE);

            Assert.That(exception.Frames, Has.Count.EqualTo(3));
            Assert.That(exception.Frames[2].ToDisplayString(), Is.EqualTo("UI.Tools.BugReporterManualTestTrigger.Update [BugReporterManualTestTrigger.cs:52]"));
            Assert.That(exception.Frames, Has.None.Matches<CompactStackFrame>(frame => frame.Method.Contains("UnityEngine.Debug", StringComparison.Ordinal)));
        }

        [Test]
        public void Parser_DistinguishesAThreadPoolContextFromTheSameProjectStack()
        {
            const string sharedStack = @"at HBP.UI.Tools.BugReporterManualTestTrigger.ThrowIndexedException () in C:\HBP\BugReporterManualTestTrigger.cs:168
at HBP.UI.Tools.BugReporterManualTestTrigger.LogIndexedException () in C:\HBP\BugReporterManualTestTrigger.cs:156";
            string threadPoolStack = sharedStack + "\nat System.Threading.ThreadPoolWorkQueue.Dispatch ()";

            CompactExceptionInfo direct = CompactExceptionParser.Parse("InvalidOperationException: failure", sharedStack);
            CompactExceptionInfo threadPool = CompactExceptionParser.Parse("InvalidOperationException: failure", threadPoolStack);

            Assert.That(direct.ExecutionContext, Is.Empty);
            Assert.That(threadPool.ExecutionContext, Is.EqualTo("ThreadPool"));
            Assert.That(threadPool.Fingerprint, Is.Not.EqualTo(direct.Fingerprint));
        }

        [Test]
        public void Tracker_GroupsRepeatedExceptionsWithoutOpeningAnotherIncident()
        {
            ExceptionIncidentTracker tracker = new(TimeSpan.FromSeconds(5));
            DateTime start = new(2026, 8, 26, 12, 0, 0, DateTimeKind.Utc);

            Assert.That(tracker.Add("NullReferenceException: failure", STACK_TRACE, start), Is.True);
            for (int i = 1; i < 600; i++)
            {
                Assert.That(tracker.Add("NullReferenceException: failure", STACK_TRACE, start.AddMilliseconds(i * 16)), Is.False);
            }

            ExceptionIncidentSnapshot incident = tracker.CreateSnapshot();
            Assert.That(incident.TotalOccurrences, Is.EqualTo(600));
            Assert.That(incident.Exceptions, Has.Count.EqualTo(1));
            Assert.That(incident.Exceptions[0].Count, Is.EqualTo(600));
        }

        [Test]
        public void Tracker_PreservesDistinctExceptionsInTheSameIncident()
        {
            ExceptionIncidentTracker tracker = new(TimeSpan.FromSeconds(5));
            DateTime start = new(2026, 8, 26, 12, 0, 0, DateTimeKind.Utc);

            tracker.Add("NullReferenceException: first", STACK_TRACE, start);
            tracker.Add("InvalidOperationException: second", STACK_TRACE.Replace("Group.cs:58", "Group.cs:59"), start.AddMilliseconds(12));
            tracker.Add("ArgumentException: third", STACK_TRACE.Replace("Group.cs:58", "Group.cs:60"), start.AddMilliseconds(19));

            ExceptionIncidentSnapshot incident = tracker.CreateSnapshot();
            Assert.That(incident.Exceptions, Has.Count.EqualTo(3));
            Assert.That(incident.Exceptions[0].Exception.Type, Is.EqualTo("NullReferenceException"));
            Assert.That(incident.Exceptions[1].FirstOffset, Is.EqualTo(TimeSpan.FromMilliseconds(12)));
            Assert.That(incident.Exceptions[2].Exception.Type, Is.EqualTo("ArgumentException"));
        }

        [Test]
        public void Tracker_SuppressesAContinuousLoopUntilItHasBeenQuiet()
        {
            ExceptionIncidentTracker tracker = new(TimeSpan.FromSeconds(5));
            DateTime start = new(2026, 8, 26, 12, 0, 0, DateTimeKind.Utc);

            Assert.That(tracker.Add("NullReferenceException: failure", STACK_TRACE, start), Is.True);
            tracker.CloseActiveIncident(start.AddSeconds(1));
            Assert.That(tracker.Add("NullReferenceException: failure", STACK_TRACE, start.AddSeconds(2)), Is.False);
            Assert.That(tracker.Add("NullReferenceException: failure", STACK_TRACE, start.AddSeconds(6)), Is.False);
            Assert.That(tracker.Add("NullReferenceException: failure", STACK_TRACE, start.AddSeconds(12)), Is.True);
        }

        [Test]
        public void Tracker_CountsAndSuppressesOverflowExceptionsByDistinctFingerprint()
        {
            ExceptionIncidentTracker tracker = new(TimeSpan.FromSeconds(5));
            DateTime start = new(2026, 8, 26, 12, 0, 0, DateTimeKind.Utc);

            for (int i = 0; i < 9; i++)
            {
                tracker.Add($"Exception{i}: failure", STACK_TRACE.Replace("Group.cs:58", $"Group.cs:{58 + i}"), start.AddMilliseconds(i));
            }

            tracker.Add("Exception8: failure", STACK_TRACE.Replace("Group.cs:58", "Group.cs:66"), start.AddSeconds(1));
            ExceptionIncidentSnapshot incident = tracker.CreateSnapshot();
            Assert.That(incident.Exceptions, Has.Count.EqualTo(8));
            Assert.That(incident.AdditionalDistinctExceptions, Is.EqualTo(1));

            tracker.CloseActiveIncident(start.AddSeconds(2));
            Assert.That(tracker.Add("Exception8: failure", STACK_TRACE.Replace("Group.cs:58", "Group.cs:66"), start.AddSeconds(3)), Is.False);
        }

        [Test]
        public void Formatter_AlwaysIncludesOverflowDetailsWithinTheBudget()
        {
            ExceptionIncidentTracker tracker = new(TimeSpan.FromSeconds(5));
            DateTime start = new(2026, 8, 26, 12, 0, 0, DateTimeKind.Utc);
            for (int i = 0; i < 9; i++)
            {
                tracker.Add($"Exception{i}: overflow {i}", STACK_TRACE.Replace("Group.cs:58", $"Group.cs:{58 + i}"), start.AddMilliseconds(i));
            }

            string formatted = IncidentDiscordFormatter.Format(tracker.CreateSnapshot(), 900);
            string[] lines = formatted.Split('\n');

            Assert.That(formatted.Length, Is.LessThanOrEqualTo(900));
            Assert.That(formatted, Does.Contain("distinct=9"));
            Assert.That(formatted, Does.Contain("E9+ Exception8"));
            Assert.That(lines, Has.Some.StartsWith("E2 "));
            Assert.That(lines, Has.None.StartsWith(" "));
        }

        [Test]
        public void Formatter_RespectsTheCharacterBudgetAndKeepsEveryExpectedException()
        {
            ExceptionIncidentTracker tracker = new(TimeSpan.FromSeconds(5));
            DateTime start = new(2026, 8, 26, 12, 0, 0, DateTimeKind.Utc);
            for (int i = 0; i < 5; i++)
            {
                tracker.Add($"Exception{i}: {new string('x', 300)}", STACK_TRACE.Replace("Group.cs:58", $"Group.cs:{58 + i}"), start.AddMilliseconds(i * 10));
            }

            string formatted = IncidentDiscordFormatter.Format(tracker.CreateSnapshot(), 900);

            Assert.That(formatted.Length, Is.LessThanOrEqualTo(900));
            for (int i = 1; i <= 5; i++)
            {
                Assert.That(formatted, Does.Contain($"E{i} "));
            }

            Assert.That(formatted.Split('\n'), Has.None.StartsWith(" "));
        }

        [Test]
        public void Formatter_OmitsAFrameInsteadOfClippingIt()
        {
            CompactExceptionInfo exception = CompactExceptionParser.Parse("InvalidOperationException: BR_FRAME_BUDGET", DEBUG_LOG_STACK_TRACE);

            string formatted = IncidentDiscordFormatter.Format(CreateSingleExceptionIncident(exception), 320);

            Assert.That(formatted.Length, Is.LessThanOrEqualTo(320));
            Assert.That(formatted, Does.Contain("MSG BR_FRAME_BUDGET"));
            foreach (string line in formatted.Split('\n'))
            {
                if (line.StartsWith("AT ", StringComparison.Ordinal) || line.StartsWith("<- ", StringComparison.Ordinal))
                {
                    Assert.That(line, Does.Not.EndWith("..."));
                }
            }
        }

        [Test]
        public void Formatter_PreservesCallerFramesBeforeClippingALongMessage()
        {
            CompactExceptionInfo exception = CompactExceptionParser.Parse($"InvalidOperationException: BR_LONG_{new string('x', 700)}", STACK_TRACE);

            string formatted = IncidentDiscordFormatter.Format(CreateSingleExceptionIncident(exception), 550);

            Assert.That(formatted.Length, Is.LessThanOrEqualTo(550));
            Assert.That(formatted, Does.Contain("AT Core.Data.Group.set_Patients/lambda [Group.cs:58]"));
            Assert.That(formatted, Does.Contain("<- UI.Tools.Lists.SelectableList<T>.Update [SelectableList.cs:358]"));
            Assert.That(formatted, Does.Contain("MSG BR_LONG_"));
            Assert.That(formatted, Does.Not.Contain(new string('x', 500)));
        }

        [Test]
        public void Formatter_NeutralizesDiscordMentionsAndCodeFences()
        {
            CompactExceptionInfo exception = CompactExceptionParser.Parse("Exception: @everyone ```", STACK_TRACE);

            string formatted = IncidentDiscordFormatter.Format(CreateSingleExceptionIncident(exception), 1000);

            Assert.That(formatted.Contains("@everyone", StringComparison.Ordinal), Is.False);
            Assert.That(formatted.Contains("```", StringComparison.Ordinal), Is.False);
        }

        private static ExceptionIncidentSnapshot CreateSingleExceptionIncident(CompactExceptionInfo exception)
        {
            DateTime timestamp = new(2026, 8, 26, 12, 0, 0, DateTimeKind.Utc);
            return new ExceptionIncidentSnapshot("test", timestamp, timestamp, 1, 0, new[]
            {
                new IncidentExceptionSnapshot(exception, 1, TimeSpan.Zero, TimeSpan.Zero)
            });
        }
    }
}
