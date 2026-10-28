using System.Collections.Generic;
using System.Linq;
using HBP.Sync;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    public sealed class QuestSignalMatrixBloc : MonoBehaviour
    {
        [SerializeField] private Text title;
        [SerializeField] private RectTransform channels;
        [SerializeField] private QuestSignalMatrixRow channelPrefab;
        [SerializeField] private LayoutElement size;
        private SignalMatrixRow[] rows;
        private readonly List<QuestSignalMatrixRow> controls = new();

        public void Bind(SignalSnapshot snapshot, SignalMatrixRow[] source, IReadOnlyDictionary<string, float[]> resources)
        {
            rows = source;
            title.text = source[0].BlocTitle;
            foreach (var row in source)
            {
                var control = Instantiate(channelPrefab, channels, false);
                controls.Add(control);
                control.Bind(snapshot, row, resources);
            }

            size.flexibleHeight = snapshot.Format == 3 ? source.Max(row => row.Selected.Length) : 0;
        }

        public bool TryUpdate(SignalSnapshot snapshot, SignalMatrixRow[] source)
        {
            if (rows.Length != source.Length || rows[0].BlocId != source[0].BlocId) return false;
            for (int i = 0; i < source.Length; i++)
                if (rows[i].ChannelId != source[i].ChannelId || !controls[i].TryUpdate(snapshot, source[i]))
                    return false;
            rows = source;
            title.text = source[0].BlocTitle;
            size.flexibleHeight = snapshot.Format == 3 ? source.Max(row => row.Selected.Length) : 0;
            return true;
        }

        public void Release()
        {
            foreach (var control in controls)
                if (control != null)
                    control.Release();
        }
    }
}
