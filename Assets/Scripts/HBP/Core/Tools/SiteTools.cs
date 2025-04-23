using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using UnityEngine;

namespace HBP.Core.Tools
{
    public class SiteTools
    {
        /// <summary>
        /// Fix the name (P,'p).
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns>Fixed name</returns>
        public static string FixName(string name)
        {
            string siteName = name.ToUpper();
            siteName = siteName.Replace("PLOT", "");
            int prime = siteName.LastIndexOf('P');
            if (prime > 0)
            {
                siteName = siteName.Remove(prime, 1).Insert(prime, "\'");
            }
            for (int i = siteName.Length - 1; i > 0; --i)
            {
                if (siteName[i] == '0' && !char.IsDigit(siteName[i - 1]))
                {
                    siteName = siteName.Remove(i, 1);
                }
            }
            return siteName;
        }
    }

    public class SiteNameComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            bool xValid = TryParse(x, out var xParts);
            bool yValid = TryParse(y, out var yParts);

            if (xValid && yValid)
            {
                // Lettres
                int letterComp = string.Compare(xParts.Letters, yParts.Letters, StringComparison.Ordinal);
                if (letterComp != 0) return letterComp;

                // Prime
                int primeComp = xParts.HasPrime.CompareTo(yParts.HasPrime);
                if (primeComp != 0) return primeComp;

                // Valeur numérique
                int numComp = xParts.NumericValue.CompareTo(yParts.NumericValue);
                if (numComp != 0) return numComp;

                // Si même valeur numérique, trier par longueur de la chaîne de chiffres
                return xParts.RawNumber.Length.CompareTo(yParts.RawNumber.Length);
            }
            else if (xValid) return -1;
            else if (yValid) return 1;
            else return string.Compare(x, y, StringComparison.Ordinal);
        }

        private bool TryParse(string input, out (string Letters, bool HasPrime, int NumericValue, string RawNumber) parts)
        {
            var match = Regex.Match(input, @"^([A-Z]+)('?)(\d+)$");
            if (match.Success)
            {
                parts = (
                    Letters: match.Groups[1].Value,
                    HasPrime: match.Groups[2].Value == "'",
                    NumericValue: int.Parse(match.Groups[3].Value),
                    RawNumber: match.Groups[3].Value
                );
                return true;
            }

            parts = default;
            return false;
        }
    }

}