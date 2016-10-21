using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace HBP.Data.Localizer
{
	public class Header
	{
		#region Attributs
		Measure[] m_measures;
		public Measure[] Measures{get{return m_measures;}}

		Channel[] m_channels;
		public Channel[] Channels{get{return m_channels;}}

		Dictionary<DataChannel,IndexDataChannel> m_DataChannelToIndex = new Dictionary<DataChannel, IndexDataChannel>();
		Dictionary<IndexDataChannel,DataChannel> m_IndexToDataChannel = new Dictionary<IndexDataChannel, DataChannel>();

		float m_samplingFrequency;
		public float SamplingFrequency{get{return m_samplingFrequency;}}

		int m_sampleNumber;
		public int SampleNumber{get{return m_sampleNumber;}}

		IntPtr m_cStruct;
		public IntPtr cStruct{get{return m_cStruct;}}
		#endregion

		#region Dll_import
		[DllImport("EEG",EntryPoint="ReadEEGHeader", CallingConvention =CallingConvention.Cdecl,CharSet = CharSet.Ansi)]
		static extern IntPtr ReadEEGHeader([MarshalAs(UnmanagedType.LPStr)] string eegFilePath);
		
		[DllImport("EEG",EntryPoint="GetBackEEGHeader", CallingConvention =CallingConvention.Cdecl,CharSet = CharSet.Ansi)]
		static extern void GetBackEEGHeader(IntPtr pEEGStruct,ref Header_marshalling elanHeader);
		
		[DllImport("EEG",EntryPoint="ReadMeasure", CallingConvention =CallingConvention.Cdecl,CharSet = CharSet.Ansi)]
		static extern void ReadMeasure(IntPtr pEEGStruct,ref Measure measure,int nbMeasure);
		
		[DllImport("EEG",EntryPoint="ReadChannel", CallingConvention =CallingConvention.Cdecl,CharSet = CharSet.Ansi)]
		static extern void ReadChannel(IntPtr pEEGStruct,ref Channel channel,int nbChannel);
		
		[DllImport("EEG",EntryPoint="FreeData", CallingConvention =CallingConvention.Cdecl,CharSet = CharSet.Ansi)]
		static extern void FreeDllData(IntPtr headerStruct);
		#endregion

		#region Constructor / Destructor
		public Header(string path)
		{
			// Create the C struct and read the header in the dll
			Header_marshalling l_header_marshalling = new Header_marshalling();
			m_cStruct = ReadEEGHeader(path);

			// Start to recup the header on the c struct
			GetBackEEGHeader(cStruct, ref l_header_marshalling);

			// Load the c struct on the c# struct
			Load(path,l_header_marshalling);

			// GenerateDictionary
			GenerateDictionary();

		}

		~Header()
		{
			// Free the struct in the dll
			FreeDllData(cStruct);

			// Assign the pointer at Null
			m_cStruct=IntPtr.Zero;
		}
		#endregion

		#region Public Methods
		public IndexDataChannel TranslateDataChannel(DataChannel dataChannel)
		{
			if(!m_DataChannelToIndex.ContainsKey(dataChannel))
			{
				Debug.LogError("Key not found!");
				return new IndexDataChannel(-1,-1);
			}
			else
			{
				return m_DataChannelToIndex[dataChannel];
			}
		}

		public DataChannel TranslateDataChannel(IndexDataChannel indexDataChannel)
		{
			if(!m_IndexToDataChannel.ContainsKey(indexDataChannel))
			{
				Debug.LogError("Key not found!");
				return new DataChannel();
			}
			else
			{
				return m_IndexToDataChannel[indexDataChannel];
			}
		}
		#endregion

		#region Private Methods
		void Load(string path,Header_marshalling eegHeader)
		{
			// Create the measures array then read the measures in the loop
			m_measures = new Measure[eegHeader.measure_channel_nb];
			for (int i = 0; i < m_measures.Length; i++) 
			{
				ReadMeasure(cStruct,ref m_measures[i],i);
			}

			// Create the channels array then read the channels in the loop
			m_channels = new Channel[eegHeader.chan_nb];
			for (int i = 0; i < m_channels.Length; i++) 
			{
				ReadChannel(cStruct,ref m_channels[i],i);
			}

			// Assign the sample number and sampling frequency
			m_sampleNumber = eegHeader.samp_nb;
			m_samplingFrequency = eegHeader.sampling_freq;
		}

		void GenerateDictionary()
		{
            m_DataChannelToIndex = new Dictionary<DataChannel, IndexDataChannel>();
            m_IndexToDataChannel = new Dictionary<IndexDataChannel, DataChannel>();
			for (int m = 0; m < Measures.Length; m++) 
			{
				for (int c = 0; c < Channels.Length; c++) 
				{
                    DataChannel l_Datachannel = new DataChannel(Measures[m], Channels[c]);
                    IndexDataChannel l_indexDataChannel = new IndexDataChannel(m, c);
                    if (!m_DataChannelToIndex.ContainsKey(l_Datachannel) || !m_IndexToDataChannel.ContainsKey(l_indexDataChannel))
                    {
                        m_DataChannelToIndex.Add(new DataChannel(Measures[m], Channels[c]), new IndexDataChannel(m, c));
                        m_IndexToDataChannel.Add(new IndexDataChannel(m, c), new DataChannel(Measures[m], Channels[c]));
                    }
				}
			}
		}
		#endregion

		#region Private MarshallingStruct
		[StructLayout(LayoutKind.Sequential)]
		struct Header_marshalling
		{
			public int chan_nb; /* Number of channels. */
			public int measure_channel_nb; /* Number of measures for each channel. */
			public int samp_nb; /* Number of samples. */
			public float sampling_freq; /* Sampling frequency. */
		}
		#endregion

		#region Public Struct
		public struct IndexDataChannel
		{
			public int Channel;
			public int Measure;

			public IndexDataChannel(int measure,int channel)
			{
				Measure = measure;
				Channel = channel;
			}
		}

		public struct DataChannel
		{
			public Measure Measure;
			public Channel Channel;
			
			public DataChannel(Measure measure,Channel channel)
			{
				Measure = measure;
				Channel = channel;
			}
		}

		#endregion
	}
}
