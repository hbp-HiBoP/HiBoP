using System.Collections.Generic;
using System.Linq;
using HBP.Core.Tools;
using HBP.Sync;
using HBP.UI.Informations.TrialMatrix;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    public sealed class QuestSignalMatrixGroup : MonoBehaviour
    {
        [SerializeField] private Text title;
        [SerializeField] private RectTransform blocks, timeLegends;
        [SerializeField] private QuestSignalMatrixBloc blocPrefab;
        [SerializeField] private TimeLegend timeLegendPrefab;
        [SerializeField] private RawImage colors;
        [SerializeField] private ValuesLegend values;
        [SerializeField] private LayoutElement size;
        private SignalSnapshot snapshot;
        private SignalMatrixRow[] rows;
        private Texture2D colormap;
        private readonly List<QuestSignalMatrixBloc> controls = new();

        public void Bind(SignalSnapshot state, SignalMatrixRow[] source, IReadOnlyDictionary<string, float[]> resources)
        {
            snapshot = state;
            rows = source;
            title.text = source[0].GroupTitle;
            values.Limits = new Vector2(source[0].Min, source[0].Max);
            int count = Mathf.Max(1, state.Colormap.Length / 4);
            colormap = new Texture2D(count, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var palette = new Color32[count];
            for (int i = 0; i < count; i++) palette[i] = state.Colormap.Length == 0 ? new Color32(128, 128, 128, 255) : new Color32(state.Colormap[4 * i], state.Colormap[4 * i + 1], state.Colormap[4 * i + 2], state.Colormap[4 * i + 3]);
            colormap.SetPixels32(palette);
            colormap.Apply(false, true);
            colors.texture = colormap;
            foreach (var bloc in source.GroupBy(row => row.BlocId))
            {
                var control = Instantiate(blocPrefab, blocks, false);
                controls.Add(control);
                control.Bind(state, bloc.ToArray(), resources);
            }

            foreach (var channel in source.GroupBy(row => row.ChannelId))
            {
                var legend = Instantiate(timeLegendPrefab, timeLegends, false);
                legend.Limits = channel.First().Cells.Select(cell => new TimeWindow(Mathf.RoundToInt(cell.WindowStart), Mathf.RoundToInt(cell.WindowEnd))).ToArray();
            }

            Resize();
        }

        public bool TryUpdate(SignalSnapshot state, SignalMatrixRow[] source)
        {
            if (rows == null || rows.Length != source.Length || rows[0].Group != source[0].Group || rows[0].Min != source[0].Min || rows[0].Max != source[0].Max || !snapshot.Colormap.SequenceEqual(state.Colormap)) return false;
            var blocs = source.GroupBy(row => row.BlocId).ToArray();
            if (blocs.Length != controls.Count) return false;
            for (int i = 0; i < blocs.Length; i++)
                if (!controls[i].TryUpdate(state, blocs[i].ToArray()))
                    return false;
            snapshot = state;
            rows = source;
            title.text = source[0].GroupTitle;
            Resize();
            return true;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (snapshot != null) Resize();
        }

        private void Resize()
        {
            size.enabled = snapshot.Format == 3;
            if (size.enabled) size.minHeight = size.preferredHeight = ((RectTransform)transform).rect.width * snapshot.ProtocolRatio;
        }

        public void Release()
        {
            foreach (var control in controls)
                if (control != null)
                    control.Release();
            if (colormap != null)
            {
                if (Application.isPlaying) Destroy(colormap);
                else DestroyImmediate(colormap);
            }

            colormap = null;
        }

        private void OnDestroy() => Release();
    }
}
