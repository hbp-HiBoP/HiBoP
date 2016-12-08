using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using HBP.Data.Experience.Dataset;

namespace HBP.Data.Visualisation
{
    [Serializable]
    public abstract class Visualisation : ICloneable , ICopiable
    {
        #region Properties
        [SerializeField]
        protected string id;
        public string ID
        {
            get { return id; }
            private set { id = value; }
        }

        [SerializeField]
        protected string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [SerializeField]
        protected List<Column> columns;
        public ReadOnlyCollection<Column> Columns
        {
            get { return new ReadOnlyCollection<Column>(columns); }
        }
        #endregion

        #region Constructor
        protected Visualisation(string name, List<Column> columns, string id)
        {
            ID = id;
            Name = name;
            this.columns = columns;
        }
        protected Visualisation(string name, List<Column> columns) : this(name,columns,Guid.NewGuid().ToString())
        {
        }
        protected Visualisation() : this("New visualisation",new List<Column>())
        {
        }
        #endregion

        #region Public Methods
        public void AddColumn(Column column)
        {
            columns.Add(column);
        }
        public void AddColumn(Column[] columns)
        {
            foreach(Column column in columns)
            {
                AddColumn(column);
            }
        }
        public void RemoveColumn(Column column)
        {
            columns.Remove(column);
        }
        public void RemoveColumn(Column[] columns)
        {
            foreach (Column column in columns)
            {
                RemoveColumn(column);
            }
        }
        public void RemoveColumn(int column)
        {
            columns.RemoveAt(column);
        }
        public void SwapColumns(int column1,int column2)
        {
            Column tmp = columns[column1];
            columns[column1] = columns[column2];
            columns[column2] = tmp;
        }
        public void SetColumns(Column[] columns)
        {
            this.columns = new List<Column>(columns);
        }
        public void ClearColumns()
        {
            columns = new List<Column>();
        }
        public abstract bool isVisualisable();
        public abstract DataInfo[] GetDataInfo(Column column);
        public abstract void SaveXML(string path);
        public abstract void SaveJSon(string path);
        public virtual void Copy(object copy)
        {
            Visualisation visualisation = copy as Visualisation;
            Name = visualisation.Name;
            SetColumns(visualisation.Columns.ToArray());
            ID = visualisation.ID;
        }
        #endregion

        #region Operators
        public abstract object Clone();
        #endregion
    }
}