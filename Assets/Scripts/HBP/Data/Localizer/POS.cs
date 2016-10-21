using System.Collections.Generic;
using System.IO;

namespace HBP.Data.Localizer
{
	public class POS
	{
		#region Attributs
		Dictionary<int,List<int>> m_eventDictionary;
		public Dictionary<int,List<int>> EventDictionary {get{return m_eventDictionary;}}
		#endregion

		#region Constructor
		public POS(string filePath)
		{
			// instantiate the dictionary
			m_eventDictionary = new Dictionary<int,List<int>>();

			// Read the file
			string[] l_lines = File.ReadAllLines(filePath);

			// initialize some values for the optimisation
			int nSample;
			int nEvent;

			// Read the lines to complete the dictionary
			foreach(string l_line in l_lines)
			{
				// Splite the line
				string[] l_line_splitted = l_line.Split('	');
				if(l_line_splitted.Length == 3)
				{
					nSample = int.Parse(l_line_splitted[0]);
					nEvent  = int.Parse(l_line_splitted[1]);

					// Test if the dictionary contains the key
					if(EventDictionary.ContainsKey(nEvent))
					{
						// Add the value at the key
						m_eventDictionary[nEvent].Add(nSample);
					}
					else
					{
						// add a new key in the dictionary
						List<int> l_value = new List<int>();
						l_value.Add(nSample);
						m_eventDictionary.Add(nEvent,l_value);
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
            //foreach(int i in CodeEvents)
            //{
            //    UnityEngine.Debug.Log(i);
            //}

            List<TrialMatrix.Event> l_result = new List<TrialMatrix.Event>();
            foreach (int codeEvent in CodeEvents)
            {
                if(EventDictionary.ContainsKey(codeEvent))
                {
                    List<int> l_list = EventDictionary[codeEvent];
                    l_list.Sort();
                    int[] l_positions = l_list.ToArray();
                    foreach (int position in l_positions)
                    {
                        //UnityEngine.Debug.Log("Position :" + position + "  Code event : " + codeEvent);
                        l_result.Add(new TrialMatrix.Event(position, codeEvent));
                    }
                }
            }
            return l_result.ToArray();
        }
		#endregion
	}
}