



// unity
using System;
/**
* \file    Site.cs
* \author  Lance Florian
* \date    2015
* \brief   Define site related classes
*/
using UnityEngine;

namespace HBP.Module3D
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
    public class SiteInformation
    {
        public SiteInformation(SiteInformation info)
        {
            Patient = info.Patient;
            Name = info.Name;
            IsMasked = info.IsMasked;
            IsExcluded = info.IsExcluded;
            IsBlackListed = info.IsBlackListed;
            IsOutOfROI = info.IsOutOfROI;
            IsHighlighted = info.IsHighlighted;
            IsMarked = info.IsMarked;
            GlobalID = info.GlobalID;
            SitePatientID = info.SitePatientID;
            PatientNumber = info.PatientNumber;
            ElectrodeNumber = info.ElectrodeNumber;
            SiteNumber = info.SiteNumber;
            MarsAtlasIndex = info.MarsAtlasIndex;
        }
        public SiteInformation() { }

        public Data.Patient Patient { get; set; }
        public string Name { get; set; }

        public bool IsMasked { get; set; }     /**< is the site masked on the column ? */
        public bool IsExcluded { get; set; }        /**< is the site excluded ? */
        public bool IsBlackListed { get; set; }      /**< is the site blacklisted ? */
        public bool IsOutOfROI { get; set; }      /**< is the site in a ROI ? */
        public bool IsHighlighted { get; set; }       /**< is the site highlighted ? */
        public bool IsMarked { get; set; }          /**< is the site marked ? */

        public int GlobalID { get; set; }        /**< global site id (all patients) */
        public int SitePatientID { get; set; }    /**< site id of the patient */

        public int PatientNumber { get; set; }       /**< patient id */
        public int ElectrodeNumber { get; set; }     /**< electrode id of the patient */
        public int SiteNumber { get; set; }        /**< site id of the electrode */

        public int MarsAtlasIndex { get; set; }   /**< label (corresponding index) mars atlas */

        public string PatientID
        {
            get
            {
                return Patient.ID;
            }
        }
        public string FullID
        {
            get
            {
                return Patient.ID + "_" + Name;
            }
        }
        public string DisplayedName
        {
            get
            {
                return Patient.Place + " | " + Patient.Date + " | " + Patient.Name + " | " + Name;
            }
        }
    }

    /// <summary>
    /// Class for defining informations about a plot
    /// </summary>
    public class Site : MonoBehaviour, IConfigurable
    {
        #region Properties
        /// <summary>
        /// Has this site been initialized ?
        /// </summary>
        public bool IsInitialized = false;

        bool m_IsActive;
        /// <summary>
        /// Is this site active ?
        /// </summary>
        public bool IsActive
        {
            get
            {
                return m_IsActive;
            }
            set
            {
                m_IsActive = value;
                gameObject.SetActive(value);
            }
        }

        /// <summary>
        /// Information about this site
        /// </summary>
        public SiteInformation Information { get; set; }

        /// <summary>
        /// Configuration of this site
        /// </summary>
        public Data.Visualization.SiteConfiguration Configuration { get; set; }
        #endregion

        #region Private Methods
        private void Awake()
        {
            Information = new SiteInformation();
            Configuration = new Data.Visualization.SiteConfiguration();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hightlighted"></param>
        /// <param name="siteType"></param>
        /// <returns></returns>
        public static SiteShaderInfo InstanceShaderInfo(bool hightlighted, SiteType siteType)
        {
            SiteShaderInfo siteShaderInfo = new SiteShaderInfo();

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

            return siteShaderInfo;
        }

        public void LoadConfiguration(bool firstCall = true)
        {
            if (firstCall) ResetConfiguration(false);
            Information.IsBlackListed = Configuration.IsBlacklisted;
            Information.IsExcluded = Configuration.IsExcluded;
            Information.IsHighlighted = Configuration.IsHighlighted;
            Information.IsMarked = Configuration.IsMarked;
            if (firstCall) ApplicationState.Module3D.OnRequestUpdateInUI.Invoke();
        }

        public void SaveConfiguration()
        {
            Configuration.IsBlacklisted = Information.IsBlackListed;
            Configuration.IsExcluded = Information.IsExcluded;
            Configuration.IsHighlighted = Information.IsHighlighted;
            Configuration.IsMarked = Information.IsMarked;
        }

        public void ResetConfiguration(bool firstCall = true)
        {
            Information.IsBlackListed = false;
            Information.IsExcluded = false;
            Information.IsHighlighted = false;
            Information.IsMarked = false;
            if (firstCall) ApplicationState.Module3D.OnRequestUpdateInUI.Invoke();
        }
        #endregion
    }
}