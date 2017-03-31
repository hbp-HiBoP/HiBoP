using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Tools.CSharp;

namespace HBP.Data.Localizer
{
	public class Bloc
	{
		#region Attributs
		public float[] Data;
		public int MainEventPosition;
		public int[] SecondaryEventsPosition;
		#endregion

		#region Constructor
		public Bloc(float[] data,int mainEventPosition,int[] secondaryEventsPosition)
		{
			Data = data;
			MainEventPosition = mainEventPosition;
			SecondaryEventsPosition = secondaryEventsPosition;
		}

        public Bloc()
        {
            Data = new float[0];
            MainEventPosition = 0;
            SecondaryEventsPosition = new int[0];
        }
		#endregion

		#region Public static Methods
		public static Bloc AverageBlocs(Bloc[] blocs)
		{
			Bloc l_bloc = new Bloc();
			// initialize some important values
			int l_sampNb = blocs[0].Data.Length;
			int l_secondEventNb = blocs[0].SecondaryEventsPosition.Length;
			int l_blocNb = blocs.Length;
			
			// Set the main event position
			l_bloc.MainEventPosition = blocs[0].MainEventPosition;
			
			// Set the secondary events position
			l_bloc.SecondaryEventsPosition = new int[l_secondEventNb];
			for (int i = 0; i < l_secondEventNb; i++) 
			{
				List<int> l_secondEventIndexInBlocs = new List<int>();
				for (int p = 0; p < l_blocNb; p++) 
				{
					if(blocs[p].SecondaryEventsPosition[i] >= 0)
					{
						l_secondEventIndexInBlocs.Add(blocs[p].SecondaryEventsPosition[i]);
					}
				}
				l_bloc.SecondaryEventsPosition[i] = l_secondEventIndexInBlocs.ToArray().Median();
			}
			
			// Set the data array
			l_bloc.Data = new float[blocs[0].Data.Length];
			float l_sum;
			for (int i = 0; i < l_sampNb; i++) 
			{
				l_sum = 0;
				for (int p = 0; p < l_blocNb; p++) 
				{
					l_sum += blocs[p].Data[i];
				}
				l_sum = l_sum/l_blocNb;
				l_bloc.Data[i] = l_sum;
			}
			return l_bloc;
		}

		public static Bloc AverageBlocs(float[] data, POS pos, Experience.Protocol.Bloc bloc,float frequency)
		{
			return AverageBlocs(EpochData(data,pos,bloc,frequency));
		}

		public static Bloc[] AverageBlocs(float[][] data, POS pos, Experience.Protocol.Bloc bloc,float frequency)
		{
			Bloc[] l_blocs = new Bloc[data.Length];
			Bloc[][] l_blocsEpoched = EpochData(data,pos,bloc,frequency);
			for (int i = 0; i < l_blocs.Length; i++) 
			{
				l_blocs[i] =  AverageBlocs(l_blocsEpoched[i]);
			}
			return l_blocs;
		}


		public static Bloc[] EpochData(float[] data, POS pos, Experience.Protocol.Bloc bloc,float frequency)
		{
			// Read Index
			int[] l_mainEventIndex = pos.ConvertEventCodeToSampleIndex(bloc.MainEvent.Codes.ToArray());
			int[][] l_secondaryEventIndex = new int[bloc.SecondaryEvents.Count][];
			int nbSecondaryEventsInBloc = bloc.SecondaryEvents.Count;
			for (int i = 0; i < nbSecondaryEventsInBloc; i++) 
			{
				int[] l_secondaryEventIndexTemp = pos.ConvertEventCodeToSampleIndex(bloc.SecondaryEvents[i].Codes.ToArray());
				l_secondaryEventIndex[i] = l_secondaryEventIndexTemp;
			}

            // Calcul the size of a bloc and initialize bloc list
            int l_max = Mathf.FloorToInt((bloc.DisplayInformations.Window.End)* 0.001f * frequency);
            int l_min = Mathf.CeilToInt((bloc.DisplayInformations.Window.Start) * 0.001f * frequency);
            int l_size = l_max - l_min;
            int l_mainEventPosition = - l_min;
            List<Bloc> l_bloc = new List<Bloc>();
			
			// Create and complete blocs
			int nbMainEvent = l_mainEventIndex.Length;
			for (int i = 0; i < nbMainEvent; i++) 
			{
				// test if the bloc is in range
				int MainEventIndex = l_mainEventIndex[i];
				int StartEventIndex = MainEventIndex + l_min;
				int EndEventIndex = MainEventIndex + l_max;
				if(( StartEventIndex >=0)&&(EndEventIndex <data.Length))
				{
					// Copy the data into the bloc
					float[] l_dataTemps = new float[l_size];
					for (int p = 0; p < l_size; p++) 
					{
						int l_index = p+l_mainEventIndex[i]+ l_min;
						l_dataTemps[p] = data[l_index];
					}
					
					// Find the index of everySecondaryEvent
					int[] l_secondaryBlocEvent = new int[nbSecondaryEventsInBloc];
					for (int p = 0; p < nbSecondaryEventsInBloc; p++) 
					{
						int[] l_secondaryEvent = l_secondaryEventIndex[p];
						foreach(int index in l_secondaryEvent)
						{
							int value;
							// If the index is in range
							if((index >= StartEventIndex)&&(index <= EndEventIndex))
							{
								value = index - (l_mainEventIndex[i]+ l_min);
							}
							// If the index is not in range
							else
							{
								value = -1;
							}
							l_secondaryBlocEvent[p] = value;
							if(index > EndEventIndex)
							{
								break;
							}
						}
					}
					
					// Add the bloc
					l_bloc.Add(new Bloc(l_dataTemps,l_mainEventPosition,l_secondaryBlocEvent));
				}
			}
			return l_bloc.ToArray();
		}

		public static Bloc[][] EpochData(float[][] data, POS pos, Experience.Protocol.Bloc bloc,float frequency)
		{
			Bloc[][] l_blocs = new Bloc[data.Length][];
			for (int i = 0; i < data.Length; i++) 
			{
				l_blocs[i] = EpochData(data[i],pos,bloc,frequency);
			}
			return l_blocs;
		}
		#endregion
	}
}