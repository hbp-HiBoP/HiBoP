using System;
using System.Linq;
using System.Text.RegularExpressions;
using HBP.Core.Data;
using HBP.Core.DLL;
using HBP.Core.Exceptions;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Data.Tools;

namespace HBP.Data.Module3D
{
    /// <summary>Scientific site predicates shared by controls and restored scenes.</summary>
    public sealed class SiteConditions
    {
        private readonly Base3DScene m_Scene;
        public SiteConditions(Base3DScene scene) => m_Scene = scene;

        public Func<Core.Object3D.Site, bool> Parse(string text)
        {
            var expression = BooleanExpressionParser.Parse(text.Replace("\n", "").Replace("\r", ""));
            return site =>
            {
                foreach (var value in expression.GetAllBooleanValuesUnderThisOne())
                    value.SetBooleanValue(condition => ParseConditionAndCheckValue(site, condition));
                return expression.Evaluate();
            };
        }

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
        public const string ATLAS_AREA = "ATLAS_AREA";
        public const string POS_X = "X";
        public const string POS_Y = "Y";
        public const string POS_Z = "Z";
        public const string NAME = "NAME";
        public const string PATIENT_NAME = "PAT_NAME";
        public const string TAG = "TAG";
        public const string MEAN = "MEAN";
        public const string MEDIAN = "MEDIAN";
        public const string MAX = "MAX";
        public const string MIN = "MIN";
        public const string STANDARD_DEVIATION = "STDEV";

        /// <summary>
        /// Check if the site is highlighted
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <returns>True if the site is highlighted</returns>
        public bool CheckHighlighted(Core.Object3D.Site site)
        {
            return site.State.IsHighlighted;
        }

        /// <summary>
        /// Check if the site is blacklisted
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <returns>True if the site is blacklisted</returns>
        public bool CheckBlacklisted(Core.Object3D.Site site)
        {
            return site.State.IsBlackListed;
        }

        /// <summary>
        /// Check if the site has a label which contains the input string
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="label">Label to use for the checking</param>
        /// <returns>True if the site has a label which contains the input string</returns>
        public bool CheckLabel(Core.Object3D.Site site, string label)
        {
            return site.State.Labels.Any(l => l.ToLower().Contains(label.ToLower()));
        }

        /// <summary>
        /// Check if the site is in the currently selected ROI
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <returns>True if the site is in the currently selected ROI</returns>
        public bool CheckInROI(Core.Object3D.Site site)
        {
            return !site.State.IsOutOfROI;
        }

        /// <summary>
        /// Check if the site is in the currently selected mesh
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <returns>True if the site is in the currently selected mesh</returns>
        public bool CheckInMesh(Core.Object3D.Site site)
        {
            return m_Scene.MeshManager.SelectedMesh.SimplifiedBoth.IsPointInside(site.Information.DefaultPosition);
        }

        /// <summary>
        /// Check if the site is in the left hemisphere of the currently selected mesh
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <returns>True if the site is in the left hemisphere of the currently selected mesh</returns>
        public bool CheckInLeftHemisphere(Core.Object3D.Site site)
        {
            if (m_Scene.MeshManager.SelectedMesh is Core.Object3D.LeftRightMesh3D mesh)
            {
                return mesh.SimplifiedLeft.IsPointInside(site.Information.DefaultPosition);
            }
            else
            {
                throw new InvalidBasicConditionException("The selected mesh is a single file mesh.\nYou can not filter by hemisphere.");
            }
        }

        /// <summary>
        /// Check if the site is in the right hemisphere of the currently selected mesh
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <returns>True if the site is in the right hemisphere of the currently selected mesh</returns>
        public bool CheckInRightHemisphere(Core.Object3D.Site site)
        {
            if (m_Scene.MeshManager.SelectedMesh is Core.Object3D.LeftRightMesh3D mesh)
            {
                return mesh.SimplifiedRight.IsPointInside(site.Information.DefaultPosition);
            }
            else
            {
                throw new InvalidBasicConditionException("The selected mesh is a single file mesh.\nYou can not filter by hemisphere.");
            }
        }

        /// <summary>
        /// Check if the site is on any cut plane of the scene
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <returns>True if the site is on any cut plane of the scene</returns>
        public bool CheckOnPlane(Core.Object3D.Site site)
        {
            return m_Scene.ImplantationManager.SelectedImplantation.RawSiteList.IsSiteOnAnyPlane(site, from cut in m_Scene.Cuts select cut as Core.DLL.Plane, 1.0f);
        }

