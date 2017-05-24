using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HBP.Data.Localizer
{
	public class POS
	{
        #region Attributs
        public const string EXTENSION = ".pos";
		Dictionary<int,List<int>> m_eventDictionary;
		public Dictionary<int,List<int>> EventDictionary {get{return m_eventDictionary;}}
		#endregion

		#region Constructor
		public POS(string filePath)
		{
			m_eventDictionary = new Dictionary<int,List<int>>(); // Instantiate dictionary.
            string[] lines = File.ReadAllLines(filePath); // Read file.

            // Read lines to complete the dictionary
            int sampleIndex, eventCode;
            foreach (string line in lines)
			{
                string[] lineElements = line.Split(new char[] { ' ', '\t' },System.StringSplitOptions.RemoveEmptyEntries); // Split the line.
                if (lineElements.Length == 3 && int.TryParse(lineElements[0], out sampleIndex) && int.TryParse(lineElements[1], out eventCode)) // Test if the line is compatible.
                {
                    if(m_eventDictionary.ContainsKey(eventCode))
                    {
                        m_eventDictionary[eventCode].Add(sampleIndex); // Add new sample index to existing event code.
                    }
                    else
                    {
                        m_eventDictionary.Add(eventCode, new List<int>() { sampleIndex }); // Add new event code with its first sample index.
                    }
                }
			}
		}
		#endregion

		#region Public Methods
		public int[] ConvertEventCodeToSampleIndex(int[] CodeEvents)
		{
            List<int> l_result = new List<int>();
            foreach(int codeEvent in CodeEvents)
            {
                l_result.AddRange(ConvertEventCodeToSampleIndex(codeEvent));
            }
            return l_result.ToArray();
        }

		public int[] ConvertEventCodeToSampleIndex(int CodeEvent)
		{
            if(EventDictionary.ContainsKey(CodeEvent))
            {
                return EventDictionary[CodeEvent].ToArray();
            }
            else
            {
                return new int[0];
            }
		}

        public TrialMatrix.Event[] ConvertEventCodeToSampleIndexAndCode(int[] CodeEvents)
        {
            List<TrialMatrix.Event> l_result = new List<TrialMatrix.Event>();
            foreach (int codeEvent in CodeEvents)
            {
                if (EventDictionary.ContainsKey(codeEvent))
                {
                    List<int> l_list = EventDictionary[codeEvent];
                    l_list.Sort();
                    int[] l_positions = l_list.ToArray();
                    foreach (int position in l_positions)
                    {
                        l_result.Add(new TrialMatrix.Event(position, codeEvent));
                    }
                }
            }
            return l_result.ToArray();
        }
		#endregion
	}
}