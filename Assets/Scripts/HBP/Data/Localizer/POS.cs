using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace HBP.Data.Localizer
{
	public class POS
	{
        #region Properties
        public const string EXTENSION = ".pos";
        Dictionary<int, List<Tuple<int,int>>> m_IndexByCode;
		#endregion

		#region Constructor
        public POS(Dictionary<int,List<Tuple<int,int>>> indexByCode)
        {
            m_IndexByCode = indexByCode;
        }
		public POS(string path)
		{
            POS pos = Load(path);
            m_IndexByCode = pos.m_IndexByCode;
		}
        public POS() : this(new Dictionary<int, List<Tuple<int,int>>>()) { }
        #endregion

        #region Public Methods
        public static POS Load(string path)
        {
            if (String.IsNullOrEmpty(path)) throw new EmptyFilePathException();
            FileInfo posFile = new FileInfo(path);
            if(!posFile.Exists) throw new FileNotFoundException();
            if (posFile.Extension != EXTENSION) throw new Exception("Wrong extension");

            Dictionary<int, List<Tuple<int,int>>> indexByCode = new Dictionary<int, List<Tuple<int,int>>();
            int code, index, state;
            foreach (string line in File.ReadAllLines(path))
            {
                if (ReadLine(line, out code, out index, out state))
                {
                    if (!indexByCode.ContainsKey(code)) indexByCode[code] = new List<Tuple<int,int>>();
                    indexByCode[code].Add(new Tuple<int, int>(index,state));
                }
            }
            return new POS(indexByCode);
        }
        public void Save(string path)
        {
            if (String.IsNullOrEmpty(path)) throw new EmptyFilePathException();
            FileInfo posFile = new FileInfo(path);
            if (posFile.Extension != EXTENSION) throw new Exception("Wrong extension");

            IEnumerable<Tuple<int, int>> indexAndCode = from pair in m_IndexByCode from index in pair.Value select new Tuple<int, int>(index, pair.Key);
            IEnumerable<Tuple<int, int>> notUsedIndexAndCode = from pair in m_NotUsedIndexAndStateByCode from index in pair.Value select new Tuple<int, int>(index, pair.Key);
            IOrderedEnumerable<Tuple<int,int>> sortedIndexAndCode = indexAndCode.Concat(notUsedIndexAndCode).OrderBy((tuple) => tuple.Object1);
            IEnumerable<string> lines = sortedIndexAndCode.Select((tuple) => GenerateLine(tuple.Object2, tuple.Object1));
            using (StreamWriter streamWriter = new StreamWriter(posFile.FullName))
            {
                foreach (var line in lines) streamWriter.WriteLine(line);
            }
        }
		public IEnumerable<int> GetIndexes(IEnumerable<int> codes)
		{
            return from code in codes from index in GetIndexes(code) select index;
        }
		public IEnumerable<int> GetIndexes(int code)
		{
            return m_IndexByCode.ContainsKey(code) ? from tuple in m_IndexByCode[code] where tuple.Object2 == 0 select tuple.Object1 : new List<int>();
		}
        public bool IsCompatible(Experience.Protocol.Protocol protocol)
        {
            return protocol.Blocs.All(bloc => bloc.MainEvent.Codes.Any(code => m_IndexByCode.ContainsKey(code)));
        }
        #endregion

        #region Private Methods
        static bool ReadLine(string line, out int code, out int index,out int state)
        {
            state = int.MinValue; code = int.MinValue; index = int.MinValue;
            string[] elements = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            bool parsing = int.TryParse(elements[0], out index) && int.TryParse(elements[1], out code) && int.TryParse(elements[2], out state);
            bool format = elements.Length == 3;
            return parsing && format;
        }
        static string GenerateLine(int code,int index)
        {
            return index + "\t" + code + "\t" + 0.ToString();
        }
        #endregion
    }
}