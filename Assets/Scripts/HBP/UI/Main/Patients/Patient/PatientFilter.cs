using HBP.Core.Data;
using HBP.Core.Exceptions;
using HBP.Data.Preferences;
using HBP.UI.Tools;
using System.Linq;
using System.Text.RegularExpressions;

namespace HBP.UI.Main
{
    public class PatientFilter : ListFilter<Patient>
    {
        #region Private Methods
        protected override bool ParseConditionAndCheckValue(Patient obj, string s)
        {
            s = s.ToUpper();
            Regex conditionRegex = new Regex(@"(.+)([=><]{1})(.+)");
            Match match = conditionRegex.Match(s);
            if (match.Success)
            {
                GroupCollection groups = match.Groups;
                string label = groups[1].Value.Replace("\"", "");
                string deblankedLabel = Regex.Replace(label, "^\\s+", "");
                deblankedLabel = Regex.Replace(deblankedLabel, "\\s+$", "");
                string value = groups[3].Value.Replace("\"", "");
                string deblankedValue = Regex.Replace(value, "^\\s+", "");
                deblankedValue = Regex.Replace(deblankedValue, "\\s+$", "");
                if (deblankedLabel == "NAME")
                {
                    if (groups[2].Value == "=")
                        return obj.Name.ToUpper().Contains(deblankedValue);
                    else if (groups[2].Value == ">")
                        return obj.Name.ToUpper().CompareTo(deblankedValue) > 0;
                    else if (groups[2].Value == "<")
                        return obj.Name.ToUpper().CompareTo(deblankedValue) < 0;
                }
                else if (deblankedLabel == "PLACE")
                {
                    if (groups[2].Value == "=")
                        return obj.Place.ToUpper().Contains(deblankedValue);
                    else if (groups[2].Value == ">")
                        return obj.Place.ToUpper().CompareTo(deblankedValue) > 0;
                    else if (groups[2].Value == "<")
                        return obj.Place.ToUpper().CompareTo(deblankedValue) < 0;
                }
                else if (deblankedLabel == "DATE")
                {
                    if (int.TryParse(deblankedValue, out int dateValue))
                    {
                        if (groups[2].Value == "=")
                            return obj.Date == dateValue;
                        else if (groups[2].Value == ">")
                            return obj.Date > dateValue;
                        else if (groups[2].Value == "<")
                            return obj.Date < dateValue;
                    }
                }
                else
                {
                    BaseTag tag = PersistentDataManager.Tags.PatientsTags.FirstOrDefault(t => t.Name.ToUpper() == deblankedLabel) ?? PersistentDataManager.Tags.GeneralTags.FirstOrDefault(t => t.Name.ToUpper() == deblankedLabel);
                    if (tag != null)
                    {
                        BaseTagValue tagValue = obj.Tags.FirstOrDefault(t => t.Tag == tag);
                        if (tagValue != null)
                        {
                            if (groups[2].Value == "=")
                                return tagValue.DisplayableValue.ToUpper().Contains(deblankedValue);
                            else if (groups[2].Value == ">")
                                return tagValue.DisplayableValue.ToUpper().CompareTo(deblankedValue) > 0;
                            else if (groups[2].Value == "<")
                                return tagValue.DisplayableValue.ToUpper().CompareTo(deblankedValue) < 0;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (s == "TRUE")
                {
                    return true;
                }
                else if (s == "FALSE")
                {
                    return false;
                }
            }
            throw new InvalidConditionException(s);
        }
        #endregion
    }
}