using HBP.Theme;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HBP.UI.Tools;
using HBP.Core.Tools;

namespace HBP.UI.Tools
{
    public static class UITools
    {
        public static void SetIEnumerableFieldInItem(this UnityEngine.UI.Text text, string title, IEnumerable<string> values, State emptyState, int max = 3)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(string.Format("{0} :", title));
            stringBuilder.Append(values.ToTooltip(max));
            var themeElement = text.GetComponent<ThemeElement>();
            if (themeElement)
            {
                if (values.Count() == 0) themeElement.Set(emptyState);
                else themeElement.Set();
            }
            var tooltip = text.GetComponent<Tooltip>();
            if (tooltip) tooltip.Text = stringBuilder.ToString();
            text.text = values.Count().ToString();
        }
        public static void SetIEnumerableFieldInItem(this UnityEngine.UI.Text text, int size, State emptyState)
        {
            var themeElement = text.GetComponent<ThemeElement>();
            if (themeElement)
            {
                if (size == 0) themeElement.Set(emptyState);
                else themeElement.Set();
            }
            text.text = size.ToString();
        }
    }
}