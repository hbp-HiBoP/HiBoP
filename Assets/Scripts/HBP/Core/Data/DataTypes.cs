using System;

namespace HBP.Core.Data
{
    public class DataAttribute : Attribute { }
    public class IEEG : DataAttribute { }
    public class CCEP : DataAttribute { }
    public class FMRI : DataAttribute { }
    public class MEGv : DataAttribute { }
    public class MEGc : DataAttribute { }
    public class Static : DataAttribute { }
    public class Hide : Attribute { }
}