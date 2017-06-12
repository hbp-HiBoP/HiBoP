using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace HBP.Data.Localizer
{
	public class POS
	{
        #region Attributs
        public const string EXTENSION = ".pos";
		public Dictionary<int,List<int>> IndexSampleByCode { get; set; }
		#endregion

		#region Constructor
        public POS()
        { 
            IndexSampleByCode = new Dictionary<int, List<int>>();
        }
		public POS(string path): this()
		{
            if (!String.IsNullOrEmpty(path)) throw new ArgumentException();
            if (!File.Exists(path)) throw new FileNotFoundException();

            foreach (string line in File.ReadAllLines(path))
            {
                int code, sample;
                if(ReadLine(line,out code, out sample))
                {
                    if (IndexSampleByCode.ContainsKey(code))
                    {
                        IndexSampleByCode[code].Add(sample);
                    }
                    else
                    {
                        List<int> samples = new List<int>();
                        samples.Add(sample);
                        IndexSampleByCode.Add(code, samples);
                    }
                }                  
            }      
		}
		#endregion

		#region Public Methods
		public IEnumerable<int> GetSamples(IEnumerable<int> codes)
		{
            List<int> samples = new List<int>();
            foreach (int code in codes)
            {
                samples.AddRange(GetSamples(code));
            }
            return samples.ToArray();
        }
		public IEnumerable<int> GetSamples(int code)
		{
            if(IndexSampleByCode.ContainsKey(code))
            {
                return IndexSampleByCode[code].ToArray();
            }
            else
            {
                return new int[0];
            }
		}
        #endregion

        #region Private Methods
        bool ReadLine(string line,out int code,out int sample)
        {
            code = -1;
            sample = -1;
            string[] elements = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            return elements.Length == 3 && int.TryParse(elements[0], out sample) && int.TryParse(elements[1], out code);
        }
        #endregion
    }
}