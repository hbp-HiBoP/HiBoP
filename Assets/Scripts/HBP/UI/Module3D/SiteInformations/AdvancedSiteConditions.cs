using HBP.Module3D;
using Tools.CSharp.BooleanExpressionParser;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// Class used to define a set of conditions to check on the sites from a string
    /// </summary>
    public class AdvancedSiteConditions : BaseSiteConditions
    {
        #region Properties
        public const string TRUE = "TRUE";
        public const string FALSE = "FALSE";
        public const string HIGHLIGHTED = "H";
        public const string BLACKLISTED = "B";
        public const string LABEL = "LABEL";
        public const string IN_ROI = "ROI";
        public const string IN_MESH = "MESH";
        public const string IN_LEFT_HEMISPHERE = "L";
        public const string IN_RIGHT_HEMISPHERE = "R";
        public const string ON_PLANE = "CUT";
        public const string NAME = "NAME";
        public const string PATIENT_NAME = "PAT_NAME";
        public const string PATIENT_PLACE = "PAT_PLACE";
        public const string PATIENT_DATE = "PAT_DATE";
        public const string MARS_ATLAS = "MA";
        public const string BROADMAN = "BA";
        public const string FREESURFER = "FS";
        public const string MEAN = "MEAN";
        public const string MEDIAN = "MEDIAN";
        public const string MAX = "MAX";
        public const string MIN = "MIN";
        public const string STANDARD_DEVIATION = "STDEV";

        /// <summary>
        /// InputField used to write the string to be parsed as a set of conditions
        /// </summary>
        [SerializeField] InputField m_InputField;
        /// <summary>
        /// Boolean expression parsed from the string
        /// </summary>
        private BooleanExpression m_BooleanExpression;
        #endregion

        #region Private Methods
        /// <summary>
        /// Check all the set conditions for a specific site
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <returns>True if the conditions are met</returns>
        protected override bool CheckConditions(Site site)
        {
            foreach (var booleanValue in m_BooleanExpression.GetAllBooleanValuesUnderThisOne())
            {
                booleanValue.SetBooleanValue((s) => ParseConditionAndCheckValue(site, s));
            }
            return m_BooleanExpression.Evaluate();
        }
        /// <summary>
        /// Parse the string containing the conditions and get the value 
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="s">String to be parsed</param>
        /// <returns>True if the site matches the set of conditions</returns>
        private bool ParseConditionAndCheckValue(Site site, string s)
        {
            s = s.ToUpper();
            s = s.Replace(" ", "");
            if (s == TRUE)
            {
                return true;
            }
            else if (s == FALSE)
            {
                return false;
            }
            else if (s == HIGHLIGHTED)
            {
                return CheckHighlighted(site);
            }
            else if (s == BLACKLISTED)
            {
                return CheckBlacklisted(site);
            }
            else if (s.Contains(LABEL))
            {
                string[] array = s.Split('=');
                if (array.Length == 2)
                {
                    return CheckLabel(site, array[1].Replace("\"", ""));
                }
            }
            else if (s == IN_ROI)
            {
                return CheckInROI(site);
            }
            else if (s == IN_MESH)
            {
                return CheckInMesh(site);
            }
            else if (s == IN_LEFT_HEMISPHERE)
            {
                return CheckInLeftHemisphere(site);
            }
            else if (s == IN_RIGHT_HEMISPHERE)
            {
                return CheckInRightHemisphere(site);
            }
            else if (s == ON_PLANE)
            {
                return CheckOnPlane(site);
            }
            else if (s.Contains(NAME))
            {
                string[] array = s.Split('=');
                if (array.Length == 2)
                {
                    return CheckName(site, array[1].Replace("\"", ""));
                }
            }
            else if (s.Contains(PATIENT_NAME))
            {
                string[] array = s.Split('=');
                if (array.Length == 2)
                {
                    return CheckPatientName(site, array[1].Replace("\"", ""));
                }
            }
            else if (s.Contains(PATIENT_PLACE))
            {
                string[] array = s.Split('=');
                if (array.Length == 2)
                {
                    return CheckPatientPlace(site, array[1].Replace("\"", ""));
                }
            }
            else if (s.Contains(PATIENT_DATE))
            {
                string[] array = s.Split('=');
                if (array.Length == 2)
                {
                    return CheckPatientDate(site, array[1].Replace("\"", ""));
                }
            }
            else if (s.Contains(MARS_ATLAS))
            {
                string[] array = s.Split('=');
                if (array.Length == 2)
                {
                    return CheckMarsAtlasName(site, array[1].Replace("\"", ""));
                }
            }
            else if (s.Contains(BROADMAN))
            {
                string[] array = s.Split('=');
                if (array.Length == 2)
                {
                    return CheckBroadmanAreaName(site, array[1].Replace("\"", ""));
                }
            }
            else if (s.Contains(FREESURFER))
            {
                string[] array = s.Split('=');
                if (array.Length == 2)
                {
                    return CheckFreesurferName(site, array[1].Replace("\"", ""));
                }
            }
            else if (s.Contains(MEAN))
            {
                if (s.Contains("<"))
                {
                    string[] array = s.Split('<');
                    if (array.Length == 2)
                    {
                        return CheckMean(site, false, array[1].Replace("\"", "").Replace(" ", ""));
                    }
                }
                else if (s.Contains(">"))
                {
                    string[] array = s.Split('>');
                    if (array.Length == 2)
                    {
                        return CheckMean(site, true, array[1].Replace("\"", "").Replace(" ", ""));
                    }
                }
            }
            else if (s.Contains(MEDIAN))
            {
                if (s.Contains("<"))
                {
                    string[] array = s.Split('<');
                    if (array.Length == 2)
                    {
                        return CheckMedian(site, false, array[1].Replace("\"", "").Replace(" ", ""));
                    }
                }
                else if (s.Contains(">"))
                {
                    string[] array = s.Split('>');
                    if (array.Length == 2)
                    {
                        return CheckMedian(site, true, array[1].Replace("\"", "").Replace(" ", ""));
                    }
                }
            }
            else if (s.Contains(MAX))
            {
                if (s.Contains("<"))
                {
                    string[] array = s.Split('<');
                    if (array.Length == 2)
                    {
                        return CheckMax(site, false, array[1].Replace("\"", "").Replace(" ", ""));
                    }
                }
                else if (s.Contains(">"))
                {
                    string[] array = s.Split('>');
                    if (array.Length == 2)
                    {
                        return CheckMax(site, true, array[1].Replace("\"", "").Replace(" ", ""));
                    }
                }
            }
            else if (s.Contains(MIN))
            {
                if (s.Contains("<"))
                {
                    string[] array = s.Split('<');
                    if (array.Length == 2)
                    {
                        return CheckMin(site, false, array[1].Replace("\"", "").Replace(" ", ""));
                    }
                }
                else if (s.Contains(">"))
                {
                    string[] array = s.Split('>');
                    if (array.Length == 2)
                    {
                        return CheckMin(site, true, array[1].Replace("\"", "").Replace(" ", ""));
                    }
                }
            }
            else if (s.Contains(STANDARD_DEVIATION))
            {
                if (s.Contains("<"))
                {
                    string[] array = s.Split('<');
                    if (array.Length == 2)
                    {
                        return CheckStandardDeviation(site, false, array[1].Replace("\"", "").Replace(" ", ""));
                    }
                }
                else if (s.Contains(">"))
                {
                    string[] array = s.Split('>');
                    if (array.Length == 2)
                    {
                        return CheckStandardDeviation(site, true, array[1].Replace("\"", "").Replace(" ", ""));
                    }
                }
            }
            throw new InvalidAdvancedConditionException(s);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Parse the whole string and store it to a BooleanExpression object
        /// </summary>
        public void ParseConditions()
        {
            m_BooleanExpression = Parser.Parse(m_InputField.text);
        }
        #endregion
    }
}