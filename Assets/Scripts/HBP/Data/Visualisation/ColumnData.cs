using System;
using System.Linq;
using HBP.Data.Experience.Dataset;
using System.Collections.Generic;

namespace HBP.Data.Visualisation
{
    /**
    * \class ColumnData
    * \author Adrien Gannerie
    * \version 1.0
    * \date 10 janvier 2017
    * \brief Data of the visualisation Column.
    * 
    * \details ColumnData is a class which contains all the data of the visualisation Column and contains:
    *   - \a Label.
    *   - \a Mask of the plots.
    *   - \a Time line.
    *   - \a Iconic scenario.
    *   - \a Values.
    */
    public class ColumnData
    {
        #region Properties
        private string label;
        /// <summary>
        /// Label of the column.
        /// </summary>
        public string Label { get { return label; } set { label = value; } }

        private bool[] plotMask;
        /// <summary>
        /// Mask which define if a plot have to be display. \a True if he is masked and \a False otherwhise.
        /// </summary>
        public bool[] PlotMask { get { return plotMask; } set { plotMask = value; } }

        private TimeLine timeLine;
        /// <summary>
        /// TimeLine which define the size,limits,events.
        /// </summary>
        public TimeLine TimeLine { get { return timeLine; } set { timeLine = value; } }

        private IconicScenario iconicScenario;
        /// <summary>
        /// Iconic scenario which define the labels,images to display during the timeLine. 
        /// </summary>
        public IconicScenario IconicScenario { get { return iconicScenario; } set { iconicScenario = value; } }

        private float[][] values;
        /// <summary>
        /// Values of the data for each plots.
        /// float[x][ ] : Plots.
        /// float[ ][x] : values of the plot selected.
        /// </summary>
        public float[][] Values { get { return values; } set { values = value; } }

