using System.Runtime.InteropServices;

namespace Elan
{
    public class Option
    {
        #region Properties
        public enum DataTypeEnum { Float, Double };
        public enum CompressionTypeEnum { None, GZIP }

        public bool Compressed
        {
            get
            {
                if (GetCompressed(m_HandleOfParentObject) == 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            private set
            {
                if (value)
                {
                    SetCompressed(1, m_HandleOfParentObject);
                }
                else
                {
                    SetCompressed(0, m_HandleOfParentObject);
                }
            }
        }
        public CompressionTypeEnum CompressionType
        {
            get
            {
                return (CompressionTypeEnum)GetCompressionType(m_HandleOfParentObject);
            }
            private set
            {
                SetCompressionType((int)value, m_HandleOfParentObject);
            }
        }
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
        HandleRef m_HandleOfParentObject;
        #endregion

        #region Constructor
        public Option(HandleRef handle)
        {
            m_HandleOfParentObject = handle;
        }
        #endregion

        #region Methods
        public void Init()
        {
            InitOptions(m_HandleOfParentObject);
        }
        #endregion

        #region DLLImport
        [DllImport("elan", EntryPoint = "GetCompressed", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetCompressed(HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "SetCompressed", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int SetCompressed(int compressed, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "GetCompressionType", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetCompressionType(HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "SetCompressionType", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int SetCompressionType(int compressionType, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "GetDataType", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetDataType(HandleRef cppStruct);
        [DllImport("elan", EntryPoint = "SetDataType", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int SetDataType(int dataType, HandleRef cppStruct);

        [DllImport("elan", EntryPoint = "InitOptions", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int InitOptions(HandleRef cppStruct);
        #endregion
    }
}