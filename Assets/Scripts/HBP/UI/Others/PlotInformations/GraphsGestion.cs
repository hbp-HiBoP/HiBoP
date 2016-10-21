using UnityEngine;
using System.Collections.Generic;
using HBP.Data.Experience.Dataset;
using p = HBP.Data.Patient;
using d = Tools.Unity.Graph.Data;
using System.Linq;
using HBP.UI.TrialMatrix;

namespace HBP.UI.Graph
{
    public class GraphsGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        HBPLinker m_Module3DLinker;

        [SerializeField]
        UnityEngine.UI.Slider m_horizontalSlider;

        // Curves
        [SerializeField]
        GraphGestion m_graph;

        [SerializeField]
        Color[] m_Colors = new Color[7] { Color.blue, Color.red, Color.green, Color.cyan, Color.grey, Color.magenta, Color.yellow };

        d.Curve[][] m_curves = new d.Curve[0][];
        d.Curve[] m_ROIcurves = new d.Curve[0];

        // Trial matrix
        [SerializeField]
        TrialMatrixList m_trialMatrix;

        Dictionary<Data.Experience.Protocol.Protocol, Vector2> m_trialLimitsByProtocol = new Dictionary<Data.Experience.Protocol.Protocol, Vector2>();
        Dictionary<Data.Experience.Protocol.Protocol, Data.TrialMatrix.TrialMatrix[]> m_trialMatrixByProtocol = new Dictionary<Data.Experience.Protocol.Protocol, Data.TrialMatrix.TrialMatrix[]>();
        bool m_lineSelectable = false;

        // Column
        bool[] m_singleMaskColumns = new bool[0];
        bool[] m_multiMaskColumns = new bool[0];

        // Plots
        p.PlotID[] m_plotsCompared;
        bool[][] m_plotsUsed;

        // Type
        enum TypeEnum { None, Single, Multi };
        TypeEnum m_type = TypeEnum.None;
        TypeEnum Type
        {
            get
            {
                return m_type;
            }
            set
            {
                m_type = value;
                if (value == TypeEnum.None && gameObject.activeInHierarchy)
                {
                    m_horizontalSlider.value = 1.0f;
                }
                else if (value != TypeEnum.None && !gameObject.activeInHierarchy)
                {
                    m_horizontalSlider.value = 0.6f;
                }
            }
        }
        #endregion

        #region Public Methods
        public void SetMask(int nbColumns,bool sp)
        {
            bool[] l_mask = new bool[nbColumns];
            for (int i = 0; i < nbColumns; i++)
            {
                l_mask[i] = false;
            }
            if(sp)
            {
                m_singleMaskColumns = l_mask;
            }
            else
            {
                m_multiMaskColumns = l_mask;
            }
        }
        #endregion

        #region Handlers Methods
        void Awake()
        {
            m_Module3DLinker.Command3DModule.PlotInfoRequestEvent.AddListener((plotResquest) => HandlePlotRequest(plotResquest));
            m_Module3DLinker.Command3DModule.UpdateColumnMinimizeStateEvent.AddListener((sp, columns) => HandleMinimizeColumns(sp, columns.ToArray()));
        }

        public void HandleSelectLines(int[] lines, Data.Experience.Protocol.Bloc bloc, bool additive)
        {
            if(m_lineSelectable)
            {
                foreach (TrialMatrix.TrialMatrix trial in m_trialMatrix.TrialMatrix)
                {
                    trial.SelectLines(lines, bloc, additive);
                }
                if(Type == TypeEnum.Single)
                {
                    GenerateCurves(m_plotsCompared, m_plotsUsed, true);
                    UpdateCurves();
                }
                else if(Type == TypeEnum.Multi)
                {
                    GenerateCurves(m_plotsCompared, m_plotsUsed, false);
                    UpdateCurves();
                }
            }  
        }

        void HandlePlotRequest(VISU3D.plotRequest plotRequest)
        {
            // Declare plots.
            p.PlotID[] l_plotsID;
            List<p.PlotID> l_plotsToDisplay = new List<p.PlotID>();
            bool[][] l_plotsUsed = plotRequest.maskColumn.Select(a => a.ToArray()).ToArray();


            // Read plots.
            if (plotRequest.spScene)
            {
                l_plotsID = p.Electrode.Read(new p.Patient[1] { VisualisationLoaded.SP_VisualisationData.Patient }, false);
            }
            else
            {
                l_plotsID = p.Electrode.Read(VisualisationLoaded.MP_VisualisationData.Patients.ToArray(), true);
            }
            if (plotRequest.idPlot >= 0 && plotRequest.idPlot < l_plotsID.Length)
            {
                l_plotsToDisplay.Add(l_plotsID[plotRequest.idPlot]);
            }
            if (plotRequest.idPlot2 >= 0 && plotRequest.idPlot2 < l_plotsID.Length)
            {
                l_plotsToDisplay.Add(l_plotsID[plotRequest.idPlot2]);
            }
            ComparePlots(l_plotsToDisplay.ToArray(), l_plotsUsed, plotRequest.spScene);
        }

