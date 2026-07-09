using System;
using System.Collections.Generic;
using HBP.Core.Enums;
using UnityEngine;

namespace HBP.Core.Tools
{
    public static class UnityTextureFactory
    {
        public const int ColormapSize = 255;
        public const int HistogramBinCount = 50;

        private static readonly Color32 HistogramBackground = new(40, 40, 40, 255);
        private static readonly Color32 HistogramGreyArea = new(90, 90, 90, 255);
        private static readonly Color32 HistogramLine = new(255, 0, 0, 255);
        private const int HistogramLineThickness = 2;
        private static readonly Color32 VideoTextColor = new(220, 220, 220, 255);
        private static readonly Color32 SiteMarkerColor = new(255, 0, 0, 255);

        public static Texture2D Generate1DColorTexture(ColorType colorType)
        {
            Color32[] pixels = Generate1DColorPixels(colorType);
            Texture2D texture = CreateTexture(ColormapSize, 1, FilterMode.Bilinear);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        public static Texture2D Generate2DColorTexture(ColorType horizontalColorType, ColorType verticalColorType)
        {
            Color32[] pixels = Generate2DColorPixels(horizontalColorType, verticalColorType, out int width, out int height);
            Texture2D texture = CreateTexture(width, height, FilterMode.Bilinear);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        public static Color32[] Generate1DColorPixels(ColorType colorType)
        {
            return Generate1DColorPixels(colorType, ColormapSize);
        }

        public static Color32[] Generate2DColorPixels(ColorType horizontalColorType, ColorType verticalColorType)
        {
            return Generate2DColorPixels(horizontalColorType, verticalColorType, out _, out _);
        }

        public static Color32[] Generate2DColorPixels(ColorType horizontalColorType, ColorType verticalColorType, out int width, out int height)
        {
            width = Mathf.Max(HistoricalColorMapWidth(horizontalColorType), HistoricalColorMapWidth(verticalColorType));
            height = ColormapSize;
            Color32[] horizontal = GenerateHistorical1DColorPixels(horizontalColorType);
            Color32[] vertical = GenerateHistorical1DColorPixels(verticalColorType);
            if (horizontal.Length != width)
            {
                horizontal = Resize1DColorPixels(horizontal, width);
            }
            if (vertical.Length != width)
            {
                vertical = Resize1DColorPixels(vertical, width);
            }

            Color32[] pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                float t = (height - 1 - y) / (float)height;
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = LerpRgb(horizontal[x], vertical[x], t);
                }
            }

            return pixels;
        }

        public static Texture2D GenerateDistributionHistogram(float[] data, int height, int width, float min = 0.0f, float max = 0.0f, bool withGreyArea = true)
        {
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));

