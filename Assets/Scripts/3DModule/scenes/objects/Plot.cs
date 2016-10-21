

/**
 * \file    Plot.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define Plot class
 */


// system
using System.Collections;

// unity
using UnityEngine;

namespace HBP.VISU3D
{
    public enum PlotType { Normal, Positive, Negative, Excluded, Source, NotASource, NoLatencyData, BlackListed, NonePos, NoneNeg };

    public struct PlotShaderInfo
    {
        public PlotShaderInfo(Color color, float smoothness)
        {
            this.color = color;
            this.smoothness = smoothness;
        }

        public Color color;
        public float smoothness;
    }

    public struct PlotColors
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
    }

    public struct PlotShaderInput
    {
        // normal
        public static PlotShaderInfo BlackListed = new PlotShaderInfo(PlotColors.BlackListed, 0f);
        public static PlotShaderInfo Excluded = new PlotShaderInfo(PlotColors.Excluded, 0f);
        public static PlotShaderInfo Negative = new PlotShaderInfo(PlotColors.Negative, 0f);
        public static PlotShaderInfo Normal = new PlotShaderInfo(PlotColors.Normal, 0f);
        public static PlotShaderInfo Positive = new PlotShaderInfo(PlotColors.Positive, 0f);
        public static PlotShaderInfo Source = new PlotShaderInfo(PlotColors.Source, 0f);
        public static PlotShaderInfo NotASource = new PlotShaderInfo(PlotColors.NotASource, 0f);
        public static PlotShaderInfo NoLatencyData = new PlotShaderInfo(PlotColors.NoLatencyData, 0f);
        public static PlotShaderInfo NoneNeg = new PlotShaderInfo(PlotColors.NoneNeg, 0f);
        public static PlotShaderInfo NonePos = new PlotShaderInfo(PlotColors.NonePos, 0f);
        // highlighted
        public static PlotShaderInfo NoneNegHighlighted = new PlotShaderInfo(PlotColors.NoneNegHighlighted, 0f);
        public static PlotShaderInfo NonePosHighlighted = new PlotShaderInfo(PlotColors.NonePosHighlighted, 0f);
        public static PlotShaderInfo NormalHighlighted = new PlotShaderInfo(PlotColors.NormalHighlighted, 1f);
        public static PlotShaderInfo BlackListedHighlighted = new PlotShaderInfo(PlotColors.BlackListedHighlighted, 1f);
        public static PlotShaderInfo ExcludedHighlighted = new PlotShaderInfo(PlotColors.ExcludedHighlighted, 1f);
        public static PlotShaderInfo NegativeHighlighted = new PlotShaderInfo(PlotColors.NegativeHighlighted, 1f);
        public static PlotShaderInfo PositiveHighlighted = new PlotShaderInfo(PlotColors.PositiveHighlighted, 1f);
        public static PlotShaderInfo SourceHighlighted = new PlotShaderInfo(PlotColors.SourceHighlighted, 0f);
        public static PlotShaderInfo NotASourceHighlighted = new PlotShaderInfo(PlotColors.NotASourceHighlighted, 0f);
        public static PlotShaderInfo NoLatencyDataHighlighted = new PlotShaderInfo(PlotColors.NoLatencyDataHighlighted, 0f);
    }

    [SerializeField]
    public struct PlotI
    {
        public bool columnMask;     /**< is the plot masked on the column ? */
        public bool exclude;        /**< is the plot excluded ? */
        public bool blackList;      /**< is the plot blacklisted ? */
        public bool columnROI;      /**< is the plot in a ROI ? */
        public bool highlight;      /**< is the plot highlighted ? */

        public int idGlobal;        /**< global plot id (all patients) */
        public int idPlotPatient;   /**< plot id of the patient */

        public int idPatient;       /**< patient id */
        public int idElectrode;     /**< electrode id of the patient */
        public int idPlot;          /**< plot id of the electrode */

        public string patientName;  /**< patient name */
        public string name;         /**< plot full name */
    }

    /// <summary>
    /// Class for defining informations about a plot
    /// </summary>
    public class Plot : MonoBehaviour
    {
        public bool columnMask;     /**< is the plot masked on the column ? */
        public bool exclude;        /**< is the plot excluded ? */
        public bool blackList;      /**< is the plot blacklisted ? */
        public bool columnROI;      /**< is the plot in a ROI ? */
        public bool highlight;      /**< is the plot highlighted ? */

        public int idGlobal;        /**< global plot id (all patients) */
        public int idPlotPatient;   /**< plot id of the patient */

        public int idPatient;       /**< patient id */
        public int idElectrode;     /**< electrode id of the patient */
        public int idPlot;          /**< plot id of the electrode */

        public string patientName;  /**< patient name */
        public string fullName;     /**< plot full name */


        public static PlotShaderInfo instanceShaderInfo(bool hightlighted, PlotType plotType)
        {
            PlotShaderInfo plot = new PlotShaderInfo();

            if (!hightlighted)
            {
                switch (plotType)
                {
                    case PlotType.Normal: return PlotShaderInput.Normal;
                    case PlotType.Positive: return PlotShaderInput.Positive;
                    case PlotType.Negative: return PlotShaderInput.Negative;
                    case PlotType.Excluded: return PlotShaderInput.Excluded;
                    case PlotType.Source: return PlotShaderInput.Source;
                    case PlotType.NotASource: return PlotShaderInput.NotASource;
                    case PlotType.NoLatencyData: return PlotShaderInput.NoLatencyData;
                    case PlotType.BlackListed: return PlotShaderInput.BlackListed;
                    case PlotType.NonePos: return PlotShaderInput.NonePos;
                    case PlotType.NoneNeg: return PlotShaderInput.NoneNeg;
                }
            }
            else
            {
                switch (plotType)
                {
                    case PlotType.Normal: return PlotShaderInput.NormalHighlighted;
                    case PlotType.Positive: return PlotShaderInput.PositiveHighlighted;
                    case PlotType.Negative: return PlotShaderInput.NegativeHighlighted;
                    case PlotType.Excluded: return PlotShaderInput.ExcludedHighlighted;
                    case PlotType.Source: return PlotShaderInput.SourceHighlighted;
                    case PlotType.NotASource: return PlotShaderInput.NotASourceHighlighted;
                    case PlotType.NoLatencyData: return PlotShaderInput.NoLatencyDataHighlighted;
                    case PlotType.BlackListed: return PlotShaderInput.BlackListedHighlighted;
                    case PlotType.NonePos: return PlotShaderInput.NonePosHighlighted;
                    case PlotType.NoneNeg: return PlotShaderInput.NoneNegHighlighted;
                }
            }

            return plot;
        }
    }
}