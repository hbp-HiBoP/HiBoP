using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using HBP.Module3D;
using HBP.UI.TrialMatrix;
using HBP.Data.Anatomy;
using HBP.Data.Visualisation;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using Tools.Unity.Graph.Data;

namespace HBP.UI.Graph
{
    [RequireComponent(typeof(ZoneResizer))]
    public class GraphsGestion : MonoBehaviour
    {
        #region Properties
        // Trial matrix
        TrialMatrixList trialMatrixList;
        Dictionary<Protocol, Vector2> trialLimitsByProtocol = new Dictionary<Protocol, Vector2>();
        Dictionary<Protocol, Data.TrialMatrix.TrialMatrix[]> trialMatrixByProtocol = new Dictionary<Protocol, Data.TrialMatrix.TrialMatrix[]>();
        bool lineSelectable = false;

        // Curves
        GraphGestion graphGestion;
        Color[] colors = new Color[7] { Color.blue, Color.red, Color.green, Color.cyan, Color.grey, Color.magenta, Color.yellow };
        Curve[][] curves = new Curve[0][];
        Curve[] ROIcurves = new Curve[0];

        // Plots
        PlotID[] plots;
        bool[][] maskPlots;

        // Columns
        Column[] columns
        {
            get
            {
                switch(type)
                {
                    default:
                        return new Column[0];
                    case TypeEnum.Single:
                        return VisualisationLoaded.SP_Visualisation.Columns.ToArray();
                    case TypeEnum.Multi:
                        return VisualisationLoaded.MP_Visualisation.Columns.ToArray(); 
                }
            }
        }
        bool[] maskColumns
        {
            get
            {
                switch (type)
                {
                    default:
                        return new bool[0];
                    case TypeEnum.Single:
                        return VisualisationLoaded.SP_Columns;
                    case TypeEnum.Multi:
                        return VisualisationLoaded.MP_Columns;
                }
            }
        }

        // Type
        ZoneResizer zoneResizer;
        enum TypeEnum { None, Single, Multi };
        TypeEnum type = TypeEnum.None;
        TypeEnum Type
        {
            get
            {
                return type;
            }
            set
            {
                switch (value)
                {
                    case TypeEnum.None:
                        zoneResizer.Ratio = 1.0f;
                        break;
                    case TypeEnum.Single:
                        if(type == TypeEnum.None) zoneResizer.Ratio = 0.5f;
                        break;
                    case TypeEnum.Multi:
                        if (type == TypeEnum.None) zoneResizer.Ratio = 0.5f;
                        break;
                }
                type = value;
            }
        }
        #endregion

        #region Initialize
        void Awake()
        {
            trialMatrixList = transform.FindChild("TrialZone").FindChild("TrialMatrix").FindChild("Viewport").FindChild("Content").GetComponent<TrialMatrixList>();
            graphGestion = transform.FindChild("Graph").GetComponent<GraphGestion>();
            zoneResizer = transform.parent.GetComponent<ZoneResizer>();
            AddListerners();
            Type = TypeEnum.None;
        }
        void AddListerners()
        {
            HBP3DModule command;
            command = FindObjectOfType<HBP3DModule>();
            command.SiteInfoRequest.AddListener((plotResquest) => OnDisplayPlots(plotResquest));
            command.UpdateColumnMinimizedState.AddListener((sp, columns) => OnMinimizeColumns());
        }
        #endregion

