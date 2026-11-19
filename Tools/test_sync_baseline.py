"""Deterministic analyzer integrity tests. Run: python -m unittest discover -s Tools -p test_sync_baseline.py -v"""
import csv
import importlib.util
import tempfile
import unittest
from pathlib import Path

SPEC = importlib.util.spec_from_file_location("t00_analyzer", Path(__file__).with_name("analyze-sync-baseline.py"))
a = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(a)


def sample(milestone, timestamp, *, logical=1, attempt=0, profile="SiteColor", thread=1,
           generation=1, frequency=1000, allocated=None, kind="Exact"):
    return dict(profile=profile, milestone=milestone, scopeId="epoch", logicalTraceId=logical,
                logicalTraceIdKind=kind, captureGeneration=1, captureGenerationKind=kind,
                attemptId=attempt, attemptIdKind="Exact" if attempt else "Unavailable",
                timestamp=timestamp, frequency=frequency, allocatedBytes=timestamp if allocated is None else allocated,
                thread=thread, isMainThread="true" if thread == 1 else "false", payloadBytes=0,
                recordingGeneration=generation)


def recording(rows, *, identifier="recording-A", state="closed", dropped=0):
    return {"rows": rows, "path": "synthetic.csv", "sha256": "synthetic",
            "metadata": {"recordingId": identifier, "captureState": state, "schemaVersion": "2",
                         "sampleCount": str(len(rows)), "dropped": str(dropped)}, "dropped": dropped}


