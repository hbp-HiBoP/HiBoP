

/**
 * \file    Site.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define site related classes
 */

// unity
using UnityEngine;

namespace HBP.VISU3D
{
    public enum SiteType { Normal, Positive, Negative, Excluded, Source, NotASource, NoLatencyData, BlackListed, NonePos, NoneNeg, Marked};

    /// <summary>
    /// Structure containing informations related to the site shaders (not used for now, shader uniform too slow)
    /// </summary>
    public struct SiteShaderInfo
    {
        public SiteShaderInfo(Color color, float smoothness)
        {
            this.color = color;
            this.smoothness = smoothness;
        }

        public Color color;
        public float smoothness;
    }

    public struct SiteColors
    {
        // normal
        public static Color BlackListed = new Color(0, 0, 0, 0.2f);
        public static Color Excluded = new Color(1f, 1f, 1f, 0.2f);
        public static Color Negative = new Color(0, 0, 1f, 0.31f);
        public static Color Normal = new Color(0.54f, 0.15f, 0.15f, 0.63f);
        public static Color Positive = new Color(1f, 0, 0, 0.31f);
        public static Color NoneNeg = new Color(0, 0, 1f, 0);
        public static Color NonePos = new Color(1f, 0, 0, 0);
        public static Color Source = new Color(1f, 1f, 0f, 0.48f);
        public static Color NoLatencyData = new Color(0.72f, 0.74f, 0.68f, 0.04f);
        public static Color NotASource = new Color(0.29f, 0.36f, 0.32f, 0.45f);
        public static Color Marked = new Color(0.29f, 0.36f, 0.32f, 0.45f);
        // highlighted
        public static Color SourceHighlighted = new Color(1f, 1f, 0f, 1f);
        public static Color NotASourceHighlighted = new Color(.29f, 0.36f, 0.32f, 1f);
        public static Color NoLatencyDataHighlighted = new Color(0.72f, 0.74f, 0.68f, 1f);
        public static Color NormalHighlighted = new Color(0.54f, 0.15f, 0.15f, 1f);
        public static Color BlackListedHighlighted = new Color(0, 0, 0, 1f);
        public static Color ExcludedHighlighted = new Color(1f, 1f, 1f, 1f);
        public static Color NegativeHighlighted = new Color(0, 0, 1f, 1f);
        public static Color PositiveHighlighted = new Color(1f, 0, 0, 1f);
        public static Color NonePosHighlighted = new Color(0, 0, 1f, 0);
        public static Color NoneNegHighlighted = new Color(1f, 0, 0, 0);
        public static Color MarkedHighlighted = new Color(0.29f, 0.36f, 0.32f, 0.45f); 
    }

    public struct SiteShaderInput
    {
        // normal
        public static SiteShaderInfo BlackListed = new SiteShaderInfo(SiteColors.BlackListed, 0f);
        public static SiteShaderInfo Excluded = new SiteShaderInfo(SiteColors.Excluded, 0f);
        public static SiteShaderInfo Negative = new SiteShaderInfo(SiteColors.Negative, 0f);
        public static SiteShaderInfo Normal = new SiteShaderInfo(SiteColors.Normal, 0f);
        public static SiteShaderInfo Positive = new SiteShaderInfo(SiteColors.Positive, 0f);
        public static SiteShaderInfo Source = new SiteShaderInfo(SiteColors.Source, 0f);
        public static SiteShaderInfo NotASource = new SiteShaderInfo(SiteColors.NotASource, 0f);
        public static SiteShaderInfo NoLatencyData = new SiteShaderInfo(SiteColors.NoLatencyData, 0f);
        public static SiteShaderInfo NoneNeg = new SiteShaderInfo(SiteColors.NoneNeg, 0f);
        public static SiteShaderInfo NonePos = new SiteShaderInfo(SiteColors.NonePos, 0f);
        public static SiteShaderInfo Marked = new SiteShaderInfo(SiteColors.Marked, 0f);
        // highlighted
        public static SiteShaderInfo NoneNegHighlighted = new SiteShaderInfo(SiteColors.NoneNegHighlighted, 0f);
        public static SiteShaderInfo NonePosHighlighted = new SiteShaderInfo(SiteColors.NonePosHighlighted, 0f);
        public static SiteShaderInfo NormalHighlighted = new SiteShaderInfo(SiteColors.NormalHighlighted, 1f);
        public static SiteShaderInfo BlackListedHighlighted = new SiteShaderInfo(SiteColors.BlackListedHighlighted, 1f);
        public static SiteShaderInfo ExcludedHighlighted = new SiteShaderInfo(SiteColors.ExcludedHighlighted, 1f);
        public static SiteShaderInfo NegativeHighlighted = new SiteShaderInfo(SiteColors.NegativeHighlighted, 1f);
        public static SiteShaderInfo PositiveHighlighted = new SiteShaderInfo(SiteColors.PositiveHighlighted, 1f);
        public static SiteShaderInfo SourceHighlighted = new SiteShaderInfo(SiteColors.SourceHighlighted, 0f);
        public static SiteShaderInfo NotASourceHighlighted = new SiteShaderInfo(SiteColors.NotASourceHighlighted, 0f);
        public static SiteShaderInfo NoLatencyDataHighlighted = new SiteShaderInfo(SiteColors.NoLatencyDataHighlighted, 0f);
        public static SiteShaderInfo MarkedHighlighted = new SiteShaderInfo(SiteColors.MarkedHighlighted, 0f);
    }

