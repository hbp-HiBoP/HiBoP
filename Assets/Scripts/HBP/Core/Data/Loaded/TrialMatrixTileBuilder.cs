using System;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.Core.Data
{
    public sealed class TrialMatrixTile
    {
        public int CoreX { get; }
        public int CoreY { get; }
        public int CoreWidth { get; }
        public int CoreHeight { get; }
        public int TextureWidth { get; }
        public int TextureHeight { get; }
        public Rect UvRect { get; }
        public Color32[] Pixels { get; }

        internal TrialMatrixTile(int coreX, int coreY, int coreWidth, int coreHeight,
            int textureWidth, int textureHeight, Rect uvRect, Color32[] pixels)
        {
            CoreX = coreX;
            CoreY = coreY;
            CoreWidth = coreWidth;
            CoreHeight = coreHeight;
            TextureWidth = textureWidth;
            TextureHeight = textureHeight;
            UvRect = uvRect;
            Pixels = pixels;
        }
    }

    public sealed class TrialMatrixTiles
    {
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<TrialMatrixTile> Tiles { get; }

        internal TrialMatrixTiles(int width, int height, IReadOnlyList<TrialMatrixTile> tiles)
        {
            Width = width;
            Height = height;
            Tiles = tiles;
        }
    }

    public static class TrialMatrixTileBuilder
    {
        public static TrialMatrixTiles Build(float[][] trials, Vector2 limits, Color32[] colors,
            bool smooth, int smoothingFactor, bool smooth2D, int maxTextureSize)
        {
            Validate(trials, colors, maxTextureSize);
            if (trials.Length == 0)
                return new TrialMatrixTiles(0, 0, Array.Empty<TrialMatrixTile>());

            int inputHeight = trials.Length;
            int inputWidth = trials[0].Length;
            if (inputWidth == 0)
                return new TrialMatrixTiles(0, inputHeight, Array.Empty<TrialMatrixTile>());

            int factor = smooth ? Math.Max(1, smoothingFactor) : 1;
            int outputWidth = CheckedSmoothedLength(inputWidth, factor);
            int outputHeight = smooth && smooth2D ? CheckedSmoothedLength(inputHeight, factor) : inputHeight;
            int halo = smooth && smooth2D ? 1 : 0;
            int maxCoreSize = maxTextureSize - 2 * halo;
            if (maxCoreSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxTextureSize), "Texture size is too small for the smoothing halo.");

            List<TrialMatrixTile> tiles = new();
            for (int coreY = 0; coreY < outputHeight; coreY += maxCoreSize)
            {
                int coreHeight = Math.Min(maxCoreSize, outputHeight - coreY);
                int bottomHalo = coreY > 0 ? halo : 0;
                int topHalo = coreY + coreHeight < outputHeight ? halo : 0;
                for (int coreX = 0; coreX < outputWidth; coreX += maxCoreSize)
                {
                    int coreWidth = Math.Min(maxCoreSize, outputWidth - coreX);
                    int leftHalo = coreX > 0 ? halo : 0;
                    int rightHalo = coreX + coreWidth < outputWidth ? halo : 0;
                    int textureWidth = leftHalo + coreWidth + rightHalo;
                    int textureHeight = bottomHalo + coreHeight + topHalo;
                    Color32[] pixels = new Color32[checked(textureWidth * textureHeight)];

                    int textureStartX = coreX - leftHalo;
                    int textureStartY = coreY - bottomHalo;
                    FillPixels(trials, limits, colors, factor, smooth && smooth2D,
                        outputHeight, textureStartX, textureStartY, textureWidth, textureHeight, pixels);

                    Rect uvRect = new(
                        (float)leftHalo / textureWidth,
                        (float)bottomHalo / textureHeight,
                        (float)coreWidth / textureWidth,
                        (float)coreHeight / textureHeight);
                    tiles.Add(new TrialMatrixTile(coreX, coreY, coreWidth, coreHeight,
                        textureWidth, textureHeight, uvRect, pixels));
                }
            }
            return new TrialMatrixTiles(outputWidth, outputHeight, tiles);
        }

        private static void FillPixels(float[][] trials, Vector2 limits, Color32[] colors,
            int factor, bool smoothTrials, int outputHeight, int startX, int startY,
            int width, int height, Color32[] pixels)
        {
            for (int y = 0; y < height; y++)
            {
                int displayY = startY + y;
                int dataY = outputHeight - 1 - displayY;
                for (int x = 0; x < width; x++)
                {
                    int dataX = startX + x;
                    float value = Sample(trials, dataX, dataY, factor, smoothTrials);
                    pixels[y * width + x] = GetColor(value, limits, colors);
                }
            }
        }

        private static float Sample(float[][] trials, int outputX, int outputY, int factor, bool smoothTrials)
        {
            int x0 = outputX / factor;
            int x1 = Math.Min(x0 + 1, trials[0].Length - 1);
            float tx = (outputX % factor) / (float)factor;
            if (!smoothTrials)
                return Mathf.Lerp(trials[outputY][x0], trials[outputY][x1], tx);

            int y0 = outputY / factor;
            int y1 = Math.Min(y0 + 1, trials.Length - 1);
            float ty = (outputY % factor) / (float)factor;
            float bottom = Mathf.Lerp(trials[y0][x0], trials[y0][x1], tx);
            float top = Mathf.Lerp(trials[y1][x0], trials[y1][x1], tx);
            return Mathf.Lerp(bottom, top, ty);
        }

        private static Color32 GetColor(float value, Vector2 limits, Color32[] colors)
        {
            float ratio = limits.y == limits.x ? 0.5f : Mathf.Clamp01((value - limits.x) / (limits.y - limits.x));
            int index = Mathf.RoundToInt(ratio * (colors.Length - 1));
            return colors[index];
        }

        private static int CheckedSmoothedLength(int inputLength, int factor)
        {
            long length = (long)(inputLength - 1) * factor + 1;
            if (length > int.MaxValue)
                throw new OverflowException("The smoothed Trial Matrix dimension exceeds Int32 capacity.");
            return (int)length;
        }

        private static void Validate(float[][] trials, Color32[] colors, int maxTextureSize)
        {
            if (trials == null)
                throw new ArgumentNullException(nameof(trials));
            if (colors == null || colors.Length == 0)
                throw new ArgumentException("A non-empty colormap is required.", nameof(colors));
            if (maxTextureSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxTextureSize));
            if (trials.Length == 0)
                return;
            if (trials[0] == null)
                throw new ArgumentException("Trial rows cannot be null.", nameof(trials));
            int width = trials[0].Length;
            for (int i = 1; i < trials.Length; i++)
            {
                if (trials[i] == null || trials[i].Length != width)
                    throw new ArgumentException("All Trial Matrix rows must have the same length.", nameof(trials));
            }
        }
    }
}