class AnalyzerTests(unittest.TestCase):
    def profile(self, rows, **kwargs):
        return a.analyze_recording(recording(rows, **kwargs), "USB")["profiles"]["SiteColor"]

    def test_nearest_rank_and_empty_percentiles(self):
        self.assertEqual(a.percentile(list(range(1, 101)), 95), 95)
        self.assertEqual(a.percentile([3], 99), 3)
        self.assertIsNone(a.percentile([], 95))

    def test_retry_uses_original_setter_but_only_one_logical_completion(self):
        rows = [sample("Setter", 0), sample("FirstByteWritten", 2, attempt=1),
                sample("AppliedAck", 10, attempt=2), sample("VisibleAck", 12, attempt=2),
                sample("VisibleAck", 30, attempt=3)]
        p = self.profile(rows)
        self.assertEqual(p["metrics"]["setterToVisibleAckMs"]["count"], 1)
        self.assertEqual(p["metrics"]["setterToVisibleAckMs"]["p95"], 12)
        self.assertEqual(p["counts"]["attempts"], 3)
        self.assertEqual(p["counts"]["attemptsWithoutCompletion"], 1)

    def test_does_not_join_write_bounds_across_attempts(self):
        p = self.profile([sample("Setter", 0), sample("FirstByteWritten", 3, attempt=1),
                          sample("LastByteWritten", 5, attempt=2)])
        self.assertNotIn("writeSpanMs", p["metrics"])
        self.assertEqual(p["counts"]["incomplete"], 1)

    def test_replacement_and_disjoint_profile_remain_separate(self):
        rows = [sample("Setter", 0), sample("Replaced", 3), sample("Setter", 4, logical=2),
                sample("VisibleAck", 10, logical=2, attempt=1),
                sample("Setter", 2, profile="CutDefinition"), sample("VisibleAck", 25, profile="CutDefinition", attempt=1)]
        result = a.analyze_recording(recording(rows), "USB")
        self.assertEqual(result["profiles"]["SiteColor"]["counts"]["replaced-uncompleted"], 1)
        self.assertEqual(result["profiles"]["SiteColor"]["metrics"]["setterToVisibleAckMs"]["p95"], 6)
        self.assertEqual(result["profiles"]["CutDefinition"]["metrics"]["setterToVisibleAckMs"]["p95"], 23)

    def test_replaced_trace_cannot_also_have_transmission_attempt(self):
        rows = [
            sample("Setter", 0),
            sample("Replaced", 3),
            sample("Encoded", 4, attempt=1),
            sample("FirstByteWritten", 5, attempt=1),
            sample("VisibleAck", 10, attempt=1),
        ]

        result = a.analyze_recording(recording(rows), "USB")

        self.assertEqual(result["integrity"], "invalid")
        self.assertEqual(
            result["profiles"]["SiteColor"]["counts"]["ambiguous-invalid"],
            1,
        )
        self.assertEqual(
            result["profiles"]["SiteColor"]["counts"].get("replaced-uncompleted", 0),
            0,
        )
        self.assertTrue(
            any(
                "Replaced logical trace also has a transmission attempt"
                in issue
                for issue in result["issues"]
            )
        )

    def test_thread_change_does_not_report_main_thread_cost_or_gc(self):
        p = self.profile([sample("CaptureStart", 0), sample("CaptureEnd", 5, thread=2)])
        self.assertNotIn("captureObservedMainThreadWallMs", p["metrics"])
        self.assertNotIn("captureObservedThreadAllocatedBytes", p["metrics"])

    def test_sync_segment_has_observed_not_total_business_cost(self):
        p = self.profile([sample("CaptureStart", 2, allocated=100), sample("CaptureEnd", 7, allocated=164)])
        self.assertEqual(p["metrics"]["captureObservedThreadAllocatedBytes"]["p95"], 64)
        self.assertTrue(p["totalBusinessCpuBudget"].startswith("unavailable"))

    def test_initial_async_apply_not_presented_as_synchronous_main_thread_cost(self):
        rows = [sample("ApplyStart", 1, profile="InitialTransfer"), sample("ApplyEnd", 200, profile="InitialTransfer")]
        p = a.analyze_recording(recording(rows), "USB")["profiles"]["InitialTransfer"]
        self.assertNotIn("applyObservedMainThreadWallMs", p["metrics"])

    def test_proxy_identity_never_produces_exact_ack_latency(self):
        rows = [sample("Setter", 1, kind="LegacyProxy"), sample("VisibleAck", 5, attempt=1, kind="LegacyProxy")]
        p = self.profile(rows)
        self.assertNotIn("setterToVisibleAckMs", p["metrics"])
        self.assertEqual(p["counts"]["proxy-or-unknown-local-only"], 1)

    def test_different_recordings_are_not_joined(self):
        one = recording([sample("Setter", 0)], identifier="one")
        two = recording([sample("VisibleAck", 10, attempt=1)], identifier="two")
        selected = a.select_exports([one, two])
        self.assertEqual(len(selected), 2)
        for r in selected:
            self.assertNotIn("setterToVisibleAckMs", a.analyze_recording(r, "USB")["profiles"]["SiteColor"]["metrics"])

    def test_generation_change_is_never_joined(self):
        p = self.profile([sample("Setter", 0, generation=1), sample("VisibleAck", 10, attempt=1, generation=2)])
        self.assertNotIn("setterToVisibleAckMs", p["metrics"])

    def test_duplicate_same_milestone_is_ambiguous_even_same_timestamp(self):
        result = a.analyze_recording(recording([sample("Setter", 0), sample("Setter", 0)]), "USB")
        self.assertEqual(result["integrity"], "invalid")
        self.assertEqual(result["profiles"]["SiteColor"]["counts"]["ambiguous-invalid"], 1)

    def test_negative_duration_and_different_frequency_are_invalid(self):
        for end in (sample("VisibleAck", -1, attempt=1), sample("VisibleAck", 2, attempt=1, frequency=100)):
            result = a.analyze_recording(recording([sample("Setter", 0), end]), "USB")
            self.assertEqual(result["integrity"], "invalid")

    def test_same_thread_allocation_counter_cannot_decrease(self):
        with self.assertRaises(ValueError):
            a.interval(sample("CaptureStart", 1, allocated=100), sample("CaptureEnd", 2, allocated=50), True)

    def test_checkpoint_is_partial_never_budget_pass(self):
        p = self.profile([sample("Setter", 0), sample("VisibleAck", 10, attempt=1)], state="checkpoint")
        self.assertEqual(p["latencyBudgets"]["setterToVisibleAckMs"]["status"], "checkpoint-only")

    def test_overflow_invalidates_even_otherwise_valid_latencies(self):
        p = self.profile([sample("Setter", 0), sample("VisibleAck", 1, attempt=1)], dropped=1)
        self.assertEqual(p["latencyBudgets"]["setterToVisibleAckMs"]["status"], "invalid-capture")

    def test_insufficient_samples_not_a_pass(self):
        p = self.profile([sample("Setter", 0), sample("VisibleAck", 1, attempt=1)])
        self.assertEqual(p["latencyBudgets"]["setterToVisibleAckMs"]["status"], "insufficient-samples")

    def test_populated_budget_and_incomplete_failure_denominator(self):
        rows = [r for i in range(100) for r in (sample("Setter", i * 100, logical=i+1), sample("VisibleAck", i*100+10, logical=i+1, attempt=1))]
        self.assertEqual(self.profile(rows)["latencyBudgets"]["setterToVisibleAckMs"]["status"], "pass")
        rows.append(sample("Setter", 10001, logical=101))
        self.assertEqual(self.profile(rows)["latencyBudgets"]["setterToVisibleAckMs"]["status"], "incomplete-traces")

    def test_largest_consistent_export_selected_only_once(self):
        rows = [sample("Setter", 0), sample("VisibleAck", 5, attempt=1)]
        short = recording(rows[:1], state="checkpoint")
        long = recording(rows)
        selected = a.select_exports([short, long])
        self.assertEqual(len(selected), 1)
        self.assertEqual(len(selected[0]["rows"]), 2)
        self.assertEqual(selected[0]["metadata"]["captureState"], "closed")

    def test_conflicting_exports_are_rejected(self):
        with self.assertRaises(ValueError):
            a.select_exports([recording([sample("Setter", 1)]), recording([sample("Setter", 2)])])

    def write(self, path, rows, **kwargs):
        r = recording(rows, **kwargs)
        with path.open("w", newline="", encoding="utf-8") as f:
            writer = csv.writer(f)
            writer.writerow(a.HEADER)
            for row in rows:
                writer.writerow([row[name] for name in a.HEADER])
            for key, value in r["metadata"].items():
                writer.writerow([key, value])

    def test_schema_two_roundtrip_and_quoted_scope(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "capture.csv"
            row = sample("Setter", 12)
            row["scopeId"] = 'with,comma"and quote'
            self.write(path, [row])
            r = a.read_recording(path)
            self.assertEqual(r["rows"][0]["scopeId"], row["scopeId"])
            self.assertEqual(r["rows"][0]["timestamp"], 12)

    def test_missing_footer_and_legacy_header_are_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "capture.csv"
            path.write_text(",".join(a.HEADER) + "\n", encoding="utf-8")
            with self.assertRaises(ValueError): a.read_recording(path)
            path.write_text("profile,milestone\n", encoding="utf-8")
            with self.assertRaises(ValueError): a.read_recording(path)

    def test_inconsistent_provenance_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "capture.csv"
            row = sample("Setter", 0)
            row["attemptIdKind"] = "Exact"
            self.write(path, [row])
            with self.assertRaises(ValueError): a.read_recording(path)

    def test_markdown_and_json_structures_exist_without_measurements(self):
        result = a.analyze_recording(recording([]), "unknown")
        text = a.markdown({"recordings": [result]})
        self.assertIn("Missing evidence is not a pass", text)
        self.assertEqual(result["profiles"]["SiteColor"]["latencyBudgets"]["setterToAppliedAckMs"]["status"], "unavailable")


if __name__ == "__main__":
    unittest.main()