    [SerializeField]
    public struct SiteI
    {
        public bool columnMask;     /**< is the site masked on the column ? */
        public bool exclude;        /**< is the site excluded ? */
        public bool blackList;      /**< is the site blacklisted ? */
        public bool columnROI;      /**< is the site in a ROI ? */
        public bool highlight;      /**< is the site highlighted ? */

        public int idGlobal;        /**< INUSED global plot id (all patients) */
        public int idPlotPatient;   /**< INUSED plot id of the patient */

        public int idPatient;       /**< INUSED patient id */
        public int idElectrode;     /**< INUSED electrode id of the patient */
        public int idSite;          /**< INUSED plot id of the electrode */

        public int labelMarsAtlas;  /**< INUSED  label (corresponding index) mars atlas */
        public int greyWhiteMatter; /**< INUSED  */

        public string patientName;  /**< patient name */
        public string name;         /**< plot full name */
    }

    /// <summary>
    /// Class for defining informations about a plot
    /// </summary>
    public class Site : MonoBehaviour
    {
        public bool firstUse = true;
        public bool isActive;       /**< activity boolean (mimic gameObject.activeSelf, because it's access is to slow) */

        public bool columnMask;     /**< is the site masked on the column ? */
        public bool exclude;        /**< is the site excluded ? */
        public bool blackList;      /**< is the site blacklisted ? */
        public bool columnROI;      /**< is the site in a ROI ? */
        public bool highlight;      /**< is the site highlighted ? */
        public bool marked;         /**< is the site marked ? */

        public int idGlobal;        /**< global site id (all patients) */
        public int idSitePatient;   /**< site id of the patient */

        public int idPatient;       /**< patient id */
        public int idElectrode;     /**< electrode id of the patient */
        public int idSite;          /**< site id of the electrode */

        public int labelMarsAtlas;  /**< label (corresponding index) mars atlas */
        public int greyWhiteMatter;

        public string patientName;  /**< patient name */
        public string fullName;     /**< site full name */

        public static SiteShaderInfo instance_shader_info(bool hightlighted, SiteType siteType)
        {
            SiteShaderInfo site = new SiteShaderInfo();

            if (!hightlighted)
            {
                switch (siteType)
                {
                    case SiteType.Normal: return SiteShaderInput.Normal;
                    case SiteType.Positive: return SiteShaderInput.Positive;
                    case SiteType.Negative: return SiteShaderInput.Negative;
                    case SiteType.Excluded: return SiteShaderInput.Excluded;
                    case SiteType.Source: return SiteShaderInput.Source;
                    case SiteType.NotASource: return SiteShaderInput.NotASource;
                    case SiteType.NoLatencyData: return SiteShaderInput.NoLatencyData;
                    case SiteType.BlackListed: return SiteShaderInput.BlackListed;
                    case SiteType.NonePos: return SiteShaderInput.NonePos;
                    case SiteType.NoneNeg: return SiteShaderInput.NoneNeg;
                    case SiteType.Marked: return SiteShaderInput.Marked;
                }
            }
            else
            {
                switch (siteType)
                {
                    case SiteType.Normal: return SiteShaderInput.NormalHighlighted;
                    case SiteType.Positive: return SiteShaderInput.PositiveHighlighted;
                    case SiteType.Negative: return SiteShaderInput.NegativeHighlighted;
                    case SiteType.Excluded: return SiteShaderInput.ExcludedHighlighted;
                    case SiteType.Source: return SiteShaderInput.SourceHighlighted;
                    case SiteType.NotASource: return SiteShaderInput.NotASourceHighlighted;
                    case SiteType.NoLatencyData: return SiteShaderInput.NoLatencyDataHighlighted;
                    case SiteType.BlackListed: return SiteShaderInput.BlackListedHighlighted;
                    case SiteType.NonePos: return SiteShaderInput.NonePosHighlighted;
                    case SiteType.NoneNeg: return SiteShaderInput.NoneNegHighlighted;
                    case SiteType.Marked: return SiteShaderInput.MarkedHighlighted;
                }
            }

            return site;
        }
    }
}