using System.Runtime.InteropServices;

namespace Elan
{
    public class Coordinate
    {
        #region Properties
        public string Label
        {
            get
            {
                return GetLabel(_handle);
            }
            set
            {
                SetLabel(value, _handle);
            }
        }
        public int ValueNumber
        {
            get
            {
                return GetValueNumber( _handle);
            }
            private set
            {
                SetValueNumber(value, _handle);
            }
        }
        public float[] Values
        {
            get
            {
                float[] values = new float[ValueNumber];
                GetValues(values, channel, _handle);
                return values;
            }
            set
            {
                ValueNumber = value.Length;
                SetValues(value, channel, _handle);
            }
        }

        HandleRef _handle;
        int channel;
        #endregion

        #region Constructor
        public Coordinate(HandleRef _handle, int channel, int property)
        {
            this._handle = _handle;
            this.channel = channel;
        }
        #endregion

        #region DLLImport
        [DllImport("elan", EntryPoint = "coordinate_GetLabel", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern string GetLabel(HandleRef ccpStruct);
        [DllImport("elan", EntryPoint = "coordinate_SetLabel", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetLabel(string label, HandleRef handle);

        [DllImport("elan", EntryPoint = "coordinate_GetValueNumber", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetValueNumber( HandleRef handle);
        [DllImport("elan", EntryPoint = "coordinate_SetValueNumber", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetValueNumber(int valueNumber, HandleRef handle);

        [DllImport("elan", EntryPoint = "coordinate_GetValues", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void GetValues([Out] float[] values, int channel, HandleRef handle);
        [DllImport("elan", EntryPoint = "coordinate_SetValues", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void SetValues([In] float[] values, int channel, HandleRef handle);
        #endregion
    }
}