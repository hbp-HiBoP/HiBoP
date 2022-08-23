using System.Collections.Generic;
using UnityEngine;
using HBP.Display.Informations.Graphs;

namespace HBP.UI.Informations.Graphs
{
    public class StructWrapper : MonoBehaviour
    {
        #region Properties
        [SerializeField] LegendsGestion.LegendsEvent m_OnLegendsResult;
        public LegendsGestion.LegendsEvent OnLegendResult
        {
            get
            {
                return m_OnLegendsResult;
            }
        }
        [SerializeField] CurvesDataEvent m_OnCurveDataResult;
        public CurvesDataEvent OnCurveDataResult
        {
            get
            {
                return m_OnCurveDataResult;
            }
        }
        #endregion

        #region Public Methods
        public void Set(Graph.Curve[] curves)
        {
            LegendsGestion.Legend[] legends = new LegendsGestion.Legend[curves.Length];
            List<CurveData> curveDatas = new List<CurveData>();
            for (int i = 0; i < legends.Length; i++)
            {
                legends[i] = GraphCurveToLegend(curves[i]);
                curveDatas.AddRange(GraphCurveToCurveData(curves[i]));
            }
            m_OnLegendsResult.Invoke(legends);
            m_OnCurveDataResult.Invoke(curveDatas.ToArray());
        }
        #endregion

        #region Private Methods
        LegendsGestion.Legend GraphCurveToLegend(Graph.Curve curve)
        {
            LegendsGestion.Legend legend = new LegendsGestion.Legend();
            legend.ID = curve.ID;
            legend.Label = curve.Name;
            legend.Color = curve.Color;
            legend.Enabled = curve.Enabled;
            for (int i = 0; i < curve.SubCurves.Count; i++)
            {
                LegendsGestion.Legend subLegend = GraphCurveToLegend(curve.SubCurves[i]);
                legend.AddSubLegend(subLegend);
            }
            return legend;
        }
        CurveData[] GraphCurveToCurveData(Graph.Curve curve)
        {
            List<CurveData> curveDatas = new List<CurveData>();
            if(curve.Enabled)
            {
                if(curve.Data != null) curveDatas.Add(curve.Data);
                foreach (var subCurve in curve.SubCurves)
                {
                    curveDatas.AddRange(GraphCurveToCurveData(subCurve));
                }
            }
            return curveDatas.ToArray();
        }
        #endregion
    }
}