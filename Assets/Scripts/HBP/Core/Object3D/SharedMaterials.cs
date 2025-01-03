using HBP.Core.Enums;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.Core.Object3D
{
    /// <summary>
    /// Class managing the materials for some objects on the scene (ROI, Sites, Selection Ring)
    /// </summary>
    public static class SharedMaterials
    {
        #region Properties
        /// <summary>
        /// Materials used for the ROI spheres
        /// </summary>
        public static ROIMaterials ROI { get; private set; } = new ROIMaterials();
        /// <summary>
        /// Materials used for the sites
        /// </summary>
        public static SiteMaterials Site { get; private set; } = new SiteMaterials();
        #endregion
    }

    /// <summary>
    /// Class containing the materials of the ROI spheres
    /// </summary>
    public class ROIMaterials
    {
        #region Properties
        /// <summary>
        /// Material used for a ROI sphere in a regular state
        /// </summary>
        public Material Normal { get; private set; }
        /// <summary>
        /// Material used for a ROI sphere when it is selected
        /// </summary>
        public Material Selected { get; private set; }
        #endregion

        #region Constructors
        public ROIMaterials()
        {
            Load();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Load the materials for the ROIs
        /// </summary>
        public void Load()
        {
            Normal = Object.Instantiate(Resources.Load("Materials/ROI/ROI", typeof(Material))) as Material;
            Selected = Object.Instantiate(Resources.Load("Materials/ROI/ROISelected", typeof(Material))) as Material;
        }
        #endregion
    }

    /// <summary>
    /// Class containing the materials of the sites
    /// </summary>
    public class SiteMaterials
    {
        #region Properties
        /// <summary>
        /// Dictionary containing the site material for each color that has been used in the scene
        /// </summary>
        private Dictionary<Color, Material> m_MaterialByColor;

        /// <summary>
        /// Default material for a site
        /// </summary>
        public Material Basic { get; private set; }

        /// <summary>
        /// Material used when the activity of the site is negative
        /// </summary>
        public Material Negative { get; private set; }
        /// <summary>
        /// Material used when the activity of the site is positive
        /// </summary>
        public Material Positive { get; private set; }
        /// <summary>
        /// Material used when the site is blacklisted
        /// </summary>
        public Material BlackListed { get; private set; }

        /// <summary>
        /// Material used when the activity of the site is negative and the site is highlighted
        /// </summary>
        public Material NegativeHighlighted { get; private set; }
        /// <summary>
        /// Material used when the activity of the site is positive and the site is highlighted
        /// </summary>
        public Material PositiveHighlighted { get; private set; }
        /// <summary>
        /// Material used when the site is blacklisted and highlighted
        /// </summary>
        public Material BlackListedHighlighted { get; private set; }

        /// <summary>
        /// Material used if the site is a source for CCEP
        /// </summary>
        public Material Source { get; private set; }
        /// <summary>
        /// Material used if the site is a source for CCEP and is highlighted
        /// </summary>
        public Material SourceHighlighted { get; private set; }
        /// <summary>
        /// Material used if the site is not a source for CCEP
        /// </summary>
        public Material NotASource { get; private set; }
        /// <summary>
        /// Material used if the site is not a source for CCEP and is highlighted
        /// </summary>
        public Material NotASourceHighlighted { get; private set; }
        #endregion

        #region Constructors
        public SiteMaterials()
        {
            m_MaterialByColor = new Dictionary<Color, Material>();
            Load();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Load the materials for the sites
        /// </summary>
        private void Load()
        {
            BlackListed = Object.Instantiate(Resources.Load("Materials/Sites/Blacklisted", typeof(Material))) as Material;
            Negative = Object.Instantiate(Resources.Load("Materials/Sites/Negative", typeof(Material))) as Material;
            Basic = Object.Instantiate(Resources.Load("Materials/Sites/Basic", typeof(Material))) as Material;
            Positive = Object.Instantiate(Resources.Load("Materials/Sites/Positive", typeof(Material))) as Material;
            BlackListedHighlighted = Object.Instantiate(Resources.Load("Materials/Sites/BlacklistedHighlighted", typeof(Material))) as Material;
            NegativeHighlighted = Object.Instantiate(Resources.Load("Materials/Sites/NegativeHighlighted", typeof(Material))) as Material;
            PositiveHighlighted = Object.Instantiate(Resources.Load("Materials/Sites/PositiveHighlighted", typeof(Material))) as Material;
            Source = Object.Instantiate(Resources.Load("Materials/Sites/Source", typeof(Material))) as Material;
            SourceHighlighted = Object.Instantiate(Resources.Load("Materials/Sites/SourceHighlighted", typeof(Material))) as Material;
            NotASource = Object.Instantiate(Resources.Load("Materials/Sites/NotASource", typeof(Material))) as Material;
            NotASourceHighlighted = Object.Instantiate(Resources.Load("Materials/Sites/NotASourceHighlighted", typeof(Material))) as Material;
        }
        /// <summary>
        /// Get the site material corresponding to the input color
        /// </summary>
        /// <param name="baseColor">Color used to get the material</param>
        /// <param name="highlighted">Is the site highlighted ?</param>
        /// <returns>The corresponding material</returns>
        private Material GetMaterial(Color baseColor, bool highlighted)
        {
            Color color = new Color(baseColor.r, baseColor.g, baseColor.b, highlighted ? 1 : 0.5f);
            if (!m_MaterialByColor.TryGetValue(color, out Material material))
            {
                material = Object.Instantiate(Basic);
                material.color = color;
                m_MaterialByColor.Add(color, material);
            }
            return material;
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
        public Material GetSharedMaterial(bool highlighted, SiteType siteType, Color baseColor)
        {
            return siteType switch
            {
                SiteType.Positive => highlighted ? PositiveHighlighted : Positive,
                SiteType.Negative => highlighted ? NegativeHighlighted : Negative,
                SiteType.Source => highlighted ? SourceHighlighted : Source,
                SiteType.NotASource => highlighted ? NotASourceHighlighted : NotASource,
                SiteType.BlackListed => highlighted ? BlackListedHighlighted : BlackListed,
                _ => GetMaterial(baseColor, highlighted),
            };
        }
        #endregion
    }
}