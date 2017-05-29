using System.Collections.Generic;
using System.IO;

namespace HBP.Data.Localizer
{
	public class POS
	{
        #region Attributs
        public const string EXTENSION = ".pos";
		public Dictionary<int,List<int>> IndexSampleByCode { get; set; }
		#endregion

		#region Constructor
		public POS(string filePath)
		{
            // instantiate the dictionary
            IndexSampleByCode = new Dictionary<int,List<int>>();

			// Read the file
			string[] lines = File.ReadAllLines(filePath);

			// initialize some values for the optimisation
			int nSample;
			int nEvent;

			// Read the lines to complete the dictionary
			foreach(string line in lines)
			{
				// Splite the line
				string[] lineElements = line.Split('	');
				if(lineElements.Length == 3)
				{
					nSample = int.Parse(lineElements[0]);
					nEvent  = int.Parse(lineElements[1]);

					// Test if the dictionary contains the key
					if(IndexSampleByCode.ContainsKey(nEvent))
					{
                        // Add the value at the key
                        IndexSampleByCode[nEvent].Add(nSample);
					}
					else
					{
						// add a new key in the dictionary
						List<int> l_value = new List<int>();
						l_value.Add(nSample);
                        IndexSampleByCode.Add(nEvent,l_value);
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
            if(IndexSampleByCode.ContainsKey(CodeEvent))
            {
                return IndexSampleByCode[CodeEvent].ToArray();
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
                if(IndexSampleByCode.ContainsKey(codeEvent))
                {
                    List<int> l_list = IndexSampleByCode[codeEvent];
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