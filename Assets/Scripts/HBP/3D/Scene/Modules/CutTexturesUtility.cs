using HBP.Module3D.DLL;
using System.Collections.Generic;
using Tools.Unity;
using UnityEngine;
using HBP.Core.Enums;

namespace HBP.Module3D
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
        /// Color scheme for the cut
        /// </summary>
        public Core.DLL.Texture DLLCutColorScheme;
        /// <summary>
        /// Generator for the MRI textures of the cuts
        /// </summary>
        public List<Core.DLL.CutGenerator> CutGenerators = new List<Core.DLL.CutGenerator>();
        /// <summary>
        /// DLL textures for the cuts for the 3D
        /// </summary>
        public List<Core.DLL.Texture> DLLBrainCutTextures = new List<Core.DLL.Texture>();
        /// <summary>
        /// DLL textures for the cuts for the GUI
        /// </summary>
        public List<Core.DLL.Texture> DLLGUIBrainCutTextures = new List<Core.DLL.Texture>();
        /// <summary>
        /// Unity textures for the cuts for the 3D
        /// </summary>
        public List<Texture2D> BrainCutTextures = new List<Texture2D>();
        /// <summary>
        /// Unity textures for the cuts for the GUI
        /// </summary>
        public List<Texture2D> GUIBrainCutTextures = new List<Texture2D>();
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
                BrainCutTextures.Add(Texture2DExtension.Generate());
                GUIBrainCutTextures.Add(Texture2DExtension.Generate(1, 1, -10, 9, FilterMode.Point));
                DLLBrainCutTextures.Add(new Core.DLL.Texture());
                DLLGUIBrainCutTextures.Add(new Core.DLL.Texture());
                CutGenerators.Add(new Core.DLL.CutGenerator());
                Size++;
            }
            while (Size > size)
            {
                Object.Destroy(BrainCutTextures[BrainCutTextures.Count - 1]);
                BrainCutTextures.RemoveAt(BrainCutTextures.Count - 1);
                Object.Destroy(GUIBrainCutTextures[GUIBrainCutTextures.Count - 1]);
                GUIBrainCutTextures.RemoveAt(GUIBrainCutTextures.Count - 1);
                DLLBrainCutTextures[DLLBrainCutTextures.Count - 1].Dispose();
                DLLBrainCutTextures.RemoveAt(DLLBrainCutTextures.Count - 1);
                DLLGUIBrainCutTextures[DLLGUIBrainCutTextures.Count - 1].Dispose();
                DLLGUIBrainCutTextures.RemoveAt(DLLGUIBrainCutTextures.Count - 1);
                CutGenerators[CutGenerators.Count - 1].Dispose();
                CutGenerators.RemoveAt(CutGenerators.Count - 1);
                Size--;
            }
            for (int i = 0; i < size; i++)
            {
                CutGenerators[i].Initialize(activityGenerator, cutGeometryGenerators[i], 4);
            }
        }
        /// <summary>
        /// Create a MRI texture with parameters
        /// </summary>
        /// <param name="volume">MRI volume</param>
        /// <param name="indexCut">Index of the cut</param>
        /// <param name="MRICalMinFactor">Cal Min Factor</param>
        /// <param name="MRICalMaxFactor">Cal Max Factor</param>
        public void CreateMRITexture(Core.DLL.Volume volume, int indexCut, float MRICalMinFactor, float MRICalMaxFactor, int blurFactor)
        {
            Core.DLL.CutGenerator cutGenerator = CutGenerators[indexCut];
            UnityEngine.Profiling.Profiler.BeginSample("FillTextureWithVolume");
            cutGenerator.FillTextureWithVolume(DLLCutColorScheme, MRICalMinFactor, MRICalMaxFactor);
            UnityEngine.Profiling.Profiler.EndSample();
            cutGenerator.UpdateTextureWithVolume(DLLBrainCutTextures[indexCut]);
            DLLBrainCutTextures[indexCut].UpdateTexture2D(BrainCutTextures[indexCut]);
        }
        /// <summary>
        /// Create MRI textures for the GUI
        /// </summary>
        /// <param name="cuts">Cuts of these textures</param>
        public void CreateGUIMRITextures(List<Core.Object3D.Cut> cuts)
        {
            foreach (Core.Object3D.Cut cut in cuts)
            {
                if (DLLBrainCutTextures[cut.ID].Height > 0)
                {
                    DLLGUIBrainCutTextures[cut.ID].CloneAndRotate(DLLBrainCutTextures[cut.ID], cut.Orientation.ToString(), cut.Flip, false, cut.ID, cuts, CutGenerators[cut.ID]);
                }
            }
        }
        /// <summary>
        /// Draw the sites on the gui texture
        /// </summary>
        /// <param name="cuts"></param>
        /// <param name="rawList"></param>
        public void DrawSitesOnMRITextures(List<Core.Object3D.Cut> cuts, Core.DLL.RawSiteList rawList)
        {
            foreach (Core.Object3D.Cut cut in cuts)
            {
                if (DLLBrainCutTextures[cut.ID].Height > 0)
                {
                    DLLBrainCutTextures[cut.ID].DrawSites(cut, rawList, 1, CutGenerators[cut.ID]);
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
                    int textureMax = Mathf.Max(DLLGUIBrainCutTextures[cut.ID].Width, DLLGUIBrainCutTextures[cut.ID].Height);
                    if (textureMax > max)
                    {
                        max = textureMax;
                    }
                }
            }
            for (int i = 0; i < DLLGUIBrainCutTextures.Count; ++i)
            {
                DLLGUIBrainCutTextures[i].ResizeToSquare(max);
            }
        }
        /// <summary>
        /// Update the Unity Textures from DLL textures
        /// </summary>
        public void UpdateTextures2D()
        {
            for (int i = 0; i < DLLGUIBrainCutTextures.Count; ++i)
            {
                DLLGUIBrainCutTextures[i].UpdateTexture2D(GUIBrainCutTextures[i]);
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

            for (int i = 0; i < CutGenerators.Count; ++i)
            {
                Core.DLL.CutGenerator generator = CutGenerators[i];
                generator.FillTextureWithActivity(DLLCutColorScheme, timelineIndex, Column.ActivityAlpha);

                Core.DLL.Texture cutTexture = DLLBrainCutTextures[i];
                generator.UpdateTextureWithActivity(cutTexture);
                cutTexture.UpdateTexture2D(BrainCutTextures[i]);
            }
        }
        public void ColorCutsTexturesWithBrainAtlas(Core.DLL.BrainAtlas selectedAtlas, float alpha, int selectedArea)
        {
            for (int i = 0; i < CutGenerators.Count; i++)
            {
                Core.DLL.CutGenerator generator = CutGenerators[i];
                generator.FillTextureWithAtlas(selectedAtlas, alpha, selectedArea);

                Core.DLL.Texture cutTexture = DLLBrainCutTextures[i];
                generator.UpdateTextureWithAtlas(cutTexture);
                cutTexture.UpdateTexture2D(BrainCutTextures[i]);
            }
        }
        public void ColorCutsTexturesWithFMRIAtlas(Core.DLL.Volume volume, float negativeMin, float negativeMax, float positiveMin, float positiveMax, float alpha)
        {
            for (int i = 0; i < CutGenerators.Count; i++)
            {
                Core.DLL.CutGenerator generator = CutGenerators[i];
                generator.FillTextureWithFMRI(volume, negativeMin, negativeMax, positiveMin, positiveMax, alpha);

                Core.DLL.Texture cutTexture = DLLBrainCutTextures[i];
                generator.UpdateTextureWithAtlas(cutTexture);
                cutTexture.UpdateTexture2D(BrainCutTextures[i]);
            }
        }
        /// <summary>
        /// Reset the color schemes
        /// </summary>
        /// <param name="colormap">Color map to be used</param>
        /// <param name="colorBrainCut">Cut color to be used</param>
        public void ResetColorSchemes(ColorType colormap, ColorType colorBrainCut)
        {
            DLLCutColorScheme?.Dispose();
            DLLCutColorScheme = Core.DLL.Texture.Generate2DColorTexture(colorBrainCut, colormap);
        }
        /// <summary>
        /// Clean the Cut Textures Utility class
        /// </summary>
        public void Clean()
        {
            foreach (var dllMRITextureCutGenerator in CutGenerators) dllMRITextureCutGenerator?.Dispose();
            DLLCutColorScheme?.Dispose();
            foreach (var texture in DLLBrainCutTextures) texture?.Dispose();
            foreach (var texture in DLLGUIBrainCutTextures) texture?.Dispose();
        }
        #endregion
    }
}