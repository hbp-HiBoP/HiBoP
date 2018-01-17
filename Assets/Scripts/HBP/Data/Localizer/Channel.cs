using System;
using System.Runtime.InteropServices;

namespace Elan
{
    public class Channel
    {
        #region Properties
        int channel;
        public string Label
        {
            get { return Marshal.PtrToStringAnsi(GetLabel(channel, _handle)); }
            set { SetLabel(value, channel, _handle); }
        }
        public string Type
        {
            get { return Marshal.PtrToStringAnsi(GetType(channel, _handle)); }
            set { SetType(value, channel, _handle); }
        }
        public string Unit
        {
            get { return Marshal.PtrToStringAnsi(GetUnit(channel, _handle)); }
            set { SetUnit(value, channel, _handle); }
        }
        public int CoordinatesNumber
        {
            get { return GetCoordinateNumber(channel, _handle); }
            set { SetCoordinateNumber(value, channel, _handle); }
        }
        public Coordinate[] Coordinates
        {
            get
            {
                Coordinate[] coordinates = new Coordinate[CoordinatesNumber];
                GetCoordinate(coordinates, channel, _handle);
                return coordinates;
            }
            set
            {
                CoordinatesNumber = value.Length;
                SetCoordinate(value, channel, _handle);
            }
        }
        HandleRef _handle;
        #endregion

        #region Constructor
        public Channel(HandleRef _handle, int channel)
        {
            this._handle = _handle;
            this.channel = channel;
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