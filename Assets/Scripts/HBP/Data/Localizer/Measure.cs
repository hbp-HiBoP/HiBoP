using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

namespace HBP.Data.Localizer
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Measure
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=256)]
		public string Label;

		public override bool Equals(object obj)
		{
			if(!(obj is Measure)) return false;
			else
			{
				Measure l_measure = (Measure) obj;
				if(l_measure.Label == Label) return true;
				else return false;
			}
		}

		public override int GetHashCode()
		{
			return Label.GetHashCode();
		}
		
		public static bool operator ==(Measure c1, Measure c2) 
		{
			return c1.Equals(c2);
		}
		
		public static bool operator !=(Measure c1, Measure c2) 
		{
			return !c1.Equals(c2);
		}
	}
}