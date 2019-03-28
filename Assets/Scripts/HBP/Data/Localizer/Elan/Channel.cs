using System;
using System.Runtime.InteropServices;

namespace Elan
{
    public class Channel
    {
        #region Properties
        int m_ChannelID;
        public string Label
        {
            get { return Marshal.PtrToStringAnsi(GetLabel(m_ChannelID, m_HandleOfParentObject)); }
            set { SetLabel(value, m_ChannelID, m_HandleOfParentObject); }
        }
        public string Type
        {
            get { return Marshal.PtrToStringAnsi(GetType(m_ChannelID, m_HandleOfParentObject)); }
            set { SetType(value, m_ChannelID, m_HandleOfParentObject); }
        }
        public string Unit
        {
            get { return Marshal.PtrToStringAnsi(GetUnit(m_ChannelID, m_HandleOfParentObject)); }
            set { SetUnit(value, m_ChannelID, m_HandleOfParentObject); }
        }
        public int CoordinatesNumber
        {
            get { return GetCoordinateNumber(m_ChannelID, m_HandleOfParentObject); }
            set { SetCoordinateNumber(value, m_ChannelID, m_HandleOfParentObject); }
        }
        public Coordinate[] Coordinates
        {
            get
            {
                Coordinate[] coordinates = new Coordinate[CoordinatesNumber];
                GetCoordinate(coordinates, m_ChannelID, m_HandleOfParentObject);
                return coordinates;
            }
            set
            {
                CoordinatesNumber = value.Length;
                SetCoordinate(value, m_ChannelID, m_HandleOfParentObject);
            }
        }
        HandleRef m_HandleOfParentObject;
        #endregion

        #region Constructor
        public Channel(HandleRef handle, int channel)
        {
            m_HandleOfParentObject = handle;
            m_ChannelID = channel;
        }
        #endregion

        #region DLLImport
        [DllImport("elan", EntryPoint = "Channel_GetLabel", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetLabel(int channel, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "Channel_SetLabel", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetLabel(string dataType, int channel, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "Channel_GetType", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetType(int channel, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "Channel_SetType", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetType(string type, int channel, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "Channel_GetUnit", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetUnit(int channel, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "Channel_SetUnit", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetUnit(string unit, int channel, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "Channel_GetCoordinateNumber", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetCoordinateNumber(int channel, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "Channel_SetCoordinateNumber", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetCoordinateNumber(int coordinateNumber, int channel, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "Channel_GetCoordinate", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void GetCoordinate([Out] Coordinate[] coordinates, int channel, HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "Channel_SetCoordinate", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetCoordinate([In] Coordinate[] coordinates, int channel, HandleRef cppStruct);
        #endregion
    }
}