        public ROI[] ROIList { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Create a new ColumData instance with default values.
        /// </summary>
        public ColumnData() : this("",new bool[0],new TimeLine(),new IconicScenario(),new float[0][])
        {
        }
        /// <summary>
        /// Create a new ColumnData instance.
        /// </summary>
        /// <param name="label">Label of the column.</param>
        /// <param name="plotMask">Mask of the plots.</param>
        /// <param name="timeLine">Time line of the column.</param>
        /// <param name="iconicScenario">Iconic scenario of the column.</param>
        /// <param name="values">Data of the column.</param>
        public ColumnData(string label,bool[] plotMask,TimeLine timeLine,IconicScenario iconicScenario,float[][] values)
        {
            Label = label;
            PlotMask = plotMask;
            TimeLine = timeLine;
            IconicScenario = iconicScenario;
            Values = values;
        }
        /// <summary>
        /// Create a new ColumnData instance by patients, data and column information.
        /// </summary>
        /// <param name="patients">Patients used in the Column.</param>
        /// <param name="data">Data of the eegFiles.</param>
        /// <param name="column">Column used.</param>
        public ColumnData(Patient[] patients,Experience.Dataset.Data[] data, Column column)
        {
            Setup(patients, data, column);
        }
        /// <summary>
        /// Create a new ColumnData instance by a patient, his data and the column information.
        /// </summary>
        /// <param name="patient">Patient used in the Column.</param>
        /// <param name="data">Data of the eegFiles.</param>
        /// <param name="column">Column information used.</param>
        public ColumnData(Patient patient, Experience.Dataset.Data data, Column column)
        {
            Setup( new Patient[] { patient }, new Experience.Dataset.Data[] { data }, column);
        }
        /// <summary>
        /// Create a new ColumnData instance with a visualisation.
        /// </summary>
        /// <param name="visualisation">Single patient visualisation.</param>
        /// <param name="columnIndex">Index of the column.</param>
        public ColumnData(SinglePatientVisualisation visualisation, int columnIndex)
        {
            // read experiencesData and electrodes.
            Column column = visualisation.Columns[columnIndex];
            DataInfo[] dataInfo = visualisation.GetDataInfo(column);

            DataInfo dataInfoOfThePatient = Array.Find(dataInfo, (d) => d.Patient == visualisation.Patient);
            Experience.Dataset.Data data = new Experience.Dataset.Data(dataInfoOfThePatient, false);
            Setup(new Patient[] { visualisation.Patient }, new Experience.Dataset.Data[] { data }, column);
        }
        /// <summary>
        /// Create a new ColumnData instance with a visualisation.
        /// </summary>
        /// <param name="visualisation">Multi patient visualisation.</param>
        /// <param name="columnIndex">Index of the column.</param>
        public ColumnData(MultiPatientsVisualisation visualisation, int columnIndex)
        {
            Column column = visualisation.Columns[columnIndex];
            DataInfo[] dataInfo = visualisation.GetDataInfo(column);
            Experience.Dataset.Data[] data = new Experience.Dataset.Data[dataInfo.Length];
            for (int d = 0; d < dataInfo.Length; d++)
            {
                data[d] = new Experience.Dataset.Data(dataInfo[d], true);
            }
            Setup(visualisation.Patients.ToArray(),data, column);
        }
        #endregion

        #region  Public Methods
        /// <summary>
        /// Standardize data column.
        /// </summary>
        /// <param name="before">Sample before the main event.</param>
        /// <param name="after">Sample after the main event.</param>
        public void Standardize(int before, int after)
        {
            // Calculate differences.
            int l_difBefore = before - TimeLine.MainEvent.Position;
            int l_difAfter = after - (TimeLine.Lenght - TimeLine.MainEvent.Position);

            // Resize TimeLine.
            TimeLine.Resize(l_difBefore, l_difAfter);

            // Resize Iconic Scenario.
            IconicScenario.Reposition(l_difBefore);

            // Change Values
            for (int p = 0; p < Values.Length; p++)
            {
                float[] l_data = new float[Values[p].Length + l_difBefore + l_difAfter];
                for (int c = 0; c < l_data.Length; c++)
                {
                    if (c < l_difBefore)
                    {
                        l_data[c] = Values[p][0];
                    }
                    else if (c >= l_difBefore && c < Values[p].Length + l_difBefore)
                    {
                        l_data[c] = Values[p][c - l_difBefore];
                    }
                    else
                    {
                        l_data[c] = Values[p][Values[p].Length - 1];
                    }
                }
                Values[p] = l_data;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Setup the ColumnData.
        /// </summary>
        /// <param name="patients">Patients used in the columnData.</param>
        /// <param name="data">Data used in the columnData.</param>
        /// <param name="column">Column used in the columnData.</param>
        void Setup(Patient[] patients, Experience.Dataset.Data[] data,Column column)
        {
            List<bool> maskPlots = new List<bool>();
            List<float[]> values = new List<float[]>();
            bool first = true;
            foreach(Patient patient in patients)
            {
                Experience.Dataset.Data dataForThisPatient = data.First((d) => d.Patient == patient);
                maskPlots.AddRange(dataForThisPatient.Mask);
                Localizer.Bloc[] blocs = Localizer.Bloc.AverageBlocs(dataForThisPatient.Values, dataForThisPatient.POS, column.Bloc, dataForThisPatient.Frequency);
                foreach (Localizer.Bloc bloc in blocs)
                {
                    values.Add(bloc.Data);
                }
                if (first)
                {
                    // Set TimeLine
                    Event mainEvent = new Event(column.Bloc.MainEvent.Name, blocs[0].MainEventPosition);
                    Event[] secondaryEvents = new Event[column.Bloc.SecondaryEvents.Count];
                    for (int p = 0; p < secondaryEvents.Length; p++)
                    {
                        secondaryEvents[p] = new Event(column.Bloc.SecondaryEvents[p].Name, blocs[0].SecondaryEventsPosition[p]);
                    }
                    TimeLine = new TimeLine(column.Bloc.DisplayInformations, mainEvent, secondaryEvents, data[0].Frequency);
                    IconicScenario = new IconicScenario(column.Bloc, data[0].Frequency, TimeLine);
                    first = false;
                }
            }

            // Set.
            Label = "Data : " + column.DataLabel + " Dataset : " + column.Dataset.Name + " Protocol : " + column.Protocol.Name + " Bloc : " + column.Bloc.DisplayInformations.Name;
            PlotMask = maskPlots.ToArray();
            Values = values.ToArray();
        }
        #endregion
    }
}