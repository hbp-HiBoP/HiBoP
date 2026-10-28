using System.Collections.Generic;
using System.Linq;
using HBP.Sync;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    public sealed class QuestSignalMatrixRow : MonoBehaviour
    {
        [SerializeField] private RectTransform cells;
        [SerializeField] private QuestSignalMatrixCell cellPrefab;
        [SerializeField] private LayoutElement cellHeight;
        private SignalSnapshot snapshot;
        private SignalMatrixRow row;
        private readonly List<QuestSignalMatrixCell> controls = new();

        public bool TryUpdate(SignalSnapshot state, SignalMatrixRow value)
        {
            if (snapshot == null || row.Title != value.Title || row.Min != value.Min || row.Max != value.Max || snapshot.Smooth != state.Smooth || snapshot.Smooth2D != state.Smooth2D || snapshot.SmoothingFactor != state.SmoothingFactor || !snapshot.Colormap.SequenceEqual(state.Colormap) || row.Cells.Length != value.Cells.Length) return false;
            for (int i = 0; i < row.Cells.Length; i++)
            {
                var before = row.Cells[i];
                var next = value.Cells[i];
                if (before.Resource != next.Resource || before.Start != next.Start || before.End != next.End || before.WindowStart != next.WindowStart || before.WindowEnd != next.WindowEnd || before.Rows != next.Rows || before.Columns != next.Columns) return false;
            }

            snapshot = state;
            row = value;
            for (int i = 0; i < controls.Count; i++) controls[i].UpdateOverlay(value.Cells[i], value.Selected);
            Resize();
            return true;
        }

        public void Bind(SignalSnapshot state, SignalMatrixRow value, IReadOnlyDictionary<string, float[]> resources)
        {
            snapshot = state;
            row = value;
            foreach (var cell in value.Cells)
            {
                var control = Instantiate(cellPrefab, cells, false);
                controls.Add(control);
                control.Bind(state, value, cell, resources);
            }

            Resize();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (snapshot != null) Resize();
        }

        public void Release()
        {
            foreach (var control in controls)
                if (control != null)
                    control.Release();
        }

        private void Resize()
        {
            float width = Mathf.Max(1, ((RectTransform)transform).rect.width), count = Mathf.Max(1, row.Selected.Length);
            cellHeight.preferredHeight = Mathf.Max(1, snapshot.Format switch
            {
                0 => snapshot.TrialHeight * count,
                1 => snapshot.TrialRatio * width * count,
                2 => snapshot.BlocRatio * width,
                _ => count * 5
            });
            cellHeight.flexibleHeight = snapshot.Format == 3 ? count : 0;
        }
    }
}
