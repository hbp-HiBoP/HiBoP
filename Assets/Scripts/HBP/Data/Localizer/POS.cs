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
        Dictionary<int, List<Occurence>> m_OccurencesByCode;
        #endregion

		#region Constructor
        public POS(Dictionary<int,List<Occurence>> occurencesByCode)
        {
            m_OccurencesByCode = occurencesByCode;
        }
        public POS(string path, Frequency frequency)
        {
            if (String.IsNullOrEmpty(path)) throw new EmptyFilePathException();
            FileInfo posFile = new FileInfo(path);
            if (!posFile.Exists) throw new FileNotFoundException();
            if (posFile.Extension != EXTENSION && posFile.Extension != BIDS_EXTENSION) throw new Exception("Wrong extension");

            Dictionary<int, List<Occurence>> occurencesByCode = new Dictionary<int, List<Occurence>>();
            int code, index, state;
            if(posFile.Extension == EXTENSION)
            {
                foreach (string line in File.ReadAllLines(path))
                {
                    if (ReadLine(line, out code, out index, out state))
                    {
                        if (!occurencesByCode.ContainsKey(code)) occurencesByCode[code] = new List<Occurence>();
                        occurencesByCode[code].Add(new Occurence(code, index, state, frequency.ConvertNumberOfSamplesToMilliseconds(index)));
                    }
                }
            }
            else if(posFile.Extension == BIDS_EXTENSION)
            {
                foreach (string line in File.ReadAllLines(path))
                {
                    if (ReadBIDSLine(line, frequency, out code, out index, out state))
                    {
                        if (!occurencesByCode.ContainsKey(code)) occurencesByCode[code] = new List<Occurence>();
                        occurencesByCode[code].Add(new Occurence(code, index, state, frequency.ConvertNumberOfSamplesToMilliseconds(index)));
                    }
                }
            }
            m_OccurencesByCode = occurencesByCode;
        }
		public POS(string path)
		{
            if (String.IsNullOrEmpty(path)) throw new EmptyFilePathException();
            FileInfo posFile = new FileInfo(path);
            if (!posFile.Exists) throw new FileNotFoundException();
            if (posFile.Extension != EXTENSION) throw new Exception("Wrong extension");

            Dictionary<int, List<Occurence>> occurencesByCode = new Dictionary<int, List<Occurence>>();
            int code, index, state;
            foreach (string line in File.ReadAllLines(path))
            {
                if (ReadLine(line, out code, out index, out state))
                {
                    if (!occurencesByCode.ContainsKey(code)) occurencesByCode[code] = new List<Occurence>();
                    occurencesByCode[code].Add(new Occurence(code, index, state));
                }
            }
            m_OccurencesByCode = occurencesByCode;
        }
        public POS() : this(new Dictionary<int, List<Occurence>>()) { }
        #endregion

        #region Public Methods
        public void Save(string path)
        {
            if (String.IsNullOrEmpty(path)) throw new EmptyFilePathException();
            FileInfo posFile = new FileInfo(path);
            if (posFile.Extension != EXTENSION) throw new Exception("Wrong extension");

            IEnumerable<Tuple<int,int,int>> lineInfo = from pair in m_OccurencesByCode from occurence in pair.Value select new Tuple<int, int,int>(pair.Key, occurence.Index, occurence.State);
            IOrderedEnumerable<Tuple<int,int,int>> sortedIndexAndCode = lineInfo.OrderBy((tuple) => tuple.Item2);
            IEnumerable<string> lines = sortedIndexAndCode.Select((tuple) => GenerateLine(tuple.Item1, tuple.Item2, tuple.Item3));
            using (StreamWriter streamWriter = new StreamWriter(posFile.FullName))
            {
                foreach (var line in lines) streamWriter.WriteLine(line);
            }
        }
		public IEnumerable<Occurence> GetOccurences(IEnumerable<int> codes)
		{
            return from code in codes from occurence in GetOccurences(code) select occurence;
        }
		public IEnumerable<Occurence> GetOccurences(int code)
		{
            return m_OccurencesByCode.ContainsKey(code) ? from occurence in m_OccurencesByCode[code] where occurence.IsOk select occurence : new List<Occurence>();
		}
        public bool IsCompatible(Experience.Protocol.Protocol protocol)
        {
            return protocol.Blocs.All(bloc => bloc.MainSubBloc.MainEvent.Codes.Any(code => m_OccurencesByCode.ContainsKey(code)));
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
        static bool ReadBIDSLine(string line, Frequency frequency, out int code, out int index, out int state)
        {
            state = int.MinValue; code = int.MinValue; index = int.MinValue;
            string[] elements = line.Split(new char[] { '\t' });
            float seconds;
            bool parsing = float.TryParse(elements[0], out seconds) && int.TryParse(elements[2], out code);
            if (parsing)
            {
                state = 0;
                index = frequency.ConvertToRoundedNumberOfSamples(seconds);
            }
            bool format = elements.Length == 3;
            return parsing && format;
        }
        static string GenerateLine(int code,int index, int state)
        {
            return index + "\t" + code + "\t" + state;
        }
        #endregion

        #region Struct
        public struct Occurence
        {
            #region Properties
            public int Code { get; set; }
            public int Index { get; set; }
            public int State { get; set; }
            public bool IsOk { get { return State == 0; } }
            public float Time { get; set; }
            #endregion

            #region Constructors
            public Occurence(int code, int index, int state, float time = -1)
            {
                Code = code;
                Index = index;
                State = state;
                Time = time;
            }
            #endregion
        }
        #endregion
    }
}