"""Summarize explicitly selected Desktop and Quest traces; no synchronized clocks required."""
import argparse
import json
from pathlib import Path


def event(trace, name):
    return next((item["atMs"] for item in trace["events"] if item["name"] == name and "atMs" in item), None)


def summarize(trace):
    facts = trace["facts"]
    print(f"\n{facts['role']} / {facts['attempt']} / {facts.get('outcome', 'incomplete')}")
    print(f"Visualization: {facts.get('visualization', '?')}; archive: {facts.get('archiveBytes', 0):,} bytes")
    print(f"Operation: {facts.get('operationMs', 0):,.1f} ms; render: {facts.get('renderOutcome', 'n/a')}")
    print(f"Build: editor={facts.get('editor', '?')}, development={facts.get('developmentBuild', '?')}; automatic activity={facts.get('automaticActivity', '?')}")
    if facts['role'] == 'desktop':
        print(f"Route: {facts.get('route', 'unknown')}")
        if facts.get('route') == 'usb-forward':
            print("WARNING: USB forwarding was used; this run does not measure Wi-Fi performance.")
    if trace.get("droppedEvents"):
        print(f"WARNING: {trace['droppedEvents']} timeline events omitted (aggregates retained).")
    metrics = trace["metrics"]
    frames = metrics.get("frames.interval", {})
    print(f"Largest frame interval: {frames.get('maxMs', 0):,.1f} ms; >100ms: {facts.get('frames.over100ms', 0)}")
    start, end = ("send.payload.begin", "send.payload.end") if facts["role"] == "desktop" else ("receive.payload.begin", "receive.payload.end")
    a, b = event(trace, start), event(trace, end)
    if a is not None and b is not None and b > a:
        print(f"Payload interval: {b-a:,.1f} ms; effective rate: {facts['archiveBytes'] / ((b-a)/1000) / 1048576:.2f} MiB/s")
    if facts.get("expandedBytes") and facts.get("archiveBytes"):
        print(f"Archive/expanded: {facts['archiveBytes'] / facts['expandedBytes']:.3f}")
    print("\nInclusive metrics (nested/overlapping scopes MUST NOT be added):")
    print(f"{'phase':57s} {'count':>8s} {'total ms':>12s} {'max ms':>12s} {'bytes':>14s}")
    for name, metric in sorted(metrics.items(), key=lambda item: -item[1]["totalMs"]):
        if name == "frames.interval":
            continue
        print(f"{name:57s} {metric['count']:8,d} {metric['totalMs']:12,.2f} {metric['maxMs']:12,.2f} {metric['bytes']:14,d}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("desktop", type=Path)
    parser.add_argument("quest", type=Path)
    args = parser.parse_args()
    desktop, quest = (json.loads(path.read_text(encoding="utf-8-sig")) for path in (args.desktop, args.quest))
    for trace, role in ((desktop, "desktop"), (quest, "quest")):
        if trace["facts"].get("schema") != "quest-transfer-trace/1" or trace["facts"].get("role") != role:
            parser.error(f"Expected a {role} quest-transfer-trace/1 file")
    if not desktop["facts"].get("archiveHash") or desktop["facts"]["archiveHash"] != quest["facts"].get("archiveHash"):
        parser.error("Archive hashes differ or are absent; select the two sides of the same attempt")
    if desktop["facts"].get("retry"):
        print("Retry: verify these files belong to the same attempt; an archive hash may occur repeatedly.")
    summarize(desktop)
    summarize(quest)
    # Bounds on the offset of Quest's monotonic origin in Desktop's timeline:
    # Send initiation precedes reception. A WriteAsync continuation may run AFTER
    # the peer receives bytes, so its completion is not a valid causal anchor.
    ds, dr = event(desktop, "send.payload.begin"), event(desktop, "desktop.receipt")
    qr, qa, qf = (event(quest, name) for name in ("receive.payload.begin", "receive.receipt.begin", "quest.render.firstMainCameraEnd"))
    if all(value is not None for value in (ds, dr, qr, qa, qf)):
        lower, upper = ds + qf - qr, dr + qf - qa
        if 0 <= lower <= upper:
            print(f"\nClick -> first main-camera CPU render end: causal interval [{lower:,.1f}, {upper:,.1f}] ms.")
            print("No clock synchronization assumed; small clock-rate drift remains. This is not GPU/photon latency.")
        else:
            print(f"\nInconsistent causal interval: lower={lower:,.4f} ms, upper={upper:,.4f} ms, inversion={lower-upper:,.4f} ms.")
            print("Verify attempt pairing and clock-rate drift; no strict end-to-end interval inferred.")
    else:
        print("\nNo end-to-end render interval: a required milestone is missing (retry, failure or camera not rendered).")


if __name__ == "__main__":
    main()
