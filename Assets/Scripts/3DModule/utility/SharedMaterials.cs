
/* \file SharedMaterials.cs
 * \author Lance Florian
 * \date    22/04/2016
 * \brief Define SharedMaterials
 */

// system
using System;
using System.Text;

// unity
using UnityEngine;

namespace HBP.VISU3D
{
    public class SharedMaterials : MonoBehaviour
    {
        public struct ROI
        {
            public static Material Normal = null;
            public static Material Selected = null;
        }

        public struct Ring
        {
            public static Material Selected = null;
        }

        public struct Plot
        {
            public static Material BlackListed = null;
            public static Material Excluded = null;
            public static Material Negative = null;
            public static Material Normal = null;
            public static Material Positive = null;
            public static Material NormalHighlighted = null;
            public static Material BlackListedHighlighted = null;
            public static Material ExcludedHighlighted = null;
            public static Material NegativeHighlighted = null;
            public static Material PositiveHighlighted = null;
            public static Material Source = null;
            public static Material SourceHighlighted = null;
            public static Material NotASource = null;
            public static Material NotASourceHighlighted = null;
            public static Material NoLatencyData = null;
            public static Material NoLatencyDataHighlighted = null;
        }

        void Awake()
        {
            // ROI
            ROI.Normal = Instantiate(Resources.Load("Materials/ROI/ROI", typeof(Material))) as Material;
            ROI.Selected = Instantiate(Resources.Load("Materials/ROI/ROISelected", typeof(Material))) as Material;

            // Plot
            Plot.BlackListed = Instantiate(Resources.Load("Materials/Plots/Blacklisted", typeof(Material))) as Material;
            Plot.Excluded = Instantiate(Resources.Load("Materials/Plots/Excluded", typeof(Material))) as Material;
            Plot.Negative = Instantiate(Resources.Load("Materials/Plots/Negative", typeof(Material))) as Material;
            Plot.Normal = Instantiate(Resources.Load("Materials/Plots/Normal", typeof(Material))) as Material;
            Plot.Positive = Instantiate(Resources.Load("Materials/Plots/Positive", typeof(Material))) as Material;
            Plot.NormalHighlighted = Instantiate(Resources.Load("Materials/Plots/NormalHighlighted", typeof(Material))) as Material;
            Plot.BlackListedHighlighted = Instantiate(Resources.Load("Materials/Plots/BlacklistedHighlighted", typeof(Material))) as Material;
            Plot.ExcludedHighlighted = Instantiate(Resources.Load("Materials/Plots/ExcludedHighlighted", typeof(Material))) as Material;
            Plot.NegativeHighlighted = Instantiate(Resources.Load("Materials/Plots/NegativeHighlighted", typeof(Material))) as Material;
            Plot.PositiveHighlighted = Instantiate(Resources.Load("Materials/Plots/PositiveHighlighted", typeof(Material))) as Material;
            Plot.Source = Instantiate(Resources.Load("Materials/Plots/Source", typeof(Material))) as Material;
            Plot.SourceHighlighted = Instantiate(Resources.Load("Materials/Plots/SourceHighlighted", typeof(Material))) as Material;
            Plot.NotASource = Instantiate(Resources.Load("Materials/Plots/notASource", typeof(Material))) as Material;
            Plot.NotASourceHighlighted = Instantiate(Resources.Load("Materials/Plots/notASourceHighlighted", typeof(Material))) as Material;
            Plot.NoLatencyData = Instantiate(Resources.Load("Materials/Plots/noLatencyData", typeof(Material))) as Material;
            Plot.NoLatencyDataHighlighted = Instantiate(Resources.Load("Materials/Plots/noLatencyDataHighlighted", typeof(Material))) as Material;

            // Ring
            Ring.Selected = Instantiate(Resources.Load("Materials/Rings/Selected", typeof(Material))) as Material;
        }

        public static Material plotMat(bool hightlighted, PlotType plotType)
        {
            if (!hightlighted)
            {
                switch (plotType)
                {
                    case PlotType.Normal: return Plot.Normal;
                    case PlotType.Positive: return Plot.Positive;
                    case PlotType.Negative: return Plot.Negative;
                    case PlotType.Excluded: return Plot.Excluded;
                    case PlotType.Source: return Plot.Source;
                    case PlotType.NotASource: return Plot.NotASource;
                    case PlotType.NoLatencyData: return Plot.NoLatencyData;
                    case PlotType.BlackListed: return Plot.BlackListed;
                    case PlotType.NonePos: return Plot.Positive;
                    case PlotType.NoneNeg: return Plot.Negative;
                }
            }
            else
            {
                switch (plotType)
                {
                    case PlotType.Normal: return Plot.NormalHighlighted;
                    case PlotType.Positive: return Plot.PositiveHighlighted;
                    case PlotType.Negative: return Plot.NegativeHighlighted;
                    case PlotType.Excluded: return Plot.ExcludedHighlighted;
                    case PlotType.Source: return Plot.SourceHighlighted;
                    case PlotType.NotASource: return Plot.NotASourceHighlighted;
                    case PlotType.NoLatencyData: return Plot.NoLatencyDataHighlighted;
                    case PlotType.BlackListed: return Plot.BlackListedHighlighted;
                    case PlotType.NonePos: return Plot.Positive;
                    case PlotType.NoneNeg: return Plot.Negative;
                }
            }

            return null;
        }
    }
}