        void HandleMinimizeColumns(bool sp, bool[] columns)
        {
            if(sp)
            {
                m_singleMaskColumns = columns;
            }
            else
            {
                m_multiMaskColumns = columns;
            }
            if(Type != TypeEnum.None)
            {
                UpdateTrialMatrix();
                UpdateCurves();
            }
        }
        #endregion

        #region Private Methods
        void ComparePlots(p.PlotID[] plotsToCompare, bool[][] plotsUsed, bool sp)
        {
            m_plotsCompared = plotsToCompare;
            m_plotsUsed = plotsUsed;
            if (IsTheSamePatient(plotsToCompare))
            {
                m_lineSelectable = true;
            }
            else
            {
                m_lineSelectable = false;
            }
            if (sp)
            {
                Type = TypeEnum.Single;
            }
            else
            {
                Type = TypeEnum.Multi;
            }
            m_graph.SetManually = false;
            GenerateTrialMatrix(plotsToCompare, sp);
            UpdateTrialMatrix();
            GenerateCurves(plotsToCompare,plotsUsed, sp);
            UpdateCurves();
        }

        void UpdateTrialMatrix()
        {
            // Find columns
            Data.Visualisation.Column[] l_columns = new Data.Visualisation.Column[0];
            bool[] l_maskColumns = new bool[0];
            if (Type == TypeEnum.Single)
            {
                l_columns = VisualisationLoaded.SP_Visualisation.Columns.ToArray();
                l_maskColumns = m_singleMaskColumns;
            }
            else if(Type == TypeEnum.Multi)
            {
                l_columns = VisualisationLoaded.MP_Visualisation.Columns.ToArray();
                l_maskColumns = m_multiMaskColumns;
            }

            List<Data.Experience.Protocol.Protocol> l_protocolsToDisplay = new List<Data.Experience.Protocol.Protocol>();
            for (int c = 0; c < l_maskColumns.Length; c++)
            {
                if(!l_maskColumns[c])
                {
                    Data.Experience.Protocol.Protocol l_protocol = l_columns[c].Protocol;
                    if(!l_protocolsToDisplay.Contains(l_protocol))
                    {
                        l_protocolsToDisplay.Add(l_protocol);
                    }
                }
            }
            Data.TrialMatrix.TrialMatrix[][] l_trialMatrixToDisplay = new Data.TrialMatrix.TrialMatrix[l_protocolsToDisplay.Count][];
            for (int p = 0; p < l_protocolsToDisplay.Count; p++)
            {
                l_trialMatrixToDisplay[p] = m_trialMatrixByProtocol[l_protocolsToDisplay[p]];
            }
            m_trialMatrix.Set(l_trialMatrixToDisplay);
        }

        //void UpdateTrialMatrix()
        //{
        //    List<Data.TrialMatrix.TrialMatrix[]> trialsToDisplay = new List<Data.TrialMatrix.TrialMatrix[]>();
        //    for (int c = 0; c < m_columns.Length; c++)
        //    {
        //        Debug.Log("Column n° : " + c);
        //        if (!m_columns[c])
        //        {
        //            Debug.Log("Ok");
        //            List<Data.TrialMatrix.TrialMatrix> l_trialColumn = new List<Data.TrialMatrix.TrialMatrix>();
        //            int sameColumn = m_trialsByColumns[c];
        //            for (int p = 0; p < m_trialsData[sameColumn].Length; p++)
        //            {
        //                l_trialColumn.Add(m_trialsData[sameColumn][p]);
        //            }
        //            if (!trialsToDisplay.Contains(l_trialColumn.ToArray()))
        //            {
        //                Debug.Log("Add");
        //                trialsToDisplay.Add(l_trialColumn.ToArray());
        //            }
        //        }
        //    }
        //    Debug.Log(trialsToDisplay.Count);
        //    m_trialMatrix.Set(trialsToDisplay.ToArray());
        //}