            Texture2D texture = CreateTexture(width, height, FilterMode.Point);
            Color32[] pixels = GenerateDistributionHistogramPixels(data, height, width, min, max, withGreyArea);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        public static Texture2D GenerateDistributionHistogram(int[] bins, int height, int width, bool withGreyArea = true)
        {
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));

            Texture2D texture = CreateTexture(width, height, FilterMode.Point);
            UpdateDistributionHistogram(texture, bins, height, width, withGreyArea);
            return texture;
        }

        public static void UpdateDistributionHistogram(Texture2D texture, int[] bins, int height, int width, bool withGreyArea = true)
        {
            if (texture == null) throw new ArgumentNullException(nameof(texture));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));

            if (texture.width != width || texture.height != height)
            {
                texture.Reinitialize(width, height);
            }
            texture.SetPixels32(GenerateDistributionHistogramPixels(bins, height, width, withGreyArea));
            texture.Apply(false, false);
        }

        public static Texture2D GenerateSolidTexture(int width, int height, Color32 color)
        {
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));

            Texture2D texture = CreateTexture(width, height, FilterMode.Point);
            UpdateSolidTexture(texture, width, height, color);
            return texture;
        }

        public static void UpdateSolidTexture(Texture2D texture, int width, int height, Color32 color)
        {
            if (texture == null) throw new ArgumentNullException(nameof(texture));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));

            if (texture.width != width || texture.height != height)
            {
                texture.Reinitialize(width, height);
            }

            Color32[] pixels = new Color32[width * height];
            Fill(pixels, color);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        public static Color32[] GenerateDistributionHistogramPixels(float[] data, int height, int width, float min = 0.0f, float max = 0.0f, bool withGreyArea = true)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));

            Color32[] pixels = new Color32[width * height];
            Fill(pixels, HistogramBackground);

            if (data.Length == 0)
            {
                return pixels;
            }

            if (withGreyArea && min < max)
            {
                FillVerticalBand(pixels, width, height, 0.0f, 1.0f, HistogramGreyArea);
            }

            int[] bins = new int[HistogramBinCount];
            ComputeDataRange(data, ref min, ref max);
            float diff = max - min;
            if (diff == 0.0f)
            {
                min -= 1.0f;
                max += 1.0f;
                diff = max - min;
            }

            for (int i = 0; i < data.Length; i++)
            {
                float coeff = Mathf.Abs((data[i] - min) / diff);
                int bin = Mathf.Clamp((int)(coeff * (HistogramBinCount - 1)), 0, HistogramBinCount - 1);
                bins[bin]++;
            }

            DrawHistogramBins(pixels, width, height, bins);
            return pixels;
        }

        public static Color32[] GenerateDistributionHistogramPixels(int[] bins, int height, int width, bool withGreyArea = true)
        {
            if (bins == null) throw new ArgumentNullException(nameof(bins));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));

            Color32[] pixels = new Color32[width * height];
            Fill(pixels, HistogramBackground);

            if (withGreyArea)
            {
                FillVerticalBand(pixels, width, height, 0.0f, 1.0f, HistogramGreyArea);
            }

            DrawHistogramBins(pixels, width, height, bins);
            return pixels;
        }

        private static void DrawHistogramBins(Color32[] pixels, int width, int height, int[] bins)
        {
            int maxBin = 0;
            for (int i = 0; i < bins.Length; i++)
            {
                if (bins[i] > maxBin) maxBin = bins[i];
            }
            if (maxBin == 0)
            {
                return;
            }

            int previousX = 0;
            int previousY = HistogramY(bins[0], maxBin, height);
            for (int i = 1; i < bins.Length; i++)
            {
                int x = Mathf.RoundToInt(i * (width - 1) / (float)(bins.Length - 1));
                int y = HistogramY(bins[i], maxBin, height);
                DrawLine(pixels, width, height, previousX, previousY, x, y, HistogramLine, HistogramLineThickness);
                previousX = x;
                previousY = y;
            }
        }

        public static void ResizeToSquare(Texture2D texture, int size)
        {
            if (texture == null) throw new ArgumentNullException(nameof(texture));
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));

            Color32[] source = texture.GetPixels32();
            Color32[] target = new Color32[size * size];
            Fill(target, new Color32(0, 0, 0, 255));

            int offsetX = (size - texture.width) / 2;
            int offsetY = (size - texture.height) / 2;
            int copyWidth = Mathf.Min(texture.width, size);
            int copyHeight = Mathf.Min(texture.height, size);
            for (int y = 0; y < copyHeight; y++)
            {
                int targetY = y + offsetY;
                if (targetY < 0 || targetY >= size) continue;
                for (int x = 0; x < copyWidth; x++)
                {
                    int targetX = x + offsetX;
                    if (targetX < 0 || targetX >= size) continue;
                    target[targetY * size + targetX] = source[y * texture.width + x];
                }
            }

            texture.Reinitialize(size, size);
            texture.SetPixels32(target);
            texture.Apply(false, false);
        }

        public static void CopyAndRotateCutTexture(Texture2D source, Texture2D target, CutOrientation orientation, bool flip)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            Color32[] sourcePixels = source.GetPixels32();
            Color32[] targetPixels = RotateCutPixels(
                sourcePixels,
                source.width,
                source.height,
                orientation,
                flip,
                out int targetWidth,
                out int targetHeight);

            if (target.width != targetWidth || target.height != targetHeight)
            {
                target.Reinitialize(targetWidth, targetHeight);
            }
            target.filterMode = FilterMode.Point;
            target.wrapMode = TextureWrapMode.Clamp;
            target.mipMapBias = -10.0f;
            target.anisoLevel = 1;
            target.SetPixels32(targetPixels);
            target.Apply(false, false);
        }

        public static Color32[] RotateCutPixels(Color32[] source, int sourceWidth, int sourceHeight, CutOrientation orientation, bool flip, out int targetWidth, out int targetHeight)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (sourceWidth <= 0) throw new ArgumentOutOfRangeException(nameof(sourceWidth));
            if (sourceHeight <= 0) throw new ArgumentOutOfRangeException(nameof(sourceHeight));
            if (source.Length < sourceWidth * sourceHeight) throw new ArgumentException("Pixel buffer is smaller than width * height.", nameof(source));

            TextureOrientation textureOrientation = ConvertOrientation(orientation, flip);
            bool transpose = textureOrientation is TextureOrientation.PosteriorToAnterior or TextureOrientation.AnteriorToPosterior or TextureOrientation.InferiorToSuperior or TextureOrientation.SuperiorToInferior;
            targetWidth = transpose ? sourceHeight : sourceWidth;
            targetHeight = transpose ? sourceWidth : sourceHeight;
            Color32[] target = new Color32[targetWidth * targetHeight];

            for (int targetRow = 0; targetRow < targetHeight; targetRow++)
            {
                for (int targetColumn = 0; targetColumn < targetWidth; targetColumn++)
                {
                    SourceCoordinates(textureOrientation, sourceWidth, sourceHeight, targetRow, targetColumn, out int sourceRow, out int sourceColumn);
                    target[UnityIndex(targetWidth, targetHeight, targetRow, targetColumn)] = source[UnityIndex(sourceWidth, sourceHeight, sourceRow, sourceColumn)];
                }
            }

            return target;
        }

        public static void DrawCenteredText(Texture2D texture, string text, int centerX, int centerYFromTop, int scale = 2)
        {
            if (texture == null) throw new ArgumentNullException(nameof(texture));
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale));

            Color32[] pixels = texture.GetPixels32();
            DrawCenteredText(pixels, texture.width, texture.height, text, centerX, centerYFromTop, scale);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        public static void DrawSiteMarkers(Texture2D texture, IEnumerable<Vector2> normalizedPositions, int radius = 0)
        {
            if (texture == null) throw new ArgumentNullException(nameof(texture));
            if (normalizedPositions == null) throw new ArgumentNullException(nameof(normalizedPositions));
            if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));

            Color32[] pixels = texture.GetPixels32();
            DrawSiteMarkers(pixels, texture.width, texture.height, normalizedPositions, radius);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        public static void DrawSiteMarkers(Color32[] pixels, int width, int height, IEnumerable<Vector2> normalizedPositions, int radius = 0)
        {
            if (pixels == null) throw new ArgumentNullException(nameof(pixels));
            if (normalizedPositions == null) throw new ArgumentNullException(nameof(normalizedPositions));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (pixels.Length < width * height) throw new ArgumentException("Pixel buffer is smaller than width * height.", nameof(pixels));
            if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));

            foreach (Vector2 position in normalizedPositions)
            {
                if (float.IsNaN(position.x) || float.IsInfinity(position.x) || float.IsNaN(position.y) || float.IsInfinity(position.y))
                {
                    continue;
                }

                int centerX = Mathf.Clamp(Mathf.RoundToInt(position.x * (width - 1)), 0, width - 1);
                int centerY = Mathf.Clamp(Mathf.RoundToInt(position.y * (height - 1)), 0, height - 1);
                FillCircle(pixels, width, height, centerX, centerY, radius, SiteMarkerColor);
            }
        }

        public static void DrawCenteredText(Color32[] pixels, int width, int height, string text, int centerX, int centerYFromTop, int scale = 2)
        {
            if (pixels == null) throw new ArgumentNullException(nameof(pixels));
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (pixels.Length < width * height) throw new ArgumentException("Pixel buffer is smaller than width * height.", nameof(pixels));
            if (scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale));

            string normalizedText = NormalizeVideoText(text);
            int textWidth = MeasureTextWidth(normalizedText, scale);
            int textHeight = 7 * scale;
            int cursorX = centerX - textWidth / 2;
            int startY = height - centerYFromTop - textHeight / 2;

            for (int i = 0; i < normalizedText.Length; i++)
            {
                char character = normalizedText[i];
                DrawCharacter(pixels, width, height, character, cursorX, startY, scale);
                cursorX += (CharacterWidth(character) + 1) * scale;
            }
        }

        private static Texture2D CreateTexture(int width, int height, FilterMode filterMode)
        {
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = filterMode,
                wrapMode = TextureWrapMode.Clamp,
                mipMapBias = -10.0f,
                anisoLevel = 1
            };
            return texture;
        }

        private static Color32[] Generate1DColorPixels(ColorType colorType, int width)
        {
            GetColorStops(colorType, out Color32[] colors, out float[] factors);
            Color32[] pixels = new Color32[width];

            if (factors.Length == 0)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[x] = LerpRgb(colors[0], colors[1], x / (float)width);
                }
                return pixels;
            }

            for (int x = 0; x < width; x++)
            {
                float position = x / (float)width;
                int segment = 0;
                while (segment < factors.Length && position > factors[segment])
                {
                    segment++;
                }

                float leftFactor = segment == 0 ? 0.0f : factors[segment - 1];
                float rightFactor = segment == factors.Length ? 1.0f : factors[segment];
                float local = rightFactor > leftFactor ? (position - leftFactor) / (rightFactor - leftFactor) : 0.0f;
                pixels[x] = LerpRgb(colors[segment], colors[segment + 1], local);
            }

            return pixels;
        }

        private static int HistoricalColorMapWidth(ColorType colorType)
        {
            GetColorStops(colorType, out _, out float[] factors);
            if (factors.Length == 0)
            {
                return ColormapSize;
            }

            float min = 1.1f;
            for (int i = 0; i <= factors.Length; i++)
            {
                float size;
                if (i == 0)
                {
                    size = factors[i];
                }
                else if (i == factors.Length)
                {
                    size = 1.0f - factors[i - 1];
                }
                else
                {
                    size = factors[i] - factors[i - 1];
                }

                if (size < min)
                {
                    min = size;
                }
            }

            int totalColumns = 0;
            for (int i = 0; i < factors.Length; i++)
            {
                totalColumns += (int)(ColormapSize * (factors[i] / min));
            }
            return totalColumns;
        }

        private static Color32[] GenerateHistorical1DColorPixels(ColorType colorType)
        {
            return Generate1DColorPixels(colorType, HistoricalColorMapWidth(colorType));
        }

        private static Color32[] Resize1DColorPixels(Color32[] source, int targetWidth)
        {
            if (source.Length == targetWidth)
            {
                return source;
            }

            Color32[] target = new Color32[targetWidth];
            float scale = source.Length / (float)targetWidth;
            for (int x = 0; x < targetWidth; x++)
            {
                float sourcePosition = (x + 0.5f) * scale - 0.5f;
                int leftIndex = Mathf.FloorToInt(sourcePosition);
                float t = sourcePosition - leftIndex;
                if (leftIndex < 0)
                {
                    leftIndex = 0;
                    t = 0.0f;
                }

                int rightIndex = Mathf.Min(leftIndex + 1, source.Length - 1);
                target[x] = LerpRgbRounded(source[leftIndex], source[rightIndex], t);
            }
            return target;
        }

        private static void GetColorStops(ColorType colorType, out Color32[] colors, out float[] factors)
        {
            Color32 red = Rgb(255, 0, 0);
            Color32 blue = Rgb(0, 0, 255);
            Color32 darkBlue = Rgb(0, 0, 133);
            Color32 coolBlue = Rgb(0, 127, 255);
            Color32 coolLightBlue = Rgb(0, 255, 255);
            Color32 green = Rgb(0, 255, 0);
            Color32 darkGreen = Rgb(19, 139, 29);
            Color32 white = Rgb(255, 255, 255);
            Color32 black = Rgb(0, 0, 0);
            Color32 yellow = Rgb(255, 255, 0);
            Color32 orange = Rgb(255, 165, 0);
            Color32 pink = Rgb(200, 107, 107);
            Color32 lightGreen = Rgb(0, 255, 128);
            Color32 lightGray = Rgb(150, 150, 150);
            Color32 darkGray = Rgb(100, 100, 100);
            Color32 brainColor = Rgb(235, 181, 120);

            switch (colorType)
            {
                case ColorType.Hot:
                    colors = new[] { black, red, orange, yellow, white };
                    factors = new[] { 0.10f, 0.20f, 0.50f };
                    break;
                case ColorType.Winter:
                    colors = new[] { blue, lightGreen };
                    factors = Array.Empty<float>();
                    break;
                case ColorType.Warm:
                    colors = new[] { orange, yellow };
                    factors = Array.Empty<float>();
                    break;
                case ColorType.Surface:
                    colors = new[] { black, pink, white };
                    factors = new[] { 0.50f };
                    break;
                case ColorType.Cool:
                    colors = new[] { coolBlue, coolLightBlue };
                    factors = Array.Empty<float>();
                    break;
                case ColorType.RedYellow:
                    colors = new[] { red, yellow };
                    factors = Array.Empty<float>();
                    break;
                case ColorType.BlueGreen:
                    colors = new[] { blue, green };
                    factors = Array.Empty<float>();
                    break;
                case ColorType.ACTC:
                    colors = new[] { black, darkBlue, darkGreen, yellow, orange, red };
                    factors = new[] { 0.25f, 0.50f, 0.66f, 0.84f };
                    break;
                case ColorType.Bone:
                    colors = new[] { black, Rgb(86, 105, 138), white };
                    factors = new[] { 0.50f };
                    break;
                case ColorType.GEColor:
                    colors = new[] { black, Rgb(0, 98, 96), Rgb(84, 44, 210), Rgb(175, 48, 160), orange, white };
                    factors = new[] { 0.20f, 0.40f, 0.60f, 0.80f };
                    break;
                case ColorType.Gold:
                    colors = new[] { black, Rgb(121, 72, 12), Rgb(185, 127, 45), Rgb(227, 170, 77), white };
                    factors = new[] { 0.25f, 0.50f, 0.75f };
                    break;
                case ColorType.XRain:
                    colors = new[] { black, blue, Rgb(36, 255, 0), yellow, orange, red };
                    factors = new[] { 0.25f, 0.50f, 0.66f, 0.84f };
                    break;
                case ColorType.MatLab:
                    colors = new[] { blue, coolBlue, green, yellow, red };
                    factors = new[] { 0.40f, 0.50f, 0.60f };
                    break;
                case ColorType.Default:
                    colors = new[] { black, Rgb(112, 95, 95), Rgb(239, 184, 122), white };
                    factors = new[] { 0.05f, 0.50f };
                    break;
                case ColorType.BrainColor:
                    colors = new[] { brainColor, brainColor };
                    factors = Array.Empty<float>();
                    break;
                case ColorType.White:
                    colors = new[] { white, white };
                    factors = Array.Empty<float>();
                    break;
                case ColorType.SoftGrayscale:
                    colors = new[] { lightGray, darkGray };
                    factors = Array.Empty<float>();
                    break;
                case ColorType.Grayscale:
                default:
                    colors = new[] { black, white };
                    factors = Array.Empty<float>();
                    break;
            }
        }

        private static Color32 Rgb(byte r, byte g, byte b)
        {
            return new Color32(r, g, b, 255);
        }

        private static Color32 LerpRgb(Color32 left, Color32 right, float t)
        {
            if (left.r == right.r && left.g == right.g && left.b == right.b && left.a == right.a)
            {
                return left;
            }

            float inv = 1.0f - t;
            return new Color32(
                (byte)(left.r * inv + right.r * t),
                (byte)(left.g * inv + right.g * t),
                (byte)(left.b * inv + right.b * t),
                255);
        }

        private static Color32 LerpRgbRounded(Color32 left, Color32 right, float t)
        {
            if (left.r == right.r && left.g == right.g && left.b == right.b && left.a == right.a)
            {
                return left;
            }

            float inv = 1.0f - t;
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(left.r * inv + right.r * t), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(left.g * inv + right.g * t), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(left.b * inv + right.b * t), 0, 255),
                255);
        }

        private static void ComputeDataRange(float[] data, ref float min, ref float max)
        {
            if (min != 0.0f || max != 0.0f)
            {
                return;
            }

            min = float.MaxValue;
            max = float.MinValue;
            for (int i = 0; i < data.Length; i++)
            {
                float value = data[i];
                if (value < min) min = value;
                if (value > max) max = value;
            }
        }

        private static int HistogramY(int value, int maxValue, int height)
        {
            float normalized = Mathf.Sqrt(value) / Mathf.Sqrt(maxValue);
            return Mathf.Clamp(Mathf.RoundToInt(normalized * (height - 1)), 0, height - 1);
        }

        private static TextureOrientation ConvertOrientation(CutOrientation orientation, bool flip)
        {
            TextureOrientation textureOrientation = orientation switch
            {
                CutOrientation.Axial => TextureOrientation.InferiorToSuperior,
                CutOrientation.Coronal => TextureOrientation.AnteriorToPosterior,
                CutOrientation.Sagittal => TextureOrientation.LeftToRight,
                _ => TextureOrientation.Custom
            };

            if (!flip)
            {
                return textureOrientation;
            }

            return textureOrientation switch
            {
                TextureOrientation.LeftToRight => TextureOrientation.RightToLeft,
                TextureOrientation.RightToLeft => TextureOrientation.LeftToRight,
                TextureOrientation.PosteriorToAnterior => TextureOrientation.AnteriorToPosterior,
                TextureOrientation.AnteriorToPosterior => TextureOrientation.PosteriorToAnterior,
                TextureOrientation.InferiorToSuperior => TextureOrientation.SuperiorToInferior,
                TextureOrientation.SuperiorToInferior => TextureOrientation.InferiorToSuperior,
                _ => textureOrientation
            };
        }

        private static void SourceCoordinates(TextureOrientation orientation, int sourceWidth, int sourceHeight, int targetRow, int targetColumn, out int sourceRow, out int sourceColumn)
        {
            switch (orientation)
            {
                case TextureOrientation.RightToLeft:
                    sourceRow = sourceHeight - 1 - targetRow;
                    sourceColumn = sourceWidth - 1 - targetColumn;
                    break;
                case TextureOrientation.PosteriorToAnterior:
                    sourceRow = sourceHeight - 1 - targetColumn;
                    sourceColumn = targetRow;
                    break;
                case TextureOrientation.AnteriorToPosterior:
                case TextureOrientation.InferiorToSuperior:
                    sourceRow = targetColumn;
                    sourceColumn = sourceWidth - 1 - targetRow;
                    break;
                case TextureOrientation.SuperiorToInferior:
                    sourceRow = sourceHeight - 1 - targetColumn;
                    sourceColumn = sourceWidth - 1 - targetRow;
                    break;
                case TextureOrientation.LeftToRight:
                case TextureOrientation.Custom:
                default:
                    sourceRow = targetRow;
                    sourceColumn = targetColumn;
                    break;
            }
        }

        private static int UnityIndex(int width, int height, int rowFromTop, int column)
        {
            return (height - 1 - rowFromTop) * width + column;
        }

        private static int MeasureTextWidth(string text, int scale)
        {
            if (text.Length == 0)
            {
                return 0;
            }

            int width = -1;
            for (int i = 0; i < text.Length; i++)
            {
                width += CharacterWidth(text[i]) + 1;
            }
            return width * scale;
        }

        private static int CharacterWidth(char character)
        {
            return character switch
            {
                ' ' => 3,
                '.' or ',' or '\'' or '\u00B0' => 2,
                _ => 5
            };
        }

        private static void DrawCharacter(Color32[] pixels, int width, int height, char character, int startX, int startY, int scale)
        {
            int[] rows = GlyphRows(character);
            int characterWidth = CharacterWidth(character);
            for (int row = 0; row < rows.Length; row++)
            {
                int bits = rows[row];
                for (int column = 0; column < characterWidth; column++)
                {
                    if ((bits & (1 << (characterWidth - 1 - column))) == 0)
                    {
                        continue;
                    }

                    FillRectangle(
                        pixels,
                        width,
                        height,
                        startX + column * scale,
                        startY + (rows.Length - 1 - row) * scale,
                        scale,
                        scale,
                        VideoTextColor);
                }
            }
        }

        private static string NormalizeVideoText(string text)
        {
            return text
                .Replace('\u00A0', ' ')
                .Replace('\u202F', ' ')
                .Replace('\u2212', '-')
                .Replace('\u00BA', '\u00B0');
        }

        private static void FillRectangle(Color32[] pixels, int width, int height, int startX, int startY, int rectWidth, int rectHeight, Color32 color)
        {
            for (int y = 0; y < rectHeight; y++)
            {
                int py = startY + y;
                if (py < 0 || py >= height) continue;
                for (int x = 0; x < rectWidth; x++)
                {
                    int px = startX + x;
                    if (px < 0 || px >= width) continue;
                    pixels[py * width + px] = color;
                }
            }
        }

        private static void FillCircle(Color32[] pixels, int width, int height, int centerX, int centerY, int radius, Color32 color)
        {
            int radiusSquared = radius * radius;
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                if (y < 0 || y >= height) continue;
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    if (x < 0 || x >= width) continue;
                    int dx = x - centerX;
                    int dy = y - centerY;
                    if (dx * dx + dy * dy <= radiusSquared)
                    {
                        pixels[y * width + x] = color;
                    }
                }
            }
        }

        private static int[] GlyphRows(char character)
        {
            return character switch
            {
                'A' => new[] { 0x0E, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 },
                'B' => new[] { 0x1E, 0x11, 0x11, 0x1E, 0x11, 0x11, 0x1E },
                'C' => new[] { 0x0F, 0x10, 0x10, 0x10, 0x10, 0x10, 0x0F },
                'D' => new[] { 0x1E, 0x11, 0x11, 0x11, 0x11, 0x11, 0x1E },
                'E' => new[] { 0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x1F },
                'F' => new[] { 0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x10 },
                'G' => new[] { 0x0F, 0x10, 0x10, 0x13, 0x11, 0x11, 0x0E },
                'H' => new[] { 0x11, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 },
                'I' => new[] { 0x1F, 0x04, 0x04, 0x04, 0x04, 0x04, 0x1F },
                'J' => new[] { 0x07, 0x02, 0x02, 0x02, 0x12, 0x12, 0x0C },
                'K' => new[] { 0x11, 0x12, 0x14, 0x18, 0x14, 0x12, 0x11 },
                'L' => new[] { 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x1F },
                'M' => new[] { 0x11, 0x1B, 0x15, 0x15, 0x11, 0x11, 0x11 },
                'N' => new[] { 0x11, 0x19, 0x15, 0x13, 0x11, 0x11, 0x11 },
                'O' => new[] { 0x0E, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E },
                'P' => new[] { 0x1E, 0x11, 0x11, 0x1E, 0x10, 0x10, 0x10 },
                'Q' => new[] { 0x0E, 0x11, 0x11, 0x11, 0x15, 0x12, 0x0D },
                'R' => new[] { 0x1E, 0x11, 0x11, 0x1E, 0x14, 0x12, 0x11 },
                'S' => new[] { 0x0F, 0x10, 0x10, 0x0E, 0x01, 0x01, 0x1E },
                'T' => new[] { 0x1F, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04 },
                'U' => new[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E },
                'V' => new[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x0A, 0x04 },
                'W' => new[] { 0x11, 0x11, 0x11, 0x15, 0x15, 0x15, 0x0A },
                'X' => new[] { 0x11, 0x11, 0x0A, 0x04, 0x0A, 0x11, 0x11 },
                'Y' => new[] { 0x11, 0x11, 0x0A, 0x04, 0x04, 0x04, 0x04 },
                'Z' => new[] { 0x1F, 0x01, 0x02, 0x04, 0x08, 0x10, 0x1F },
                'a' => new[] { 0x00, 0x00, 0x0E, 0x01, 0x0F, 0x11, 0x0F },
                'b' => new[] { 0x10, 0x10, 0x1E, 0x11, 0x11, 0x11, 0x1E },
                'c' => new[] { 0x00, 0x00, 0x0F, 0x10, 0x10, 0x10, 0x0F },
                'd' => new[] { 0x01, 0x01, 0x0F, 0x11, 0x11, 0x11, 0x0F },
                'e' => new[] { 0x00, 0x00, 0x0E, 0x11, 0x1F, 0x10, 0x0E },
                'f' => new[] { 0x03, 0x04, 0x04, 0x0E, 0x04, 0x04, 0x04 },
                'g' => new[] { 0x00, 0x0F, 0x11, 0x11, 0x0F, 0x01, 0x0E },
                'h' => new[] { 0x10, 0x10, 0x1E, 0x11, 0x11, 0x11, 0x11 },
                'i' => new[] { 0x04, 0x00, 0x0C, 0x04, 0x04, 0x04, 0x0E },
                'j' => new[] { 0x02, 0x00, 0x06, 0x02, 0x02, 0x12, 0x0C },
                'k' => new[] { 0x10, 0x10, 0x12, 0x14, 0x18, 0x14, 0x12 },
                'l' => new[] { 0x0C, 0x04, 0x04, 0x04, 0x04, 0x04, 0x0E },
                'm' => new[] { 0x00, 0x00, 0x1A, 0x15, 0x15, 0x15, 0x15 },
                'n' => new[] { 0x00, 0x00, 0x1E, 0x11, 0x11, 0x11, 0x11 },
                'o' => new[] { 0x00, 0x00, 0x0E, 0x11, 0x11, 0x11, 0x0E },
                'p' => new[] { 0x00, 0x1E, 0x11, 0x11, 0x1E, 0x10, 0x10 },
                'q' => new[] { 0x00, 0x0F, 0x11, 0x11, 0x0F, 0x01, 0x01 },
                'r' => new[] { 0x00, 0x00, 0x17, 0x18, 0x10, 0x10, 0x10 },
                's' => new[] { 0x00, 0x00, 0x0F, 0x10, 0x0E, 0x01, 0x1E },
                't' => new[] { 0x04, 0x04, 0x0E, 0x04, 0x04, 0x04, 0x03 },
                'u' => new[] { 0x00, 0x00, 0x11, 0x11, 0x11, 0x13, 0x0D },
                'v' => new[] { 0x00, 0x00, 0x11, 0x11, 0x11, 0x0A, 0x04 },
                'w' => new[] { 0x00, 0x00, 0x11, 0x11, 0x15, 0x15, 0x0A },
                'x' => new[] { 0x00, 0x00, 0x11, 0x0A, 0x04, 0x0A, 0x11 },
                'y' => new[] { 0x00, 0x11, 0x11, 0x11, 0x0F, 0x01, 0x0E },
                'z' => new[] { 0x00, 0x00, 0x1F, 0x02, 0x04, 0x08, 0x1F },
                '0' => new[] { 0x0E, 0x11, 0x13, 0x15, 0x19, 0x11, 0x0E },
                '1' => new[] { 0x04, 0x0C, 0x04, 0x04, 0x04, 0x04, 0x0E },
                '2' => new[] { 0x0E, 0x11, 0x01, 0x02, 0x04, 0x08, 0x1F },
                '3' => new[] { 0x1E, 0x01, 0x01, 0x0E, 0x01, 0x01, 0x1E },
                '4' => new[] { 0x02, 0x06, 0x0A, 0x12, 0x1F, 0x02, 0x02 },
                '5' => new[] { 0x1F, 0x10, 0x10, 0x1E, 0x01, 0x01, 0x1E },
                '6' => new[] { 0x0E, 0x10, 0x10, 0x1E, 0x11, 0x11, 0x0E },
                '7' => new[] { 0x1F, 0x01, 0x02, 0x04, 0x08, 0x08, 0x08 },
                '8' => new[] { 0x0E, 0x11, 0x11, 0x0E, 0x11, 0x11, 0x0E },
                '9' => new[] { 0x0E, 0x11, 0x11, 0x0F, 0x01, 0x01, 0x0E },
                '-' => new[] { 0x00, 0x00, 0x00, 0x1F, 0x00, 0x00, 0x00 },
                '_' => new[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1F },
                '.' => new[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x03 },
                ',' => new[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x02 },
                ':' => new[] { 0x00, 0x0C, 0x0C, 0x00, 0x0C, 0x0C, 0x00 },
                '\'' => new[] { 0x03, 0x02, 0x02, 0x00, 0x00, 0x00, 0x00 },
                '\u00B0' => new[] { 0x03, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00 },
                '/' => new[] { 0x01, 0x02, 0x02, 0x04, 0x08, 0x08, 0x10 },
                '\\' => new[] { 0x10, 0x08, 0x08, 0x04, 0x02, 0x02, 0x01 },
                '(' => new[] { 0x02, 0x04, 0x08, 0x08, 0x08, 0x04, 0x02 },
                ')' => new[] { 0x08, 0x04, 0x02, 0x02, 0x02, 0x04, 0x08 },
                ' ' => new[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                _ => new[] { 0x1F, 0x01, 0x02, 0x04, 0x00, 0x04, 0x04 }
            };
        }

        private static void Fill(Color32[] pixels, Color32 color)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
        }

        private static void FillVerticalBand(Color32[] pixels, int width, int height, float start, float end, Color32 color)
        {
            int x0 = Mathf.Clamp((int)(start * width), 0, width);
            int x1 = Mathf.Clamp((int)(end * width), 0, width);
            for (int y = 0; y < height; y++)
            {
                for (int x = x0; x < x1; x++)
                {
                    pixels[y * width + x] = color;
                }
            }
        }

        private static void DrawLine(Color32[] pixels, int width, int height, int x0, int y0, int x1, int y1, Color32 color, int thickness = 1)
        {
            int dx = Mathf.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int error = dx + dy;

            while (true)
            {
                FillCircle(pixels, width, height, x0, y0, thickness / 2, color);

                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * error;
                if (e2 >= dy)
                {
                    error += dy;
                    x0 += sx;
                }
                if (e2 <= dx)
                {
                    error += dx;
                    y0 += sy;
                }
            }
        }

        private enum TextureOrientation
        {
            LeftToRight,
            RightToLeft,
            PosteriorToAnterior,
            AnteriorToPosterior,
            InferiorToSuperior,
            SuperiorToInferior,
            Custom
        }
    }
}
