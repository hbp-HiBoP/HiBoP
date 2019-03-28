using System;
using System.Runtime.InteropServices;

namespace Elan
{
    public class TF
    {
        #region Properties
        public enum DataTypeEnum { Float, Double };
        public enum WaveletTypeEnum { None, Morlet, Gabor };

        // GENERAL
        public DataTypeEnum DataType
        {
            get
            {
                return (DataTypeEnum)GetDataType(m_HandleOfParentObject);
            }
            private set
            {
                SetDataType((int)value, m_HandleOfParentObject);
            }
        }
        public int SampleNumber
        {
            get { return GetSampleNumber(m_HandleOfParentObject); }
            private set { SetSampleNumber(value, m_HandleOfParentObject); }
        }
        public float SamplingFrequency
        {
            get { return GetSamplingFrequency(m_HandleOfParentObject); }
            private set { SetSamplingFrequency(value, m_HandleOfParentObject); }
        }
        public int PreStimSampleNumber
        {
            get { return GetPreStimSampleNumber(m_HandleOfParentObject); }
            private set { SetPreStimSampleNumber(value, m_HandleOfParentObject); }
        }
        public int EventCode
        {
            get { return GetEventCode(m_HandleOfParentObject); }
            private set { GetEventCode(value, m_HandleOfParentObject); }
        }
        public int EventNumber
        {
            get
            {
                return GetEventNumber(m_HandleOfParentObject);
            }
            private set
            {
                SetEventNumber(value, m_HandleOfParentObject);
            }
        }
        public WaveletTypeEnum WaveletType
        {
            get
            {
                return (WaveletTypeEnum)GetWaveletType(m_HandleOfParentObject);
            }
            private set
            {
                SetWaveletType((int)value, m_HandleOfParentObject);
            }
        }
        public int BlackmanWindow
        {
            get
            {
                return GetBlackmanWindow(m_HandleOfParentObject);
            }
            private set
            {
                SetBlackmanWindow(value, m_HandleOfParentObject);
            }
        }
        public int FrequencyNumber
        {
            get
            {
                return GetFrequencyNumber(m_HandleOfParentObject);
            }
            private set
            {
                SetFrequencyNumber(value, m_HandleOfParentObject);
            }
        }
        public float[] Frequencies
        {
            get
            {
                float[] frequencies = new float[FrequencyNumber];
                GetFrenquencies(frequencies, m_HandleOfParentObject);
                return frequencies;
            }
            private set
            {
                FrequencyNumber = value.Length;
                SetFrenquencies(value, m_HandleOfParentObject);
            }
        }
        public float[] WaveletCharacteristic
        {
            get
            {
                float[] waveletCharacteristic = new float[FrequencyNumber];
                GetWaveletCharacteristic(waveletCharacteristic, m_HandleOfParentObject);
                return waveletCharacteristic;
            }
            private set
            {
                if (value.Length == FrequencyNumber)
                {
                    SetWaveletCharacteristic(value, m_HandleOfParentObject);
                }
                else
                {
                    Console.WriteLine("Wavelet characteristic cannot be set because the length need to be equals to number of frequency.");
                }
            }
        }
        public int[] OtherEventNumberByMeasure
        {
            get
            {
                int[] eventNumberByMeasure = new int[Readed.GetLength(0)];
                GetOtherEventNumberByMeasure(eventNumberByMeasure, m_HandleOfParentObject);
                return eventNumberByMeasure;
            }
            private set
            {
                if (value.Length == Readed.GetLength(0))
                {
                    SetOtherEventNumberByMeasure(value, m_HandleOfParentObject);
                }
                else
                {
                    Console.WriteLine("The other event number by measure cannot be set because the lenght need to be equals to number of frequency.");
                }
            }
        }
        public int[][] OtherEventByMeasure
        {
            get
            {
                int[][] otherEventByMeasure = new int[Readed.GetLength(0)][];
                for (int m = 0; m < otherEventByMeasure.Length; m++)
                {
                    int[] otherEvent = new int[OtherEventNumberByMeasure[m]];
                    GetOtherEventByMeasure(otherEvent, m, m_HandleOfParentObject);
                    otherEventByMeasure[m] = otherEvent;
                }
                return otherEventByMeasure;
            }
            private set
            {
                if (CanBeusedForOtherEventByMeasure(value))
                {
                    for (int m = 0; m < Readed.GetLength(0); m++)
                    {
                        SetOtherEventByMeasure(value[m], m, m_HandleOfParentObject);
                    }
                }
            }
        }

        HandleRef m_HandleOfParentObject;
        public bool[,] Readed { get; private set; }
        #endregion