        void UpdateCurves()
        {
            List<d.Curve> l_curvesToDisplay = new List<d.Curve>();
            bool[] l_maskColumn = new bool[0];
            if(Type == TypeEnum.Single)
            {
                l_maskColumn = m_singleMaskColumns;
            }
            else
            {
                l_maskColumn = m_multiMaskColumns;
            }
            for (int c = 0; c < l_maskColumn.Length; c++)
            {
                if(!l_maskColumn[c])
                {
                    l_curvesToDisplay.AddRange(m_curves[c]);
                }
            }
            m_graph.Set(l_curvesToDisplay.ToArray());
        }

        //void GenerateTrialMatrix(p.PlotID[] plotsToCompare, bool sp)
        //{
        //    // Save value
        //    m_limitsByProtocol = new Dictionary<Data.Experience.Protocol.Protocol, Vector2>();
        //    foreach (TrialMatrix.TrialMatrix trialUI in m_trialMatrix.TrialMatrix)
        //    {
        //        m_limitsByProtocol[trialUI.TrialMatrixData.Protocol] = trialUI.Limits;
        //    }

        //    // Find columns
        //    Data.Visualisation.Column[] l_columns;
        //    if(sp)
        //    {
        //        l_columns = VisualisationLoaded.SP_Visualisation.Columns.ToArray();
        //    }
        //    else
        //    {
        //        l_columns = VisualisationLoaded.MP_Visualisation.Columns.ToArray();
        //    }

        //    // Find all differents protocols used on the visu.
        //    List<Data.Experience.Protocol.Protocol> l_protocols = new List<Data.Experience.Protocol.Protocol>();
        //    List<List<Data.TrialMatrix.TrialMatrix>> l_trials = new List<List<Data.TrialMatrix.TrialMatrix>>();
        //    List<int> l_trialsByColumns = new List<int>();

        //    for (int c = 0; c < m_columns.Length; c++)
        //    {
        //        Data.Visualisation.Column column;

        //        if (!l_protocols.Contains(column.Protocol))
        //        {
        //            List<Data.TrialMatrix.TrialMatrix> l_trialMatrix = new List<Data.TrialMatrix.TrialMatrix>();
        //            for (int i = 0; i < plotsToCompare.Length; i++)
        //            {
        //                DataInfo l_dataInfo;
        //                if (sp)
        //                {
        //                    l_dataInfo = VisualisationLoaded.SP_Visualisation.GetDataInfo(plotsToCompare[i].Patient, column);
        //                }
        //                else
        //                {
        //                    l_dataInfo = VisualisationLoaded.MP_Visualisation.GetDataInfo(plotsToCompare[i].Patient, column);
        //                }
        //                Data.TrialMatrix.TrialMatrix l_trial = new Data.TrialMatrix.TrialMatrix(l_dataInfo, plotsToCompare[i]);
        //                if (m_limitsByProtocol.ContainsKey(l_dataInfo.Protocol))
        //                {
        //                    l_trial.ValuesLimits = m_limitsByProtocol[l_dataInfo.Protocol];
        //                }
        //                l_trialMatrix.Add(l_trial);
        //            }
        //            l_protocols.Add(column.Protocol);
        //            l_trials.Add(l_trialMatrix);
        //        }
        //        int p = l_protocols.FindIndex(x => x.Equals(column.Protocol));
        //        l_trialsByColumns.Add(p);
        //    }

        //    // Set trials.
        //    m_trialsData = l_trials.Select(a => a.ToArray()).ToArray();
        //    m_trialsByColumns = l_trialsByColumns.ToArray();
        //}

