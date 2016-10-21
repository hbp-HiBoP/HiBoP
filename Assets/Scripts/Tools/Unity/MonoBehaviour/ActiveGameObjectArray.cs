using UnityEngine;

namespace Tools.Unity
{	
	public class ActiveGameObjectArray : MonoBehaviour 
	{
		public GameObject[] GameObjects;

		public void SetActive(bool isActive)
		{
			foreach(GameObject objectToEnable in GameObjects)
			{
				objectToEnable.SetActive(isActive);
			}
		}
		public void SetActive()
		{
			foreach(GameObject objectToEnable in GameObjects)
			{
					objectToEnable.SetActive(!objectToEnable.activeSelf);
			}
		}
	}
}
