using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using HBP.Module3D;
using HBP.UI.TrialMatrix;
using HBP.Data.Visualization;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using Tools.Unity.Graph.Data;

namespace HBP.UI.Graph
{
    [RequireComponent(typeof(ZoneResizer))]
    public class GraphsGestion : MonoBehaviour
    {
        #region Properties
        Base3DScene m_Scene;
        public Base3DScene Scene
        {
            get { return m_Scene; }
            set
            {
                m_Scene = value;
                m_Scene.OnRequestSiteInformation.AddListener(OnRequestSiteInformation);
                m_Scene.OnChangeColumnMinimizedState.AddListener(OnMinimizeColumns);
            }
        }

        // Trial matrix
        [SerializeField] TrialMatrixList m_TrialMatrixList;
        Dictionary<Protocol, Vector2> m_LimitsByProtocol = new Dictionary<Protocol, Vector2>();
        Dictionary<Protocol, Data.TrialMatrix.TrialMatrix[]> m_TrialMatrixByProtocol = new Dictionary<Protocol, Data.TrialMatrix.TrialMatrix[]>();
        bool m_LineSelectable = false;

        // Curves
        //[SerializeField] GraphGestion m_GraphGestion;
        //Color[] m_Colors = new Color[7] { Color.blue, Color.red, Color.green, Color.cyan, Color.grey, Color.magenta, Color.yellow };
        //Curve[][] m_Curves = new Curve[0][];
        //Curve[] m_ROIcurves = new Curve[0];

        // Plots
        Site[] m_Sites;

        // Type
        [SerializeField] ZoneResizer m_ZoneResizer;
        #endregion

        #region Handlers Methods
        public void OnSelectLines(int[] lines, Data.Experience.Protocol.Bloc bloc, bool additive)
        {
            if (m_LineSelectable)
            {
                foreach (TrialMatrix.TrialMatrix trial in m_TrialMatrixList.TrialMatrix)
                {
                    trial.SelectLines(lines, bloc, additive);
                }
                //GenerateCurves();
                //DisplayCurves();
            }
        }
        void OnRequestSiteInformation(IEnumerable<Site> sites)
        {
            m_Sites = sites.ToArray();
            m_LineSelectable = sites.All((s) => s.Information.Patient == sites.FirstOrDefault().Information.Patient);

            GenerateTrialMatrix();
            DisplayTrialMatrix();
            //GenerateCurves();
            //DisplayCurves();
        }
        void OnMinimizeColumns()
        {
            DisplayTrialMatrix();
            //GenerateCurves();
            //DisplayCurves();
        }
        #endregion

        #region Private Methods
        // Trial matrix
        void GenerateTrialMatrix()
        {
            UnityEngine.Profiling.Profiler.BeginSample("GenerateTrialMatrix()");

            // Save value
            UnityEngine.Profiling.Profiler.BeginSample("Save value");
            m_LimitsByProtocol = new Dictionary<Protocol, Vector2>();
            foreach (TrialMatrix.TrialMatrix trialMatrix in m_TrialMatrixList.TrialMatrix)
            {
                m_LimitsByProtocol[trialMatrix.Data.Protocol] = trialMatrix.Limits;
            }
            UnityEngine.Profiling.Profiler.EndSample();

            // Find protocols to display
            UnityEngine.Profiling.Profiler.BeginSample("Find Protocols");
            IEnumerable<Protocol> protocols = (from column in Scene.ColumnManager.ColumnsIEEG select column.ColumnData.Protocol).Distinct();
            UnityEngine.Profiling.Profiler.EndSample();

            // Generate trialMatrix and create the dictionary
            UnityEngine.Profiling.Profiler.BeginSample("Generate");
            Dictionary<Protocol, Data.TrialMatrix.TrialMatrix[]> trialMatrixByProtocol = new Dictionary<Protocol, Data.TrialMatrix.TrialMatrix[]>();
            foreach (Protocol protocol in protocols)
            {
                UnityEngine.Profiling.Profiler.BeginSample("new TrialMatrix array");
                Column column = Scene.ColumnManager.ColumnsIEEG.First(c => c.ColumnData.Protocol == protocol).ColumnData;
                Data.TrialMatrix.TrialMatrix[] trialMatrixData = new Data.TrialMatrix.TrialMatrix[m_Sites.Length];
                UnityEngine.Profiling.Profiler.EndSample();

                UnityEngine.Profiling.Profiler.BeginSample("Find DataInfoBySite");
                Dictionary<Site, DataInfo> dataInfoBySite = m_Sites.ToDictionary(s => s, s => Scene.Visualization.GetDataInfo(s.Information.Patient, column));
                IEnumerable<DataInfo> dataInfoToRead = dataInfoBySite.Values.Distinct();
                UnityEngine.Profiling.Profiler.EndSample();

                UnityEngine.Profiling.Profiler.BeginSample("GetData from Manager");
                Dictionary<Data.Experience.Dataset.DataInfo, Dictionary<Data.Experience.Protocol.Bloc, Data.Localizer.Bloc[]>> epochedBlocsByProtocolBlocByDataInfo = new Dictionary<Data.Experience.Dataset.DataInfo, Dictionary<Data.Experience.Protocol.Bloc, Data.Localizer.Bloc[]>>();
                foreach (var data in dataInfoToRead)
                {
                    Dictionary<Data.Experience.Protocol.Bloc, Data.Localizer.Bloc[]> epochedBlocsByProtocolBloc = new Dictionary<Data.Experience.Protocol.Bloc, Data.Localizer.Bloc[]>();
                    foreach (var bloc in protocol.Blocs)
                    {
                        epochedBlocsByProtocolBloc.Add(bloc,DataManager.GetData(data,bloc).Blocs);
                    }
                    epochedBlocsByProtocolBlocByDataInfo.Add(data, epochedBlocsByProtocolBloc);
                }
                UnityEngine.Profiling.Profiler.EndSample();

                UnityEngine.Profiling.Profiler.BeginSample("new TrialMatrix");
                for (int i = 0; i < m_Sites.Length; i++) trialMatrixData[i] = new Data.TrialMatrix.TrialMatrix(protocol, dataInfoBySite[m_Sites[i]], epochedBlocsByProtocolBlocByDataInfo[dataInfoBySite[m_Sites[i]]], m_Sites[i]);
                trialMatrixByProtocol.Add(protocol, trialMatrixData);
                UnityEngine.Profiling.Profiler.EndSample();
            }
            this.m_TrialMatrixByProtocol = trialMatrixByProtocol;
            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.EndSample();
        }
        void DisplayTrialMatrix()
        {
            UnityEngine.Profiling.Profiler.BeginSample("DisplayTrialMatrix()");
            IEnumerable<Protocol> protocols = (from column in Scene.ColumnManager.ColumnsIEEG where !column.IsMinimized select column.ColumnData.Protocol).Distinct();
            Data.TrialMatrix.TrialMatrix[][] trialMatrix = m_TrialMatrixByProtocol.Where(m => protocols.Contains(m.Key)).Select(m => m.Value).ToArray();
            m_TrialMatrixList.Set(trialMatrix);
            UnityEngine.Profiling.Profiler.EndSample();
        }