        void GenerateTrialMatrix(p.PlotID[] plotsToCompare, bool sp)
        {
            // Save value
            m_trialLimitsByProtocol = new Dictionary<Data.Experience.Protocol.Protocol, Vector2>();
            foreach (TrialMatrix.TrialMatrix trialUI in m_trialMatrix.TrialMatrix)
            {
                m_trialLimitsByProtocol[trialUI.TrialMatrixData.Protocol] = trialUI.Limits;
            }

            // Find columns
            Data.Visualisation.Column[] l_columns;
            bool[] l_maskColumns = new bool[0];
            if (sp)
            {
                l_columns = VisualisationLoaded.SP_Visualisation.Columns.ToArray();
                l_maskColumns = m_singleMaskColumns;
            }
            else
            {
                l_columns = VisualisationLoaded.MP_Visualisation.Columns.ToArray();
                l_maskColumns = m_multiMaskColumns;
            }

            // Find all protocols
            List<Data.Experience.Protocol.Protocol> l_protocolsToUse = new List<Data.Experience.Protocol.Protocol>();
            List<Data.Visualisation.Column> l_columnsToUse = new List<Data.Visualisation.Column>();
            for (int c = 0; c < l_maskColumns.Length; c++)
            {
                Data.Experience.Protocol.Protocol l_protocol = l_columns[c].Protocol;
                if(!l_protocolsToUse.Contains(l_protocol))
                {
                    l_protocolsToUse.Add(l_protocol);
                    l_columnsToUse.Add(l_columns[c]);
                }
            }

            // Generate trialMatrix and create the dictionary
            Dictionary<Data.Experience.Protocol.Protocol, Data.TrialMatrix.TrialMatrix[]> l_trialsMatrixByProtocol = new Dictionary<Data.Experience.Protocol.Protocol, Data.TrialMatrix.TrialMatrix[]>();
            for (int p = 0; p < l_protocolsToUse.Count; p++)
            {
                Data.Visualisation.Column l_columnToUse = l_columnsToUse[p];
                Data.TrialMatrix.TrialMatrix[] l_trialsTemps = new Data.TrialMatrix.TrialMatrix[plotsToCompare.Length];
                for (int i = 0; i < plotsToCompare.Length; i++)
                {
                    DataInfo l_dataInfo;
                    if (sp)
                    {
                        l_dataInfo = VisualisationLoaded.SP_Visualisation.GetDataInfo(l_columnToUse)[0];
                    }
                    else
                    {
                        l_dataInfo = VisualisationLoaded.MP_Visualisation.GetDataInfo(plotsToCompare[i].Patient, l_columnToUse);
                    }
                    l_trialsTemps[i] = new Data.TrialMatrix.TrialMatrix(l_dataInfo, plotsToCompare[i]);
                }
                l_trialsMatrixByProtocol.Add(l_protocolsToUse[p], l_trialsTemps);
            }
            m_trialMatrixByProtocol = l_trialsMatrixByProtocol;
        }

        //void GenerateCurves(p.PlotID[] plotsToCompare,bool sp)
        //{
        //    TrialMatrix.TrialMatrix[] l_trials = m_trialMatrix.TrialMatrix;
        //    d.Curve[] l_curves = new d.Curve[m_columns.Length];
        //    for (int i = 0; i < m_columns.Length; i++)
        //    {
        //        if (m_columns[i])
        //        {
        //            // Display
        //            TrialMatrix.TrialMatrix l_trial = m_trialMatrix.TrialMatrix[m_trialsByColumns[i]];
        //            Bloc l_bloc = new Bloc();
        //            foreach(Line line in l_trial.Lines)
        //            {
        //                foreach(Bloc bloc in line.Blocs)
        //                {
        //                    if(sp)
        //                    {
        //                        if (bloc.Data.PBloc == VisualisationLoaded.SP_Visualisation.Columns[i].Bloc)
        //                        {
        //                            l_bloc = bloc;
        //                            break;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        if (bloc.Data.PBloc == VisualisationLoaded.MP_Visualisation.Columns[i].Bloc)
        //                        {
        //                            l_bloc = bloc;
        //                            break;
        //                        }
        //                    }
        //                }
        //            }
        //            // Average data.
        //            float[] l_sum = new float[l_bloc.Data.Lines[0].Data.Length];
        //            foreach(int lineSelected in l_bloc.SelectedLines)
        //            {
        //                for (int v = 0; v < l_sum.Length; v++)
        //                {
        //                    l_sum[v] += l_bloc.Data.Lines[lineSelected].Data[v];
        //                }
        //            }
        //            float[] l_average = new float[l_sum.Length];
        //            for (int v = 0; v < l_average.Length; v++)
        //            {
        //                l_average[v] = l_sum[v] / l_bloc.Data.Lines.Length;
        //            }

        //            // Read timeLine
        //            Data.Visualisation.TimeLine l_timeLine;
        //            if (sp)
        //            {
        //                l_timeLine = VisualisationLoaded.SP_VisualisationData.Columns[i].TimeLine;
        //            }
        //            else
        //            {
        //                l_timeLine = VisualisationLoaded.MP_VisualisationData.Columns[i].TimeLine;
        //            }

