using System;
using System.Runtime.InteropServices;

namespace Elan
{
    public class EP
    {
        #region Properties
        public enum DataTypeEnum { Float, Double };

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
            get
            {
                return GetPreStimSampleNumber(_handle);
            }
            private set
            {
                SetPreStimSampleNumber(value, _handle);
            }
        }
        public int EventCode
        {
            get
            {
                return GetEventCode(_handle);
            }
            private set
            {
                SetEventCode(value, _handle);
            }
        }
        public int AveragedEventNumber
        {
            get
            {
                return GetAveragedEventNumber(_handle);
            }
            private set
            {
                SetAveragedEventNumber(value, _handle);
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
                    Console.WriteLine("Other event number by measure cannot be set because the length need to be equals to number of frequency.");
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
                if (canBeUsedForOtherEventByMeasure(value))
                {
                    for (int m = 0; m < value.Length; m++)
                    {
                        SetOtherEventByMeasure(value[m], m, _handle);
                    }
                }
            }
        }

        bool[,] readed;
        public bool[,] Readed
        {
            get { return readed; }
            private set { readed = value; }
        }

        HandleRef _handle;
        #endregion

        #region Constructor
        public EP(bool[,] readed, HandleRef _handle)
        {
            this._handle = _handle;
            Readed = readed; 
        }
        #endregion

        #region Public Methods
        public float[,][] GetFloatData()
        {
            float[,][] data = new float[readed.GetLength(0), readed.GetLength(1)][];
            for (int m = 0; m < data.GetLength(0); m++)
            {
                for (int c = 0; c < data.GetLength(1); c++)
                {
                    data[m, c] = GetFloatData(new Track(m, c));
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
                    GetFloatData(data, track.Measure, track.Channel, _handle);
                }
            }
            return data;
        }

        public double[,][] GetDoubleData()
        {
            double[,][] data = new double[readed.GetLength(0), readed.GetLength(1)][];
            for (int m = 0; m < data.GetLength(0); m++)
            {
                for (int c = 0; c < data.GetLength(1); c++)
                {
                    data[m, c] = GetDoubleData(new Track(m, c));
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
                    GetDoubleData(data, track.Measure, track.Channel, _handle);
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
                if (flatData.Length == SampleNumber * readed.GetLength(0) * readed.GetLength(1))
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
                if (flatData.Length == SampleNumber * readed.GetLength(0) * readed.GetLength(1))
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
        bool CanBeUsed(float[][][] data)
        {
            if (DataType == DataTypeEnum.Float)
            {
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
                                if (data[m][c].Length != SampleNumber)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("EEG use double : FloatData can't be set.");
            }
            return true;
        }
        bool CanBeUsed(double[][][] data)
        {
            if (DataType == DataTypeEnum.Double)
            {
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
                                if (data[m][c].Length != SampleNumber)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("EEG use float : return null for DoubleData.");
            }
            return true;
        }
        bool canBeUsedForOtherEventByMeasure(int[][] otherEventByMeasure)
        {
            int measureNumber = readed.GetLength(0);
            if (otherEventByMeasure.GetLength(0) == measureNumber)
            {
                for (int i = 0; i < measureNumber; i++)
                {
                    if (OtherEventNumberByMeasure[i] != otherEventByMeasure[i].Length)
                    {
                        Console.WriteLine("Other event by measure cannot be set becuse the second dimension length need to be equals to the OtherEventNumberByMeasure of the measure.");
                        return false;
                    }
                }
            }
            else
            {
                Console.WriteLine("Other event by measure cannot be set because the first dimension length need to be equals to the number of measure.");
            }
            return true;
        }
        #endregion

        #region DLLImport
        [DllImport("elan", EntryPoint = "EP_GetDataType", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetDataType(HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "EP_SetDataType", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetDataType(int dataType, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "EP_GetSampleNumber", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetSampleNumber(HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "EP_SetSampleNumber", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetSampleNumber(int sampleNumber, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "EP_GetSamplingFrequency", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern float GetSamplingFrequency(HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "EP_SetSamplingFrequency", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetSamplingFrequency(float samplingFrequency, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "EP_GetPreStimSampleNumber", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetPreStimSampleNumber(HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "EP_SetPreStimSampleNumber", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetPreStimSampleNumber(int preStimSampleNumber, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "EP_GetEventCode", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetEventCode(HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "EP_SetEventCode", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetEventCode(int eventCode, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "EP_GetAveragedEventNumber", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetAveragedEventNumber(HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "EP_SetAveragedEventNumber", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetAveragedEventNumber(int averageEventNumber, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "EP_GetOtherEventNumberByMeasure", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void GetOtherEventNumberByMeasure([Out] int[] otherEventNumberByMeasure, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "EP_SetOtherEventNumberByMeasure", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetOtherEventNumberByMeasure([In] int[] otherEventNumberByMeasure, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "EP_GetOtherEventByMeasure", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void GetOtherEventByMeasure([Out]int[] otherEventByMeasure, int measure, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "EP_SetOtherEventByMeasure", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetOtherEventByMeasure([In] int[] otherEventByMeasure, int measure, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "EP_GetFloatData", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void GetFloatData([Out] float[] data, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "EP_SetFloatData", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetFloatData([In] float[] data, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "EP_GetFloatDataChannel", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void GetFloatData([Out] float[] data, int measure, int channel, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "EP_SetFloatDataChannel", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetFloatData([In] float[] data, int measure, int channel, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "EP_GetDoubleData", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void GetDoubleData([Out] double[] data, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "EP_SetDoubleData", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetDoubleData([In] double[] data, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "EP_GetDoubleDataChannel", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetDoubleData([Out] double[] data, int measure, int channel, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "EP_SetDoubleDataChannel", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetDoubleData([In] double[] data, int measure, int channel, HandleRef cppStruct);
        #endregion
    }
}
