using System.Collections.Generic;
using System.Linq;
using HBP.Sync;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>Desktop arrangement: site columns, data groups, stacked blocks, aligned time columns.</summary>
    public sealed class QuestSignalMatrixGrid : MonoBehaviour
    {
        [SerializeField] private RectTransform headers, groups;
        [SerializeField] private GameObject headerPrefab;
        [SerializeField] private QuestSignalMatrixGroup groupPrefab;
        private readonly List<QuestSignalMatrixGroup> controls = new();
        private string[] channelOrder = System.Array.Empty<string>();

        public void Bind(SignalSnapshot snapshot, IReadOnlyDictionary<string, float[]> resources)
        {
            var data = snapshot.Matrices.GroupBy(row => row.Group).ToArray();
            var channels = data.FirstOrDefault()?.GroupBy(row => row.ChannelId).Select(group => group.First()).ToArray() ?? System.Array.Empty<SignalMatrixRow>();
            var order = channels.Select(row => row.ChannelId).ToArray();
            if (!channelOrder.SequenceEqual(order) || headers.childCount != channels.Length)
            {
                for (int i = headers.childCount - 1; i >= 0; i--) Remove(headers.GetChild(i).gameObject);
                for (int i = 0; i < channels.Length; i++)
                {
                    var header = Instantiate(headerPrefab, headers, false);
                    header.GetComponentInChildren<Text>().text = channels[i].ChannelTitle;
                }

                channelOrder = order;
            }
            else
                for (int i = 0; i < channels.Length; i++)
                    headers.GetChild(i).GetComponentInChildren<Text>().text = channels[i].ChannelTitle;

            for (int i = 0; i < data.Length; i++)
            {
                var rows = data[i].ToArray();
                if (i < controls.Count && controls[i].TryUpdate(snapshot, rows)) continue;
                if (i < controls.Count)
                {
                    controls[i].Release();
                    Remove(controls[i].gameObject);
                }

                var control = Instantiate(groupPrefab, groups, false);
                // Register before reconstruction so an error also releases partially built groups.
                if (i < controls.Count) controls[i] = control;
                else controls.Add(control);
                control.transform.SetSiblingIndex(i);
                control.Bind(snapshot, rows, resources);
            }

            while (controls.Count > data.Length)
            {
                int index = controls.Count - 1;
                controls[index].Release();
                Remove(controls[index].gameObject);
                controls.RemoveAt(index);
            }

            headers.parent.gameObject.SetActive(channels.Length > 0);
        }

        public void Clear()
        {
            if (headers != null)
                for (int i = headers.childCount - 1; i >= 0; i--)
                    Remove(headers.GetChild(i).gameObject);
            foreach (var control in controls)
                if (control != null)
                {
                    control.Release();
                    Remove(control.gameObject);
                }

            controls.Clear();
            channelOrder = System.Array.Empty<string>();
        }

        internal static void Remove(GameObject value)
        {
            value.SetActive(false);
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }
    }
}
