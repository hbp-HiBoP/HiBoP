using HBP.Core.Enums;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.Core.Object3D
{
    /// <summary>
    /// Class managing the materials for some objects on the scene (ROI, Sites, Selection Ring)
    /// </summary>
    public class SharedMaterials : MonoBehaviour
    {
        #region Struct
        /// <summary>
        /// Struct containing the materials of the ROI spheres
        /// </summary>
        public struct ROI
        {
            #region Properties
            /// <summary>
            /// Material used for a ROI sphere in a regular state
            /// </summary>
            public static Material Normal { get; private set; }
            /// <summary>
            /// Material used for a ROI sphere when it is selected
            /// </summary>
            public static Material Selected { get; private set; }
            #endregion

            #region Public Methods
            /// <summary>
            /// Load the materials for the ROIs
            /// </summary>
            public static void Load()
            {
                Normal = Instantiate(Resources.Load("Materials/ROI/ROI", typeof(Material))) as Material;
                Selected = Instantiate(Resources.Load("Materials/ROI/ROISelected", typeof(Material))) as Material;
            }
            #endregion
        }

        /// <summary>
        /// Struct containing the materials of the sites
        /// </summary>
        public struct Site
        {
            #region Properties
            /// <summary>
            /// Dictionary containing the site material for each color that has been used in the scene
            /// </summary>
            private static Dictionary<Color, Material> m_MaterialByColor = new Dictionary<Color, Material>();

            /// <summary>
            /// Default material for a site
            /// </summary>
            public static Material Basic { get; private set; }

            /// <summary>
            /// Material used when the activity of the site is negative
            /// </summary>
            public static Material Negative { get; private set; }
            /// <summary>
            /// Material used when the activity of the site is positive
            /// </summary>
            public static Material Positive { get; private set; }
            /// <summary>
            /// Material used when the site is blacklisted
            /// </summary>
            public static Material BlackListed { get; private set; }

            /// <summary>
            /// Material used when the activity of the site is negative and the site is highlighted
            /// </summary>
            public static Material NegativeHighlighted { get; private set; }
            /// <summary>
            /// Material used when the activity of the site is positive and the site is highlighted
            /// </summary>
            public static Material PositiveHighlighted { get; private set; }
            /// <summary>
            /// Material used when the site is blacklisted and highlighted
            /// </summary>
            public static Material BlackListedHighlighted { get; private set; }

            /// <summary>
            /// Material used if the site is a source for CCEP
            /// </summary>
            public static Material Source { get; private set; }
            /// <summary>
            /// Material used if the site is a source for CCEP and is highlighted
            /// </summary>
            public static Material SourceHighlighted { get; private set; }
            /// <summary>
            /// Material used if the site is not a source for CCEP
            /// </summary>
            public static Material NotASource { get; private set; }
            /// <summary>
            /// Material used if the site is not a source for CCEP and is highlighted
            /// </summary>
            public static Material NotASourceHighlighted { get; private set; }
            #endregion

            #region Public Methods
            /// <summary>
            /// Load the materials for the sites
            /// </summary>
            public static void Load()
            {
                BlackListed = Instantiate(Resources.Load("Materials/Sites/Blacklisted", typeof(Material))) as Material;
                Negative = Instantiate(Resources.Load("Materials/Sites/Negative", typeof(Material))) as Material;
                Basic = Instantiate(Resources.Load("Materials/Sites/Basic", typeof(Material))) as Material;
                Positive = Instantiate(Resources.Load("Materials/Sites/Positive", typeof(Material))) as Material;
                BlackListedHighlighted = Instantiate(Resources.Load("Materials/Sites/BlacklistedHighlighted", typeof(Material))) as Material;
                NegativeHighlighted = Instantiate(Resources.Load("Materials/Sites/NegativeHighlighted", typeof(Material))) as Material;
                PositiveHighlighted = Instantiate(Resources.Load("Materials/Sites/PositiveHighlighted", typeof(Material))) as Material;
                Source = Instantiate(Resources.Load("Materials/Sites/Source", typeof(Material))) as Material;
                SourceHighlighted = Instantiate(Resources.Load("Materials/Sites/SourceHighlighted", typeof(Material))) as Material;
                NotASource = Instantiate(Resources.Load("Materials/Sites/NotASource", typeof(Material))) as Material;
                NotASourceHighlighted = Instantiate(Resources.Load("Materials/Sites/NotASourceHighlighted", typeof(Material))) as Material;
            }
            /// <summary>
            /// Get the site material corresponding to the input color
            /// </summary>
            /// <param name="baseColor">Color used to get the material</param>
            /// <param name="highlighted">Is the site highlighted ?</param>
            /// <returns>The corresponding material</returns>
            public static Material GetMaterial(Color baseColor, bool highlighted)
            {
                Color color = new Color(baseColor.r, baseColor.g, baseColor.b, highlighted ? 1 : 0.5f);
                if (!m_MaterialByColor.TryGetValue(color, out Material material))
                {
                    material = Instantiate(Basic) as Material;
                    material.color = color;
                    m_MaterialByColor.Add(color, material);
                }
                return material;
            }
            #endregion
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            ROI.Load();
            Site.Load();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Get the material for a specific site
        /// </summary>
        /// <param name="highlighted">Is the site highlighted</param>
        /// <param name="siteType">Current state of the site</param>
        /// <param name="baseColor">Color of the site</param>
        /// <returns>The corresponding material</returns>
        public static Material SiteSharedMaterial(bool highlighted, SiteType siteType, Color baseColor)
        {
            switch (siteType)
            {
                case SiteType.Positive:
                    return highlighted ? Site.PositiveHighlighted : Site.Positive;
                case SiteType.Negative:
                    return highlighted ? Site.NegativeHighlighted : Site.Negative;
                case SiteType.Source:
                    return highlighted ? Site.SourceHighlighted : Site.Source;
                case SiteType.NotASource:
                    return highlighted ? Site.NotASourceHighlighted : Site.NotASource;
                case SiteType.BlackListed:
                    return highlighted ? Site.BlackListedHighlighted : Site.BlackListed;
                case SiteType.Normal:
                default:
                    return Site.GetMaterial(baseColor, highlighted);
            }
        }
        #endregion
    }
}