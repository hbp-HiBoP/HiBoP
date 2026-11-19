#!/usr/bin/env python3
"""Analyze T00 schema-2 recordings, never joining different processes or attempts.

Python 3.10+, standard library only. Percentiles use nearest-rank, one completion
per logical Desktop operation. Repeated checkpoint exports are not extra trials.
Observed synchronous segment wall time/GC is NOT total business CPU/GC.
"""
from __future__ import annotations

import argparse
import csv
import hashlib
import json
import math
import sys
from collections import defaultdict
from pathlib import Path
from typing import Any

PROFILES = ("InitialTransfer", "SiteColor", "CutDefinition", "TimelineAnchor")
KINDS = {"Exact", "LegacyProxy", "Unavailable"}
HEADER = ("profile,milestone,scopeId,logicalTraceId,logicalTraceIdKind,captureGeneration,"
          "captureGenerationKind,attemptId,attemptIdKind,timestamp,frequency,allocatedBytes,"
          "thread,isMainThread,payloadBytes,recordingGeneration").split(",")
NUMBERS = ("logicalTraceId", "captureGeneration", "attemptId", "timestamp", "frequency",
           "allocatedBytes", "thread", "payloadBytes", "recordingGeneration")
MINIMUM = {"InitialTransfer": 10, "SiteColor": 100, "CutDefinition": 300, "TimelineAnchor": 100}


def percentile(values: list[float], percent: int) -> float | None:
    if not values:
        return None
    ordered = sorted(values)
    return ordered[max(0, math.ceil(len(ordered) * percent / 100) - 1)]


def stats(values: list[float]) -> dict[str, Any]:
    return {"count": len(values), "p50": percentile(values, 50),
            "p95": percentile(values, 95), "p99": percentile(values, 99)}


def read_recording(path: Path) -> dict[str, Any]:
    if path.stat().st_size > 16 * 1024 * 1024:
        raise ValueError(f"{path}: exceeds the 16 MiB input limit")
    rows, metadata, footer_started = [], {}, False
    with path.open(encoding="utf-8-sig", newline="") as source:
        reader = csv.reader(source)
        if next(reader, None) != HEADER:
            raise ValueError(f"{path}: expected schema-2 header; legacy CSV is not current-code evidence")
        for line, cells in enumerate(reader, 2):
            if not cells:
                continue
            if len(cells) == 2:
                footer_started = True
                if cells[0] in metadata:
                    raise ValueError(f"{path}:{line}: duplicate footer key")
                metadata[cells[0]] = cells[1]
                continue
            if footer_started or len(cells) != len(HEADER):
                raise ValueError(f"{path}:{line}: malformed row or data after footer")
            row = dict(zip(HEADER, cells))
            try:
                for name in NUMBERS:
                    row[name] = int(row[name])
            except ValueError as error:
                raise ValueError(f"{path}:{line}: invalid integer") from error
            if (row["profile"] not in PROFILES or not row["scopeId"]
                    or row["frequency"] <= 0 or row["recordingGeneration"] <= 0
                    or row["thread"] <= 0 or row["isMainThread"] not in ("true", "false")
                    or row["allocatedBytes"] < 0 or row["payloadBytes"] < 0):
                raise ValueError(f"{path}:{line}: invalid profile, scope, counter, clock or thread")
            for name in ("logicalTraceId", "captureGeneration", "attemptId"):
                kind = row[name + "Kind"]
                if kind not in KINDS or (kind == "Unavailable") != (row[name] == 0):
                    raise ValueError(f"{path}:{line}: inconsistent identity provenance")
            rows.append(row)
            if len(rows) > 8192:
                raise ValueError(f"{path}: exceeds the production capture capacity")
    if metadata.get("schemaVersion") != "2" or not metadata.get("recordingId"):
        raise ValueError(f"{path}: missing schema/recording identity footer")
    if metadata.get("captureState") not in ("closed", "checkpoint"):
        raise ValueError(f"{path}: missing capture state")
    try:
        count, dropped = int(metadata["sampleCount"]), int(metadata["dropped"])
    except (KeyError, ValueError) as error:
        raise ValueError(f"{path}: missing/invalid counts footer") from error
    if count != len(rows) or dropped < 0:
        raise ValueError(f"{path}: inconsistent footer counts")
    return {"path": str(path.resolve()), "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
            "metadata": metadata, "rows": rows, "dropped": dropped}


def select_exports(recordings: list[dict[str, Any]]) -> list[dict[str, Any]]:
    groups = defaultdict(list)
    for recording in recordings:
        groups[recording["metadata"]["recordingId"]].append(recording)
    selected = []
    for exports in groups.values():
        exports.sort(key=lambda r: (len(r["rows"]), r["metadata"]["captureState"] == "closed"), reverse=True)
        chosen = exports[0]
        for other in exports[1:]:
            if other["rows"] != chosen["rows"][:len(other["rows"])]:
                raise ValueError("Conflicting exports share one recordingId; do not combine them")
            for field in ("buildGuid", "unityVersion", "platform"):
                if other["metadata"].get(field) != chosen["metadata"].get(field):
                    raise ValueError(f"Conflicting {field} for one recordingId")
        chosen["exportsConsidered"] = [r["path"] for r in exports]
        chosen["dropped"] = max(r["dropped"] for r in exports)
        selected.append(chosen)
    return selected


