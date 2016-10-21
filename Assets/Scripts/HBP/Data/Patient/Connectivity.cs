using System;

namespace HBP.Data.Patient
{
    public class Connectivity : ICloneable
    {
        #region Properties
        string m_label;
        public string Label { get { return m_label; } set { m_label = value; } }

        string m_path;
        public string Path  { get { return m_path;  } set { m_path =  value; } }
        #endregion

        #region Constructor
        public Connectivity()
        {
            Label = string.Empty;
            Path = string.Empty;
        }
        public Connectivity(string label,string path)
        {
            Label = label;
            Path = path;
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new Connectivity(Label,Path);
        }
        #endregion
    }
}