        #region Constructor
        public TF(bool[,] readed, HandleRef handle)
        {
            m_HandleOfParentObject = handle;
            Readed = readed;
        }
        #endregion

        #region Public Methods
        public float[,,][] GetFloatData()
        {
            float[,,][] data = new float[Readed.GetLength(0), Readed.GetLength(1), Readed.GetLength(2)][];
            for (int m = 0; m < data.GetLength(0); m++)
            {
                for (int c = 0; c < data.GetLength(1); c++)
                {
                    for (int f = 0; f < data.GetLength(2); f++)
                    {
                        data[m, c,f] = GetFloatData(new Track(m, c,f));
                    }
                }
            }
            return data;
        }
        public float[][] GetFloatData(Track[] tracks)
        {
            float[][] data = new float[tracks.Length][];
            for (int i = 0; i < tracks.Length; i++)
            {
                data[i] = GetFloatData(tracks[i]);
            }
            return data;
        }
        public float[] GetFloatData(Track track)
        {
            float[] data = new float[SampleNumber];
            if (DataType == DataTypeEnum.Float)
            {
                if (Readed[track.Measure, track.Channel])
                {
                    GetFloatData(data, track.Measure, track.Channel, track.Frequency, m_HandleOfParentObject);
                }
            }
            return data;
        }

        public double[,,][] GetDoubleData()
        {
            double[,,][] data = new double[Readed.GetLength(0), Readed.GetLength(1), Readed.GetLength(2)][];
            for (int m = 0; m < data.GetLength(0); m++)
            {
                for (int c = 0; c < data.GetLength(1); c++)
                {
                    for (int f = 0; f < data.GetLength(2); f++)
                    {
                        data[m, c, f] = GetDoubleData(new Track(m, c, f));
                    }
                }
            }
            return data;
        }
        public double[][] GetDoubleData(Track[] tracks)
        {
            double[][] data = new double[tracks.Length][];
            for (int i = 0; i < tracks.Length; i++)
            {
                data[i] = GetDoubleData(tracks[i]);
            }
            return data;
        }
        public double[] GetDoubleData(Track track)
        {
            double[] data = new double[SampleNumber];
            if (DataType == DataTypeEnum.Double)
            {
                if (Readed[track.Measure, track.Channel])
                {
                    GetDoubleData(data, track.Measure, track.Channel, track.Frequency, m_HandleOfParentObject);
                }
            }
            return data;
        }
        #endregion