def interval(start: dict | None, end: dict | None, synchronous_main: bool = False) -> dict | None:
    if start is None or end is None:
        return None
    if (start["recordingGeneration"] != end["recordingGeneration"]
            or start["frequency"] != end["frequency"] or end["timestamp"] < start["timestamp"]):
        raise ValueError("Inconsistent clock/generation or negative duration")
    same_thread = start["thread"] == end["thread"]
    if synchronous_main and not (same_thread and start["isMainThread"] == end["isMainThread"] == "true"):
        return None
    allocated = end["allocatedBytes"] - start["allocatedBytes"] if same_thread else None
    if allocated is not None and allocated < 0:
        raise ValueError("Decreasing same-thread allocation counter")
    return {"milliseconds": (end["timestamp"] - start["timestamp"]) * 1000 / start["frequency"],
            "sameThreadObservedBytes": allocated}


def analyze_recording(recording: dict[str, Any], transport: str) -> dict[str, Any]:
    grouped = defaultdict(list)
    for row in recording["rows"]:
        key = tuple(row[name] for name in ("recordingGeneration", "profile", "scopeId", "logicalTraceId",
                    "logicalTraceIdKind", "captureGeneration", "captureGenerationKind"))
        grouped[key].append(row)
    metrics = {profile: defaultdict(list) for profile in PROFILES}
    counts = {profile: defaultdict(int) for profile in PROFILES}
    details, issues = [], []
    for key, rows in grouped.items():
        profile, scope, logical_id = key[1], key[2], key[3]
        exact = key[4] == key[6] == "Exact"
        counts[profile]["logicalGroups"] += 1
        attempts = defaultdict(dict)
        invalid = None
        try:
            for row in rows:
                attempt_key = (row["attemptId"], row["attemptIdKind"])
                milestones = attempts[attempt_key]
                milestone = row["milestone"]
                # Even identical repetitions are ambiguous: they may be retries with a proxy identity.
                if milestone in milestones:
                    raise ValueError(f"Duplicate {milestone} for attempt {attempt_key}")
                milestones[milestone] = row
            root = attempts.get((0, "Unavailable"), {})

            # Replaced means the logical trace was superseded before it was accepted
            # for transmission. It must therefore never have a network attempt.
            if "Replaced" in root:
                transmission_attempts = [
                    attempt_key
                    for attempt_key in attempts
                    if attempt_key != (0, "Unavailable")
                ]
                if transmission_attempts:
                    raise ValueError(
                        "Replaced logical trace also has a transmission attempt: "
                        + ", ".join(
                            f"{attempt_id}/{attempt_kind}"
                            for attempt_id, attempt_kind in transmission_attempts
                        )
                    )

            origin = root.get("UserRequest" if profile == "InitialTransfer" else "Setter")
            completed = {}
            observed = defaultdict(list)
            for (attempt_id, attempt_kind), phases in attempts.items():
                if exact and attempt_kind == "Exact":
                    counts[profile]["attempts"] += 1
                    # The root origin can be joined only to its own exact logical operation.
                    for milestone, label in (("AppliedAck", "setterToAppliedAckMs"),
                                              ("VisibleAck", "setterToVisibleAckMs"),
                                              ("PublicationReceipt", "requestToPublicationReceiptMs")):
                        value = interval(origin, phases.get(milestone))
                        if value is not None:
                            completed[label] = min(completed.get(label, float("inf")), value["milliseconds"])
                    if not any(name in phases for name in ("VisibleAck", "PublicationReceipt")):
                        counts[profile]["attemptsWithoutCompletion"] += 1
                # Only sync adapter capture/apply segments: never call async initial apply a CPU segment.
                if profile != "InitialTransfer":
                    for start_name, end_name, label in (("CaptureStart", "CaptureEnd", "capture"),
                                                       ("ApplyStart", "ApplyEnd", "apply")):
                        value = interval(phases.get(start_name), phases.get(end_name), synchronous_main=True)
                        if value is not None:
                            observed[label + "ObservedMainThreadWallMs"].append(value["milliseconds"])
                            observed[label + "ObservedThreadAllocatedBytes"].append(value["sameThreadObservedBytes"])
                for start_name, end_name, label in (("FirstByteWritten", "LastByteWritten", "writeSpanMs"),
                                                   ("FirstByteReceived", "LastByteReceived", "receiveSpanMs"),
                                                   ("ApplyEnd", "NextVisible", "applyEndToNextVisibleMs")):
                    value = interval(phases.get(start_name), phases.get(end_name))
                    if value is not None:
                        observed[label].append(value["milliseconds"])
            terminal = "requestToPublicationReceiptMs" if profile == "InitialTransfer" else "setterToVisibleAckMs"
            status = ("completed" if terminal in completed else "replaced-uncompleted" if "Replaced" in root
                      else "incomplete" if exact else "proxy-or-unknown-local-only")
            for label, value in completed.items():
                metrics[profile][label].append(value)
            for label, values in observed.items():
                metrics[profile][label].extend(values)
            counts[profile][status] += 1
        except ValueError as error:
            invalid = str(error)
            status = "ambiguous-invalid"
            counts[profile][status] += 1
            issues.append(f"{profile}/{scope}/{logical_id}: {error}")
        details.append({"profile": profile, "scopeId": scope, "logicalTraceId": logical_id,
                        "captureGeneration": key[5], "recordingGeneration": key[0],
                        "status": status, "issue": invalid,
                        "attempts": [{"id": a[0], "kind": a[1], "milestones": sorted(p)} for a, p in attempts.items()]})
    invalid_capture = recording["dropped"] > 0 or bool(issues)
    profiles = {}
    for profile in PROFILES:
        measured = {name: stats(values) for name, values in metrics[profile].items()}
        gates = {}
        if profile != "InitialTransfer":
            for name, threshold in (("setterToAppliedAckMs", 16.7 if transport == "USB" else 33.3),
                                    ("setterToVisibleAckMs", 33.3 if transport == "USB" else 50.0)):
                metric = measured.get(name, stats([]))
                incomplete = counts[profile]["incomplete"] + counts[profile]["ambiguous-invalid"]
                state = ("invalid-capture" if invalid_capture else "checkpoint-only" if recording["metadata"]["captureState"] != "closed"
                         else "unavailable" if transport == "unknown" or metric["count"] == 0
                         else "insufficient-samples" if metric["count"] < MINIMUM[profile]
                         else "incomplete-traces" if incomplete else "pass" if metric["p95"] <= threshold else "fail")
                gates[name] = {"status": state, "limitMs": threshold if transport != "unknown" else None,
                               "requiredCompletions": MINIMUM[profile]}
        profiles[profile] = {"counts": dict(counts[profile]), "metrics": measured, "latencyBudgets": gates,
            "totalBusinessCpuBudget": "unavailable-not-measured-by-these-probes",
            "totalBusinessAllocationBudget": "unavailable-not-measured-by-these-probes",
            "scientificStable": "unavailable-no-independent-production-boundary"}
    return {key: value for key, value in recording.items() if key != "rows"} | {
        "transport": transport, "integrity": "invalid" if invalid_capture else "valid-local-observations",
        "issues": issues, "profiles": profiles, "traces": details}


