using System;
using System.Collections.Generic;
using UnityEngine;
using HBP.Core.Enums;
using HBP.Core.Tools;
using HBP.Core.Preferences;

namespace HBP.Data.Module3D
{
    /// <summary>
    /// Contains the textures for the cuts and methods to compute them
    /// </summary>
    public class CutTexturesUtility
    {
        #region Properties
        /// <summary>
        /// Column linked to this CutTexturesUtility
        /// </summary>
        public Column3D Column { get; set; }
        /// <summary>
        /// Generator for the MRI textures of the cuts
        /// </summary>
        public List<Core.DLL.CutGenerator> CutGenerators = new();
        /// <summary>
        /// Unity textures for the anatomical cuts.
        /// </summary>
        public List<Texture2D> BaseBrainCutTextures = new();
        /// <summary>
        /// Unity textures for the cuts displayed in 3D, potentially with activity or atlas coloring.
        /// </summary>
        public List<Texture2D> BrainCutTextures = new();
        /// <summary>
        /// Unity textures for the cuts for the GUI
        /// </summary>
        public List<Texture2D> GUIBrainCutTextures = new();
        private Color32[] m_BrainCutColorSchemePixels = Array.Empty<Color32>();
        private Color32[] m_CutActivityColorSchemePixels = Array.Empty<Color32>();
        /// <summary>
        /// Size of the cuts arrays
        /// </summary>
        public int Size { get; private set; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Resize every lists
        /// </summary>
        /// <param name="size">New size for the lists</param>
        public void Resize(int size, List<Core.DLL.CutGeometryGenerator> cutGeometryGenerators, Core.DLL.ActivityGenerator activityGenerator)
        {
            while (Size < size)
            {
                BaseBrainCutTextures.Add(Texture2DExtension.Generate());
                BrainCutTextures.Add(Texture2DExtension.Generate());
                GUIBrainCutTextures.Add(Texture2DExtension.Generate(1, 1, -10, 9, FilterMode.Point));
                CutGenerators.Add(new Core.DLL.CutGenerator());
                Size++;
            }
            while (Size > size)
            {
                UnityEngine.Object.Destroy(BaseBrainCutTextures[BaseBrainCutTextures.Count - 1]);
                BaseBrainCutTextures.RemoveAt(BaseBrainCutTextures.Count - 1);
                UnityEngine.Object.Destroy(BrainCutTextures[BrainCutTextures.Count - 1]);
                BrainCutTextures.RemoveAt(BrainCutTextures.Count - 1);
                UnityEngine.Object.Destroy(GUIBrainCutTextures[GUIBrainCutTextures.Count - 1]);
                GUIBrainCutTextures.RemoveAt(GUIBrainCutTextures.Count - 1);
                CutGenerators[CutGenerators.Count - 1].Dispose();
                CutGenerators.RemoveAt(CutGenerators.Count - 1);
                Size--;
            }
            for (int i = 0; i < size; i++)
            {
                CutGenerators[i].Initialize(activityGenerator, cutGeometryGenerators[i], PersistentDataManager.UserPreferences.Visualization._3D.RawCuts ? 0 : 4);
            }
        }
        /// <summary>
        /// Create a MRI texture with parameters
        /// </summary>
        /// <param name="volume">MRI volume</param>
        /// <param name="indexCut">Index of the cut</param>
        /// <param name="MRICalMinFactor">Cal Min Factor</param>
        /// <param name="MRICalMaxFactor">Cal Max Factor</param>
        public void CreateMRITexture(Core.DLL.Volume volume, int indexCut, float MRICalMinFactor, float MRICalMaxFactor)
        {
            Core.DLL.CutGenerator cutGenerator = CutGenerators[indexCut];
            UnityEngine.Profiling.Profiler.BeginSample("FillTextureWithVolume");
            cutGenerator.FillTextureWithVolume(m_BrainCutColorSchemePixels, MRICalMinFactor, MRICalMaxFactor);
            UnityEngine.Profiling.Profiler.EndSample();
            ApplyPixels(BaseBrainCutTextures[indexCut], cutGenerator.CopyBasePixels(), cutGenerator.CutGeometryGenerator.TextureSize);
            CopyTexture(BaseBrainCutTextures[indexCut], BrainCutTextures[indexCut]);
        }
        /// <summary>
        /// Create MRI textures for the GUI
        /// </summary>
        /// <param name="cuts">Cuts of these textures</param>
        public void CreateGUIMRITextures(List<Core.Object3D.Cut> cuts)
        {
            foreach (Core.Object3D.Cut cut in cuts)
            {
                if (cut.ID >= 0 && cut.ID < BrainCutTextures.Count && cut.ID < GUIBrainCutTextures.Count && BrainCutTextures[cut.ID].width > 1 && BrainCutTextures[cut.ID].height > 1)
                {
                    UnityTextureFactory.CopyAndRotateCutTexture(BrainCutTextures[cut.ID], GUIBrainCutTextures[cut.ID], cut.Orientation, cut.Flip);
                }
            }
        }
        /// <summary>
        /// Resize the MRI textures for the GUI to squares
        /// </summary>
        /// <param name="cuts">Cuts of these textures</param>
        public void ResizeGUIMRITextures(List<Core.Object3D.Cut> cuts)
        {
            int max = 0;
            foreach (var cut in cuts)
            {
                if (cut.Orientation != CutOrientation.Custom)
                {
                    int textureMax = Mathf.Max(GUIBrainCutTextures[cut.ID].width, GUIBrainCutTextures[cut.ID].height);
                    if (textureMax > max)
                    {
                        max = textureMax;
                    }
                }
            }
            if (max <= 0)
            {
                return;
            }
            for (int i = 0; i < GUIBrainCutTextures.Count; ++i)
            {
                UnityTextureFactory.ResizeToSquare(GUIBrainCutTextures[i], max);
            }
        }
        /// <summary>
        /// Draw sites on the Unity-owned MRI cut textures.
        /// </summary>
        /// <param name="cuts">Cuts corresponding to the textures.</param>
        /// <param name="siteInfos">Sites to project on the cut textures.</param>
        /// <param name="precision">Maximum distance from a site to a cut plane.</param>
        public void DrawSitesOnMRITextures(List<Core.Object3D.Cut> cuts, IEnumerable<Core.Object3D.Implantation3D.SiteInfo> siteInfos, float precision = 1.0f)
        {
            if (cuts == null || siteInfos == null || precision < 0.0f)
            {
                return;
            }

            float precisionSquared = precision * precision;
            List<Vector2> projectedSites = new();
            foreach (Core.Object3D.Cut cut in cuts)
            {
                if (cut.ID < 0 || cut.ID >= BaseBrainCutTextures.Count || cut.ID >= CutGenerators.Count)
                {
                    continue;
                }

                Texture2D texture = BaseBrainCutTextures[cut.ID];
                Core.DLL.CutGeometryGenerator geometryGenerator = CutGenerators[cut.ID].CutGeometryGenerator;
                if (texture == null || texture.width <= 0 || texture.height <= 0 || geometryGenerator == null)
                {
                    continue;
                }

                projectedSites.Clear();
                foreach (Core.Object3D.Implantation3D.SiteInfo siteInfo in siteInfos)
                {
                    if (siteInfo == null || !IsSiteOnCut(siteInfo.NativePosition, cut, precisionSquared))
                    {
                        continue;
                    }

                    Vector2 ratio = geometryGenerator.GetPositionRatioOnTexture(siteInfo.UnityPosition);
                    if (ratio.x >= 0.0f && ratio.x <= 1.0f && ratio.y >= 0.0f && ratio.y <= 1.0f)
                    {
                        projectedSites.Add(ratio);
                    }
                }

                if (projectedSites.Count > 0)
                {
                    UnityTextureFactory.DrawSiteMarkers(texture, projectedSites);
                }
            }
        }
        /// <summary>
        /// Color cuts with iEEG values
        /// </summary>
        /// <param name="column">Column from which iEEG values are taken</param>
        public void ColorCutsTexturesWithActivity()
        {
            int timelineIndex = 0;
            if (Column is Column3DDynamic dynamicColumn)
            {
                timelineIndex = dynamicColumn.Timeline.CurrentIndex;
            }
            else if (Column is Column3DFMRI fmriColumn)
            {
                timelineIndex = fmriColumn.SelectedVolumeIndex;
            }
            else if (Column is Column3DMEG megColumn)
            {
                timelineIndex = megColumn.SelectedVolumeIndex;
            }
            else if (Column is Column3DStatic staticColumn)
            {
                timelineIndex = staticColumn.SelectedLabelIndex;
            }

            for (int i = 0; i < CutGenerators.Count; ++i)
            {
                Core.DLL.CutGenerator generator = CutGenerators[i];
                generator.FillTextureWithActivity(m_CutActivityColorSchemePixels, timelineIndex, Column.ActivityAlpha);
                ApplyPixels(BrainCutTextures[i], generator.CopyOverlayPixels(), generator.CutGeometryGenerator.TextureSize);
            }
        }
        public void ColorCutsTexturesWithBrainAtlas(Core.DLL.BrainAtlas selectedAtlas, float alpha, int selectedArea)
        {
            for (int i = 0; i < CutGenerators.Count; i++)
            {
                Core.DLL.CutGenerator generator = CutGenerators[i];
                generator.FillTextureWithAtlas(selectedAtlas, alpha, selectedArea);
                ApplyPixels(BrainCutTextures[i], generator.CopyOverlayPixels(), generator.CutGeometryGenerator.TextureSize);
            }
        }
        public void ColorCutsTexturesWithFMRIAtlas(Core.DLL.Volume volume, float negativeMin, float negativeMax, float positiveMin, float positiveMax, float alpha)
        {
            for (int i = 0; i < CutGenerators.Count; i++)
            {
                Core.DLL.CutGenerator generator = CutGenerators[i];
                generator.FillTextureWithFMRI(volume, negativeMin, negativeMax, positiveMin, positiveMax, alpha);
                ApplyPixels(BrainCutTextures[i], generator.CopyOverlayPixels(), generator.CutGeometryGenerator.TextureSize);
            }
        }
        /// <summary>
        /// Color cuts with Localizers atlas using min, middle, max parameters (with mask)
        /// </summary>
        public void ColorCutsTexturesWithLocalizersAtlas(Core.DLL.Volume volume, float min, float middle, float max, Core.DLL.Volume mask, Color32[] colorScheme)
        {
            for (int i = 0; i < CutGenerators.Count; i++)
            {
                Core.DLL.CutGenerator generator = CutGenerators[i];
                generator.FillTextureWithLocalizer(volume, min, middle, max, mask, colorScheme);
                ApplyPixels(BrainCutTextures[i], generator.CopyOverlayPixels(), generator.CutGeometryGenerator.TextureSize);
            }
        }
        /// <summary>
        /// Reset the color schemes
        /// </summary>
        /// <param name="colormap">Color map to be used</param>
        /// <param name="colorBrainCut">Cut color to be used</param>
        public void ResetColorSchemes(ColorType colormap, ColorType colorBrainCut)
        {
            m_BrainCutColorSchemePixels = UnityTextureFactory.Generate1DColorPixels(colorBrainCut);
            m_CutActivityColorSchemePixels = UnityTextureFactory.Generate1DColorPixels(colormap);
        }
        /// <summary>
        /// Clean the Cut Textures Utility class
        /// </summary>
        public void Clean()
        {
            foreach (var dllMRITextureCutGenerator in CutGenerators) dllMRITextureCutGenerator?.Dispose();
        }
        #endregion

        #region Private Methods
        private static bool IsSiteOnCut(Vector3 sitePosition, Core.Object3D.Cut cut, float precisionSquared)
        {
            Vector3 normal = cut.Normal;
            float normalSquaredMagnitude = normal.sqrMagnitude;
            if (normalSquaredMagnitude <= 0.0f)
            {
                return false;
            }

            float distance = Vector3.Dot(sitePosition - cut.Point, normal);
            return (distance * distance / normalSquaredMagnitude) < precisionSquared;
        }

        private static void CopyTexture(Texture2D source, Texture2D target)
        {
            if (source == null || target == null)
            {
                return;
            }

            if (target.width != source.width || target.height != source.height)
            {
                target.Reinitialize(source.width, source.height);
            }

            target.SetPixels32(source.GetPixels32());
            target.Apply(false, false);
        }

        private static void ApplyPixels(Texture2D texture, Color32[] pixels, Vector2Int size)
        {
            if (texture == null)
            {
                return;
            }
            if (pixels == null || pixels.Length == 0 || size.x <= 0 || size.y <= 0)
            {
                texture.Reinitialize(10, 10);
                Color32[] emptyPixels = new Color32[100];
                for (int i = 0; i < emptyPixels.Length; ++i)
                {
                    emptyPixels[i] = new Color32(0, 0, 0, 255);
                }
                texture.SetPixels32(emptyPixels);
                texture.Apply(false, false);
                return;
            }
            if (pixels.Length < size.x * size.y)
            {
                throw new ArgumentException("Pixel buffer is smaller than width * height.", nameof(pixels));
            }
            if (texture.width != size.x || texture.height != size.y)
            {
                texture.Reinitialize(size.x, size.y);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }
        #endregion
    }
}
