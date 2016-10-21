using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;


namespace HBP.Data.Localizer
{
	public class EEG 
	{
		#region Attributs
		Header m_header;
		public Header Header{get{return m_header;}}

		Data m_data;
		public Data Data{get{return m_data;}}

		string m_filePath;
		public string FilePath{get{return m_filePath;}}

        public static string Extension = ".eeg";

		const int m_LIMITFOROPTIMISATION = 32;

		#region Dll_importation
		[DllImport("EEG",EntryPoint="ReadDataChannelSampBloc", CallingConvention =CallingConvention.Cdecl,CharSet = CharSet.Ansi)]
		static extern void ReadDllData([MarshalAs(UnmanagedType.LPStr)] string eegFilePath, IntPtr headerStruct , [In,Out]float[] data, int channelNb, int measureNb, int firstIndexSample, int sizeSample);

		[DllImport("EEG",EntryPoint="ReadDataChannel", CallingConvention =CallingConvention.StdCall)]
		static extern void ReadDataChannel(IntPtr cStruct, [MarshalAs(UnmanagedType.LPStr)] string eegFilePath, [In,Out]float[] data, int channelNb, int measureNb);

		[DllImport("EEG",EntryPoint="ReadDataAllChannels", CallingConvention =CallingConvention.StdCall)]
		static extern void ReadDataAllChannels(IntPtr cStruct, [MarshalAs(UnmanagedType.LPStr)] string eegFilePath, [In,Out]float[] data);
		#endregion
		#endregion

		#region Constructor
		public EEG(string path)
		{
			m_filePath = path;
			m_header = new Header(path);
			m_data = new Data(Header);
		}
		#endregion

		#region Public Methods
		public float[][] ReadData(Header.DataChannel[] dataChannels)
		{
			float[][] l_data = new float[dataChannels.Length][];
            
			// Calculate the number of file we have to read
			int l_NbfileToRead=0;
			foreach(Header.DataChannel dataChannel in dataChannels)
			{
				if(!Data.AlreadyRead(dataChannel))
				{
					l_NbfileToRead +=1;
				}
			}
			if(l_NbfileToRead >= m_LIMITFOROPTIMISATION)
			{
                // ReadAll
                ReadData();
				for (int c = 0; c < dataChannels.Length; c++) 
				{
					l_data[c] = Data.DataBase[dataChannels[c]];
				}
			}
			else
			{
				for (int c = 0; c < dataChannels.Length; c++) 
				{
					l_data[c] = ReadData(dataChannels[c]);
				}
			}
            return l_data;
		}

		public float[,][] ReadData()
		{
			//  Create the arrays and the read the data
			float[] l_flatData = new float[Header.Measures.Length*Header.Channels.Length*Header.SampleNumber];
			float[,][] l_returnData = new float[Header.Measures.Length,Header.Channels.Length][];
			ReadDataAllChannels(Header.cStruct,m_filePath, l_flatData);
            for (int m = 0; m < Header.Measures.Length; m++) 
			{
				for (int c = 0; c < Header.Channels.Length; c++) 
				{
                    float[] l_data = new float[Header.SampleNumber];
                    for (int i = 0; i < Header.SampleNumber; i++) 
					{
						l_data[i] = l_flatData[m*Header.Channels.Length*Header.SampleNumber+c*Header.SampleNumber+i];
					}
					Data.DataBase[Header.TranslateDataChannel(new Header.IndexDataChannel(m,c))] = l_data;
					l_returnData[m,c] = l_data;
				}
			}
			return l_returnData;
		}

		public float[] ReadData(Header.DataChannel dataChannel ,int startIndex,int size)
		{
			// Read the data
			float[] l_dataTemp = ReadData(dataChannel);

			// Copy the data on a array
			float[] l_data = new float[size];
			Array.Copy(l_dataTemp,startIndex,l_dataTemp,0,size);
			return l_data;
		}

		public float[] ReadData(Header.DataChannel dataChannel)
		{
			if(!Data.AlreadyRead(dataChannel))
			{
				float[] l_data = new float[Header.SampleNumber];
				Header.IndexDataChannel l_index = Header.TranslateDataChannel(dataChannel);
				ReadDataChannel(Header.cStruct,FilePath,l_data,l_index.Channel,l_index.Measure);
				Data.DataBase[dataChannel] = l_data;
			}
			return Data.DataBase[dataChannel];
		}
		#endregion
	}	
}