def markdown(report: dict) -> str:
    lines = ["# T00 recording analysis", "", "Nearest-rank percentiles. No cross-process joins. Missing evidence is not a pass.",
             "Observed adapter segments are not total business CPU time or total business allocation.", ""]
    for recording in report["recordings"]:
        lines += [f"## {Path(recording['path']).name}", "",
                  f"Recording: `{recording['metadata']['recordingId']}`; state: `{recording['metadata']['captureState']}`; "
                  f"integrity: **{recording['integrity']}**; dropped: {recording['dropped']}.", "",
                  "| Profile / metric | n | p50 | p95 | p99 |", "|---|---:|---:|---:|---:|"]
        for profile, data in recording["profiles"].items():
            for name, metric in data["metrics"].items():
                lines.append(f"| {profile} / {name} | {metric['count']} | {metric['p50']:.4f} | {metric['p95']:.4f} | {metric['p99']:.4f} |")
        for profile, data in recording["profiles"].items():
            lines += ["", f"**{profile}** — counts: `{json.dumps(data['counts'], sort_keys=True)}`; "
                      f"latency budgets: `{json.dumps(data['latencyBudgets'], sort_keys=True)}`.", ""]
        if recording["issues"]:
            lines += ["Issues: " + "; ".join(recording["issues"]), ""]
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("inputs", type=Path, nargs="+", help="CSV files or directories containing CSVs")
    parser.add_argument("--transport", choices=("USB", "WiFi", "unknown"), default="unknown")
    parser.add_argument("--output", type=Path, required=True, help="Output basename; writes .json and .md")
    args = parser.parse_args()
    try:
        paths = sorted({path.resolve() for item in args.inputs for path in (item.rglob('*.csv') if item.is_dir() else [item])})
        if not paths:
            raise ValueError("No CSV input found")
        if len(paths) > 256:
            raise ValueError("At most 256 input exports per analysis")
        recordings = select_exports([read_recording(path) for path in paths])
        report = {"schemaVersion": 1, "percentileMethod": "nearest-rank", "crossProcessJoin": False,
                  "recordings": [analyze_recording(r, args.transport) for r in recordings]}
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.with_suffix(".json").write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
        args.output.with_suffix(".md").write_text(markdown(report), encoding="utf-8")
        print(f"Analyzed {len(paths)} exports / {len(recordings)} recordings: {args.output.with_suffix('.json')}")
        return 2 if any(r["integrity"] == "invalid" for r in report["recordings"]) else 0
    except (OSError, ValueError, csv.Error) as error:
        print(f"Invalid baseline input: {error}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
