#!/usr/bin/env python3
"""Generate and verify the cortical-inflation phase-0 baseline.

The command is intentionally independent from Unity and hbp_core. It creates a
deterministic, non-clinical geometry corpus, analyses every versioned surface,
classifies display transforms and records the availability of external oracles.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import shutil
import subprocess
import sys
import tempfile
from collections import Counter
from datetime import date
from pathlib import Path
from typing import Any

import nibabel as nib
import numpy as np


SCHEMA_VERSION = 1
GENERATOR_VERSION = "1.0.0"
PERCENTILES = (50, 90, 95, 99)
WORKBENCH_REFERENCE_FOLDER = Path(
    "Docs/dev/inflation/phase-0/external-references/workbench"
)
WORKBENCH_EXECUTION_MANIFEST = WORKBENCH_REFERENCE_FOLDER / "execution-manifest.json"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--verify",
        action="store_true",
        help="Fail if regenerating the committed baseline would change it.",
    )
    parser.add_argument(
        "--generate-workbench-references",
        action="store_true",
        help="Run wb_command for eligible corpus entries when it is installed.",
    )
    parser.add_argument(
        "--workbench-executable",
        type=Path,
        help="Explicit path to wb_command when it is not available on PATH.",
    )
    return parser.parse_args()


def repository_root() -> Path:
    start = Path(__file__).resolve()
    for candidate in start.parents:
        if (candidate / "Assets").is_dir() and (candidate / "ProjectSettings").is_dir():
            return candidate
    raise RuntimeError(f"Could not locate the HiBoP repository from {start}")


def write_json(path: Path, value: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        json.dumps(value, indent=2, sort_keys=False, allow_nan=False) + "\n",
        encoding="utf-8",
    )


def load_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"))


def relative_path(path: Path, root: Path) -> str:
    return path.resolve().relative_to(root.resolve()).as_posix()


def file_sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def topology_sha256(triangles: np.ndarray) -> str:
    canonical = np.asarray(triangles, dtype="<i8", order="C")
    return hashlib.sha256(canonical.tobytes()).hexdigest()


def make_icosphere(subdivisions: int) -> tuple[np.ndarray, np.ndarray]:
    phi = (1.0 + math.sqrt(5.0)) / 2.0
    vertices = [
        (-1, phi, 0),
        (1, phi, 0),
        (-1, -phi, 0),
        (1, -phi, 0),
        (0, -1, phi),
        (0, 1, phi),
        (0, -1, -phi),
        (0, 1, -phi),
        (phi, 0, -1),
        (phi, 0, 1),
        (-phi, 0, -1),
        (-phi, 0, 1),
    ]
    faces = [
        (0, 11, 5),
        (0, 5, 1),
        (0, 1, 7),
        (0, 7, 10),
        (0, 10, 11),
        (1, 5, 9),
        (5, 11, 4),
        (11, 10, 2),
        (10, 7, 6),
        (7, 1, 8),
        (3, 9, 4),
        (3, 4, 2),
        (3, 2, 6),
        (3, 6, 8),
        (3, 8, 9),
        (4, 9, 5),
        (2, 4, 11),
        (6, 2, 10),
        (8, 6, 7),
        (9, 8, 1),
    ]
    vertices_array = normalize_rows(np.asarray(vertices, dtype=np.float64))

    for _ in range(subdivisions):
        mutable_vertices = vertices_array.tolist()
        midpoint_cache: dict[tuple[int, int], int] = {}

        def midpoint(first: int, second: int) -> int:
            key = tuple(sorted((first, second)))
            if key not in midpoint_cache:
                point = np.asarray(mutable_vertices[first]) + np.asarray(
                    mutable_vertices[second]
                )
                point /= np.linalg.norm(point)
                midpoint_cache[key] = len(mutable_vertices)
                mutable_vertices.append(point.tolist())
            return midpoint_cache[key]

        refined_faces: list[tuple[int, int, int]] = []
        for first, second, third in faces:
            first_second = midpoint(first, second)
            second_third = midpoint(second, third)
            third_first = midpoint(third, first)
            refined_faces.extend(
                [
                    (first, first_second, third_first),
                    (second, second_third, first_second),
                    (third, third_first, second_third),
                    (first_second, second_third, third_first),
                ]
            )
        vertices_array = np.asarray(mutable_vertices, dtype=np.float64)
        faces = refined_faces

    return vertices_array, np.asarray(faces, dtype=np.int32)


def normalize_rows(values: np.ndarray) -> np.ndarray:
    return values / np.linalg.norm(values, axis=1, keepdims=True)


def make_patient_proxy(
    subdivisions: int, phase: float
) -> tuple[np.ndarray, np.ndarray]:
    vertices, triangles = make_icosphere(subdivisions)
    x, y, z = vertices.T
    wrinkles = 1.0 + 0.055 * np.sin(7.0 * np.arctan2(y, x) + phase) * np.cos(
        5.0 * np.arcsin(z)
    )
    transformed = vertices * wrinkles[:, None]
    transformed *= np.asarray([72.0, 48.0, 61.0])
    transformed[:, 0] -= 36.0
    return transformed.astype(np.float32), triangles


def make_open_surface() -> tuple[np.ndarray, np.ndarray]:
    vertices, triangles = make_patient_proxy(3, 0.25)
    keep = np.max(vertices[triangles, 2], axis=1) < 48.0
    return vertices, triangles[keep]


def make_degenerate_surface() -> tuple[np.ndarray, np.ndarray]:
    vertices = np.asarray(
        [[0, 0, 0], [1, 0, 0], [0, 1, 0], [2, 0, 0], [3, 0, 0]],
        dtype=np.float32,
    )
    triangles = np.asarray([[0, 1, 2], [3, 3, 4]], dtype=np.int32)
    return vertices, triangles


def make_non_manifold_surface() -> tuple[np.ndarray, np.ndarray]:
    vertices = np.asarray(
        [[0, 0, 0], [1, 0, 0], [0, 1, 0], [0, -1, 0], [0, 0, 1]],
        dtype=np.float32,
    )
    triangles = np.asarray([[0, 1, 2], [1, 0, 3], [0, 1, 4]], dtype=np.int32)
    return vertices, triangles


def make_multi_component_surface() -> tuple[np.ndarray, np.ndarray]:
    first_vertices, first_triangles = make_icosphere(1)
    second_vertices = first_vertices * 0.35 + np.asarray([3.0, 0.0, 0.0])
    vertices = np.vstack((first_vertices, second_vertices)).astype(np.float32)
    triangles = np.vstack(
        (first_triangles, first_triangles + len(first_vertices))
    ).astype(np.int32)
    return vertices, triangles


def save_gifti(path: Path, vertices: np.ndarray, triangles: np.ndarray) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    image = nib.gifti.GiftiImage(
        darrays=[
            nib.gifti.GiftiDataArray(
                np.asarray(vertices, dtype=np.float32),
                intent="NIFTI_INTENT_POINTSET",
            ),
            nib.gifti.GiftiDataArray(
                np.asarray(triangles, dtype=np.int32),
                intent="NIFTI_INTENT_TRIANGLE",
            ),
        ]
    )
    nib.save(image, path)


def generate_fixtures(root: Path, entries: list[dict[str, Any]]) -> None:
    generators = {
        "patient_proxy_low": lambda: make_patient_proxy(4, 0.0),
        "patient_proxy_medium": lambda: make_patient_proxy(5, 0.7),
        "patient_proxy_high": lambda: make_patient_proxy(6, 1.4),
        "open_surface": make_open_surface,
        "degenerate_surface": make_degenerate_surface,
        "non_manifold_surface": make_non_manifold_surface,
        "multi_component_surface": make_multi_component_surface,
    }
    for entry in entries:
        generator_name = entry.get("generator")
        if generator_name is None:
            continue
        vertices, triangles = generators[generator_name]()
        save_gifti(root / entry["path"], vertices, triangles)


def load_surface(path: Path) -> tuple[np.ndarray, np.ndarray]:
    image = nib.load(path)
    points = next(
        array.data
        for array in image.darrays
        if array.intent == nib.nifti1.intent_codes["NIFTI_INTENT_POINTSET"]
    )
    triangles = next(
        array.data
        for array in image.darrays
        if array.intent == nib.nifti1.intent_codes["NIFTI_INTENT_TRIANGLE"]
    )
    return np.asarray(points, dtype=np.float64), np.asarray(triangles, dtype=np.int64)


def distribution(values: np.ndarray) -> dict[str, float | None]:
    finite = np.asarray(values, dtype=np.float64)
    finite = finite[np.isfinite(finite)]
    if finite.size == 0:
        return {f"p{percentile}": None for percentile in PERCENTILES} | {"max": None}
    return {
        **{
            f"p{percentile}": float(np.percentile(finite, percentile))
            for percentile in PERCENTILES
        },
        "max": float(np.max(finite)),
    }


def connected_components(vertex_count: int, triangles: np.ndarray) -> tuple[int, int]:
    parents = np.arange(vertex_count, dtype=np.int64)
    used = np.zeros(vertex_count, dtype=bool)

    def find(index: int) -> int:
        while parents[index] != index:
            parents[index] = parents[parents[index]]
            index = int(parents[index])
        return index

    def union(first: int, second: int) -> None:
        first_root = find(first)
        second_root = find(second)
        if first_root != second_root:
            parents[second_root] = first_root

    for triangle in triangles:
        if np.any(triangle < 0) or np.any(triangle >= vertex_count):
            continue
        first, second, third = (int(value) for value in triangle)
        used[[first, second, third]] = True
        union(first, second)
        union(second, third)
    roots = {find(int(index)) for index in np.flatnonzero(used)}
    return len(roots), int(np.count_nonzero(~used))


def analyze_surface(path: Path, root: Path) -> dict[str, Any]:
    vertices, triangles = load_surface(path)
    valid_indices = bool(
        triangles.size == 0
        or (np.min(triangles) >= 0 and np.max(triangles) < len(vertices))
    )
    valid_triangles = (
        triangles
        if valid_indices
        else triangles[np.all((triangles >= 0) & (triangles < len(vertices)), axis=1)]
    )
    triangle_vertices = vertices[valid_triangles]
    doubled_areas = np.linalg.norm(
        np.cross(
            triangle_vertices[:, 1] - triangle_vertices[:, 0],
            triangle_vertices[:, 2] - triangle_vertices[:, 0],
        ),
        axis=1,
    )
    triangle_areas = doubled_areas * 0.5
    repeated_index = np.any(
        np.column_stack(
            (
                valid_triangles[:, 0] == valid_triangles[:, 1],
                valid_triangles[:, 1] == valid_triangles[:, 2],
                valid_triangles[:, 2] == valid_triangles[:, 0],
            )
        ),
        axis=1,
    )
    degenerate = repeated_index | (triangle_areas <= 1e-12)

    directed_edges = np.vstack(
        (
            valid_triangles[:, [0, 1]],
            valid_triangles[:, [1, 2]],
            valid_triangles[:, [2, 0]],
        )
    )
    sorted_edges = np.sort(directed_edges, axis=1)
    edge_counts = Counter(map(tuple, sorted_edges.tolist()))
    unique_edges = np.asarray(list(edge_counts), dtype=np.int64)
    edge_lengths = (
        np.linalg.norm(
            vertices[unique_edges[:, 1]] - vertices[unique_edges[:, 0]], axis=1
        )
        if len(unique_edges)
        else np.empty(0, dtype=np.float64)
    )
    component_count, isolated_vertices = connected_components(
        len(vertices), valid_triangles
    )
    finite_positions = bool(np.all(np.isfinite(vertices)))
    finite_vertices = vertices[np.all(np.isfinite(vertices), axis=1)]
    if len(finite_vertices):
        bounds_min = np.min(finite_vertices, axis=0)
        bounds_max = np.max(finite_vertices, axis=0)
        centroid = np.mean(finite_vertices, axis=0)
        rms_radius = float(
            np.sqrt(np.mean(np.sum((finite_vertices - centroid) ** 2, axis=1)))
        )
    else:
        bounds_min = bounds_max = centroid = np.full(3, np.nan)
        rms_radius = math.nan

    return {
        "path": relative_path(path, root),
        "sha256": file_sha256(path),
        "vertex_count": int(len(vertices)),
        "triangle_count": int(len(triangles)),
        "topology_sha256": topology_sha256(triangles),
        "positions_finite": finite_positions,
        "indices_valid": valid_indices,
        "connected_components": component_count,
        "isolated_vertices": isolated_vertices,
        "boundary_edges": sum(count == 1 for count in edge_counts.values()),
        "non_manifold_edges": sum(count > 2 for count in edge_counts.values()),
        "degenerate_triangles": int(np.count_nonzero(degenerate)),
        "zero_length_edges": int(np.count_nonzero(edge_lengths <= 1e-12)),
        "bounding_box": {
            "min": bounds_min.tolist(),
            "max": bounds_max.tolist(),
            "extent": (bounds_max - bounds_min).tolist(),
        },
        "centroid": centroid.tolist(),
        "surface_area": float(np.sum(triangle_areas)),
        "rms_radius": rms_radius,
        "edge_length": distribution(edge_lengths),
        "triangle_area": distribution(triangle_areas),
    }


def load_transform(path: Path) -> tuple[np.ndarray, np.ndarray]:
    rows = [
        [float(value) for value in line.split()]
        for line in path.read_text(encoding="ascii").splitlines()
        if line.strip()
    ]
    if len(rows) == 4 and all(len(row) == 3 for row in rows):
        translation = np.asarray(rows[0], dtype=np.float64)
        linear = np.asarray(rows[1:], dtype=np.float64)
        return linear, translation
    matrix = np.asarray(rows, dtype=np.float64)
    if matrix.shape == (4, 4):
        return matrix[:3, :3], matrix[:3, 3]
    raise ValueError(f"Unsupported .trm layout in {path}")


def analyze_transform(path: Path, root: Path) -> dict[str, Any]:
    linear, translation = load_transform(path)
    singular_values = np.linalg.svd(linear, compute_uv=False)
    uniform = bool(np.max(singular_values) - np.min(singular_values) <= 1e-6)
    unit_scale = bool(np.max(np.abs(singular_values - 1.0)) <= 1e-6)
    classification = (
        "rigid" if uniform and unit_scale else "uniform" if uniform else "anisotropic"
    )
    return {
        "path": relative_path(path, root),
        "sha256": file_sha256(path),
        "classification": classification,
        "translation": translation.tolist(),
        "linear": linear.tolist(),
        "singular_values": singular_values.tolist(),
        "determinant": float(np.linalg.det(linear)),
        "orientation_reversing": bool(np.linalg.det(linear) < 0.0),
    }


def compare_reference(
    source: dict[str, Any], reference: dict[str, Any]
) -> dict[str, Any]:
    source_extent = np.asarray(source["bounding_box"]["extent"])
    reference_extent = np.asarray(reference["bounding_box"]["extent"])
    return {
        "same_vertex_count": source["vertex_count"] == reference["vertex_count"],
        "same_triangle_count": source["triangle_count"] == reference["triangle_count"],
        "strictly_identical_topology": source["topology_sha256"]
        == reference["topology_sha256"],
        "surface_area_ratio": reference["surface_area"] / source["surface_area"],
        "bounding_box_extent_ratio": (reference_extent / source_extent).tolist(),
        "rms_radius_ratio": reference["rms_radius"] / source["rms_radius"],
    }


def compare_surface_files(
    source_path: Path,
    reference_path: Path,
    source_metrics: dict[str, Any],
    reference_metrics: dict[str, Any],
) -> dict[str, Any]:
    result = compare_reference(source_metrics, reference_metrics)
    source_vertices, source_triangles = load_surface(source_path)
    reference_vertices, reference_triangles = load_surface(reference_path)
    if not np.array_equal(source_triangles, reference_triangles):
        return result

    edges = np.unique(
        np.sort(
            np.vstack(
                (
                    source_triangles[:, [0, 1]],
                    source_triangles[:, [1, 2]],
                    source_triangles[:, [2, 0]],
                )
            ),
            axis=1,
        ),
        axis=0,
    )
    source_edge_lengths = np.linalg.norm(
        source_vertices[edges[:, 1]] - source_vertices[edges[:, 0]], axis=1
    )
    reference_edge_lengths = np.linalg.norm(
        reference_vertices[edges[:, 1]] - reference_vertices[edges[:, 0]], axis=1
    )
    valid_edges = source_edge_lengths > 1e-12

    source_triangle_vertices = source_vertices[source_triangles]
    reference_triangle_vertices = reference_vertices[reference_triangles]
    source_triangle_areas = 0.5 * np.linalg.norm(
        np.cross(
            source_triangle_vertices[:, 1] - source_triangle_vertices[:, 0],
            source_triangle_vertices[:, 2] - source_triangle_vertices[:, 0],
        ),
        axis=1,
    )
    reference_triangle_areas = 0.5 * np.linalg.norm(
        np.cross(
            reference_triangle_vertices[:, 1] - reference_triangle_vertices[:, 0],
            reference_triangle_vertices[:, 2] - reference_triangle_vertices[:, 0],
        ),
        axis=1,
    )
    valid_triangles = source_triangle_areas > 1e-12

    result.update(
        {
            "edge_length_ratio": distribution(
                reference_edge_lengths[valid_edges] / source_edge_lengths[valid_edges]
            ),
            "triangle_area_ratio": distribution(
                reference_triangle_areas[valid_triangles]
                / source_triangle_areas[valid_triangles]
            ),
            "vertex_displacement": distribution(
                np.linalg.norm(reference_vertices - source_vertices, axis=1)
            ),
        }
    )
    return result


def executable_info(name: str, explicit_path: Path | None = None) -> dict[str, Any]:
    executable = str(explicit_path.resolve()) if explicit_path else shutil.which(name)
    if executable is None or not Path(executable).is_file():
        return {"available": False, "path": executable}
    version = subprocess.run(
        [executable, "-version"],
        capture_output=True,
        text=True,
        check=False,
        timeout=30,
    )
    return {
        "available": True,
        "path": executable,
        "sha256": file_sha256(Path(executable)),
        "version_exit_code": version.returncode,
        "version_stdout": version.stdout,
        "version_stderr": version.stderr,
    }


def generate_workbench_archive(
    root: Path, entries: list[dict[str, Any]], executable: Path
) -> None:
    output_folder = root / WORKBENCH_REFERENCE_FOLDER
    output_folder.mkdir(parents=True, exist_ok=True)
    tool = executable_info("wb_command", executable)
    if not tool["available"]:
        raise RuntimeError(f"Workbench executable not found: {executable}")

    manifest: dict[str, Any] = {
        "schema_version": 1,
        "tool": tool,
        "working_directory": ".",
        "iterations_scale": 1.0,
        "runs": {},
    }
    write_json(root / WORKBENCH_EXECUTION_MANIFEST, manifest)

    for entry in entries:
        if not entry.get("external_reference_eligible", False):
            continue
        inflated = WORKBENCH_REFERENCE_FOLDER / f"{entry['id']}.inflated.surf.gii"
        very_inflated = (
            WORKBENCH_REFERENCE_FOLDER / f"{entry['id']}.very-inflated.surf.gii"
        )
        for output in (inflated, very_inflated):
            (root / output).unlink(missing_ok=True)
        command = [
            str(executable.resolve()),
            "-surface-generate-inflated",
            entry["path"],
            inflated.as_posix(),
            very_inflated.as_posix(),
            "-iterations-scale",
            "1.0",
        ]
        completed = subprocess.run(
            command,
            cwd=root,
            capture_output=True,
            text=True,
            check=False,
            timeout=600,
        )
        manifest["runs"][entry["id"]] = {
            "source": entry["path"],
            "source_sha256": file_sha256(root / entry["path"]),
            "command": command,
            "exit_code": completed.returncode,
            "stdout": completed.stdout,
            "stderr": completed.stderr,
            "outputs": {
                "inflated": inflated.as_posix(),
                "very_inflated": very_inflated.as_posix(),
            },
        }
        write_json(root / WORKBENCH_EXECUTION_MANIFEST, manifest)


def analyze_workbench_archive(
    root: Path,
    entries: list[dict[str, Any]],
    source_metrics: dict[str, dict[str, Any]],
) -> dict[str, Any]:
    manifest_path = root / WORKBENCH_EXECUTION_MANIFEST
    if not manifest_path.is_file():
        return {}
    manifest = load_json(manifest_path)
    entries_by_id = {entry["id"]: entry for entry in entries}
    analyzed_runs: dict[str, Any] = {}
    for identifier, run in manifest["runs"].items():
        entry = entries_by_id[identifier]
        source_path = root / run["source"]
        outputs: dict[str, Any] = {}
        for representation, relative_output in run["outputs"].items():
            output_path = root / relative_output
            output: dict[str, Any] = {
                "path": relative_output,
                "exists": output_path.is_file(),
            }
            if output_path.is_file():
                metrics = analyze_surface(output_path, root)
                output["metrics"] = metrics
                output["comparison_to_source"] = compare_surface_files(
                    source_path,
                    output_path,
                    source_metrics[identifier],
                    metrics,
                )
                local_reference_id = entry.get("local_reference")
                if local_reference_id:
                    local_reference_path = (
                        root / entries_by_id[local_reference_id]["path"]
                    )
                    output["comparison_to_local_reference"] = compare_surface_files(
                        local_reference_path,
                        output_path,
                        source_metrics[local_reference_id],
                        metrics,
                    )
            outputs[representation] = output
        analyzed_runs[identifier] = {
            **{key: value for key, value in run.items() if key != "outputs"},
            "outputs": outputs,
        }
    result = {
        "execution_manifest": WORKBENCH_EXECUTION_MANIFEST.as_posix(),
        "tool": manifest["tool"],
        "iterations_scale": manifest["iterations_scale"],
        "runs": analyzed_runs,
    }
    comparison_image = root / WORKBENCH_REFERENCE_FOLDER / "mni-comparison.png"
    if comparison_image.is_file():
        result["visual_comparison"] = {
            "path": relative_path(comparison_image, root),
            "sha256": file_sha256(comparison_image),
        }
    return result


def build_baseline(
    root: Path,
    config: dict[str, Any],
    generate_workbench: bool,
    workbench_executable: Path | None = None,
) -> dict[str, Any]:
    entries = config["surfaces"]
    generate_fixtures(root, entries)
    surfaces = {
        entry["id"]: {
            "category": entry["category"],
            "expected_admissibility": entry["expected_admissibility"],
            "source_kind": entry["source_kind"],
            **analyze_surface(root / entry["path"], root),
        }
        for entry in entries
    }
    transforms = {
        entry["id"]: analyze_transform(root / entry["path"], root)
        for entry in config["transforms"]
    }
    comparisons = {}
    for entry in entries:
        reference_id = entry.get("local_reference")
        if reference_id:
            comparisons[entry["id"]] = {
                "reference_id": reference_id,
                **compare_reference(surfaces[entry["id"]], surfaces[reference_id]),
            }

    if generate_workbench:
        executable = workbench_executable
        if executable is None:
            discovered = shutil.which("wb_command")
            executable = Path(discovered) if discovered else None
        if executable is None:
            raise RuntimeError(
                "--generate-workbench-references requested, but wb_command is not installed or specified"
            )
        generate_workbench_archive(root, entries, executable)

    tools = {
        "connectome_workbench": executable_info("wb_command", workbench_executable),
        "freesurfer_mris_inflate": executable_info("mris_inflate"),
    }
    external_runs = analyze_workbench_archive(root, entries, surfaces)

    return {
        "schema_version": SCHEMA_VERSION,
        "generator_version": GENERATOR_VERSION,
        "baseline_date": date(2026, 8, 27).isoformat(),
        "source_manifest": "Docs/dev/inflation/phase-0/corpus.json",
        "python": {
            "minimum_version": "3.10",
            "numpy_version": np.__version__,
            "nibabel_version": nib.__version__,
        },
        "surfaces": surfaces,
        "transforms": transforms,
        "local_reference_comparisons": comparisons,
        "external_tools": tools,
        "external_reference_runs": external_runs,
    }


def validate_gate(root: Path, baseline: dict[str, Any]) -> None:
    errors: list[str] = []
    surfaces = baseline["surfaces"]
    comparisons = baseline["local_reference_comparisons"]

    for identifier in ("mni_left_anatomical", "mni_right_anatomical"):
        comparison = comparisons[identifier]
        if not all(
            comparison[key]
            for key in (
                "same_vertex_count",
                "same_triangle_count",
                "strictly_identical_topology",
            )
        ):
            errors.append(f"{identifier} does not match its local inflated topology")

    open_surface = surfaces["open_surface"]
    if open_surface["boundary_edges"] <= 0 or open_surface["non_manifold_edges"] != 0:
        errors.append("open_surface is not an isolated manifold-with-boundary oracle")

    degenerate = surfaces["degenerate_surface"]
    if (
        degenerate["degenerate_triangles"] <= 0
        or degenerate["zero_length_edges"] <= 0
        or degenerate["non_manifold_edges"] != 0
    ):
        errors.append("degenerate_surface does not isolate degenerate geometry")

    non_manifold = surfaces["non_manifold_surface"]
    if (
        non_manifold["non_manifold_edges"] <= 0
        or non_manifold["degenerate_triangles"] != 0
    ):
        errors.append("non_manifold_surface does not isolate non-manifold topology")

    if surfaces["multi_component_surface"]["connected_components"] != 2:
        errors.append("multi_component_surface must contain exactly two components")

    observed_classes = {
        transform["classification"] for transform in baseline["transforms"].values()
    }
    if observed_classes != {"rigid", "uniform", "anisotropic"}:
        errors.append(f"unexpected transform classes: {sorted(observed_classes)}")

    contract = load_json(root / "Docs/dev/inflation/phase-0/product-contract.json")
    if len(contract["states"]) != 6:
        errors.append("product contract must define exactly six states")
    if (
        any(
            "Créer, modifier ou supprimer une coupe est autorisé dans tous les états."
            == invariant
            for invariant in contract["invariants"]
        )
        is False
    ):
        errors.append("product contract does not preserve cut editing in every state")
    if contract["representation"]["default"] != "anatomical":
        errors.append("anatomical must remain the default representation")

    parameters = load_json(
        root / "Docs/dev/inflation/phase-0/external-reference-parameters.json"
    )
    corpus = load_json(root / "Docs/dev/inflation/phase-0/corpus.json")
    expected_external_entries = {
        entry["id"]
        for entry in corpus["surfaces"]
        if entry.get("external_reference_eligible", False)
    }
    configured_external_entries = set(
        parameters["connectome_workbench"]["eligible_entries"]
    )
    if configured_external_entries != expected_external_entries:
        errors.append("Workbench reference parameters do not match the corpus")

    workbench = baseline["external_reference_runs"]
    for identifier in ("mni_left_anatomical", "mni_right_anatomical"):
        run = workbench.get("runs", {}).get(identifier)
        if run is None or run["exit_code"] != 0:
            errors.append(f"missing successful Workbench run for {identifier}")
            continue
        for representation in ("inflated", "very_inflated"):
            output = run["outputs"][representation]
            comparison = output.get("comparison_to_source", {})
            if not output["exists"] or not all(
                comparison.get(key, False)
                for key in (
                    "same_vertex_count",
                    "same_triangle_count",
                    "strictly_identical_topology",
                )
            ):
                errors.append(
                    f"invalid Workbench {representation} reference for {identifier}"
                )

    if errors:
        raise RuntimeError("Phase-0 gate failed:\n- " + "\n- ".join(errors))


def main() -> int:
    args = parse_args()
    root = repository_root()
    baseline_folder = root / "Docs/dev/inflation/phase-0"
    config = load_json(baseline_folder / "corpus.json")
    output_path = baseline_folder / "reference-metrics.json"

    if args.verify:
        committed = load_json(output_path)
        with tempfile.TemporaryDirectory(prefix="hibop-inflation-phase0-") as folder:
            temporary_root = Path(folder) / "HiBoP"
            shutil.copytree(
                root / "Assets/Data/Meshes", temporary_root / "Assets/Data/Meshes"
            )
            shutil.copytree(
                baseline_folder,
                temporary_root / "Docs/dev/inflation/phase-0",
            )
            generated = build_baseline(temporary_root, config, False)
            validate_gate(temporary_root, generated)
        for environment_key in (
            "python",
            "external_tools",
        ):
            generated[environment_key] = committed[environment_key]
        if generated != committed:
            print(
                "Phase-0 baseline is stale. Run phase0_baseline.py and commit the results.",
                file=sys.stderr,
            )
            return 1
        print("Phase-0 inflation baseline is reproducible.")
        return 0

    baseline = build_baseline(
        root,
        config,
        args.generate_workbench_references,
        args.workbench_executable,
    )
    validate_gate(root, baseline)
    write_json(output_path, baseline)
    print(f"Wrote {relative_path(output_path, root)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
