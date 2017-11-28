using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using HBP.Module3D;
using HBP.UI.TrialMatrix;
using HBP.Data.Visualization;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using Tools.Unity.Graph;
using UnityEngine.Events;
using Tools.CSharp;

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
                m_Scene.OnUpdateROI.AddListener(OnUpdateROI);
            }
        }

        // Minimize handling
        public const float MINIMIZED_THRESHOLD = 200.0f;
        bool m_RectTransformChanged;
        [SerializeField]
        RectTransform m_RectTransform;
        [SerializeField]
        GameObject m_MinimizedGameObject;
        Tools.Unity.ResizableGrid.ResizableGrid m_ParentGrid;
        /// <summary>
        /// Is the column minimzed ?
        /// </summary>
        public bool IsMinimized
        {
            get
            {
                return Mathf.Abs(m_RectTransform.rect.width - m_ParentGrid.MinimumViewWidth) <= MINIMIZED_THRESHOLD;
            }
        }
        public UnityEvent OnOpenGraphsWindow = new UnityEvent();

        // Trial matrix
        [SerializeField] TrialMatrixList m_TrialMatrixList;
        Dictionary<Protocol, Vector2> m_LimitsByProtocol = new Dictionary<Protocol, Vector2>();
        Dictionary<Protocol, Dictionary<Site, Data.TrialMatrix.TrialMatrix>> m_TrialMatrixByProtocolBySite = new Dictionary<Protocol, Dictionary<Site, Data.TrialMatrix.TrialMatrix>>();
        bool m_LineSelectable = false;

        // Curves
        [SerializeField] Tools.Unity.Graph.Graph m_Graph;
        [SerializeField] List<ColumnColor> m_Colors;
        Dictionary<Column, Dictionary<Site, CurveData>> m_CurveBySiteAndColumn = new Dictionary<Column, Dictionary<Site, CurveData>>();
        Dictionary<Column, CurveData> m_ROICurvebyColumn = new Dictionary<Column, CurveData>();

        // Plots
        Site[] m_Sites = new Site[0];

        // Type
        [SerializeField]
        ZoneResizer m_ZoneResizer;
        #endregion

        #region Handlers Methods
        public void OnSelectLines(int[] lines, TrialMatrix.Bloc bloc, bool additive)
        {
            if (m_LineSelectable)
            {
                switch (ApplicationState.GeneralSettings.TrialMatrixSettings.TrialsSynchronization)
                {
                    case Data.Settings.TrialMatrixSettings.TrialsSynchronizationType.Disable:
                        foreach (var trialMatrix in m_TrialMatrixList.TrialMatrix)
                        {
                            foreach (var line in trialMatrix.Lines)
                            {
                                foreach (var b in line.Blocs)
                                {
                                    if (b == bloc)
                                    {
                                        trialMatrix.SelectLines(lines, bloc.Data.ProtocolBloc, additive);
                                        goto @out;
                                    }
                                }
                            }
                        }
                        @out:
                        break;
                    case Data.Settings.TrialMatrixSettings.TrialsSynchronizationType.Enable:
                        foreach (TrialMatrix.TrialMatrix trial in m_TrialMatrixList.TrialMatrix)
                        {
                            trial.SelectLines(lines, bloc.Data.ProtocolBloc, additive);
                        }
                        break;
                    default:
                        break;
                }
                GenerateCurves();
                DisplayCurves();
            }
        }
        void OnRequestSiteInformation(IEnumerable<Site> sites)
        {
            if (IsMinimized) OnOpenGraphsWindow.Invoke();

            m_Sites = sites.ToArray();
            m_LineSelectable = sites.All((s) => s.Information.Patient == sites.FirstOrDefault().Information.Patient);

            GenerateTrialMatrix();
            DisplayTrialMatrix();
            GenerateCurves();
            DisplayCurves();
        }
        void OnMinimizeColumns()
        {
            DisplayCurves();
        }
        void OnUpdateROI()
        {
            GenerateCurves();
            DisplayCurves();
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_ParentGrid = GetComponentInParent<Tools.Unity.ResizableGrid.ResizableGrid>();
        }
        private void Update()
        {
            if (m_RectTransformChanged)
            {
                m_MinimizedGameObject.SetActive(IsMinimized);
                m_RectTransformChanged = false;
            }
        }
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
            Dictionary<Protocol, Dictionary<Site, Data.TrialMatrix.TrialMatrix>> trialMatrixByProtocol = new Dictionary<Protocol, Dictionary<Site, Data.TrialMatrix.TrialMatrix>>();
            foreach (Protocol protocol in protocols)
            {
                UnityEngine.Profiling.Profiler.BeginSample("new TrialMatrix array");
                Column column = Scene.ColumnManager.ColumnsIEEG.First(c => c.ColumnData.Protocol == protocol).ColumnData;
                Dictionary<Site, Data.TrialMatrix.TrialMatrix> trialMatrixData = new Dictionary<Site, Data.TrialMatrix.TrialMatrix>();
                UnityEngine.Profiling.Profiler.EndSample();

                UnityEngine.Profiling.Profiler.BeginSample("Find DataInfoBySite");
                Dictionary<Site, DataInfo> dataInfoBySite = m_Sites.ToDictionary(s => s, s => Scene.Visualization.GetDataInfo(s.Information.Patient, column));
                IEnumerable<DataInfo> dataInfoToRead = dataInfoBySite.Values.Distinct();
                UnityEngine.Profiling.Profiler.EndSample();

                UnityEngine.Profiling.Profiler.BeginSample("GetData from Manager");
                Dictionary<DataInfo, Dictionary<Data.Experience.Protocol.Bloc, Data.Localizer.Bloc[]>> epochedBlocsByProtocolBlocByDataInfo = new Dictionary<DataInfo, Dictionary<Data.Experience.Protocol.Bloc, Data.Localizer.Bloc[]>>();
                foreach (var data in dataInfoToRead)
                {
                    Dictionary<Data.Experience.Protocol.Bloc, Data.Localizer.Bloc[]> epochedBlocsByProtocolBloc = new Dictionary<Data.Experience.Protocol.Bloc, Data.Localizer.Bloc[]>();
                    foreach (var bloc in protocol.Blocs)
                    {
                        epochedBlocsByProtocolBloc.Add(bloc, DataManager.GetData(data, bloc).Blocs);
                    }
                    epochedBlocsByProtocolBlocByDataInfo.Add(data, epochedBlocsByProtocolBloc);
                }
                UnityEngine.Profiling.Profiler.EndSample();

                UnityEngine.Profiling.Profiler.BeginSample("new TrialMatrix");
                foreach (var site in m_Sites)
                {
                    trialMatrixData[site] = new Data.TrialMatrix.TrialMatrix(protocol, dataInfoBySite[site], epochedBlocsByProtocolBlocByDataInfo[dataInfoBySite[site]], site);
                }
                trialMatrixByProtocol.Add(protocol, trialMatrixData);
                UnityEngine.Profiling.Profiler.EndSample();
            }
            m_TrialMatrixByProtocolBySite = trialMatrixByProtocol;
            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.EndSample();
        }
        void DisplayTrialMatrix()
        {
            UnityEngine.Profiling.Profiler.BeginSample("DisplayTrialMatrix()");
            IEnumerable<Protocol> protocols = (from column in Scene.ColumnManager.ColumnsIEEG where !column.IsMinimized select column.ColumnData.Protocol).Distinct();
            Data.TrialMatrix.TrialMatrix[][] trialMatrix = m_TrialMatrixByProtocolBySite.Where(m => protocols.Contains(m.Key)).Select(m => m.Value.Values.ToArray()).ToArray();
            m_TrialMatrixList.Set(trialMatrix);
            UnityEngine.Profiling.Profiler.EndSample();
        }
        // Curves
        void GenerateCurves()
        {
            UnityEngine.Profiling.Profiler.BeginSample("GenerateCurve()");
            m_CurveBySiteAndColumn.Clear();
            m_ROICurvebyColumn.Clear();
            for (int c = 0; c < m_Scene.ColumnManager.ColumnsIEEG.Count; c++)
            {
                Column column = m_Scene.ColumnManager.ColumnsIEEG[c].ColumnData;
                m_CurveBySiteAndColumn[column] = new Dictionary<Site, CurveData>();
                for (int s = 0; s < m_Sites.Length; s++)
                {
                    Site site = m_Sites[s];
                    Data.TrialMatrix.TrialMatrix trialMatrixData = m_TrialMatrixByProtocolBySite[column.Protocol][site];
                    TrialMatrix.TrialMatrix trialMatrix = m_TrialMatrixList.TrialMatrix.First((t) => t.Data == trialMatrixData);
                    TrialMatrix.Bloc trialMatrixBloc = null;
                    foreach (var line in trialMatrix.Lines)
                    {
                        foreach (var bloc in line.Blocs)
                        {
                            if (bloc.Data.ProtocolBloc == column.Bloc)
                            {
                                trialMatrixBloc = bloc;
                                goto Found;
                            }
                        }
                    }
                    Found:
                    Data.TrialMatrix.Line[] linesToRead = trialMatrixBloc.Data.GetLines(trialMatrixBloc.SelectedLines);
                    float[] data = new float[linesToRead.Length > 0 ? linesToRead.First().NormalizedValues.Length : 0];
                    if (linesToRead.Length > 1)
                    {
                        // Shape
                        float[] standardDeviations = new float[data.Length];
                        for (int i = 0; i < data.Length; i++)
                        {
                            List<float> l_dataList = new List<float>();
                            for (int l = 0; l < linesToRead.Length; l++)
                            {
                                l_dataList.Add(linesToRead[l].NormalizedValues[i]);
                            }

                            //Find selectedLines
                            data[i] = l_dataList.ToArray().Mean();
                            standardDeviations[i] = l_dataList.ToArray().SEM();
                        }

                        // Generate points.
                        int pMin = column.TimeLine.Start.Position;
                        int pMax = column.TimeLine.End.Position;
                        float min = column.TimeLine.Start.Value;
                        float max = column.TimeLine.End.Value;
                        int lenght = pMax + 1 - pMin;
                        Vector2[] points = new Vector2[lenght];
                        for (int i = 0; i < lenght; i++)
                        {
                            int index = pMin + i;
                            float absciss = min + ((max - min) * (index - pMin) / (pMax - pMin));
                            points[i] = new Vector2(absciss, data[index]);
                        }

                        m_CurveBySiteAndColumn[column][site] = new ShapedCurveData("C" + (c + 1) + " " + site.Information.Name, points, standardDeviations, GetCurveColor(c,s),1.5f);
                    }
                    else if (linesToRead.Length == 1)
                    {
                        // Normal
                        data = trialMatrixBloc.Data.Lines[trialMatrixBloc.SelectedLines[0]].NormalizedValues;

                        // Generate points.
                        int pMin = column.TimeLine.Start.Position;
                        int pMax = column.TimeLine.End.Position;
                        float min = column.TimeLine.Start.Value;
                        float max = column.TimeLine.End.Value;
                        int lenght = pMax + 1 - pMin;
                        Vector2[] points = new Vector2[lenght];
                        for (int i = 0; i < lenght; i++)
                        {
                            int index = pMin + i;
                            float absciss = min + ((max - min) * (index - pMin) / (pMax - pMin));
                            points[i] = new Vector2(absciss, data[index]);
                        }

                        //Create curve
                        m_CurveBySiteAndColumn[column][site] = new CurveData("C" + (c + 1) + " " + site.Information.Name, points, GetCurveColor(c,s),1.5f);
                    }
                    else continue;
                }

                // ROI
                if (m_Scene.ColumnManager.ColumnsIEEG[c].ROIs.Count > 0 && m_Scene.ColumnManager.ColumnsIEEG[c].SelectedROI != null)
                {
                    Site[] sites = (from site in m_Scene.ColumnManager.ColumnsIEEG[c].Sites where !site.State.IsOutOfROI && !site.State.IsExcluded && !site.State.IsBlackListed && !site.State.IsMasked select site).ToArray();
                    if (sites.Length > 0)
                    {
                        float[] ROIdata = new float[sites.First().Configuration.Values.Length];
                        for (int i = 0; i < ROIdata.Length; i++)
                        {
                            List<float> sum = new List<float>(sites.Length);
                            foreach (var site in sites)
                            {
                                sum.Add(site.Configuration.NormalizedValues[i]);
                            }
                            ROIdata[i] = sum.ToArray().Mean();
                        }

                        // Generate points.
                        int pMin = column.TimeLine.Start.Position;
                        int pMax = column.TimeLine.End.Position;
                        float min = column.TimeLine.Start.Value;
                        float max = column.TimeLine.End.Value;
                        int lenght = pMax + 1 - pMin;
                        Vector2[] points = new Vector2[lenght];
                        for (int i = 0; i < lenght; i++)
                        {
                            int index = pMin + i;
                            float absciss = min + ((max - min) * (index - pMin) / (pMax - pMin));
                            points[i] = new Vector2(absciss, ROIdata[index]);
                        }
                        m_ROICurvebyColumn[column] = new CurveData("C" + (c + 1) + " " + m_Scene.ColumnManager.ColumnsIEEG[c].SelectedROI.Name, points, GetCurveColor(c,-1),3.0f);
                    }
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }
        void DisplayCurves()
        {
            UnityEngine.Profiling.Profiler.BeginSample("DisplayCurves()");
            List<CurveData> curvesToDisplay = new List<CurveData>();
            foreach (var column in m_Scene.ColumnManager.ColumnsIEEG)
            {
                if (!column.IsMinimized)
                {
                    foreach (var site in m_Sites)
                    {
                        if (m_CurveBySiteAndColumn[column.ColumnData].ContainsKey(site))
                        {
                            curvesToDisplay.Add(m_CurveBySiteAndColumn[column.ColumnData][site]);
                        }
                    }
                    if (m_ROICurvebyColumn.ContainsKey(column.ColumnData))
                    {
                        curvesToDisplay.Add(m_ROICurvebyColumn[column.ColumnData]);
                    }
                }
            }
            if (curvesToDisplay.Count > 0)
            {
                GraphData graphData = new GraphData("EEG", "Time(ms)", "Activity(mV)", Color.black, Color.white, curvesToDisplay.ToArray());
                m_Graph.Plot(graphData);
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }
        Color GetCurveColor(int column,int site)
        {
            ColumnColor columnColor = m_Colors[column];
            if(site == -1)
            {
                return columnColor.ROI;
            }
            else if(site == 0)
            {
                return columnColor.Site1;
            }
            else if (site == 1)
            {
                return columnColor.Site2;
            }
            else if (site == 2)
            {
                return columnColor.Site3;
            }
            else if (site == 3)
            {
                return columnColor.Site4;
            }
            else
            {
                return new Color();
            }
        }
        #endregion

        #region Public Methods
        public void OnRectTransformDimensionsChange()
        {
            m_RectTransformChanged = true;
        }
        #endregion
    }
}