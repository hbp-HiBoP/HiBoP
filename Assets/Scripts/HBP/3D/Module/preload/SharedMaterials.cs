
/* \file SharedMaterials.cs
 * \author Lance Florian
 * \date    22/04/2016
 * \brief Define SharedMaterials
 */

using System.Collections.Generic;
using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// Shared materials used by GO at runtime
    /// </summary>
    public class SharedMaterials : MonoBehaviour
    {
        #region Struct
        public struct Brain
        {
            public static Dictionary<Base3DScene, Material> BrainMaterials = new Dictionary<Base3DScene, Material>();
            public static Dictionary<Base3DScene, Material> CutMaterials = new Dictionary<Base3DScene, Material>();

            public static void Initialize()
            {
                ApplicationState.Module3D.ScenesManager.OnAddScene.AddListener((scene) =>
                {
                    AddSceneMaterials(scene);
                });
                ApplicationState.Module3D.ScenesManager.OnRemoveScene.AddListener((scene) =>
                {
                    RemoveSceneMaterials(scene);
                });
            }

            public static void AddSceneMaterials(Base3DScene scene)
            {
                Material brainMaterial = null, cutMaterial = null;
                switch (scene.Type) // Distinction is useful in the shader in order to show mars atlases in sp
                {
                    case SceneType.SinglePatient:
                        brainMaterial = Instantiate(Resources.Load("Materials/Brain/SpBrain", typeof(Material))) as Material;
                        cutMaterial = Instantiate(Resources.Load("Materials/Brain/SpCut", typeof(Material))) as Material;
                        break;
                    case SceneType.MultiPatients:
                        brainMaterial = Instantiate(Resources.Load("Materials/Brain/MpBrain", typeof(Material))) as Material;
                        cutMaterial = Instantiate(Resources.Load("Materials/Brain/MpCut", typeof(Material))) as Material;
                        break;
                    default:
                        break;
                }
                if (!BrainMaterials.ContainsKey(scene))
                {
                    BrainMaterials.Add(scene, brainMaterial);
                }
                else
                {
                    BrainMaterials[scene] = brainMaterial;
                }
                if (!CutMaterials.ContainsKey(scene))
                {
                    CutMaterials.Add(scene, cutMaterial);
                }
                else
                {
                    CutMaterials[scene] = cutMaterial;
                }
            }

            public static void RemoveSceneMaterials(Base3DScene scene)
            {
                Destroy(BrainMaterials[scene]);
                BrainMaterials.Remove(scene);
                Destroy(CutMaterials[scene]);
                CutMaterials.Remove(scene);
            }
        }

        public struct ROI
        {
            public static Material Normal = null;
            public static Material Selected = null;
        }

        public struct Ring
        {
            public static Material Selected = null;
        }

        public struct Site
        {
            public static Material BlackListed = null;
            public static Material Excluded = null;
            public static Material Negative = null;
            public static Material Normal = null;
            public static Material Positive = null;
            public static Material NormalHighlighted = null;
            public static Material BlackListedHighlighted = null;
            public static Material ExcludedHighlighted = null;
            public static Material NegativeHighlighted = null;
            public static Material PositiveHighlighted = null;
            public static Material Source = null;
            public static Material SourceHighlighted = null;
            public static Material NotASource = null;
            public static Material NotASourceHighlighted = null;
            public static Material NoLatencyData = null;
            public static Material NoLatencyDataHighlighted = null;
            public static Material Marked = null;
            public static Material MarkedHighlighted = null;
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            // ROI
            ROI.Normal = Instantiate(Resources.Load("Materials/ROI/ROI", typeof(Material))) as Material;
            ROI.Selected = Instantiate(Resources.Load("Materials/ROI/ROISelected", typeof(Material))) as Material;

            // Site
            Site.BlackListed = Instantiate(Resources.Load("Materials/Sites/Blacklisted", typeof(Material))) as Material;
            Site.Excluded = Instantiate(Resources.Load("Materials/Sites/Excluded", typeof(Material))) as Material;
            Site.Negative = Instantiate(Resources.Load("Materials/Sites/Negative", typeof(Material))) as Material;
            Site.Normal = Instantiate(Resources.Load("Materials/Sites/Normal", typeof(Material))) as Material;
            Site.Positive = Instantiate(Resources.Load("Materials/Sites/Positive", typeof(Material))) as Material;
            Site.NormalHighlighted = Instantiate(Resources.Load("Materials/Sites/NormalHighlighted", typeof(Material))) as Material;
            Site.BlackListedHighlighted = Instantiate(Resources.Load("Materials/Sites/BlacklistedHighlighted", typeof(Material))) as Material;
            Site.ExcludedHighlighted = Instantiate(Resources.Load("Materials/Sites/ExcludedHighlighted", typeof(Material))) as Material;
            Site.NegativeHighlighted = Instantiate(Resources.Load("Materials/Sites/NegativeHighlighted", typeof(Material))) as Material;
            Site.PositiveHighlighted = Instantiate(Resources.Load("Materials/Sites/PositiveHighlighted", typeof(Material))) as Material;
            Site.Source = Instantiate(Resources.Load("Materials/Sites/Source", typeof(Material))) as Material;
            Site.SourceHighlighted = Instantiate(Resources.Load("Materials/Sites/SourceHighlighted", typeof(Material))) as Material;
            Site.NotASource = Instantiate(Resources.Load("Materials/Sites/notASource", typeof(Material))) as Material;
            Site.NotASourceHighlighted = Instantiate(Resources.Load("Materials/Sites/notASourceHighlighted", typeof(Material))) as Material;
            Site.NoLatencyData = Instantiate(Resources.Load("Materials/Sites/noLatencyData", typeof(Material))) as Material;
            Site.NoLatencyDataHighlighted = Instantiate(Resources.Load("Materials/Sites/noLatencyDataHighlighted", typeof(Material))) as Material;
            Site.Marked = Instantiate(Resources.Load("Materials/Sites/Marked", typeof(Material))) as Material;
            Site.MarkedHighlighted = Instantiate(Resources.Load("Materials/Sites/MarkedHighlighted", typeof(Material))) as Material;
            // Ring
            Ring.Selected = Instantiate(Resources.Load("Materials/Rings/Selected", typeof(Material))) as Material;

            // Brain
            Brain.Initialize();
        }
        #endregion

        #region Public Methods
        public static Material SiteSharedMaterial(bool hightlighted, SiteType siteType)
        {
            if (!hightlighted)
            {
                switch (siteType)
                {
                    case SiteType.Normal: return Site.Normal;
                    case SiteType.Positive: return Site.Positive;
                    case SiteType.Negative: return Site.Negative;
                    case SiteType.Excluded: return Site.Excluded;
                    case SiteType.Source: return Site.Source;
                    case SiteType.NotASource: return Site.NotASource;
                    case SiteType.NoLatencyData: return Site.NoLatencyData;
                    case SiteType.BlackListed: return Site.BlackListed;
                    case SiteType.NonePos: return Site.Positive;
                    case SiteType.NoneNeg: return Site.Negative;
                    case SiteType.Marked: return Site.Marked;
                }
            }
            else
            {
                switch (siteType)
                {
                    case SiteType.Normal: return Site.NormalHighlighted;
                    case SiteType.Positive: return Site.PositiveHighlighted;
                    case SiteType.Negative: return Site.NegativeHighlighted;
                    case SiteType.Excluded: return Site.ExcludedHighlighted;
                    case SiteType.Source: return Site.SourceHighlighted;
                    case SiteType.NotASource: return Site.NotASourceHighlighted;
                    case SiteType.NoLatencyData: return Site.NoLatencyDataHighlighted;
                    case SiteType.BlackListed: return Site.BlackListedHighlighted;
                    case SiteType.NonePos: return Site.PositiveHighlighted;
                    case SiteType.NoneNeg: return Site.NegativeHighlighted;
                    case SiteType.Marked: return Site.MarkedHighlighted;
                }
            }

            return null;
        }
        #endregion
    }
}