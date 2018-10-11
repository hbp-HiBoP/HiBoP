



// unity
using System;
/**
* \file    Site.cs
* \author  Lance Florian
* \date    2015
* \brief   Define site related classes
*/
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Module3D
{
    public enum SiteType { Normal, Positive, Negative, Excluded, Source, NotASource, NoLatencyData, BlackListed, NonePos, NoneNeg, Marked, Suspicious };
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
            SitePatientID = info.SitePatientID;
            PatientNumber = info.PatientNumber;
            ElectrodeNumber = info.ElectrodeNumber;
            SiteNumber = info.SiteNumber;
            MarsAtlasIndex = info.MarsAtlasIndex;
        }
        public SiteInformation() { }

        public Data.Patient Patient { get; set; }
        public string Name { get; set; }

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
        public string FullCorrectedID
        {
            get
            {
                if (ApplicationState.UserPreferences.Data.Anatomic.SiteNameCorrection)
                {
                    string siteName = Name.ToUpper();
                    int prime = siteName.LastIndexOf('P');
                    if (prime > 0)
                    {
                        return PatientID + "_" + siteName.Remove(prime, 1).Insert(prime, "\'");
                    }
                    else
                    {
                        return PatientID + "_" + siteName;
                    }
                }
                else
                {
                    return FullID;
                }
            }
        }
        public string DisplayedName
        {
            get
            {
                return ChannelName + " (" + Patient.Name + " - " + Patient.Place + " - " + Patient.Date + ")";
            }
        }
        public string ChannelName
        {
            get { return FullCorrectedID.Replace(PatientID + "_", ""); }
        }
    }

    public class SiteState
    {
        public UnityEvent OnChangeState = new UnityEvent();
        public SiteState(SiteState state)
        {
            IsMasked = state.IsMasked;
            IsExcluded = state.IsExcluded;
            IsBlackListed = state.IsBlackListed;
            IsOutOfROI = state.IsOutOfROI;
            IsHighlighted = state.IsHighlighted;
            IsMarked = state.IsMarked;
            IsSuspicious = state.IsSuspicious;
        }
        public SiteState() { }
        private bool m_IsMasked;
        public bool IsMasked
        {
            get
            {
                return m_IsMasked;
            }
            set
            {
                m_IsMasked = value;
            }
        }
        private bool m_IsExcluded;
        public bool IsExcluded
        {
            get
            {
                return m_IsExcluded;
            }
            set
            {
                m_IsExcluded = value;
                OnChangeState.Invoke();
            }
        }
        private bool m_IsBlackListed;
        public bool IsBlackListed
        {
            get
            {
                return m_IsBlackListed;
            }
            set
            {
                m_IsBlackListed = value;
                OnChangeState.Invoke();
            }
        }
        private bool m_IsOutOfROI;
        public bool IsOutOfROI
        {
            get
            {
                return m_IsOutOfROI;
            }
            set
            {
                m_IsOutOfROI = value;
            }
        }
        private bool m_IsHighlighted;
        public bool IsHighlighted
        {
            get
            {
                return m_IsHighlighted;
            }
            set
            {
                m_IsHighlighted = value;
                OnChangeState.Invoke();
            }
        }
        private bool m_IsMarked;
        public bool IsMarked
        {
            get
            {
                return m_IsMarked;
            }
            set
            {
                m_IsMarked = value;
                OnChangeState.Invoke();
            }
        }
        private bool m_IsSuspicious;
        public bool IsSuspicious
        {
            get
            {
                return m_IsSuspicious;
            }
            set
            {
                m_IsSuspicious = value;
                OnChangeState.Invoke();
            }
        }
        public void ApplyState(SiteState state)
        {
            ApplyState(state.IsExcluded, state.IsBlackListed, state.IsHighlighted, state.IsMarked, state.IsSuspicious);
        }
        public void ApplyState(bool excluded, bool blacklisted, bool highlighted, bool marked, bool suspicious)
        {
            IsExcluded = excluded;
            IsBlackListed = blacklisted;
            IsHighlighted = highlighted;
            IsMarked = marked;
            IsSuspicious = suspicious;
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
        private bool m_IsInitialized = false;

        bool m_IsActive;
        /// <summary>
        /// Is this site active ?
        /// </summary>
        public bool IsActive
        {
            get
            {
                if (!m_IsInitialized)
                {
                    m_IsActive = gameObject.activeSelf;
                    m_IsInitialized = true;
                }
                return m_IsActive;
            }
            set
            {
                m_IsActive = value;
                gameObject.SetActive(value);
            }
        }

        private bool m_IsSelected;
        public bool IsSelected
        {
            get
            {
                return m_IsSelected;
            }
            set
            {
                if (m_IsSelected != value)
                {
                    m_IsSelected = value;
                    OnSelectSite.Invoke(value);
                }
            }
        }
        public GenericEvent<bool> OnSelectSite = new GenericEvent<bool>();

        /// <summary>
        /// Information about this site
        /// </summary>
        public SiteInformation Information { get; set; }

        /// <summary>
        /// State of this site
        /// </summary>
        public SiteState State { get; set; }

        /// <summary>
        /// Configuration of this site
        /// </summary>
        public Data.Visualization.SiteConfiguration Configuration { get; set; }

        public Data.Visualization.SiteData Data { get; set; }
        #endregion

        #region Private Methods
        private void Awake()
        {
            Information = new SiteInformation();
            State = new SiteState();
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
            State.IsBlackListed = Configuration.IsBlacklisted;
            State.IsExcluded = Configuration.IsExcluded;
            State.IsHighlighted = Configuration.IsHighlighted;
            State.IsMarked = Configuration.IsMarked;
            State.IsSuspicious = Configuration.IsSuspicious;
            if (firstCall) ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }

        public void SaveConfiguration()
        {
            Configuration.IsBlacklisted = State.IsBlackListed;
            Configuration.IsExcluded = State.IsExcluded;
            Configuration.IsHighlighted = State.IsHighlighted;
            Configuration.IsMarked = State.IsMarked;
            Configuration.IsSuspicious = State.IsSuspicious;
        }

        public void ResetConfiguration(bool firstCall = true)
        {
            State.IsBlackListed = false;
            State.IsExcluded = false;
            State.IsHighlighted = false;
            State.IsMarked = false;
            State.IsSuspicious = false;
            if (firstCall) ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        #endregion
    }
}