using System.Runtime.InteropServices;

namespace HBP.Data.Localizer
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Channel
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=256)]
		public string Label;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=256)]
		public string Type;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=256)]
		public string Unite;

		public override bool Equals(object obj)
		{
			if(!(obj is Channel)) return false;
			else
			{
				Channel l_channel = (Channel) obj;
				if(l_channel.Label == Label && l_channel.Type == Type && l_channel.Unite == Unite) return true;
				else return false;
			}
		}

		public override int GetHashCode()
		{
			return Label.GetHashCode()^Type.GetHashCode()^Unite.GetHashCode() ;
		}

		public static bool operator ==(Channel c1, Channel c2) 
		{
			return c1.Equals(c2);
		}
		
		public static bool operator !=(Channel c1, Channel c2) 
		{
			return !c1.Equals(c2);
		}
	}
}
