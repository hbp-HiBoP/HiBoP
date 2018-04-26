using System.Runtime.Serialization;

[DataContract]public enum AveragingType { [EnumMember] Mean, [EnumMember] Median }
public enum NormalizationType { None, Trial, Bloc, Protocol }
public enum SiteInfluenceType { Constant, Linear, Quadratic }