        //// Curves
        //void GenerateCurves()
        //{
        //    UnityEngine.Profiling.Profiler.BeginSample("GenerateCurve()");
        //    // Curves
        //    Curve[][] curves = new Curve[maskColumns.Length][];
        //    Curve[] ROIcurves = new Curve[maskColumns.Length];

        //    // PlotCurves
        //    for (int c = 0; c < maskColumns.Length; c++)
        //    {
        //        if (!maskColumns[c])
        //        {
        //            Color mainColor = m_Colors[c];
        //            Color[] secondariesColor = new Color[2];
        //            secondariesColor[0] = mainColor;
        //            secondariesColor[1] = mainColor + 0.5f * new Color(1, 1, 1);

        //            // Read timeLine
        //            Timeline timeLine;
        //            switch (Type)
        //            {
        //                case TypeEnum.Single:
        //                    timeLine = VisualizationLoaded.SP_VisualizationData.Columns[c].TimeLine;
        //                    break;
        //                case TypeEnum.Multi:
        //                    timeLine = VisualizationLoaded.MP_VisualizationData.Columns[c].TimeLine;
        //                    break;
        //                default:
        //                    timeLine = new Timeline();
        //                    break;
        //            }

        //            // Initialize Curves.
        //            List<Curve> curvesInThisColumn = new List<Curve>();

        //            for (int p = 0; p < m_Sites.Length; p++)
        //            {
        //                // Find bloc to read.
        //                Data.TrialMatrix.TrialMatrix trialMatrixData = m_TrialMatrixByProtocol[columns[c].Protocol][p];
        //                TrialMatrix.TrialMatrix trialMatrix = System.Array.Find(m_TrialMatrixList.TrialMatrix, t => t.Data == trialMatrixData);
        //                TrialMatrix.Bloc bloc = null;
        //                foreach (Line line in trialMatrix.Lines)
        //                {
        //                    foreach (TrialMatrix.Bloc blocInTheLine in line.Blocs)
        //                    {
        //                        if ((Type == TypeEnum.Single && blocInTheLine.Data.PBloc == VisualizationLoaded.SP_Visualization.Columns[c].Bloc) || (Type == TypeEnum.Multi && blocInTheLine.Data.PBloc == VisualizationLoaded.MP_Visualization.Columns[c].Bloc))
        //                        {
        //                            bloc = blocInTheLine;
        //                            break;
        //                        }
        //                    }
        //                }

