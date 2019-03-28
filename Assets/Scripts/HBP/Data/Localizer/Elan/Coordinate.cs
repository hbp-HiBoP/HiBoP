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
                return GetLabel(m_HandleOfParentObject);
            }
            set
            {
                SetLabel(value, m_HandleOfParentObject);
            }
        }
        public int ValueNumber
        {
            get
            {
                return GetValueNumber( m_HandleOfParentObject);
            }
            private set
            {
                SetValueNumber(value, m_HandleOfParentObject);
            }
        }
        public float[] Values
        {
            get
            {
                float[] values = new float[ValueNumber];
                GetValues(values, m_ChannelID, m_HandleOfParentObject);
                return values;
            }
            set
            {
                ValueNumber = value.Length;
                SetValues(value, m_ChannelID, m_HandleOfParentObject);
            }
        }

        HandleRef m_HandleOfParentObject;
        int m_ChannelID;
        #endregion

        #region Constructor
        public Coordinate(HandleRef handle, int channel, int property)
        {
            m_HandleOfParentObject = handle;
            m_ChannelID = channel;
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