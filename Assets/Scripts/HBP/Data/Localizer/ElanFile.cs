using System;
using System.Text;
using System.Runtime.InteropServices;

namespace Elan
{
    public class ElanFile : Tools.CppDLLImportBase
    {
        #region Properties
        const int COMMENT_SIZE = 512;
        const int LABEL_SIZE = 256;

        public int Endian
        {
            private set { SetEndian(value, _handle); }
            get { return GetEndian(_handle); }
        }
        public int Version
        {
            private set { SetVersion(value, _handle); }
             get { return GetVersion(_handle); }
        }
        public int Release
        {
            get { return GetRelease(_handle); }
            private set { SetRelease(value, _handle); }
        }
        public string Comment
        {
            get { return Marshal.PtrToStringAnsi(GetComment(_handle)); }
            private set
            {
                if (value.Length > COMMENT_SIZE)
                {
                    Console.WriteLine("The comment is too long. He will be cut to the comment size format.");
                }
                StringBuilder comment = new StringBuilder(value, COMMENT_SIZE);
                SetComment(comment, _handle);
            }
        }
        public int MeasureNumberByChannel
        {
            get { return GetMeasureNumberByChannel(_handle); }
            private set { SetMeasureNumberByChannel(value, _handle); }
        }
        public string[] MeasureLabels
        {
            get
            {
                string[] labels = new string[MeasureNumberByChannel];
                for (int i = 0; i < labels.Length; i++)
                {
                    labels[i] = Marshal.PtrToStringAnsi(GetMeasureLabel(i, _handle));
                }
                return labels;
            }
            private set
            {
                string[] labels = value;
                if (labels.Length == MeasureNumberByChannel)
                {
                    for (int i = 0; i < labels.Length; i++)
                    {
                        SetMeasureLabel(new StringBuilder(labels[i], LABEL_SIZE), i, _handle);
                    }
                }
                else
                {
                    Console.WriteLine("Can't set the measure labels.");
                }
            }
        }

        public int ChannelNumber
        {
            get { return GetChannelNumber(_handle); }
            private set { SetChannelNumber(value, _handle); }
        }
        public Channel[] Channels
        {
            get
            {
                Channel[] channels = new Channel[ChannelNumber];
                for (int i = 0; i < channels.Length; i++)
                {
                    channels[i] = new Channel(_handle, i);
                }
                return channels;
            }
            private set
            {
                ChannelNumber = value.Length;
                Channels = value;
            }
        }

        public bool HasEEG
        {
            get
            {
                if (GetHasEEG(_handle) == 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public EEG EEG
        {
            get
            {
                if (HasEEG)
                {
                    return new EEG(readed, _handle);
                }
                else
                {
                    return null;
                }
            }
        }

        public bool HasEP
        {
            get
            {
                if (GetHasEP(_handle) == 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public EP EP
        {
            get
            {
                if (HasEP)
                {
                    return new EP(readed, _handle);
                }
                else
                {
                    return null;
                }
            }
        }

        public bool HasTF
        {
            get
            {
                if (GetHasTF(_handle) == 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public TF TF
        {
            get
            {
                if (HasTF)
                {
                    return new TF(readed,_handle);
                }
                else
                {
                    return null;
                }
            }
        }

        public Option Option
        {
            get { return new Option(_handle); }
        }

        string path;
        public string Path
        {
            get { return path; }
            private set { path = value; }
        }

        bool dataArrayAllocated;
        bool[,] readed;
        public bool[,] Readed
        {
            get { return readed; }
            private set { readed = value; }
        }
        #endregion

        #region Constructor
        public ElanFile(string path, bool readData) : base()
        {
            Path = path;
            ReadHeader(new StringBuilder(path), _handle);
            Readed = new bool[MeasureNumberByChannel, ChannelNumber];
            if (readData)
            {
                Console.WriteLine("ReadData");
                ReadChannel();
            }
        }
        public ElanFile(string path) : this(path,false)
        {
        }
        #endregion

        #region Public Methods
        public bool ReadChannel(Track[] tracks)
        {
            if (!dataArrayAllocated) AllocDataArray();
            foreach(Track track in tracks)
            {
                if (ReadChannel(track))
                {
                    return true;
                }
            }
            return false;
        }
        public bool ReadChannel(Track track)
        {
            if (!dataArrayAllocated) AllocDataArray();
            if (!readed[track.Measure,track.Channel])
            {
                int err = ReadChannel(new StringBuilder(path), track.Measure, track.Channel, _handle);
                if (err == 0)
                {
                    readed[track.Measure, track.Channel] = true;
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
        public bool ReadChannel()
        {
            if (!dataArrayAllocated) AllocDataArray();
            int err = ReadAllChannels(new StringBuilder(path), _handle);
            if (err == 0)
            {
                for (int m = 0; m < readed.GetLength(0); m++)
                {
                    for (int c = 0; c < readed.GetLength(1); c++)
                    {
                        readed[m,c] = true;
                    }
                }
                return false;
            }
            else
            {
                return true;
            }
        }
        public Track FindTrack(string measure, string channel)
        {
            Channel[] channels = Channels;
            string[] measures = MeasureLabels;
            int channelIndex = Array.FindIndex(channels, (c) => c.Label == channel);
            int measureIndex = Array.FindIndex(measures, (m) => m == measure);
            return new Track(measureIndex, channelIndex);
        }
        #endregion

        #region Private Methods
        void AllocDataArray()
        {
            dataArrayAllocated = true;
            AllocDataArray(_handle);
        }
        #endregion

        #region Memory Management
        protected override void createDLLClass()
        {
            _handle = new HandleRef(this, CreateElanStruct());
        }
        protected override void deleteDLLClass()
        {
            DeleteElanStruct(_handle, dataArrayAllocated);
        }
        #endregion

        #region DLLImport
        // Constructor/Destructor.
        [DllImport("elan", EntryPoint = "CreateElanStruct", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr CreateElanStruct();
        [DllImport("elan", EntryPoint = "AllocDataArray", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr AllocDataArray(HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "DeleteElanStruct", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void DeleteElanStruct(HandleRef cppStruct,bool freeData);

        // Read methods.
        [DllImport("elan", EntryPoint = "ReadHeader", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int ReadHeader(StringBuilder path, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "Read", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int Read(StringBuilder path, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "ReadAllChannels", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int ReadAllChannels(StringBuilder path, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "ReadChannel", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int ReadChannel(StringBuilder path, int measure, int channel, HandleRef cppStruct);

        // Getter/Setter.
        [DllImport("elan", EntryPoint = "SetEndian", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetEndian(int endian, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "GetEndian", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetEndian(HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "SetVersion", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetVersion(int version, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "GetVersion", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetVersion(HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "SetRelease", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetRelease(int release, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "GetRelease", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetRelease(HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "SetComment", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetComment(StringBuilder comment, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "GetComment", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetComment(HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "SetChannelNumber", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetChannelNumber(int channelNumber, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "GetChannelNumber", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetChannelNumber(HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "SetMeasureNumberByChannel", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetMeasureNumberByChannel(int measureNumberByChannel, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "GetMeasureNumberByChannel", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetMeasureNumberByChannel(HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "SetMeasureLabel", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetMeasureLabel(StringBuilder measureLabels, int measure, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "GetMeasureLabel", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetMeasureLabel(int measure, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "GetHasEEG", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetHasEEG(HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "GetHasEP", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetHasEP(HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "GetHasTF", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetHasTF(HandleRef cppStruct);
        #endregion
    }
}
