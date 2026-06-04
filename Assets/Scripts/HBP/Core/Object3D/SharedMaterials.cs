using HBP.Core.Enums;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.Core.Object3D
{
    /// <summary>
    /// Class managing the materials for some objects on the scene (ROI, Sites)
    /// </summary>
    [CreateAssetMenu(fileName = "SharedMaterials", menuName = "HBP/General/SharedMaterials")]
    public class SharedMaterials : ScriptableObject
    {
        #region Properties
        /// <summary>
        /// Materials used for the ROI spheres
        /// </summary>
        [field: SerializeField] public ROIMaterials ROI { get; private set; } = new ROIMaterials();
        /// <summary>
        /// Materials used for the sites
        /// </summary>
        [field: SerializeField] public SiteMaterials Site { get; private set; } = new SiteMaterials();
        #endregion
    }

    /// <summary>
    /// Class containing the materials of the ROI spheres
    /// </summary>
    [System.Serializable]
    public class ROIMaterials
    {
        #region Properties
        /// <summary>
        /// Material used for a ROI sphere in a regular state
        /// </summary>
        [field: SerializeField] public Material Normal { get; private set; }
        /// <summary>
        /// Material used for a ROI sphere when it is selected
        /// </summary>
        [field: SerializeField] public Material Selected { get; private set; }
        #endregion
    }

    /// <summary> 
    /// Class containing the materials of the sites
    /// </summary>
    [System.Serializable]
    public class SiteMaterials
    {
        #region Properties
        /// <summary>
        /// Dictionary containing the site material for each color that has been used in the scene
        /// </summary>
        private Dictionary<Color, Material> m_MaterialByColor = new();

        /// <summary>
        /// Default material for a site
        /// </summary>
        [field: SerializeField] public Material Basic { get; private set; }

        /// <summary>
        /// Material used when the activity of the site is negative
        /// </summary>
        [field: SerializeField] public SiteMaterial Negative { get; private set; }
        /// <summary>
        /// Material used when the activity of the site is positive
        /// </summary>
        [field: SerializeField] public SiteMaterial Positive { get; private set; }
        /// <summary>
        /// Material used when the site is blacklisted
        /// </summary>
        [field: SerializeField] public SiteMaterial Blacklisted { get; private set; }

        /// <summary>
        /// Material used if the site is a source for CCEP
        /// </summary>
        [field: SerializeField] public SiteMaterial Source { get; private set; }
        /// <summary>
        /// Material used if the site is not a source for CCEP
        /// </summary>
        [field: SerializeField] public SiteMaterial NotASource { get; private set; }
        #endregion

        #region Private Methods
        /// <summary>
        /// Get the site material corresponding to the input color
        /// </summary>
        /// <param name="baseColor">Color used to get the material</param>
        /// <param name="highlighted">Is the site highlighted ?</param>
        /// <returns>The corresponding material</returns>
        private Material GetMaterial(Color baseColor, bool highlighted)
        {
            Color color = new(baseColor.r, baseColor.g, baseColor.b, highlighted ? 1 : 0.5f);
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
                SiteType.Positive => highlighted ? Positive.Highlighted : Positive.Normal,
                SiteType.Negative => highlighted ? Negative.Highlighted : Negative.Normal,
                SiteType.Source => highlighted ? Source.Highlighted : Source.Normal,
                SiteType.NotASource => highlighted ? NotASource.Highlighted : NotASource.Normal,
                SiteType.BlackListed => highlighted ? Blacklisted.Highlighted : Blacklisted.Normal,
                _ => GetMaterial(baseColor, highlighted),
            };
        }
        #endregion
    }

    [System.Serializable]
    public class SiteMaterial
    {
        [field: SerializeField] public Material Normal { get; private set; }
        [field: SerializeField] public Material Highlighted { get; private set; }
    }
}