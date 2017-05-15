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
        //#region Properties
        //// Trial matrix
        //TrialMatrixList m_TrialMatrixList;
        //Dictionary<Protocol, Vector2> m_LimitsByProtocol = new Dictionary<Protocol, Vector2>();
        //Dictionary<Protocol, Data.TrialMatrix.TrialMatrix[]> m_TrialMatrixByProtocol = new Dictionary<Protocol, Data.TrialMatrix.TrialMatrix[]>();
        //bool m_LineSelectable = false;

        //// Curves
        //GraphGestion m_GraphGestion;
        //Color[] m_Colors = new Color[7] { Color.blue, Color.red, Color.green, Color.cyan, Color.grey, Color.magenta, Color.yellow };
        //Curve[][] m_Curves = new Curve[0][];
        //Curve[] m_ROIcurves = new Curve[0];

        //// Plots
        //Data.Anatomy.Site[] m_sites;
        //bool[][] maskPlots;

        //// Columns
        //Column[] columns
        //{
        //    get
        //    {
        //        switch(type)
        //        {
        //            default:
        //                return new Column[0];
        //            case TypeEnum.Single:
        //                return VisualizationLoaded.SP_Visualization.Columns.ToArray();
        //            case TypeEnum.Multi:
        //                return VisualizationLoaded.MP_Visualization.Columns.ToArray(); 
        //        }
        //    }
        //}
        //bool[] maskColumns
        //{
        //    get
        //    {
        //        switch (type)
        //        {
        //            default:
        //                return new bool[0];
        //            case TypeEnum.Single:
        //                return VisualizationLoaded.SP_Columns;
        //            case TypeEnum.Multi:
        //                return VisualizationLoaded.MP_Columns;
        //        }
        //    }
        //}

        //// Type
        //ZoneResizer zoneResizer;
        //enum TypeEnum { None, Single, Multi };
        //TypeEnum type = TypeEnum.None;
        //TypeEnum Type
        //{
        //    get
        //    {
        //        return type;
        //    }
        //    set
        //    {
        //        switch (value)
        //        {
        //            case TypeEnum.None:
        //                zoneResizer.Ratio = 1.0f;
        //                break;
        //            case TypeEnum.Single:
        //                if(type == TypeEnum.None) zoneResizer.Ratio = 0.5f;
        //                break;
        //            case TypeEnum.Multi:
        //                if (type == TypeEnum.None) zoneResizer.Ratio = 0.5f;
        //                break;
        //        }
        //        type = value;
        //    }
        //}
        //#endregion

        //#region Initialize
        //void Awake()
        //{
        //    m_TrialMatrixList = transform.FindChild("TrialZone").FindChild("TrialMatrix").FindChild("Viewport").FindChild("Content").GetComponent<TrialMatrixList>();
        //    m_GraphGestion = transform.FindChild("Graph").GetComponent<GraphGestion>();
        //    zoneResizer = transform.parent.GetComponent<ZoneResizer>();
        //    AddListerners();
        //    Type = TypeEnum.None;
        //}
        //void AddListerners()
        //{
        //    HBP3DModule command;
        //    command = FindObjectOfType<HBP3DModule>();
        //    command.OnRequestSiteInformation.AddListener((plotResquest) => OnDisplayPlots(plotResquest));
        //    command.OnMinimizeColumn.AddListener((sp, columns) => OnMinimizeColumns());
        //}
        //#endregion

        //#region Handlers Methods
        //public void OnSelectLines(int[] lines, Data.Experience.Protocol.Bloc bloc, bool additive)
        //{
        //    if(m_LineSelectable)
        //    {
        //        foreach (TrialMatrix.TrialMatrix trial in m_TrialMatrixList.TrialMatrix)
        //        {
        //            trial.SelectLines(lines, bloc, additive);
        //        }
        //        GenerateCurves();
        //        DisplayCurves();
        //    }  
        //}
        //void OnDisplayPlots(SiteRequest siteRequest)
        //{
        //    // Declare plots.
        //    List<Data.Anatomy.Site> sitesToDisplay = new List<Data.Anatomy.Site>();
        //    bool[][] l_plotsUsed = siteRequest.maskColumn.Select(a => a.ToArray()).ToArray();

        //    // Read plots.
        //    if (siteRequest.spScene)
        //    {
        //        if(VisualizationLoaded.SP_VisualizationData != null)
        //        {
        //            if (siteRequest.idSite1 > 0 && siteRequest.idSite1 < VisualizationLoaded.SP_VisualizationData.PlotsID.Count)
        //            {
        //                sitesToDisplay.Add(VisualizationLoaded.SP_VisualizationData.PlotsID[siteRequest.idSite1]);
        //            }
        //            if (siteRequest.idSite2 > 0 && siteRequest.idSite2 < VisualizationLoaded.SP_VisualizationData.PlotsID.Count)
        //            {
        //                sitesToDisplay.Add(VisualizationLoaded.SP_VisualizationData.PlotsID[siteRequest.idSite2]);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        if(VisualizationLoaded.MP_VisualizationData != null)
        //        {
        //            if (siteRequest.idSite1 > 0 && siteRequest.idSite1 < VisualizationLoaded.MP_VisualizationData.PlotsID.Count)
        //            {
        //                sitesToDisplay.Add(VisualizationLoaded.MP_VisualizationData.PlotsID[siteRequest.idSite1]);
        //            }
        //            if (siteRequest.idSite2 > 0 && siteRequest.idSite2 < VisualizationLoaded.MP_VisualizationData.PlotsID.Count)
        //            {
        //                sitesToDisplay.Add(VisualizationLoaded.MP_VisualizationData.PlotsID[siteRequest.idSite2]);
        //            }
        //        }
        //    }
        //    ComparePlots(sitesToDisplay.ToArray(), l_plotsUsed, siteRequest.spScene);
        //}
        //void OnMinimizeColumns()
        //{
        //    if(Type != TypeEnum.None)
        //    {
        //        DisplayTrialMatrix();
        //        GenerateCurves();
        //        DisplayCurves();
        //    }
        //}
        //#endregion

        //#region Private Methods
        //// General
        //void ComparePlots(Data.Anatomy.Site[] sitesToCompare, bool[][] sitesUsed, bool sp)
        //{
        //    m_sites = sitesToCompare;
        //    maskPlots = sitesUsed;
        //    m_LineSelectable = IsSamePatient(sitesToCompare);
        //    if (sp) Type = TypeEnum.Single;
        //    else Type = TypeEnum.Multi;

        //    GenerateTrialMatrix();
        //    DisplayTrialMatrix();
        //    GenerateCurves();
        //    DisplayCurves();
        //}
        //bool IsSamePatient(Data.Anatomy.Site[] sites)
        //{
        //    bool isSamePatients = true;
        //    if (sites.Length > 0)
        //    {
        //        Data.Patient patient = sites[0].Electrode.Implantation.Brain.Patient;
        //        foreach (Data.Anatomy.Site site in sites)
        //        {
        //            if (patient != site.Electrode.Implantation.Brain.Patient)
        //            {
        //                isSamePatients = false;
        //                break;
        //            }
        //        }
        //    }
        //    return isSamePatients;
        //}

        //// Trial matrix
        //void GenerateTrialMatrix()
        //{
        //    UnityEngine.Profiling.Profiler.BeginSample("GenerateTrialMatrix()");
        //    // Save value
        //    m_LimitsByProtocol = new Dictionary<Protocol, Vector2>();
        //    foreach (TrialMatrix.TrialMatrix trialMatrix in m_TrialMatrixList.TrialMatrix)
        //    {
        //        m_LimitsByProtocol[trialMatrix.Data.Protocol] = trialMatrix.Limits;
        //    }

        //    // Find protocols to display
        //    List<Protocol> protocols = new List<Protocol>();
        //    foreach (Column column in columns)
        //    {
        //        if (!protocols.Contains(column.Protocol))
        //        {
        //            protocols.Add(column.Protocol);
        //        }
        //    }

        //    // Generate trialMatrix and create the dictionary
        //    Dictionary<Protocol, Data.TrialMatrix.TrialMatrix[]> trialMatrixByProtocol = new Dictionary<Protocol, Data.TrialMatrix.TrialMatrix[]>();
        //    foreach (Protocol protocol in protocols)
        //    {
        //        Column column = columns.First(t => t.Protocol == protocol);
        //        Data.TrialMatrix.TrialMatrix[] trialMatrixData = new Data.TrialMatrix.TrialMatrix[m_sites.Length];
        //        for (int i = 0; i < m_sites.Length; i++)
        //        {
        //            DataInfo dataInfo;
        //            switch (Type)
        //            {
        //                case TypeEnum.Single: dataInfo = VisualizationLoaded.SP_Visualization.GetDataInfo(column)[0]; break;
        //                case TypeEnum.Multi: dataInfo = VisualizationLoaded.MP_Visualization.GetDataInfo(m_sites[i].Electrode.Implantation.Brain.Patient, column); break;
        //                default: dataInfo = new DataInfo(); break;
        //            }
        //            trialMatrixData[i] = new Data.TrialMatrix.TrialMatrix(dataInfo, m_sites[i]);
        //        }
        //        trialMatrixByProtocol.Add(protocol, trialMatrixData);
        //    }
        //    this.m_TrialMatrixByProtocol = trialMatrixByProtocol;
        //    UnityEngine.Profiling.Profiler.EndSample();
        //}
        //void DisplayTrialMatrix()
        //{
        //    UnityEngine.Profiling.Profiler.BeginSample("DisplayTrialMatrix()");
        //    List<Protocol> protocols = new List<Protocol>();
        //    for (int c = 0; c < columns.Length; c++)
        //    {
        //        if(!maskColumns[c])
        //        {
        //            Protocol protocol = columns[c].Protocol;
        //            if(!protocols.Contains(protocol))
        //            {
        //                protocols.Add(protocol);
        //            }
        //        }
        //    }
        //    Data.TrialMatrix.TrialMatrix[][] trialMatrix = new Data.TrialMatrix.TrialMatrix[protocols.Count][];
        //    for (int p = 0; p < protocols.Count; p++)
        //    {
        //        trialMatrix[p] = m_TrialMatrixByProtocol[protocols[p]];
        //    }
        //    m_TrialMatrixList.Set(trialMatrix);
        //    UnityEngine.Profiling.Profiler.EndSample();
        //}

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
        //        if(!maskColumns[c])
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

        //            for (int p = 0; p < m_sites.Length; p++)
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
        //                    curvesInThisColumn.Add(new CurveWithShape("C" + (c + 1) + " " + m_sites[p].Name, 2, secondariesColor[p], points, standardDeviations, Tools.Unity.Graph.Point.Style.Round, true));
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
        //                    curvesInThisColumn.Add(new Curve("C" + (c + 1) + " " + m_sites[p].Name, 2, secondariesColor[p], points, Tools.Unity.Graph.Point.Style.Round, true));
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
        //                    for (int plot = 0; plot < maskPlots[c].Length; plot++)
        //                    {
        //                        if (maskPlots[c][plot])
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
        //        if(!maskColumns[c])
        //        {
        //            curves.AddRange(this.m_Curves[c]);
        //            if(Type == TypeEnum.Multi)
        //            {
        //                curves.Add(m_ROIcurves[c]);
        //            }
        //        }
        //    }
        //    m_GraphGestion.Set(curves.ToArray());
        //    UnityEngine.Profiling.Profiler.EndSample();
        //}
        //#endregion
    }
}