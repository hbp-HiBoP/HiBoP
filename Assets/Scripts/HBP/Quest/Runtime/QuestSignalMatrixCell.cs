using System;
using System.Collections.Generic;
using HBP.Core.Data;
using HBP.Sync;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    public sealed class QuestSignalMatrixCell : MonoBehaviour
    {
        [SerializeField] private Text title;
        [SerializeField] private RectTransform tiles;
        [SerializeField] private RawImage tilePrefab;
        [SerializeField] private QuestSignalMatrixOverlay overlay;
        private readonly List<Texture2D> textures = new();

        public void Bind(SignalSnapshot snapshot, SignalMatrixRow row, SignalMatrix cell, IReadOnlyDictionary<string, float[]> resources)
        {
            title.supportRichText = false;
            title.text = cell.Resource.Length == 0 && cell.Title.Length > 0 ? "No data" : "";
            title.gameObject.SetActive(title.text.Length > 0);
            GetComponent<LayoutElement>().flexibleWidth = Mathf.Max(1, cell.WindowEnd - cell.WindowStart);
            overlay.Bind(cell, row.Selected);
            if (cell.Resource.Length == 0)
            {
                return;
            }

            var flat = resources[cell.Resource];
            var trials = new float[cell.Rows][];
            for (int i = 0; i < trials.Length; i++)
            {
                trials[i] = new float[cell.Columns];
                Buffer.BlockCopy(flat, i * cell.Columns * 4, trials[i], 0, cell.Columns * 4);
            }

            var colors = new Color32[Math.Max(1, snapshot.Colormap.Length / 4)];
            for (int i = 0; i < colors.Length; i++) colors[i] = snapshot.Colormap.Length > 0 ? new Color32(snapshot.Colormap[4 * i], snapshot.Colormap[4 * i + 1], snapshot.Colormap[4 * i + 2], snapshot.Colormap[4 * i + 3]) : new Color32(128, 128, 128, 255);
            var built = TrialMatrixTileBuilder.Build(trials, new Vector2(row.Min, row.Max), colors, snapshot.Smooth, snapshot.SmoothingFactor, snapshot.Smooth2D, SystemInfo.maxTextureSize);
            foreach (var tile in built.Tiles)
            {
                var image = Instantiate(tilePrefab, tiles, false);
                var texture = new Texture2D(tile.TextureWidth, tile.TextureHeight, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                texture.SetPixels32(tile.Pixels);
                texture.Apply(false, true);
                textures.Add(texture);
                image.texture = texture;
                image.uvRect = tile.UvRect;
                image.raycastTarget = false;
                var rect = image.rectTransform;
                float span = Mathf.Max(1, cell.WindowEnd - cell.WindowStart), start = (cell.Start - cell.WindowStart) / span, width = (cell.End - cell.Start) / span;
                rect.anchorMin = new Vector2(start + width * tile.CoreX / built.Width, (float)tile.CoreY / built.Height);
                rect.anchorMax = new Vector2(start + width * (tile.CoreX + tile.CoreWidth) / built.Width, (float)(tile.CoreY + tile.CoreHeight) / built.Height);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }

            overlay.transform.SetAsLastSibling();
        }

        public void UpdateOverlay(SignalMatrix cell, bool[] selected) => overlay.Bind(cell, selected);

        public void Release()
        {
            foreach (var texture in textures)
                if (texture != null)
                {
                    if (Application.isPlaying) Destroy(texture);
                    else DestroyImmediate(texture);
                }

            textures.Clear();
        }

        private void OnDestroy() => Release();
    }
}
