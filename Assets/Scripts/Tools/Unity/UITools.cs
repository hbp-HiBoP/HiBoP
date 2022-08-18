using HBP.Theme.Components;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tools.CSharp;
using UnityEngine.UI;
using HBP.UI.Tools;

namespace HBP.UI
{
    public static class UITools
    {
        public static void SetIEnumerableFieldInItem(this Text text, string title, IEnumerable<string> values, Theme.State emptyState, int max = 3)
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
        public static void SetIEnumerableFieldInItem(this Text text, int size, Theme.State emptyState)
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