        //            // Generate points.l
        //            int pMin = l_timeLine.Min.Position;
        //            int pMax = l_timeLine.Max.Position;
        //            float min = l_timeLine.Min.Value;
        //            float max = l_timeLine.Max.Value;
        //            Vector2[] l_points = new Vector2[l_average.Length];
        //            for (int p = pMin; p < pMax; p++)
        //            {
        //                float absciss = min + ((max - min) * (p - pMin) / (pMax - 1 - pMin));
        //                l_points[p] = new Vector2(absciss, l_average[p]);
        //            }

        //            // Create curve
        //            d.Curve curve = new d.Curve();
        //            curve.Data = l_points;
        //            curve.Label = "C" + (c + 1) + " " + plotsToCompare[p].Name;
        //            curve.Size = 2;

        //        }
        //    }
        //}

        void GenerateCurves(p.PlotID[] plotsToCompare,bool[][] plotUsed, bool sp)
        {
            bool[] l_maskColumns = new bool[0];
            if(sp)
            {
                l_maskColumns = m_singleMaskColumns;
            }
            else
            {
                l_maskColumns = m_multiMaskColumns;
            }

            // Curves
            d.Curve[][] l_curves = new d.Curve[l_maskColumns.Length][];

            // PlotCurves
            for (int c = 0; c < l_maskColumns.Length; c++)
            {
                Color l_mainColor = m_Colors[c];
                Color[] l_secondariesColor = new Color[2];
                l_secondariesColor[0] = l_mainColor;
                l_secondariesColor[1] = l_mainColor + 0.5f * new Color(1, 1, 1);

                // Plot Curves.
                List<d.Curve> curvesInThisColumn = new List<d.Curve>();
                // Read timeLine
                Data.Visualisation.TimeLine l_timeLine;
                Data.Visualisation.Column l_column;
                if (sp)
                {
                    l_column = VisualisationLoaded.SP_Visualisation.Columns[c];
                    l_timeLine = VisualisationLoaded.SP_VisualisationData.Columns[c].TimeLine;
                }
                else
                {
                    l_column = VisualisationLoaded.MP_Visualisation.Columns[c];
                    l_timeLine = VisualisationLoaded.MP_VisualisationData.Columns[c].TimeLine;
                }

                for (int p = 0; p < plotsToCompare.Length; p++)
                {
                    // Find bloc to read.
                    Data.TrialMatrix.TrialMatrix trialMatrixData = m_trialMatrixByProtocol[l_column.Protocol][p];
                    TrialMatrix.TrialMatrix trialMatrix = System.Array.Find(m_trialMatrix.TrialMatrix, t => t.TrialMatrixData == trialMatrixData);
                    Bloc bloc = null;
                    foreach (Line line in trialMatrix.Lines)
                    {
                        foreach (Bloc blocInTheLine in line.Blocs)
                        {
                            if ((sp && blocInTheLine.Data.PBloc == VisualisationLoaded.SP_Visualisation.Columns[c].Bloc) || (!sp && blocInTheLine.Data.PBloc == VisualisationLoaded.MP_Visualisation.Columns[c].Bloc))
                            {
                                bloc = blocInTheLine;
                                break;
                            }
                        }
                    }

                    // Read and average data.
                    float[] data = new float[bloc.Data.Lines[0].Data.Length];
                    if (bloc.SelectedLines.Length > 1)
                    {
                        float[] standardDeviations = new float[data.Length];
                        for (int i = 0; i < data.Length; i++)
                        {
                            List<float> l_dataList = new List<float>();
                            for (int l = 0; l < bloc.Data.Lines.Length; l++)
                            {
                                if (bloc.SelectedLines.Contains(l))
                                {
                                    l_dataList.Add(bloc.Data.Lines[l].Data[i]);
                                }
                            }
                            //Find selectedLines
                            data[i] = Tools.CSharp.MathfExtension.Average(l_dataList.ToArray());
                            standardDeviations[i] = Tools.CSharp.MathfExtension.StandardDeviation(l_dataList.ToArray());
                        }

                        // Generate points.
                        int pMin = l_timeLine.Min.Position;
                        int pMax = l_timeLine.Max.Position;
                        float min = l_timeLine.Min.Value;
                        float max = l_timeLine.Max.Value;
                        Vector2[] points = new Vector2[pMax + 1 - pMin];
                        for (int i = pMin; i <= pMax; i++)
                        {
                            float absciss = min + ((max - min) * (i - pMin) / (pMax - pMin));
                            points[i] = new Vector2(absciss, data[i]);
                        }

                        //Create curve
                        curvesInThisColumn.Add(new d.CurveWithStandardDeviation("C" + (c + 1) + " " + plotsToCompare[p].Name, 2, l_secondariesColor[p], points, standardDeviations, Tools.Unity.Graph.Point.Style.Round, true));
                    }
                    else if(bloc.SelectedLines.Length == 1)
                    {
                        data = bloc.Data.Lines[bloc.SelectedLines[0]].Data;
                        // Generate points.
                        int pMin = l_timeLine.Min.Position;
                        int pMax = l_timeLine.Max.Position;
                        float min = l_timeLine.Min.Value;
                        float max = l_timeLine.Max.Value;
                        Vector2[] points = new Vector2[pMax + 1 - pMin];
                        for (int i = pMin; i <= pMax; i++)
                        {
                            float absciss = min + ((max - min) * (i - pMin) / (pMax - pMin));
                            points[i] = new Vector2(absciss, data[i]);
                        }

                        //Create curve
                        curvesInThisColumn.Add(new d.Curve("C" + (c + 1) + " " + plotsToCompare[p].Name, 2, l_secondariesColor[p], points, Tools.Unity.Graph.Point.Style.Round, true));
                    }
                    else
                    {
                        continue;
                    }
                }

                if (!sp)
                {
                    //ROI Curves
                    //Graph.Curve[] l_ROIcurves = new Graph.Curve[m_columns.Length];

                    //float[] l_ROIData = new float[VisualisationLoaded.MultiVisualisationData.Columns[c].Values[0].Length];
                    //for (int i = 0; i < l_ROIData.Length; i++)
                    //{
                    //    //VisualisationLoaded.MultiVisualisation
                    //}
                    //    // Average Plots ROI
                    //    float[][] l_ROIdata = new float[l_nbColumn][];
                    //    for (int i = 0; i < l_nbColumn; i++)
                    //    {
                    //        float[] l_ROIColumnData = new float[VisualisationLoaded.MultiVisualisationData.Columns[i].Values[0].Length];
                    //        for (int c = 0; c < l_ROIColumnData.Length; c++)
                    //        {
                    //            float l_sum = 0;
                    //            int l_nbPlots = 0;
                    //            for (int p = 0; p < plots.Length; p++)
                    //            {
                    //                if (plotUsed[i][p])
                    //                {
                    //                    l_nbPlots++;
                    //                    l_sum += VisualisationLoaded.MultiVisualisationData.Columns[i].Values[p][c];
                    //                }
                    //            }
                    //            l_ROIColumnData[c] = l_sum / l_nbPlots;
                    //        }
                    //        l_ROIdata[i] = l_ROIColumnData;
                    //    }

                    //    // ROICurves
                    //    for (int i = l_nbColumn; i < 2* l_nbColumn; i++)
                    //    {
                    //        l_curves[i] = new Graph.Curve();
                    //        int column = i - l_nbColumn;
                    //        if (i < 7)
                    //        {
                    //            l_curves[i].Color = l_colors[i];
                    //        }
                    //        else
                    //        {
                    //            l_curves[i].Color = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));
                    //        }
                    //        float[] l_data = l_ROIdata[column];
                    //        int l_nbData = l_data.Length;
                    //        Data.Visualisation.TimeLine l_timeLine = VisualisationLoaded.MultiVisualisationData.Columns[column].TimeLine;
                    //        int pMin = l_timeLine.Min.Position;
                    //        int pMax = l_timeLine.Max.Position;
                    //        float min = l_timeLine.Min.Value;
                    //        float max = l_timeLine.Max.Value;
                    //        Vector2[] l_points = new Vector2[l_nbData];
                    //        for (int p = pMin; p < pMax; p++)
                    //        {
                    //            float absciss = min + ((max - min) * (p - pMin) / (pMax - 1 - pMin));
                    //            l_points[p] = new Vector2(absciss, l_data[p]);
                    //        }
                    //        l_curves[i].Data = l_points;
                    //        l_curves[i].Label = "C"+(column+1)+" ROI";
                    //        l_curves[i].Size = 2;
                    //    }
                }
                l_curves[c] = curvesInThisColumn.ToArray();
            }
            m_curves = l_curves;
        }

        bool IsTheSamePatient(p.PlotID[] plots)
        {
            bool l_isTheSamePatient = true;
            if (plots.Length > 0)
            {
                p.Patient l_patient = plots[0].Patient;
                foreach (p.PlotID plot in plots)
                {
                    if(l_patient != plot.Patient)
                    {
                        l_isTheSamePatient = false;
                        break;
                    }
                }
            }
            return l_isTheSamePatient;
        }
        #endregion
    }
}