        #region Private Methods
        bool CanBeUsed(float[] flatData)
        {
            if (DataType == DataTypeEnum.Float)
            {
                int measureNumber = Readed.GetLength(0);
                int channelNumber = Readed.GetLength(1);
                if (flatData.Length == SampleNumber * channelNumber * measureNumber)
                {
                    return true;
                }
                else
                {
                    Console.WriteLine("Data passed aren't compatible : FlatDoubleData can't be set");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("EEG use float : FlatDoubleData can't be set.");
                return false;
            }

        }
        bool CanBeUsed(double[] flatData)
        {
            if (DataType == DataTypeEnum.Double)
            {
                int measureNumber = Readed.GetLength(0);
                int channelNumber = Readed.GetLength(1);
                if (flatData.Length == SampleNumber * channelNumber * measureNumber)
                {
                    return true;
                }
                else
                {
                    Console.WriteLine("Data passed aren't compatible : FlatDoubleData can't be set");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("EEG use float : FlatDoubleData can't be set.");
                return false;
            }

        }
        bool CanBeUsed(float[][][][] data)
        {
            int frequencyNumber = FrequencyNumber;
            int sampleNumber = SampleNumber;
            int measureNumber = Readed.GetLength(0);
            int channelNumber = Readed.GetLength(1);
            if (data.Length != measureNumber)
            {
                return false;
            }
            else
            {
                for (int m = 0; m < measureNumber; m++)
                {
                    if (data[m].Length != channelNumber)
                    {
                        return false;
                    }
                    else
                    {
                        for (int c = 0; c < channelNumber; c++)
                        {
                            if (data[m][c].Length != frequencyNumber)
                            {
                                return false;
                            }
                            else
                            {
                                for (int f = 0; f < frequencyNumber; f++)
                                {
                                    if (data[m][c][m].Length != sampleNumber)
                                    {
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }
        bool CanBeUsed(double[][][][] data)
        {
            int measureNumber = Readed.GetLength(0);
            int channelNumber = Readed.GetLength(1);
            int frequencyNumber = FrequencyNumber;
            int sampleNumber = SampleNumber;
            if (data.Length != measureNumber)
            {
                return false;
            }
            else
            {
                for (int m = 0; m < measureNumber; m++)
                {
                    if (data[m].Length != channelNumber)
                    {
                        return false;
                    }
                    else
                    {
                        for (int c = 0; c < channelNumber; c++)
                        {
                            if (data[m][c].Length != frequencyNumber)
                            {
                                return false;
                            }
                            else
                            {
                                for (int f = 0; f < frequencyNumber; f++)
                                {
                                    if (data[m][c][m].Length != sampleNumber)
                                    {
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }
        bool CanBeusedForOtherEventByMeasure(int[][] otherEventByMeasure)
        {
            int measureNumber = Readed.GetLength(0);
            int channelNumber = Readed.GetLength(1);
            if (otherEventByMeasure.GetLength(0) == measureNumber)
            {
                for (int m = 0; m < measureNumber; m++)
                {
                    if (otherEventByMeasure[m].Length != OtherEventNumberByMeasure[m])
                    {
                        Console.WriteLine("OtherEventByMeasure can't be set. The second dimension of the OtherEventByMeasure[][x] need to be equals to the OtherEventNumberByMeasure[x].");
                        return false;
                    }
                }
                return true;

            }
            else
            {
                Console.WriteLine("OtherEventByMeasure can't be set. The first dimension of the OtherEventByMeasure[x][] need to be equals to the number of measure.");
                return false;
            }
        }
        #endregion

        #region DLLImport
        [DllImport("elan", EntryPoint = "TF_GetDataType", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetDataType(HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "TF_SetDataType", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetDataType(int dataType, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "TF_GetSampleNumber", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetSampleNumber(HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "TF_SetSampleNumber", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetSampleNumber(int sampleNumber, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "TF_GetSamplingFrequency", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern float GetSamplingFrequency(HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "TF_SetSamplingFrequency", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetSamplingFrequency(float samplingFrequency, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "TF_GetPreStimSampleNumber", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetPreStimSampleNumber(HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "TF_SetPreStimSampleNumber", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetPreStimSampleNumber(int preStimSampleNumber, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "TF_GetEventCode", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetEventCode(HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "TF_SetEventCode", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void GetEventCode(int eventCode, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "TF_GetEventNumber", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetEventNumber(HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "TF_SetEventNumber", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetEventNumber(int epochNumber, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "TF_GetWaveletType", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetWaveletType(HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "TF_GetWaveletType", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetWaveletType(int waveletType, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "TF_GetBlackmanWindow", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetBlackmanWindow(HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "TF_SetBlackmanWindow", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetBlackmanWindow(int blackmanWindow, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "TF_GetFrenquencyNumber", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetFrequencyNumber(HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "TF_SetFrenquencyNumber", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetFrequencyNumber(int frequencyNumber, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "TF_GetFrenquencies", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void GetFrenquencies([Out] float[] frequencies, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "TF_SetFrenquencies", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetFrenquencies([In] float[] frequencies, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "TF_GetWaveletCharacteristic", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void GetWaveletCharacteristic([Out] float[] waveletCharacteristic, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "TF_SetWaveletCharacteristic", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetWaveletCharacteristic([In] float[] waveletCharacteristic, HandleRef cppStruct);


        [DllImport("elan", EntryPoint = "TF_GetOtherEventNumberByMeasure", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void GetOtherEventNumberByMeasure([Out] int[] otherEventNumberByMeasure, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "TF_SetOtherEventNumberByMeasure", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetOtherEventNumberByMeasure([In] int[] otherEventNumberByMeasure, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "TF_GetOtherEventByMeasure", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void GetOtherEventByMeasure([Out]int[] otherEventByMeasure, int measure, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "TF_SetOtherEventByMeasure", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetOtherEventByMeasure([In] int[] otherEventByMeasure, int measure, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "TF_GetFloatData", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void GetFloatData([Out] float[] data, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "TF_SetFloatData", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetFloatData([In] float[] data, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "TF_GetDoubleData", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void GetDoubleData([Out] double[] data, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "TF_SetDoubleData", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetDoubleData([In] double[] data, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "TF_GetFloatDataChannel", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void GetFloatData([Out] float[] data,int measure, int channel, int frequency, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "TF_SetFloatDataChannel", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetFloatData([In] float[] data, int measure, int channel, int frequency, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "TF_GetDoubleDataChannel", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void GetDoubleData([Out] double[] data, int measure, int channel, int frequency, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "TF_SetDoubleDataChannel", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetDoubleData([In] double[] data, int measure, int channel, int frequency, HandleRef cppStruct);
        #endregion
    }
}