        /// <summary>
        /// Check if the site is in a specific area of the selected atlas
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="areaName">Name to use for the checking</param>
        /// <returns>True if the site is in the specified area of the selected atlas</returns>
        public bool CheckAtlas(Core.Object3D.Site site, string areaName)
        {
            if (m_Scene.AtlasManager.SelectedAtlas != null)
            {
                int areaID = m_Scene.AtlasManager.SelectedAtlas.GetClosestAreaIndex(site.Information.DefaultPosition, 2);
                if (int.TryParse(areaName, out int comparedID))
                {
                    if (areaID == comparedID) return true;
                }

                string[] areaInformation = m_Scene.AtlasManager.SelectedAtlas.GetInformation(areaID);
                if (m_Scene.AtlasManager.SelectedAtlas is MarsAtlas && areaInformation.Length == 5)
                {
                    // Check in name
                    if (areaInformation[0].ToUpper().Contains(areaName.ToUpper())) return true;
                    // Check in full name
                    if (areaInformation[4].ToUpper().Contains(areaName.ToUpper())) return true;
                }
                else if (m_Scene.AtlasManager.SelectedAtlas is JuBrainAtlas && areaInformation.Length == 1 && !string.IsNullOrEmpty(areaInformation[0]))
                {
                    // Check in area name
                    if (areaInformation[0].ToUpper().Contains(areaName.ToUpper())) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if the position of the site is above or under the input value
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="superior">True if the condition is "above", false otherwise</param>
        /// <param name="stringValue">Value to be compared</param>
        /// <returns>True if the mean of the values matches the condition</returns>
        public bool CheckX(Core.Object3D.Site site, bool superior, string stringValue)
        {
            return CompareValue(-site.Information.DefaultPosition.x, superior, stringValue);
        }

        /// <summary>
        /// Check if the position of the site is above or under the input value
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="superior">True if the condition is "above", false otherwise</param>
        /// <param name="stringValue">Value to be compared</param>
        /// <returns>True if the mean of the values matches the condition</returns>
        public bool CheckY(Core.Object3D.Site site, bool superior, string stringValue)
        {
            return CompareValue(site.Information.DefaultPosition.y, superior, stringValue);
        }

        /// <summary>
        /// Check if the position of the site is above or under the input value
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="superior">True if the condition is "above", false otherwise</param>
        /// <param name="stringValue">Value to be compared</param>
        /// <returns>True if the mean of the values matches the condition</returns>
        public bool CheckZ(Core.Object3D.Site site, bool superior, string stringValue)
        {
            return CompareValue(site.Information.DefaultPosition.z, superior, stringValue);
        }

        /// <summary>
        /// Check if the site name contains the input string
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="name">Name to use for the checking</param>
        /// <returns>True if the site name contains the input string</returns>
        public bool CheckName(Core.Object3D.Site site, string name)
        {
            return site.Information.Name.ToUpper().Contains(name.ToUpper());
        }

        /// <summary>
        /// Check if the site patient name contains the input string
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="areaName">Name to use for the checking</param>
        /// <returns>True if the site patient name contains the input string</returns>
        public bool CheckPatientName(Core.Object3D.Site site, string patientName)
        {
            return site.Information.Patient.Name.ToUpper().Contains(patientName.ToUpper());
        }

        /// <summary>
        /// Check if the site has the input tag and if the value of this tag contains the input string
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="tag">Tag to be checked</param>
        /// <param name="tagValueToCompare">Value to be compared to the value of the tag</param>
        /// <returns></returns>
        public bool CheckTag(Core.Object3D.Site site, Core.Data.BaseTag tag, string tagValueToCompare)
        {
            if (tag != null)
            {
                Core.Data.BaseTagValue tagValue = site.Information.SiteData.Tags.FirstOrDefault(t => t.Tag == tag);
                if (tagValue != null)
                {
                    return tagValue.DisplayableValue.ToUpper().Contains(tagValueToCompare.ToUpper());
                }
                else return false;
            }
            else return false;
        }

        // TODO : Consider start and end for all following methods
        /// <summary>
        /// Check if the mean of the values of the associated channel if above or under the input value
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="superior">True if the condition is "above", false otherwise</param>
        /// <param name="stringValue">Value to be compared</param>
        /// <returns>True if the mean of the values matches the condition</returns>
        public bool CheckMean(Core.Object3D.Site site, bool superior, string stringValue, int start = 0, int end = 0)
        {
            if (site.Statistics != null)
            {
                float[] allValues = site.Statistics.Trial.AllValues;
                if (allValues.Length > 0)
                {
                    return CompareValue(allValues.Mean(), superior, stringValue);
                }
            }

            return false;
        }

        /// <summary>
        /// Check if the median of the values of the associated channel if above or under the input value
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="superior">True if the condition is "above", false otherwise</param>
        /// <param name="stringValue">Value to be compared</param>
        /// <returns>True if the median of the values matches the condition</returns>
        public bool CheckMedian(Core.Object3D.Site site, bool superior, string stringValue, int start = 0, int end = 0)
        {
            if (site.Statistics != null)
            {
                float[] allValues = site.Statistics.Trial.AllValues;
                if (allValues.Length > 0)
                {
                    return CompareValue(allValues.Median(), superior, stringValue);
                }
            }

            return false;
        }

        /// <summary>
        /// Check if the maximum value of the associated channel if above or under the input value
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="superior">True if the condition is "above", false otherwise</param>
        /// <param name="stringValue">Value to be compared</param>
        /// <returns>True if the maximum value matches the condition</returns>
        public bool CheckMax(Core.Object3D.Site site, bool superior, string stringValue, int start = 0, int end = 0)
        {
            if (site.Statistics != null)
            {
                float[] allValues = site.Statistics.Trial.AllValues;
                if (allValues.Length > 0)
                {
                    return CompareValue(allValues.Max(), superior, stringValue);
                }
            }

            return false;
        }

        /// <summary>
        /// Check if the minimum value of the associated channel if above or under the input value
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="superior">True if the condition is "above", false otherwise</param>
        /// <param name="stringValue">Value to be compared</param>
        /// <returns>True if the minimum value matches the condition</returns>
        public bool CheckMin(Core.Object3D.Site site, bool superior, string stringValue, int start = 0, int end = 0)
        {
            if (site.Statistics != null)
            {
                float[] allValues = site.Statistics.Trial.AllValues;
                if (allValues.Length > 0)
                {
                    return CompareValue(allValues.Min(), superior, stringValue);
                }
            }

            return false;
        }

        /// <summary>
        /// Check if the standard deviation of the associated channel if above or under the input value
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="superior">True if the condition is "above", false otherwise</param>
        /// <param name="stringValue">Value to be compared</param>
        /// <returns>True if the standard deviation matches the condition</returns>
        public bool CheckStandardDeviation(Core.Object3D.Site site, bool superior, string stringValue, int start = 0, int end = 0)
        {
            if (site.Statistics != null)
            {
                float[] allValues = site.Statistics.Trial.AllValues;
                if (allValues.Length > 0)
                {
                    return CompareValue(allValues.StandardDeviation(), superior, stringValue);
                }
            }

            return false;
        }

        /// <summary>
        /// Compare two values (one is a float, the other is a parsable to float string)
        /// </summary>
        /// <param name="value">Float value to compare</param>
        /// <param name="superior">True if the comparison is "greater than", false otherwise</param>
        /// <param name="stringValueToCompare">String value to compare</param>
        /// <returns>True if the float value matches the condition</returns>
        private bool CompareValue(float value, bool superior, string stringValueToCompare)
        {
            if (NumberExtension.TryParseFloat(stringValueToCompare, out float valueToCompare))
            {
                return superior ? value > valueToCompare : value < valueToCompare;
            }
            else
            {
                throw new ParsingValueException(stringValueToCompare);
            }
        }

        private bool ParseConditionAndCheckValue(Core.Object3D.Site site, string s)
        {
            s = s.ToUpper();
            if (s.Contains("=") || s.Contains(">") || s.Contains("<"))
            {
                string[] elements = s.Split('=', '<', '>');
                if (elements.Length == 2)
                {
                    string label = elements[0].Replace(" ", "").Replace("\"", "").Replace("[", "").Replace("]", "");
                    string value = elements[1].Replace("\"", "");
                    string deblankedValue = Regex.Replace(value, "^\\s+", "");
                    deblankedValue = Regex.Replace(deblankedValue, "\\s+$", "");
                    if (label == LABEL)
                    {
                        return CheckLabel(site, deblankedValue);
                    }
                    else if (label == NAME)
                    {
                        return CheckName(site, deblankedValue);
                    }
                    else if (label == PATIENT_NAME)
                    {
                        return CheckPatientName(site, deblankedValue);
                    }
                    else if (label == ATLAS_AREA)
                    {
                        return CheckAtlas(site, deblankedValue);
                    }
                    else if (label == POS_X)
                    {
                        if (s.Contains("<"))
                        {
                            return CheckX(site, false, deblankedValue);
                        }
                        else if (s.Contains(">"))
                        {
                            return CheckX(site, true, deblankedValue);
                        }
                    }
                    else if (label == POS_Y)
                    {
                        if (s.Contains("<"))
                        {
                            return CheckY(site, false, deblankedValue);
                        }
                        else if (s.Contains(">"))
                        {
                            return CheckY(site, true, deblankedValue);
                        }
                    }
                    else if (label == POS_Z)
                    {
                        if (s.Contains("<"))
                        {
                            return CheckZ(site, false, deblankedValue);
                        }
                        else if (s.Contains(">"))
                        {
                            return CheckZ(site, true, deblankedValue);
                        }
                    }
                    else if (label == TAG)
                    {
                        string[] splits = deblankedValue.Split(':');
                        if (splits.Length == 2)
                        {
                            string tagName = Regex.Replace(splits[0], "^\\s+", "");
                            tagName = Regex.Replace(tagName, "\\s+$", "");
                            string tagValue = Regex.Replace(splits[1], "^\\s+", "");
                            tagValue = Regex.Replace(tagValue, "\\s+$", "");
                            BaseTag tag = PersistentDataManager.Tags.SitesTags.FirstOrDefault(t => t.Name.ToUpper() == tagName);
                            if (tag == null) tag = PersistentDataManager.Tags.GeneralTags.FirstOrDefault(t => t.Name.ToUpper() == tagName);
                            return CheckTag(site, tag, tagValue);
                        }
                    }
                    else if (label == MEAN)
                    {
                        if (s.Contains("<"))
                        {
                            return CheckMean(site, false, deblankedValue);
                        }
                        else if (s.Contains(">"))
                        {
                            return CheckMean(site, true, deblankedValue);
                        }
                    }
                    else if (label == MEDIAN)
                    {
                        if (s.Contains("<"))
                        {
                            return CheckMedian(site, false, deblankedValue);
                        }
                        else if (s.Contains(">"))
                        {
                            return CheckMedian(site, true, deblankedValue);
                        }
                    }
                    else if (label == MAX)
                    {
                        if (s.Contains("<"))
                        {
                            return CheckMax(site, false, deblankedValue);
                        }
                        else if (s.Contains(">"))
                        {
                            return CheckMax(site, true, deblankedValue);
                        }
                    }
                    else if (label == MIN)
                    {
                        if (s.Contains("<"))
                        {
                            return CheckMin(site, false, deblankedValue);
                        }
                        else if (s.Contains(">"))
                        {
                            return CheckMin(site, true, deblankedValue);
                        }
                    }
                    else if (label == STANDARD_DEVIATION)
                    {
                        if (s.Contains("<"))
                        {
                            return CheckStandardDeviation(site, false, deblankedValue);
                        }
                        else if (s.Contains(">"))
                        {
                            return CheckStandardDeviation(site, true, deblankedValue);
                        }
                    }
                    else if (label.StartsWith(MEAN) || label.StartsWith(MEDIAN) || label.StartsWith(MAX) || label.StartsWith(MIN) || label.StartsWith(STANDARD_DEVIATION))
                    {
                        Regex regex = new("(\\w+){(\\d+):(\\d+)}");
                        Match match = regex.Match(label);
                        if (match.Success)
                        {
                            string subLabel = match.Groups[1].ToString();
                            if (int.TryParse(match.Groups[2].ToString(), out int start) && int.TryParse(match.Groups[3].ToString(), out int end) && site.GetComponentInParent<Column3DDynamic>() is Column3DDynamic dynamicColumn)
                            {
                                int startIndex = dynamicColumn.Timeline.Frequency.ConvertToFlooredNumberOfSamples(start);
                                int endIndex = dynamicColumn.Timeline.Frequency.ConvertToCeiledNumberOfSamples(end);
                                if (subLabel == MEAN)
                                {
                                    if (s.Contains("<"))
                                    {
                                        return CheckMean(site, false, deblankedValue, start, end);
                                    }
                                    else if (s.Contains(">"))
                                    {
                                        return CheckMean(site, true, deblankedValue, start, end);
                                    }
                                }
                                else if (subLabel == MEDIAN)
                                {
                                    if (s.Contains("<"))
                                    {
                                        return CheckMedian(site, false, deblankedValue, start, end);
                                    }
                                    else if (s.Contains(">"))
                                    {
                                        return CheckMedian(site, true, deblankedValue, start, end);
                                    }
                                }
                                else if (subLabel == MAX)
                                {
                                    if (s.Contains("<"))
                                    {
                                        return CheckMax(site, false, deblankedValue, start, end);
                                    }
                                    else if (s.Contains(">"))
                                    {
                                        return CheckMax(site, true, deblankedValue, start, end);
                                    }
                                }
                                else if (subLabel == MIN)
                                {
                                    if (s.Contains("<"))
                                    {
                                        return CheckMin(site, false, deblankedValue, start, end);
                                    }
                                    else if (s.Contains(">"))
                                    {
                                        return CheckMin(site, true, deblankedValue, start, end);
                                    }
                                }
                                else if (subLabel == STANDARD_DEVIATION)
                                {
                                    if (s.Contains("<"))
                                    {
                                        return CheckStandardDeviation(site, false, deblankedValue, start, end);
                                    }
                                    else if (s.Contains(">"))
                                    {
                                        return CheckStandardDeviation(site, true, deblankedValue, start, end);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                s = s.Replace(" ", "").Replace("\"", "").Replace("[", "").Replace("]", "");
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
            }

            throw new InvalidConditionException(s);
        }
    }
}
