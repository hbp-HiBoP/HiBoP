using System.Collections.Generic;
using System.Linq;
using HBP.Module3D.DLL;
using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// Contains the textures for the cuts and methods to compute them
    /// </summary>
    public class CutTexturesUtility
    {
        #region Properties
        /// <summary>
        /// Color scheme for the cut
        /// </summary>
        public DLL.Texture DLLCutColorScheme;
        /// <summary>
        /// Color scheme for the FMRI
        /// </summary>
        public DLL.Texture DLLCutFMRIColorScheme;
        /// <summary>
        /// Generator for the MRI textures of the cuts
        /// </summary>
        public List<DLL.MRITextureCutGenerator> DLLMRITextureCutGenerators = new List<DLL.MRITextureCutGenerator>();
        /// <summary>
        /// DLL textures for the cuts for the 3D
        /// </summary>
        public List<DLL.Texture> DLLBrainCutTextures = new List<DLL.Texture>();
        /// <summary>
        /// DLL textures for the cuts for the GUI
        /// </summary>
        public List<DLL.Texture> DLLGUIBrainCutTextures = new List<DLL.Texture>();
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

        #region Constructors
        public CutTexturesUtility(int size = 0)
        {
            Resize(size);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Resize every lists
        /// </summary>
        /// <param name="size">New size for the lists</param>
        public void Resize(int size)
        {
            while (Size < size)
            {
                BrainCutTextures.Add(Texture2Dutility.GenerateCut());
                GUIBrainCutTextures.Add(Texture2Dutility.GenerateGUI());
                DLLBrainCutTextures.Add(new DLL.Texture());
                DLLGUIBrainCutTextures.Add(new DLL.Texture());
                DLLMRITextureCutGenerators.Add(new DLL.MRITextureCutGenerator());
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
                DLLMRITextureCutGenerators[DLLMRITextureCutGenerators.Count - 1].Dispose();
                DLLMRITextureCutGenerators.RemoveAt(DLLMRITextureCutGenerators.Count - 1);
                Size--;
            }
        }
        /// <summary>
        /// Create a MRI texture with parameters
        /// </summary>
        /// <param name="geometryGenerator">MRI generator</param>
        /// <param name="volume">MRI volume</param>
        /// <param name="indexCut">Index of the cut</param>
        /// <param name="MRICalMinFactor">Cal Min Factor</param>
        /// <param name="MRICalMaxFactor">Cal Max Factor</param>
        public void CreateMRITexture(DLL.MRIGeometryCutGenerator geometryGenerator, DLL.Volume volume, int indexCut, float MRICalMinFactor, float MRICalMaxFactor, int blurFactor)
        {
            UnityEngine.Profiling.Profiler.BeginSample("TEST-Column3DView create_MRI_texture reset 0  ");
            DLL.MRITextureCutGenerator textureGenerator = DLLMRITextureCutGenerators[indexCut];
            textureGenerator.Reset(geometryGenerator, blurFactor);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("TEST-Column3DView create_MRI_texture fill_texture_with_volume 1  ");
            textureGenerator.FillTextureWithVolume(volume, DLLCutColorScheme, MRICalMinFactor, MRICalMaxFactor);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("TEST-Column3DView create_MRI_texture updateTexture 2  ");
            textureGenerator.UpdateTexture(DLLBrainCutTextures[indexCut]);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("TEST-Column3DView create_MRI_texture update_texture_2D 3  ");
            DLLBrainCutTextures[indexCut].UpdateTexture2D(BrainCutTextures[indexCut]);
            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// Create MRI textures for the GUI
        /// </summary>
        /// <param name="cuts">Cuts of these textures</param>
        public void CreateGUIMRITextures(List<Cut> cuts)
        {
            foreach (Cut cut in cuts)
            {
                if (DLLBrainCutTextures[cut.ID].TextureSize[0] > 0)
                {
                    DLLGUIBrainCutTextures[cut.ID].CopyAndRotate(DLLBrainCutTextures[cut.ID], cut.Orientation.ToString(), cut.Flip, false, cut.ID, cuts, DLLMRITextureCutGenerators[cut.ID]);
                }
            }
        }
        /// <summary>
        /// Draw the sites on the gui texture
        /// </summary>
        /// <param name="cuts"></param>
        /// <param name="rawList"></param>
        public void DrawSitesOnMRITextures(List<Cut> cuts, RawSiteList rawList)
        {
            foreach (Cut cut in cuts)
            {
                if (DLLBrainCutTextures[cut.ID].TextureSize[0] > 0)
                {
                    DLLBrainCutTextures[cut.ID].DrawSites(cut, rawList, 1, DLLMRITextureCutGenerators[cut.ID]);
                }
            }
        }
        /// <summary>
        /// Resize the MRI textures for the GUI to squares
        /// </summary>
        /// <param name="cuts">Cuts of these textures</param>
        public void ResizeGUIMRITextures(List<Cut> cuts)
        {
            int max = 0;
            foreach (var cut in cuts)
            {
                if (cut.Orientation != Data.Enums.CutOrientation.Custom)
                {
                    int textureMax = DLLGUIBrainCutTextures[cut.ID].TextureSize.Max();
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
        public void ColorCutsTexturesWithIEEG(Column3DDynamic column)
        {
            if (column.CutTextures != this)
            {
                throw new System.Exception("Column and CutTexturesUtility do not match.");
            }

            for (int i = 0; i < DLLMRITextureCutGenerators.Count; ++i)
            {
                DLL.MRITextureCutGenerator generator = DLLMRITextureCutGenerators[i];
                generator.FillTextureWithIEEG(column, DLLCutColorScheme);

                DLL.Texture cutTexture = DLLBrainCutTextures[i];
                generator.UpdateTextureWithIEEG(cutTexture);
                cutTexture.UpdateTexture2D(BrainCutTextures[i]);
            }
        }
        /// <summary>
        /// Color cuts with FMRI
        /// </summary>
        /// <param name="volume">FMRI volume</param>
        /// <param name="indexCut">Index of the cut</param>
        /// <param name="FMRICalMinFactor">FMRI Cal Min Factor</param>
        /// <param name="FMRICalMaxFactor">FMRI Cal Max Factor</param>
        /// <param name="FMRIAlpha">Transparency of the FMRI</param>
        public void ColorCutsTexturesWithFMRI(DLL.Volume volume, int indexCut, float FMRICalMinFactor, float FMRICalMaxFactor, float FMRIAlpha)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Compute FMRI textures");
            DLL.MRITextureCutGenerator generator = DLLMRITextureCutGenerators[indexCut];
            generator.FillTextureWithFMRI(DLLCutFMRIColorScheme, volume, FMRICalMinFactor, FMRICalMaxFactor, FMRIAlpha);

            DLL.Texture cutTexture = DLLBrainCutTextures[indexCut];
            generator.UpdateTextureWithFMRI(cutTexture);
            cutTexture.UpdateTexture2D(BrainCutTextures[indexCut]);
            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// Reset the color schemes
        /// </summary>
        /// <param name="colormap">Color map to be used</param>
        /// <param name="colorBrainCut">Cut color to be used</param>
        public void ResetColorSchemes(Data.Enums.ColorType colormap, Data.Enums.ColorType colorBrainCut)
        {
            DLLCutColorScheme?.Dispose();
            DLLCutColorScheme = DLL.Texture.Generate2DColorTexture(colorBrainCut, colormap);
            DLLCutFMRIColorScheme?.Dispose();
            DLLCutFMRIColorScheme = DLL.Texture.Generate2DColorTexture(colorBrainCut, colormap);
        }
        /// <summary>
        /// Set the DLL MRI Volume Generator
        /// </summary>
        /// <param name="dllMRIVolumeGenerator"></param>
        public void SetMRIVolumeGenerator(MRIVolumeGenerator dllMRIVolumeGenerator)
        {
            foreach (var dllMRITextureCutGenerator in DLLMRITextureCutGenerators)
            {
                dllMRITextureCutGenerator.SetMRIVolumeGenerator(dllMRIVolumeGenerator);
            }
        }
        /// <summary>
        /// Clean the Cut Textures Utility class
        /// </summary>
        public void Clean()
        {
            foreach (var dllMRITextureCutGenerator in DLLMRITextureCutGenerators) dllMRITextureCutGenerator?.Dispose();
            DLLCutColorScheme?.Dispose();
            DLLCutFMRIColorScheme?.Dispose();
            foreach (var texture in DLLBrainCutTextures) texture?.Dispose();
            foreach (var texture in DLLGUIBrainCutTextures) texture?.Dispose();
        }
        #endregion
    }
}