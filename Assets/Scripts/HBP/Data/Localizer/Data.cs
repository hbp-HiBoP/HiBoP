using System.Collections.Generic;

namespace HBP.Data.Localizer
{
	public class Data
	{
		#region Attributs
		Dictionary<Header.DataChannel,float[]> m_dataBase;
		public Dictionary<Header.DataChannel,float[]> DataBase{get{return m_dataBase;}}
		#endregion

		#region Constructor
		public Data(Header header)
		{
			m_dataBase = new Dictionary<Header.DataChannel,float[]>();
		}
		#endregion

		#region Public Methods
		public bool AlreadyRead(Header.DataChannel dataChannel)
		{
			return m_dataBase.ContainsKey(dataChannel);
		}
		#endregion
	}
}