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
        Dictionary<int, List<int>> m_IndexByCode;
        Dictionary<int, List<int>> m_NotUsedIndexByCode;
		#endregion

		#region Constructor
        public POS(Dictionary<int,List<int>> indexByCode)
        {
            m_IndexByCode = indexByCode;
        }
		public POS(string path)
		{
            m_IndexByCode = Load(path).m_IndexByCode;
		}
        public POS() : this(new Dictionary<int, List<int>>()) { }
        #endregion

        #region Public Methods
        public static POS Load(string path)
        {
            if (String.IsNullOrEmpty(path)) throw new EmptyFilePathException();
            FileInfo posFile = new FileInfo(path);
            if(!posFile.Exists) throw new FileNotFoundException();
            if (posFile.Extension != EXTENSION) throw new Exception("Wrong extension");

            Dictionary<int, List<int>> indexByCode = new Dictionary<int, List<int>>();
            int code, index;
            foreach (string line in File.ReadAllLines(path))
            {
                if (ReadLine(line, out code, out index))
                {
                    if (!indexByCode.ContainsKey(code)) indexByCode[code] = new List<int>();
                    indexByCode[code].Add(index);
                }
            }
            return new POS(indexByCode);
        }
        public void Save(string path)
        {
            if (String.IsNullOrEmpty(path)) throw new EmptyFilePathException();
            FileInfo posFile = new FileInfo(path);
            if (posFile.Extension != EXTENSION) throw new Exception("Wrong extension");

            IOrderedEnumerable<Tuple<int,int>> indexAndCode = (from pair in m_IndexByCode from index in pair.Value select new Tuple<int,int>(index,pair.Key)).OrderBy((tuple) => tuple.Object1);
            using (StreamWriter streamWriter = new StreamWriter(posFile.FullName))
            {
                foreach (Tuple<int,int> pair in indexAndCode)
                {
                    streamWriter.WriteLine(GenerateLine(pair.Object2, pair.Object1));
                }
            }
        }
		public IEnumerable<int> GetIndexes(IEnumerable<int> codes)
		{
            return from code in codes from index in GetIndexes(code) select index;
        }
		public IEnumerable<int> GetIndexes(int code)
		{
            return m_IndexByCode.ContainsKey(code) ? m_IndexByCode[code] : new List<int>();
		}
        public bool IsCompatible(Experience.Protocol.Protocol protocol)
        {
            return protocol.Blocs.All(bloc => bloc.MainEvent.Codes.Any(code => m_IndexByCode.ContainsKey(code)));
        }
        #endregion

        #region Private Methods
        static bool ReadLine(string line, out int code, out int index)
        {
            int state; code = -1; index = -1;
            string[] elements = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            return elements.Length == 3 && int.TryParse(elements[0], out index) && int.TryParse(elements[1], out code) && int.TryParse(elements[2], out state) && state == 0;
        }
        static string GenerateLine(int code,int index)
        {
            return index + '\t' + code + '\t' + 0.ToString();
        }
        #endregion
    }
}