        //                // Read and average data.
        //                Data.TrialMatrix.Line[] linesToRead = bloc.Data.GetLines(bloc.SelectedLines);
        //                float[] data = new float[bloc.Data.Lines[0].Data.Length];
        //                if (bloc.SelectedLines.Length > 1)
        //                {
        //                    float[] standardDeviations = new float[data.Length];
        //                    for (int i = 0; i < data.Length; i++)
        //                    {
        //                        List<float> l_dataList = new List<float>();
        //                        for (int l = 0; l < linesToRead.Length; l++)
        //                        {
        //                            l_dataList.Add(linesToRead[l].Data[i]);
        //                        }

        //                        //Find selectedLines
        //                        data[i] = Tools.CSharp.MathfExtension.Average(l_dataList.ToArray());
        //                        standardDeviations[i] = Tools.CSharp.MathfExtension.SEM(l_dataList.ToArray());
        //                    }

        //                    // Generate points.
        //                    int pMin = timeLine.Start.Position;
        //                    int pMax = timeLine.End.Position;
        //                    float min = timeLine.Start.Value;
        //                    float max = timeLine.End.Value;
        //                    Vector2[] points = new Vector2[pMax + 1 - pMin];
        //                    for (int i = pMin; i <= pMax; i++)
        //                    {
        //                        float absciss = min + ((max - min) * (i - pMin) / (pMax - pMin));
        //                        points[i] = new Vector2(absciss, data[i]);
        //                    }

        //                    //Create curve
        //                    curvesInThisColumn.Add(new CurveWithShape("C" + (c + 1) + " " + m_Sites[p].Name, 2, secondariesColor[p], points, standardDeviations, Tools.Unity.Graph.Point.Style.Round, true));
        //                }
        //                else if (bloc.SelectedLines.Length == 1)
        //                {
        //                    data = bloc.Data.Lines[bloc.SelectedLines[0]].Data;
        //                    // Generate points.
        //                    int pMin = timeLine.Start.Position;
        //                    int pMax = timeLine.End.Position;
        //                    float min = timeLine.Start.Value;
        //                    float max = timeLine.End.Value;
        //                    Vector2[] points = new Vector2[pMax + 1 - pMin];
        //                    for (int i = pMin; i <= pMax; i++)
        //                    {
        //                        float absciss = min + ((max - min) * (i - pMin) / (pMax - pMin));
        //                        points[i] = new Vector2(absciss, data[i]);
        //                    }

        //                    //Create curve
        //                    curvesInThisColumn.Add(new Curve("C" + (c + 1) + " " + m_Sites[p].Name, 2, secondariesColor[p], points, Tools.Unity.Graph.Point.Style.Round, true));
        //                }
        //                else
        //                {
        //                    continue;
        //                }
        //            }
        //            curves[c] = curvesInThisColumn.ToArray();

        //            // ROI
        //            if (Type == TypeEnum.Multi)
        //            {
        //                float[] l_ROIColumnData = new float[VisualizationLoaded.MP_VisualizationData.Columns[c].Values[0].Length];
        //                for (int i = 0; i < l_ROIColumnData.Length; i++)
        //                {
        //                    float l_sum = 0;
        //                    int l_nbPlots = 0;
        //                    for (int plot = 0; plot < m_MaskPlots[c].Length; plot++)
        //                    {
        //                        if (m_MaskPlots[c][plot])
        //                        {
        //                            l_nbPlots++;
        //                            l_sum += VisualizationLoaded.MP_VisualizationData.Columns[c].Values[plot][i];
        //                        }
        //                    }
        //                    l_ROIColumnData[i] = l_sum / l_nbPlots;
        //                }
        //                int pMin = timeLine.Start.Position;
        //                int pMax = timeLine.End.Position;
        //                float min = timeLine.Start.Value;
        //                float max = timeLine.End.Value;
        //                Vector2[] l_points = new Vector2[pMax - pMin];
        //                for (int p = pMin; p < pMax; p++)
        //                {
        //                    float absciss = min + ((max - min) * (p - pMin) / (pMax - 1 - pMin));
        //                    l_points[p] = new Vector2(absciss, l_ROIColumnData[p]);
        //                }
        //                ROIcurves[c] = new Curve("C" + (c + 1) + " ROI", 4, mainColor, l_points, Tools.Unity.Graph.Point.Style.Round, true);
        //            }
        //        }
        //    }
        //    this.m_ROIcurves = ROIcurves;
        //    this.m_Curves = curves;
        //    UnityEngine.Profiling.Profiler.EndSample();
        //}
        //void DisplayCurves()
        //{
        //    UnityEngine.Profiling.Profiler.BeginSample("DisplayCurves()");
        //    List<Curve> curves = new List<Curve>();
        //    for (int c = 0; c < maskColumns.Length; c++)
        //    {
        //        if (!maskColumns[c])
        //        {
        //            curves.AddRange(this.m_Curves[c]);
        //            if (Type == TypeEnum.Multi)
        //            {
        //                curves.Add(m_ROIcurves[c]);
        //            }
        //        }
        //    }
        //    m_GraphGestion.Set(curves.ToArray());
        //    UnityEngine.Profiling.Profiler.EndSample();
        //}
        #endregion
    }
}