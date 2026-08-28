#!/usr/bin/env python3
"""Render the phase-0 MNI anatomical/inflated comparison sheet."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import matplotlib
import nibabel as nib
import numpy as np

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--output",
        type=Path,
        default=Path(
            "Docs/dev/inflation/phase-0/external-references/"
            "workbench/mni-comparison.png"
        ),
    )
    return parser.parse_args()


def repository_root() -> Path:
    start = Path(__file__).resolve()
    for candidate in start.parents:
        if (candidate / "Assets").is_dir() and (candidate / "ProjectSettings").is_dir():
            return candidate
    raise RuntimeError(f"Could not locate the HiBoP repository from {start}")


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
    return np.asarray(points), np.asarray(triangles)


def set_equal_limits(
    axis: plt.Axes, bounds_min: np.ndarray, bounds_max: np.ndarray
) -> None:
    center = (bounds_min + bounds_max) * 0.5
    radius = float(np.max(bounds_max - bounds_min) * 0.52)
    axis.set_xlim(center[0] - radius, center[0] + radius)
    axis.set_ylim(center[1] - radius, center[1] + radius)
    axis.set_zlim(center[2] - radius, center[2] + radius)
    axis.set_box_aspect((1, 1, 1))


def main() -> int:
    args = parse_args()
    root = repository_root()
    output = args.output if args.output.is_absolute() else root / args.output
    workbench = root / "Docs/dev/inflation/phase-0/external-references/workbench"
    manifest = json.loads(
        (workbench / "execution-manifest.json").read_text(encoding="utf-8")
    )
    version = next(
        line.removeprefix("Version: ").strip()
        for line in manifest["tool"]["version_stdout"].splitlines()
        if line.startswith("Version: ")
    )
    rows = (
        (
            "Hémisphère gauche",
            (
                root / "Assets/Data/Meshes/MNI_Lwhite.gii",
                root / "Assets/Data/Meshes/MNI_Lwhite_inflated.gii",
                workbench / "mni_left_anatomical.inflated.surf.gii",
            ),
        ),
        (
            "Hémisphère droit",
            (
                root / "Assets/Data/Meshes/MNI_Rwhite.gii",
                root / "Assets/Data/Meshes/MNI_Rwhite_inflated.gii",
                workbench / "mni_right_anatomical.inflated.surf.gii",
            ),
        ),
    )
    column_titles = ("Anatomique", "Inflated historique", f"Workbench {version}")
    colors = ("#b8b8b8", "#e1a95f", "#5f9ed1")

    figure = plt.figure(figsize=(15, 9), dpi=180, facecolor="#202124")
    for row_index, (row_title, paths) in enumerate(rows):
        loaded = [load_surface(path) for path in paths]
        bounds_min = np.min(
            [np.min(vertices, axis=0) for vertices, _ in loaded], axis=0
        )
        bounds_max = np.max(
            [np.max(vertices, axis=0) for vertices, _ in loaded], axis=0
        )
        for column_index, ((vertices, triangles), title, color) in enumerate(
            zip(loaded, column_titles, colors)
        ):
            axis = figure.add_subplot(
                2, 3, row_index * 3 + column_index + 1, projection="3d"
            )
            axis.plot_trisurf(
                vertices[:, 0],
                vertices[:, 1],
                vertices[:, 2],
                triangles=triangles,
                color=color,
                linewidth=0,
                antialiased=False,
                shade=True,
            )
            axis.view_init(elev=8, azim=0)
            set_equal_limits(axis, bounds_min, bounds_max)
            axis.set_axis_off()
            axis.set_facecolor("#202124")
            axis.set_title(title, color="white", fontsize=11, pad=4)
            if column_index == 0:
                axis.text2D(
                    -0.05,
                    0.5,
                    row_title,
                    transform=axis.transAxes,
                    color="white",
                    rotation=90,
                    va="center",
                    fontsize=11,
                )

    figure.suptitle(
        "Baseline MNI — anatomique, référence historique et Connectome Workbench",
        color="white",
        fontsize=15,
    )
    figure.subplots_adjust(
        left=0.04, right=0.99, bottom=0.02, top=0.93, wspace=0.0, hspace=0.0
    )
    output.parent.mkdir(parents=True, exist_ok=True)
    figure.savefig(output, facecolor=figure.get_facecolor(), bbox_inches="tight")
    plt.close(figure)
    print(output.relative_to(root).as_posix())
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