        #region Handlers Methods
        public void OnSelectLines(int[] lines, Data.Experience.Protocol.Bloc bloc, bool additive)
        {
            if(lineSelectable)
            {
                foreach (TrialMatrix.TrialMatrix trial in trialMatrixList.TrialMatrix)
                {
                    trial.SelectLines(lines, bloc, additive);
                }
                GenerateCurves();
                DisplayCurves();
            }  
        }
        void OnDisplayPlots(SiteRequest plotRequest)
        {
            // Declare plots.
            List<PlotID> l_plotsToDisplay = new List<PlotID>();
            bool[][] l_plotsUsed = plotRequest.maskColumn.Select(a => a.ToArray()).ToArray();

            // Read plots.
            if (plotRequest.spScene)
            {
                if(VisualisationLoaded.SP_VisualisationData != null)
                {
                    if (plotRequest.idSite1 > 0 && plotRequest.idSite1 < VisualisationLoaded.SP_VisualisationData.PlotsID.Count)
                    {
                        l_plotsToDisplay.Add(VisualisationLoaded.SP_VisualisationData.PlotsID[plotRequest.idSite1]);
                    }
                    if (plotRequest.idSite2 > 0 && plotRequest.idSite2 < VisualisationLoaded.SP_VisualisationData.PlotsID.Count)
                    {
                        l_plotsToDisplay.Add(VisualisationLoaded.SP_VisualisationData.PlotsID[plotRequest.idSite2]);
                    }
                }
            }
            else
            {
                if(VisualisationLoaded.MP_VisualisationData != null)
                {
                    if (plotRequest.idSite1 > 0 && plotRequest.idSite1 < VisualisationLoaded.MP_VisualisationData.PlotsID.Count)
                    {
                        l_plotsToDisplay.Add(VisualisationLoaded.MP_VisualisationData.PlotsID[plotRequest.idSite1]);
                    }
                    if (plotRequest.idSite2 > 0 && plotRequest.idSite2 < VisualisationLoaded.MP_VisualisationData.PlotsID.Count)
                    {
                        l_plotsToDisplay.Add(VisualisationLoaded.MP_VisualisationData.PlotsID[plotRequest.idSite2]);
                    }
                }
            }
            ComparePlots(l_plotsToDisplay.ToArray(), l_plotsUsed, plotRequest.spScene);
        }
        void OnMinimizeColumns()
        {
            if(Type != TypeEnum.None)
            {
                DisplayTrialMatrix();
                GenerateCurves();
                DisplayCurves();
            }
        }
        #endregion

        #region Private Methods
        // General
        void ComparePlots(PlotID[] plotsToCompare, bool[][] plotsUsed, bool sp)
        {
            plots = plotsToCompare;
            maskPlots = plotsUsed;
            lineSelectable = IsSamePatient(plotsToCompare);
            if (sp) Type = TypeEnum.Single;
            else Type = TypeEnum.Multi;

            GenerateTrialMatrix();
            DisplayTrialMatrix();
            GenerateCurves();
            DisplayCurves();
        }
        bool IsSamePatient(PlotID[] plots)
        {
            bool isSamePatients = true;
            if (plots.Length > 0)
            {
                Data.Patient patient = plots[0].Patient;
                foreach (PlotID plot in plots)
                {
                    if (patient != plot.Patient)
                    {
                        isSamePatients = false;
                        break;
                    }
                }
            }
            return isSamePatients;
        }

        // Trial matrix
        void GenerateTrialMatrix()
        {
            UnityEngine.Profiling.Profiler.BeginSample("GenerateTrialMatrix()");
            // Save value
            trialLimitsByProtocol = new Dictionary<Protocol, Vector2>();
            foreach (TrialMatrix.TrialMatrix trialMatrix in trialMatrixList.TrialMatrix)
            {
                trialLimitsByProtocol[trialMatrix.Data.Protocol] = trialMatrix.Limits;
            }

            // Find protocols to display
            List<Protocol> protocols = new List<Protocol>();
            foreach (Column column in columns)
            {
                if (!protocols.Contains(column.Protocol))
                {
                    protocols.Add(column.Protocol);
                }
            }

            // Generate trialMatrix and create the dictionary
            Dictionary<Protocol, Data.TrialMatrix.TrialMatrix[]> trialMatrixByProtocol = new Dictionary<Protocol, Data.TrialMatrix.TrialMatrix[]>();
            foreach (Protocol protocol in protocols)
            {
                Column column = columns.First(t => t.Protocol == protocol);
                Data.TrialMatrix.TrialMatrix[] trialMatrixData = new Data.TrialMatrix.TrialMatrix[plots.Length];
                for (int i = 0; i < plots.Length; i++)
                {
                    DataInfo dataInfo;
                    switch (Type)
                    {
                        case TypeEnum.Single: dataInfo = VisualisationLoaded.SP_Visualisation.GetDataInfo(column)[0]; break;
                        case TypeEnum.Multi: dataInfo = VisualisationLoaded.MP_Visualisation.GetDataInfo(plots[i].Patient, column); break;
                        default: dataInfo = new DataInfo(); break;
                    }
                    trialMatrixData[i] = new Data.TrialMatrix.TrialMatrix(dataInfo, plots[i]);
                }
                trialMatrixByProtocol.Add(protocol, trialMatrixData);
            }
            this.trialMatrixByProtocol = trialMatrixByProtocol;
            UnityEngine.Profiling.Profiler.EndSample();
        }
        void DisplayTrialMatrix()
        {
            UnityEngine.Profiling.Profiler.BeginSample("DisplayTrialMatrix()");
            List<Protocol> protocols = new List<Protocol>();
            for (int c = 0; c < columns.Length; c++)
            {
                if(!maskColumns[c])
                {
                    Protocol protocol = columns[c].Protocol;
                    if(!protocols.Contains(protocol))
                    {
                        protocols.Add(protocol);
                    }
                }
            }
            Data.TrialMatrix.TrialMatrix[][] trialMatrix = new Data.TrialMatrix.TrialMatrix[protocols.Count][];
            for (int p = 0; p < protocols.Count; p++)
            {
                trialMatrix[p] = trialMatrixByProtocol[protocols[p]];
            }
            trialMatrixList.Set(trialMatrix);
            UnityEngine.Profiling.Profiler.EndSample();
        }

