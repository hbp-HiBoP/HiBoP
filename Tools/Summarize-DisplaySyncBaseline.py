"""Summarize one Display run without combining clocks or adding overlapping spans."""
import argparse
import csv
import io
import json
import re
import statistics
from collections import defaultdict
from pathlib import Path


STAGE = re.compile(r"QUEST-PAIRING stage=(\S+) elapsedMs=([\d.]+)(?: scope=(\S+))?")


def stages(path, allowed_scopes=None):
    grouped = defaultdict(list)
    pending = []
    last_scope = None
    detailed_decode = False
    if path.exists():
        for line in path.read_text(encoding="utf-8-sig").splitlines():
            match = STAGE.search(line)
            if match:
                name, scope = match[1], match[3]
                sample = {"ms": float(match[2]), "scope": scope}
                if allowed_scopes is None:
                    grouped[name].append(sample)
                    continue
                # Quest preparation is serialized. Older probes report decoding
                # before the first identity-bearing restoration span. Associate
                # only that adjacent preparation, never an arbitrary clock range.
                if scope:
                    last_scope = scope
                    for pending_name, pending_sample in pending:
                        pending_sample["scope"] = scope
                        if scope in allowed_scopes:
                            grouped[pending_name].append(pending_sample)
                    pending.clear()
                elif name == "scene.decode-total" or name == "quest.scene-reception-and-preparation" or name == "quest.decode-verified-scene" and detailed_decode:
                    scope = sample["scope"] = last_scope
                    if name == "scene.decode-total":
                        detailed_decode = True
                elif name.startswith("scene.decode-") or name == "quest.decode-verified-scene":
                    pending.append((name, sample))
                    continue
                if scope in allowed_scopes:
                    grouped[name].append(sample)
    return {
        name: {"count": len(values), "medianMs": statistics.median(v["ms"] for v in values), "samples": values}
        for name, values in sorted(grouped.items())
    }


def transfers(path):
    if not path.exists():
        return []
    # Lifecycle metadata is appended after the sample rows by SyncTelemetryCapture.
    lines = path.read_text(encoding="utf-8-sig").splitlines()
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
    parser.add_argument("--quest-log", type=Path)
    args = parser.parse_args()
    root = args.directory
    display = json.loads((root / "display.json").read_text(encoding="utf-8-sig"))
    if not display.get("success"):
        parser.error("Display did not complete successfully")
    transfer_samples = transfers(root / "desktop-sync.csv")
    scope_ids = {sample["scope"] for sample in transfer_samples}
    quest_stages = stages(args.quest_log or root / "quest.log", scope_ids)
    quest_scope_ids = {sample["scope"] for stage in quest_stages.values() for sample in stage["samples"]}
    result = {
        "display": display,
        "desktopStages": stages(root / "pairing-timings.log"),
        "questStages": quest_stages,
        "questScopesMissing": sorted(scope_ids - quest_scope_ids),
        "desktopTransfers": transfer_samples,
        "limits": [
            "Async stage durations include waits and may overlap; do not add them.",
            "No Desktop/Quest timestamp subtraction is performed.",
            "A first Editor opening is not a process-cold Release measurement.",
            "An old protocol publication ACK does not prove remote post-policy scientific readiness.",
        ],
    }
    if not display.get("desktopIdleThrottlingSuspended"):
        result["limits"].append("Idle throttling was not suspended; this run cannot establish a controlled performance baseline.")
    (root / "summary.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"runs": display["runs"], "desktopTransfers": result["desktopTransfers"], "limits": result["limits"]}, indent=2))


if __name__ == "__main__":
    main()
