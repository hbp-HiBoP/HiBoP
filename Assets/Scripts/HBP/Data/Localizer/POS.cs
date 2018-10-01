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
        public const string BIDS_EXTENSION = ".tsv";
        Dictionary<int, List<Tuple<int,int>>> m_IndexByCode;
        #endregion

		#region Constructor
        public POS(Dictionary<int,List<Tuple<int,int>>> indexByCode)
        {
            m_IndexByCode = indexByCode;
        }
		public POS(string path, float frequency = -1)
		{
            POS pos = Load(path, frequency);
            m_IndexByCode = pos.m_IndexByCode;
		}
        public POS() : this(new Dictionary<int, List<Tuple<int,int>>>()) { }
        #endregion

        #region Public Methods
        public static POS Load(string path, float frequency)
        {
            if (String.IsNullOrEmpty(path)) throw new EmptyFilePathException();
            FileInfo posFile = new FileInfo(path);
            if(!posFile.Exists) throw new FileNotFoundException();
            if (posFile.Extension != EXTENSION && posFile.Extension != BIDS_EXTENSION) throw new Exception("Wrong extension");

            Dictionary<int, List<Tuple<int,int>>> indexByCode = new Dictionary<int, List<Tuple<int,int>>>();
            int code, index, state;
            foreach (string line in File.ReadAllLines(path))
            {
                if (posFile.Extension == EXTENSION)
                {
                    if (ReadLine(line, out code, out index, out state))
                    {
                        if (!indexByCode.ContainsKey(code)) indexByCode[code] = new List<Tuple<int, int>>();
                        indexByCode[code].Add(new Tuple<int, int>(index, state));
                    }
                }
                else if (posFile.Extension == BIDS_EXTENSION)
                {
                    if (ReadBIDSLine(line, frequency, out code, out index, out state))
                    {
                        if (!indexByCode.ContainsKey(code)) indexByCode[code] = new List<Tuple<int, int>>();
                        indexByCode[code].Add(new Tuple<int, int>(index, state));
                    }
                }
            }
            return new POS(indexByCode);
        }
        public void Save(string path)
        {
            if (String.IsNullOrEmpty(path)) throw new EmptyFilePathException();
            FileInfo posFile = new FileInfo(path);
            if (posFile.Extension != EXTENSION) throw new Exception("Wrong extension");

            IEnumerable<Tuple<int,int,int>> lineInfo = from pair in m_IndexByCode from tuple in pair.Value select new Tuple<int, int,int>(pair.Key,tuple.Item1,tuple.Item2);
            IOrderedEnumerable<Tuple<int,int,int>> sortedIndexAndCode = lineInfo.OrderBy((tuple) => tuple.Item2);
            IEnumerable<string> lines = sortedIndexAndCode.Select((tuple) => GenerateLine(tuple.Item1, tuple.Item2, tuple.Item3));
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
            return m_IndexByCode.ContainsKey(code) ? from tuple in m_IndexByCode[code] where tuple.Item2 == 0 select tuple.Item1 : new List<int>();
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
        static bool ReadBIDSLine(string line, float frequency, out int code, out int index, out int state)
        {
            state = int.MinValue; code = int.MinValue; index = int.MinValue;
            string[] elements = line.Split(new char[] { '\t' });
            float seconds;
            bool parsing = float.TryParse(elements[0], out seconds) && int.TryParse(elements[2], out code);
            if (parsing)
            {
                state = 0;
                index = (int)(seconds * frequency);
            }
            bool format = elements.Length == 3;
            return parsing && format;
        }
        static string GenerateLine(int code,int index, int state)
        {
            return index + "\t" + code + "\t" + state;
        }
        #endregion
    }
}