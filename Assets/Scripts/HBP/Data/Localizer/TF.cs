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
                return (DataTypeEnum)GetDataType(_handle);
            }
            private set
            {
                SetDataType((int)value, _handle);
            }
        }
        public int SampleNumber
        {
            get { return GetSampleNumber(_handle); }
            private set { SetSampleNumber(value, _handle); }
        }
        public float SamplingFrequency
        {
            get { return GetSamplingFrequency(_handle); }
            private set { SetSamplingFrequency(value, _handle); }
        }
        public int PreStimSampleNumber
        {
            get { return GetPreStimSampleNumber(_handle); }
            private set { SetPreStimSampleNumber(value, _handle); }
        }
        public int EventCode
        {
            get { return GetEventCode(_handle); }
            private set { GetEventCode(value, _handle); }
        }
        public int EventNumber
        {
            get
            {
                return GetEventNumber(_handle);
            }
            private set
            {
                SetEventNumber(value, _handle);
            }
        }
        public WaveletTypeEnum WaveletType
        {
            get
            {
                return (WaveletTypeEnum)GetWaveletType(_handle);
            }
            private set
            {
                SetWaveletType((int)value, _handle);
            }
        }
        public int BlackmanWindow
        {
            get
            {
                return GetBlackmanWindow(_handle);
            }
            private set
            {
                SetBlackmanWindow(value, _handle);
            }
        }
        public int FrequencyNumber
        {
            get
            {
                return GetFrequencyNumber(_handle);
            }
            private set
            {
                SetFrequencyNumber(value, _handle);
            }
        }
        public float[] Frequencies
        {
            get
            {
                float[] frequencies = new float[FrequencyNumber];
                GetFrenquencies(frequencies, _handle);
                return frequencies;
            }
            private set
            {
                FrequencyNumber = value.Length;
                SetFrenquencies(value, _handle);
            }
        }
        public float[] WaveletCharacteristic
        {
            get
            {
                float[] waveletCharacteristic = new float[FrequencyNumber];
                GetWaveletCharacteristic(waveletCharacteristic, _handle);
                return waveletCharacteristic;
            }
            private set
            {
                if (value.Length == FrequencyNumber)
                {
                    SetWaveletCharacteristic(value, _handle);
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
                int[] eventNumberByMeasure = new int[readed.GetLength(0)];
                GetOtherEventNumberByMeasure(eventNumberByMeasure, _handle);
                return eventNumberByMeasure;
            }
            private set
            {
                if (value.Length == readed.GetLength(0))
                {
                    SetOtherEventNumberByMeasure(value, _handle);
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
                int[][] otherEventByMeasure = new int[readed.GetLength(0)][];
                for (int m = 0; m < otherEventByMeasure.Length; m++)
                {
                    int[] otherEvent = new int[OtherEventNumberByMeasure[m]];
                    GetOtherEventByMeasure(otherEvent, m, _handle);
                    otherEventByMeasure[m] = otherEvent;
                }
                return otherEventByMeasure;
            }
            private set
            {
                if (CanBeusedForOtherEventByMeasure(value))
                {
                    for (int m = 0; m < readed.GetLength(0); m++)
                    {
                        SetOtherEventByMeasure(value[m], m, _handle);
                    }
                }
            }
        }

        HandleRef _handle;

        bool[,] readed;
        public bool[,] Readed
        {
            get { return readed; }
            private set { readed = value; }
        }
        #endregion

        #region Constructor
        public TF(bool[,] readed, HandleRef _handle)
        {
            this._handle = _handle;
            Readed = readed;
        }
        #endregion

        #region Public Methods
        public float[,,][] GetFloatData()
        {
            float[,,][] data = new float[readed.GetLength(0), readed.GetLength(1), readed.GetLength(2)][];
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
                if (readed[track.Measure, track.Channel])
                {
                    GetFloatData(data, track.Measure, track.Channel, track.Frequency, _handle);
                }
            }
            return data;
        }

        public double[,,][] GetDoubleData()
        {
            double[,,][] data = new double[readed.GetLength(0), readed.GetLength(1), readed.GetLength(2)][];
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
                if (readed[track.Measure, track.Channel])
                {
                    GetDoubleData(data, track.Measure, track.Channel, track.Frequency, _handle);
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
                int measureNumber = readed.GetLength(0);
                int channelNumber = readed.GetLength(1);
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
                int measureNumber = readed.GetLength(0);
                int channelNumber = readed.GetLength(1);
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
            int measureNumber = readed.GetLength(0);
            int channelNumber = readed.GetLength(1);
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
            int measureNumber = readed.GetLength(0);
            int channelNumber = readed.GetLength(1);
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
            int measureNumber = readed.GetLength(0);
            int channelNumber = readed.GetLength(1);
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