        // Curves
        void GenerateCurves()
        {
            UnityEngine.Profiling.Profiler.BeginSample("GenerateCurve()");
            // Curves
            Curve[][] curves = new Curve[maskColumns.Length][];
            Curve[] ROIcurves = new Curve[maskColumns.Length];

            // PlotCurves
            for (int c = 0; c < maskColumns.Length; c++)
            {
                if(!maskColumns[c])
                {
                    Color mainColor = colors[c];
                    Color[] secondariesColor = new Color[2];
                    secondariesColor[0] = mainColor;
                    secondariesColor[1] = mainColor + 0.5f * new Color(1, 1, 1);

                    // Read timeLine
                    TimeLine timeLine;
                    switch (Type)
                    {
                        case TypeEnum.Single:
                            timeLine = VisualisationLoaded.SP_VisualisationData.Columns[c].TimeLine;
                            break;
                        case TypeEnum.Multi:
                            timeLine = VisualisationLoaded.MP_VisualisationData.Columns[c].TimeLine;
                            break;
                        default:
                            timeLine = new TimeLine();
                            break;
                    }

                    // Initialize Curves.
                    List<Curve> curvesInThisColumn = new List<Curve>();

                    for (int p = 0; p < plots.Length; p++)
                    {
                        // Find bloc to read.
                        Data.TrialMatrix.TrialMatrix trialMatrixData = trialMatrixByProtocol[columns[c].Protocol][p];
                        TrialMatrix.TrialMatrix trialMatrix = System.Array.Find(trialMatrixList.TrialMatrix, t => t.Data == trialMatrixData);
                        TrialMatrix.Bloc bloc = null;
                        foreach (Line line in trialMatrix.Lines)
                        {
                            foreach (TrialMatrix.Bloc blocInTheLine in line.Blocs)
                            {
                                if ((Type == TypeEnum.Single && blocInTheLine.Data.PBloc == VisualisationLoaded.SP_Visualisation.Columns[c].Bloc) || (Type == TypeEnum.Multi && blocInTheLine.Data.PBloc == VisualisationLoaded.MP_Visualisation.Columns[c].Bloc))
                                {
                                    bloc = blocInTheLine;
                                    break;
                                }
                            }
                        }

                        // Read and average data.
                        Data.TrialMatrix.Line[] linesToRead = bloc.Data.GetLines(bloc.SelectedLines);
                        float[] data = new float[bloc.Data.Lines[0].Data.Length];
                        if (bloc.SelectedLines.Length > 1)
                        {
                            float[] standardDeviations = new float[data.Length];
                            for (int i = 0; i < data.Length; i++)
                            {
                                List<float> l_dataList = new List<float>();
                                for (int l = 0; l < linesToRead.Length; l++)
                                {
                                    l_dataList.Add(linesToRead[l].Data[i]);
                                }

                                //Find selectedLines
                                data[i] = Tools.CSharp.MathfExtension.Average(l_dataList.ToArray());
                                standardDeviations[i] = Tools.CSharp.MathfExtension.SEM(l_dataList.ToArray());
                            }

                            // Generate points.
                            int pMin = timeLine.Start.Position;
                            int pMax = timeLine.End.Position;
                            float min = timeLine.Start.Value;
                            float max = timeLine.End.Value;
                            Vector2[] points = new Vector2[pMax + 1 - pMin];
                            for (int i = pMin; i <= pMax; i++)
                            {
                                float absciss = min + ((max - min) * (i - pMin) / (pMax - pMin));
                                points[i] = new Vector2(absciss, data[i]);
                            }

                            //Create curve
                            curvesInThisColumn.Add(new CurveWithShape("C" + (c + 1) + " " + plots[p].Name, 2, secondariesColor[p], points, standardDeviations, Tools.Unity.Graph.Point.Style.Round, true));
                        }
                        else if (bloc.SelectedLines.Length == 1)
                        {
                            data = bloc.Data.Lines[bloc.SelectedLines[0]].Data;
                            // Generate points.
                            int pMin = timeLine.Start.Position;
                            int pMax = timeLine.End.Position;
                            float min = timeLine.Start.Value;
                            float max = timeLine.End.Value;
                            Vector2[] points = new Vector2[pMax + 1 - pMin];
                            for (int i = pMin; i <= pMax; i++)
                            {
                                float absciss = min + ((max - min) * (i - pMin) / (pMax - pMin));
                                points[i] = new Vector2(absciss, data[i]);
                            }

                            //Create curve
                            curvesInThisColumn.Add(new Curve("C" + (c + 1) + " " + plots[p].Name, 2, secondariesColor[p], points, Tools.Unity.Graph.Point.Style.Round, true));
                        }
                        else
                        {
                            continue;
                        }
                    }
                    curves[c] = curvesInThisColumn.ToArray();

                    // ROI
                    if (Type == TypeEnum.Multi)
                    {
                        float[] l_ROIColumnData = new float[VisualisationLoaded.MP_VisualisationData.Columns[c].Values[0].Length];
                        for (int i = 0; i < l_ROIColumnData.Length; i++)
                        {
                            float l_sum = 0;
                            int l_nbPlots = 0;
                            for (int plot = 0; plot < maskPlots[c].Length; plot++)
                            {
                                if (maskPlots[c][plot])
                                {
                                    l_nbPlots++;
                                    l_sum += VisualisationLoaded.MP_VisualisationData.Columns[c].Values[plot][i];
                                }
                            }
                            l_ROIColumnData[i] = l_sum / l_nbPlots;
                        }
                        int pMin = timeLine.Start.Position;
                        int pMax = timeLine.End.Position;
                        float min = timeLine.Start.Value;
                        float max = timeLine.End.Value;
                        Vector2[] l_points = new Vector2[pMax - pMin];
                        for (int p = pMin; p < pMax; p++)
                        {
                            float absciss = min + ((max - min) * (p - pMin) / (pMax - 1 - pMin));
                            l_points[p] = new Vector2(absciss, l_ROIColumnData[p]);
                        }
                        ROIcurves[c] = new Curve("C" + (c + 1) + " ROI", 4, mainColor, l_points, Tools.Unity.Graph.Point.Style.Round, true);
                    }
                }         
            }
            this.ROIcurves = ROIcurves;
            this.curves = curves;
            UnityEngine.Profiling.Profiler.EndSample();
        }
        void DisplayCurves()
        {
            UnityEngine.Profiling.Profiler.BeginSample("DisplayCurves()");
            List<Curve> curves = new List<Curve>();
            for (int c = 0; c < maskColumns.Length; c++)
            {
                if(!maskColumns[c])
                {
                    curves.AddRange(this.curves[c]);
                    if(Type == TypeEnum.Multi)
                    {
                        curves.Add(ROIcurves[c]);
                    }
                }
            }
            graphGestion.Set(curves.ToArray());
            UnityEngine.Profiling.Profiler.EndSample();
        }
        #endregion
    }
}