﻿using System;

public class DataAttribute : Attribute { }
public class IEEG : DataAttribute { }
public class CCEP : DataAttribute { }
public class FMRI : DataAttribute { }
public class MEGv : DataAttribute { }
public class MEGc : DataAttribute { }
public class Hide : Attribute { }