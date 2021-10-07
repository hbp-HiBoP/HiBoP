using System;

public class DataAttribute : Attribute { }
public class IEEG : DataAttribute { }
public class CCEP : DataAttribute { }
public class FMRI : DataAttribute { }
public class MEG : DataAttribute { }
public class Hide : Attribute { }