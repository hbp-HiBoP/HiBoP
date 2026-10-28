using HBP.Sync;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    public sealed class QuestSignalMatrixOverlay : MaskableGraphic
    {
        private SignalMatrix cell;
        private bool[] selected;

        public void Bind(SignalMatrix value, bool[] selection)
        {
            if ((long)(selection.Length + value.Events.Length) * 4 > 60000) throw new System.IO.InvalidDataException("Matrix overlays exceed the Quest UI rendering capacity.");
            cell = value;
            selected = selection;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (cell == null || cell.Rows == 0) return;
            Rect r = GetPixelAdjustedRect();
            float rowHeight = r.height / cell.Rows;
            for (int i = 0; i < selected.Length; i++)
            {
                if (selected[i]) continue;
                float top = r.yMax - i * rowHeight;
                QuestSignalGraphGraphic.Quad(vh, new Vector2(r.xMin, top - rowHeight), new Vector2(r.xMin, top), new Vector2(r.xMax, top), new Vector2(r.xMax, top - rowHeight), new Color(0, 0, 0, .45f));
            }

            foreach (var ev in cell.Events)
            {
                float ratio = (cell.Start + ev.Position * (cell.End - cell.Start) - cell.WindowStart) / Mathf.Max(1, cell.WindowEnd - cell.WindowStart);
                float x = r.xMin + ratio * r.width, top = r.yMax - ev.Trial * rowHeight;
                QuestSignalGraphGraphic.Line(vh, new Vector2(x, top - rowHeight), new Vector2(x, top), new Color(ev.Color[0], ev.Color[1], ev.Color[2], ev.Color[3]), 4);
            }
        }
    }
}
