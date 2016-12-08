using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HBP.Data.Visualisation
{
    public abstract class VisualisationData
    {
        #region Properties
        protected List<ColumnData> columns;
        public ReadOnlyCollection<ColumnData> Columns
        {
            get { return new ReadOnlyCollection<ColumnData>(columns); }
        }


        protected Patient.PlotID[] plotsID;
        public ReadOnlyCollection<Patient.PlotID> PlotsID
        {
            get { return new ReadOnlyCollection<Patient.PlotID>(plotsID); }
        }
        #endregion

        #region Constructors
        public VisualisationData(List<ColumnData> columns)
        {
            this.columns = columns;
        }
        #endregion

        #region  Public Methods
        public void StandardizeColumns()
        {
            // Calculate max interval.
            int l_before = 0;
            int l_after = 0;
            for (int i = 0; i < Columns.Count; i++)
            {
                int l_b = Columns[i].TimeLine.MainEvent.Position;
                int l_a = Columns[i].TimeLine.Size - l_b;
                if (l_b > l_before) l_before = l_b;
                if (l_a > l_after) l_after = l_a;
            }
            // Standardize each column.
            foreach (ColumnData column in Columns)
            {
                column.Standardize(l_before, l_after);
            }
        }
        #endregion
    }
}