"""Summarize one Display run without combining clocks or adding overlapping spans."""
import argparse
import csv
import io
import json
import re
import statistics
from collections import defaultdict
from pathlib import Path


TIMING = re.compile(r"QUEST-PAIRING ((?:\w+=[\w.\-]+ ?)+)")


def read_timings(path):
    if not path.is_file():
        return []
    with path.open(encoding="utf-8-sig", errors="replace") as stream:
        return [dict(field.split("=", 1) for field in match[1].split())
                for line in stream if (match := TIMING.search(line))]


def links(events):
    return [dict(items) for items in dict.fromkeys(
        tuple(event.items()) for event in events if {"link", "id", "scope"} <= event.keys())]


def legacy_quest_scopes(events):
    # Historical logs emitted decode timings before they knew the transfer identity.
    pending = []
    last_scope = None
    detailed_decode = False
    for event in events:
        name = event.get("stage")
        if name is None:
            continue
        if scope := event.get("scope"):
            last_scope = scope
            for earlier in pending:
                earlier["scope"] = scope
            pending.clear()
        elif name in {"scene.decode-total", "quest.scene-reception-and-preparation"} or (
                name == "quest.decode-verified-scene" and detailed_decode):
            event["scope"] = last_scope
            detailed_decode |= name == "scene.decode-total"
        elif name.startswith("scene.decode-") or name == "quest.decode-verified-scene":
            pending.append(event)


def timing_summary(events, allowed_scopes=None, receptions=None):
    stages = defaultdict(list)
    counters = defaultdict(list)
    receptions = receptions or {}
    for event in events:
        scope = event.get("scope")
        sample = {"scope": scope}
        if scope in receptions:
            sample.update(reception=scope, scope=receptions[scope])
        # Keep an interrupted reception visible even if the payload never supplied its identity.
        unidentified = scope in receptions and sample["scope"] is None
        if allowed_scopes is not None and sample["scope"] not in allowed_scopes and not unidentified:
            continue
        for field in ("attempt", "samples", "bytes", "startThread", "endThread"):
            if field in event:
                sample[field] = int(event[field])
        for field in ("startMs", "endMs"):
            if field in event:
                sample[field] = float(event[field])
        if "stage" in event and "elapsedMs" in event:
            stages[event["stage"]].append(dict(sample, ms=float(event["elapsedMs"])))
        if "counter" in event and "value" in event:
            counters[event["counter"]].append(dict(sample, value=int(event["value"])))
    return (
        {name: {"count": len(values), "medianMs": statistics.median(v["ms"] for v in values), "samples": values}
         for name, values in sorted(stages.items())},
        dict(sorted(counters.items())),
    )


def transfers(path):
    # Lifecycle metadata is appended after the sample rows by SyncTelemetryCapture.
    lines = path.read_text(encoding="utf-8-sig").splitlines() if path.is_file() else []
    if not lines:
        return []
    data = [lines[0]] + [line for line in lines[1:] if line.startswith("InitialTransfer,")]
    scopes = defaultdict(dict)
    attempts = defaultdict(dict)
    for row in csv.DictReader(io.StringIO("\n".join(data))):
        scope = row["scopeId"]
        attempt = row["attemptId"]
        if row["attemptIdKind"] == "Exact":
            attempts[scope, attempt][row["milestone"]] = row
        else:
            scopes[scope][row["milestone"]] = row
    result = []
    for (scope, attempt), attempt_milestones in attempts.items():
        milestones = {**scopes[scope], **attempt_milestones}
        def duration(start, end):
            a, b = milestones.get(start), milestones.get(end)
            if not a or not b:
                return None
            if a["frequency"] != b["frequency"] or int(a["frequency"]) <= 0:
                raise ValueError("A single-process clock changed frequency")
            return (int(b["timestamp"]) - int(a["timestamp"])) * 1000 / int(a["frequency"])

        receipt = milestones.get("PublicationReceipt", {})
        result.append({
            "scope": scope,
            "attempt": attempt,
            "bytes": int(receipt["payloadBytes"]) if receipt else None,
            "captureIncludingPreparationMs": duration("CaptureStart", "CaptureEnd"),
            "firstToLastWriteMs": duration("FirstByteWritten", "LastByteWritten"),
            "lastWriteToPublicationReceiptMs": duration("LastByteWritten", "PublicationReceipt"),
            "queuedToEncodedMs": duration("Queued", "Encoded"),
        })
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", type=Path)
    parser.add_argument("--desktop-log", type=Path, help="Analyze regular Desktop logs without an automated baseline.")
    parser.add_argument("--quest-log", type=Path)
    args = parser.parse_args()
    root = args.directory
    display_path = root / "display.json"
    if not display_path.is_file() and args.desktop_log is None:
        parser.error("Provide --desktop-log for a manual run without display.json.")
    for path in (args.desktop_log, args.quest_log):
        if path is not None and not path.is_file():
            parser.error(f"Log file not found: {path}")
    display = json.loads(display_path.read_text(encoding="utf-8-sig")) if display_path.is_file() else None
    transfer_samples = transfers(root / "desktop-sync.csv")
    desktop_events = read_timings(args.desktop_log or root / "pairing-timings.log")
    quest_events = read_timings(args.quest_log or root / "quest.log")
    desktop_links = links(desktop_events)
    scope_ids = ({sample["scope"] for sample in transfer_samples} |
                 {link["scope"] for link in desktop_links})
    reception_stages = {"quest.transport-prefix-read", "quest.scene-reception-and-preparation"}
    receptions = {event["scope"]: None for event in quest_events
                  if event.get("stage") in reception_stages and event.get("scope")}
    receptions.update((link["id"], link["scope"]) for link in links(quest_events) if link["link"] == "quest-reception")
    if not receptions and scope_ids:
        legacy_quest_scopes(quest_events)
    desktop_stages, desktop_counters = timing_summary(desktop_events)
    quest_stages, quest_counters = timing_summary(quest_events, scope_ids or None, receptions)
    quest_scope_ids = {sample["scope"] for stage in quest_stages.values() for sample in stage["samples"]}
    result = {
        "display": display,
        "desktopStages": desktop_stages,
        "desktopCounters": desktop_counters,
        "desktopLinks": desktop_links,
        "questStages": quest_stages,
        "questCounters": quest_counters,
        "questUnidentifiedReceptions": sorted(key for key, scope in receptions.items() if scope is None),
        "questScopesMissing": sorted(scope_ids - quest_scope_ids),
        "desktopTransfers": transfer_samples,
        "limits": [
            "Async stage durations include waits and may overlap; do not add them.",
            "No Desktop/Quest timestamp subtraction is performed.",
            "A first Editor opening is not a process-cold Release measurement.",
            "Publication receipt and post-policy scientific readiness are distinct milestones.",
            "Managed memory snapshots are process-wide and exclude native and GPU allocations.",
        ],
    }
    if not display or not display.get("desktopIdleThrottlingSuspended"):
        result["limits"].append("This run does not establish that Desktop idle throttling was suspended.")
    (root / "summary.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"runs": display.get("runs", []) if display else [], "desktopTransfers": transfer_samples,
                      "questScopesMissing": result["questScopesMissing"], "summary": str(root / "summary.json")}, indent=2))


if __name__ == "__main__":
    main()
