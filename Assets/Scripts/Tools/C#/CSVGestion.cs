using UnityEngine;
using System.Collections;
using System.Text;
using System.IO;

namespace Tools.CSharp
{
	public static class CSVGestion
	{
		public static void WriteCSV<T,Q>(string filePath,T[] colonne1,Q[] colonne2)
		{
			if(colonne1.Length != colonne2.Length)
			{
				Debug.LogError("Error : C1 and C2 don't have the same size");
				return;
			}
			else
			{
				StringBuilder l_stringBuilder = new StringBuilder();
				int l_length = colonne1.Length;
				for (int i = 0; i < l_length; i++) 
				{
					string c1 = colonne1[i].ToString();
					string c2 = colonne2[i].ToString();
					string l_line = c1+","+c2;
					l_stringBuilder.AppendLine(l_line);
				}
				File.WriteAllText(filePath,l_stringBuilder.ToString());
				Debug.Log("CSV written");
			}
		}
	}
}
