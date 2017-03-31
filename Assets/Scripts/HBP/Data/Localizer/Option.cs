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
                if (GetCompressed(_handle) == 1)
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
                    SetCompressed(1, _handle);
                }
                else
                {
                    SetCompressed(0, _handle);
                }
            }
        }
        public CompressionTypeEnum CompressionType
        {
            get
            {
                return (CompressionTypeEnum)GetCompressionType(_handle);
            }
            private set
            {
                SetCompressionType((int)value, _handle);
            }
        }
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
        HandleRef _handle;
        #endregion

        #region Constructor
        public Option(HandleRef _handle)
        {
            this._handle = _handle;
        }
        #endregion

        #region Methods
        public void Init()
        {
            InitOptions(_handle);
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